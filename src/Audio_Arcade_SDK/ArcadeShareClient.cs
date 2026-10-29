using ArcadeShare.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArcadeShare
{
    public class ArcadeShareClient : IDisposable
    {
        public const string SDK_VERSION = "3.0.0";

        public const string SERVER_URL = "wss://rooms.audio-arcade.fr.eu.org";
        public const string SERVER_HTTP_URL = "https://rooms.audio-arcade.fr.eu.org";

        private const string VERSION_ID = "60482719365028471935";

        public event Action<string>? OnRoomCreated;
        public event Action<RoomJoinedResponse>? OnRoomJoined;
        public event Action<StateUpdate>? OnStateReceived;
        public event Action<GuestChangedResponse>? OnGuestJoined;
        public event Action<GuestChangedResponse>? OnGuestLeft;
        public event Action? OnHostLeft;
        public event Action<string>? OnError;
        public event Action? OnDisconnected;
        public event Action? OnConnected;

        /// <summary>Déclenché quand le serveur refuse la connexion (appId inconnu/désactivé, version SDK dépréciée...). Le message est destiné à être affiché tel quel à l'utilisateur/développeur.</summary>
        public event Action<string>? OnAuthRejected;

        /// <summary>Déclenché quand la version du SDK est dépréciée : la connexion fonctionne, mais une mise à jour est conseillée. Le message est destiné à être affiché tel quel.</summary>
        public event Action<string>? OnDeprecatedVersion;

        /// <summary>Déclenché à chaque tentative de reconnexion automatique, avec le numéro de tentative.</summary>
        public event Action<int>? OnReconnecting;

        /// <summary>Déclenché quand la reconnexion automatique a réussi et que l'état (room) a été restauré.</summary>
        public event Action? OnReconnected;

        /// <summary>
        /// Déclenché quand le statut de certification de CETTE application est connu (réponse auth_ok du serveur).
        /// true = certifiée, false = non certifiée. Informatif : une application non certifiée n'est pas bloquée.
        /// </summary>
        public event Action<bool?>? OnCertificationChanged;

        /// <summary>
        /// (Invité) Déclenché à l'entrée dans une room avec la certification de l'application de l'hôte.
        /// null = information non fournie (serveur ancien ou hôte inconnu).
        /// </summary>
        public event Action<bool?>? OnHostCertification;

        private readonly string _appId;

        private ClientWebSocket? _ws;
        private static readonly HttpClient _http = new HttpClient
        {
            // Les gros fichiers (FLAC…) dépassent facilement les 100 s par défaut sur une connexion lente
            Timeout = TimeSpan.FromMinutes(10)
        };
        private CancellationTokenSource? _cts;
        private Task? _receiveTask;

        // ClientWebSocket n'accepte qu'un seul SendAsync à la fois (ping + état peuvent se croiser)
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        private string? _currentRoomCode;
        private string? _deviceSerial;
        private string? _lastUsername;
        private bool _isHost;

        private volatile bool _intentionalDisconnect = false;
        private volatile bool _authRejected = false;
        private int _reconnecting; // 0 = aucune boucle de reconnexion en cours, 1 = en cours

        // L'hôte garde sa room 5 min côté serveur (HOST_GRACE_MS) : ~25 tentatives espacées de 15 s max
        private const int MaxReconnectAttempts = 25;
        private const int MaxReconnectDelaySeconds = 15;

        private const int MaxUploadAttempts = 3;

        private TaskCompletionSource<AuthResult>? _authTcs;

        /// <summary>null = pas encore connu (aucune connexion authentifiée), true/false = réponse du serveur.</summary>
        public bool? IsCertified { get; private set; }

        /// <summary>(Invité) Certification de l'application de l'hôte de la room courante. null = inconnue.</summary>
        public bool? HostCertified { get; private set; }

        private class AuthResult
        {
            public bool Authorized;
            public bool Deprecated;
            public bool Certified;
            public string? Reason;
        }

        public bool IsConnected => _ws?.State == WebSocketState.Open;
        public string? CurrentRoomCode => _currentRoomCode;
        public bool IsHost => _isHost;

        /// <param name="appId">
        /// Identifiant d'application fourni par l'équipe Audio_Arcade. Obligatoire —
        /// chaque application intégrant le SDK doit posséder son propre identifiant,
        /// utilisé pour la traçabilité et pour autoriser/révoquer l'accès à une application spécifique.
        /// </param>
        public ArcadeShareClient(string appId)
        {
            if (string.IsNullOrWhiteSpace(appId))
                throw new ArgumentException(
                    "appId est requis. Contactez l'équipe Audio_Arcade pour obtenir un identifiant d'application.");

            _appId = appId;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private string AuthQuery() =>
            $"appId={Uri.EscapeDataString(_appId)}&versionId={Uri.EscapeDataString(VERSION_ID)}";

        private static string GetExtensionFromContentType(string? contentType) => contentType switch
        {
            "audio/mpeg" => ".mp3",
            "audio/wav" or "audio/x-wav" => ".wav",
            "audio/mp4" => ".m4a",
            "audio/aac" => ".aac",
            "audio/ogg" => ".ogg",
            "audio/flac" => ".flac",
            "audio/opus" => ".opus",
            _ => ""
        };

        private static string SanitizeExtension(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return "";
            string ext = Path.GetExtension(fileName);
            return (ext.Length > 1 && ext.Length <= 6) ? ext : "";
        }

        // ── Certification ────────────────────────────────────────────────────────

        /// <summary>
        /// Statut public d'une application d'après son NOM (jamais l'ID) : "certified", "partial",
        /// "unverified", "revoked". Retourne null si le serveur est injoignable ou ne répond pas correctement.
        /// Utilisable sans connexion ni room (page d'accueil, paramètres...).
        /// </summary>
        public static async Task<string?> FetchCertificationStatusAsync(
            string appName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(appName)) return null;

            try
            {
                using var response = await _http.GetAsync(
                    $"{SERVER_HTTP_URL}/verifier?nom={Uri.EscapeDataString(appName.Trim())}", cancellationToken);

                if (!response.IsSuccessStatusCode) return null;

                var obj = JObject.Parse(await response.Content.ReadAsStringAsync());
                return (obj["results"] as JArray)?.FirstOrDefault()?.Value<string>("status");
            }
            catch (OperationCanceledException) { throw; }
            catch { return null; }
        }

        // ── Connexion ────────────────────────────────────────────────────────────

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (IsConnected) return;

            _intentionalDisconnect = false;
            _authRejected = false;

            // Nettoie une éventuelle connexion précédente (cas de la reconnexion)
            _cts?.Cancel();
            _cts?.Dispose();
            _ws?.Dispose();

            var ws = new ClientWebSocket();
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _ws = ws;
            _cts = cts;
            var token = cts.Token;

            _authTcs = new TaskCompletionSource<AuthResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            await ws.ConnectAsync(new Uri(SERVER_URL), token);

            _receiveTask = Task.Run(() => ReceiveLoopAsync(ws, token), token);
            _ = Task.Run(() => PingLoopAsync(token), token);

            await SendAuthAsync();

            AuthResult authResult;
            try
            {
                authResult = await WaitForAuthAsync(_authTcs.Task, TimeSpan.FromSeconds(10));
            }
            catch (TimeoutException)
            {
                await DisconnectInternalAsync();
                throw new InvalidOperationException(
                    "Le serveur n'a pas répondu au handshake d'authentification (timeout).");
            }

            if (!authResult.Authorized)
            {
                string reason = authResult.Reason ?? "Application non autorisée à utiliser ce service.";
                OnAuthRejected?.Invoke(reason);
                await DisconnectInternalAsync();
                throw new UnauthorizedAccessException(reason);
            }

            if (authResult.Deprecated)
                OnDeprecatedVersion?.Invoke(authResult.Reason ?? "Cette version du SDK n'est plus à jour.");

            IsCertified = authResult.Certified;
            OnCertificationChanged?.Invoke(IsCertified);

            OnConnected?.Invoke();
        }

        private async Task<AuthResult> WaitForAuthAsync(Task<AuthResult> authTask, TimeSpan timeout)
        {
            var delayTask = Task.Delay(timeout);
            var completed = await Task.WhenAny(authTask, delayTask);

            if (completed == delayTask)
                throw new TimeoutException();

            return await authTask;
        }

        private async Task SendAuthAsync()
        {
            var msg = new AuthMessage { AppId = _appId, VersionId = VERSION_ID };
            await SendJsonInternalAsync(msg, injectIds: false); // le message d'auth porte déjà ses propres champs
        }

        // ── Rooms permanentes (HTTP) ─────────────────────────────────────────────

        public async Task<(bool Ok, string? Code, string? Error)> CreatePermanentRoomAsync(
            string username, string deviceSerial, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Le pseudo ne peut pas être vide.");

            if (string.IsNullOrWhiteSpace(deviceSerial))
                throw new ArgumentException("deviceSerial ne peut pas être vide.");

            var body = new JObject
            {
                ["username"] = username.Trim(),
                ["deviceSerial"] = deviceSerial,
                ["appId"] = _appId,
                ["versionId"] = VERSION_ID
            };

            using var content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(
                $"{SERVER_HTTP_URL}/create-permanent-room", content, cancellationToken);

            string json = await response.Content.ReadAsStringAsync();

            JObject? obj = null;
            try { obj = JObject.Parse(json); } catch { }

            if (response.IsSuccessStatusCode)
                return (true, obj?.Value<string>("code"), null);

            return (false, null, obj?.Value<string>("error") ?? $"Erreur serveur ({(int)response.StatusCode})");
        }

        /// <summary>Supprime une room permanente appartenant à cet appareil.</summary>
        public async Task<bool> DeletePermanentRoomAsync(
            string code, string deviceSerial, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Le code de la room ne peut pas être vide.");

            if (string.IsNullOrWhiteSpace(deviceSerial))
                throw new ArgumentException("deviceSerial ne peut pas être vide.");

            string url = $"{SERVER_HTTP_URL}/room/{Uri.EscapeDataString(code.Trim().ToUpper())}/{Uri.EscapeDataString(deviceSerial)}" +
                         $"?{AuthQuery()}";

            using var response = await _http.DeleteAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<JObject>> GetMyRoomsAsync(string deviceSerial, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(deviceSerial))
                throw new ArgumentException("deviceSerial ne peut pas être vide.");

            using var response = await _http.GetAsync(
                $"{SERVER_HTTP_URL}/my-rooms/{Uri.EscapeDataString(deviceSerial)}?{AuthQuery()}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new List<JObject>();

            try
            {
                string json = await response.Content.ReadAsStringAsync();
                return JArray.Parse(json).OfType<JObject>().ToList();
            }
            catch (JsonException)
            {
                return new List<JObject>();
            }
        }

        // ── Rooms (WebSocket) ────────────────────────────────────────────────────

        public async Task CreateRoomAsync(string username, string deviceSerial)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Le pseudo ne peut pas être vide.");

            _isHost = true;
            _deviceSerial = deviceSerial;
            _lastUsername = username;

            var msg = new CreateRoomMessage { Username = username.Trim(), DeviceSerial = deviceSerial };
            await SendJsonAsync(msg);
        }

        public async Task RejoinRoomAsync(string code, string username, string deviceSerial)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Le pseudo ne peut pas être vide.");

            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Le code de la room ne peut pas être vide.");

            _isHost = true;
            _deviceSerial = deviceSerial;
            _lastUsername = username;
            _currentRoomCode = code.Trim().ToUpper();

            var msg = new RejoinRoomMessage
            {
                Code = _currentRoomCode,
                Username = username.Trim(),
                DeviceSerial = deviceSerial
            };
            await SendJsonAsync(msg);
        }

        public async Task SendStateAsync(
            double position,
            bool isPlaying,
            string title,
            string artist,
            double duration,
            string? artworkUrl = null,
            string? nextTitle = null)
        {
            if (!_isHost)
                throw new InvalidOperationException("Seul l'hôte peut envoyer l'état de lecture.");

            if (_currentRoomCode == null)
                throw new InvalidOperationException("Aucune room active. Appelez CreateRoomAsync ou RejoinRoomAsync d'abord.");

            var msg = new StateMessage
            {
                Code = _currentRoomCode,
                Position = position,
                IsPlaying = isPlaying,
                Title = title,
                Artist = artist,
                Duration = duration,
                ArtworkUrl = artworkUrl,
                NextTitle = nextTitle
            };
            await SendJsonAsync(msg);
        }

        // ── Musique (HTTP) ───────────────────────────────────────────────────────

        /// <summary>
        /// Envoie la musique courante au serveur. Réessaie automatiquement (3 tentatives) sur erreur réseau/serveur,
        /// ne réessaie pas sur 413 (trop volumineux) ni 415 (format refusé) et déclenche alors <see cref="OnError"/>.
        /// Le fichier n'est considéré comme envoyé que si le serveur le sert réellement (vérification HEAD).
        /// </summary>
        public async Task<bool> UploadMusicAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (!_isHost)
                throw new InvalidOperationException("Seul l'hôte peut envoyer de la musique.");

            if (_currentRoomCode == null)
                throw new InvalidOperationException("Aucune room active.");

            if (_deviceSerial == null)
                throw new InvalidOperationException("deviceSerial non défini.");

            string roomCode = _currentRoomCode;

            for (int attempt = 1; attempt <= MaxUploadAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int status = await UploadOnceAsync(filePath, roomCode, cancellationToken);

                if (status == (int)HttpStatusCode.RequestEntityTooLarge)
                {
                    OnError?.Invoke("Fichier trop volumineux pour le serveur.");
                    return false;
                }
                if (status == (int)HttpStatusCode.UnsupportedMediaType)
                {
                    OnError?.Invoke("Format audio non pris en charge par le serveur.");
                    return false;
                }
                if (status == 401 || status == 403)
                {
                    OnError?.Invoke("Envoi refusé par le serveur (application non autorisée).");
                    return false;
                }

                if (status >= 200 && status < 300 && await VerifyUploadedTrackAsync(roomCode, cancellationToken))
                    return true;

                if (attempt < MaxUploadAttempts)
                    await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }

            OnError?.Invoke("Impossible d'envoyer la musique au serveur.");
            return false;
        }

        /// <summary>Un seul essai d'envoi. Retourne le code HTTP, ou 0 en cas d'erreur réseau.</summary>
        private async Task<int> UploadOnceAsync(string filePath, string roomCode, CancellationToken cancellationToken)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);

                // Le flux est rouvert à chaque essai (un StreamContent ne peut être envoyé qu'une fois)
                await using var fileStream = new FileStream(
                    filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);

                using var content = new MultipartFormDataContent();
                using var fileContent = new StreamContent(fileStream);

                fileContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("audio/*");

                content.Add(fileContent, "music", fileName);

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{SERVER_HTTP_URL}/upload-music")
                {
                    Content = content
                };

                request.Headers.Add("x-room-code", roomCode);
                request.Headers.Add("x-device-serial", _deviceSerial);
                request.Headers.Add("x-app-id", _appId);
                request.Headers.Add("x-version-id", VERSION_ID);

                using var response = await _http.SendAsync(request, cancellationToken);
                return (int)response.StatusCode;
            }
            catch (OperationCanceledException) { throw; }
            catch (HttpRequestException) { return 0; }
            catch (IOException) { return 0; }
        }

        /// <summary>Vérifie (HEAD) que le serveur sert bien la musique de la room.</summary>
        private async Task<bool> VerifyUploadedTrackAsync(string roomCode, CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Head, $"{SERVER_HTTP_URL}/music/{Uri.EscapeDataString(roomCode)}?{AuthQuery()}");
                using var response = await _http.SendAsync(request, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (OperationCanceledException) { throw; }
            catch { return false; }
        }

        /// <summary>
        /// URL de streaming de la musique d'une room (authentifiée). <paramref name="currentFile"/> = nom du fichier
        /// courant (StateUpdate.CurrentFile) : il change à chaque piste et évite de réutiliser un cache périmé.
        /// </summary>
        public string GetStreamUrl(string roomCode, string? currentFile = null)
        {
            string url = $"{SERVER_HTTP_URL}/music/{Uri.EscapeDataString(roomCode.Trim().ToUpper())}?{AuthQuery()}";
            if (!string.IsNullOrEmpty(currentFile))
                url += $"&f={Uri.EscapeDataString(currentFile)}";
            return url;
        }

        /// <param name="currentFile">Nom du fichier courant (StateUpdate.CurrentFile), pour contourner tout cache.</param>
        public async Task<string?> DownloadRoomMusicAsync(
            string roomCode,
            string destinationDirectory,
            CancellationToken cancellationToken = default,
            string? currentFile = null)
        {
            if (string.IsNullOrWhiteSpace(roomCode))
                throw new ArgumentException("roomCode ne peut pas être vide.");

            roomCode = roomCode.Trim().ToUpper();
            Directory.CreateDirectory(destinationDirectory);

            string url = GetStreamUrl(roomCode, currentFile);

            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            string ext = GetExtensionFromContentType(response.Content.Headers.ContentType?.MediaType);
            if (ext.Length == 0) ext = SanitizeExtension(currentFile);
            if (ext.Length == 0) ext = ".mp3";

            string tempPath = Path.Combine(
                destinationDirectory,
                $"ArcadeShare_{roomCode}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{ext}");

            try
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await stream.CopyToAsync(fileStream, 81920, cancellationToken);
            }
            catch
            {
                // Pas de fichier partiel laissé sur le disque
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                throw;
            }

            return tempPath;
        }

        public async Task NotifyMusicEndedAsync()
        {
            if (!_isHost)
                throw new InvalidOperationException("Seul l'hôte peut signaler la fin de musique.");

            await SendJsonAsync(new MusicEndedMessage());
        }

        // ── Invité ───────────────────────────────────────────────────────────────

        public async Task JoinRoomAsync(string code, string username, string deviceSerial)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Le pseudo ne peut pas être vide.");

            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Le code de la room ne peut pas être vide.");

            _isHost = false;
            _deviceSerial = deviceSerial;
            _lastUsername = username;
            _currentRoomCode = code.Trim().ToUpper();

            var msg = new JoinRoomMessage
            {
                Code = _currentRoomCode,
                Username = username.Trim(),
                DeviceSerial = deviceSerial
            };
            await SendJsonAsync(msg);
        }

        public async Task LeaveRoomAsync()
        {
            if (_currentRoomCode == null) return;

            var msg = new LeaveRoomMessage { Code = _currentRoomCode };
            await SendJsonAsync(msg);
            _currentRoomCode = null;
            HostCertified = null;
        }

        public async Task DeleteRoomAsync()
        {
            if (!_isHost)
                throw new InvalidOperationException("Seul l'hôte peut supprimer la room.");

            if (_currentRoomCode == null) return;

            await SendJsonAsync(new DeleteRoomMessage());
            _currentRoomCode = null;
        }

        public async Task DisconnectAsync()
        {
            _intentionalDisconnect = true;

            if (_currentRoomCode != null)
            {
                try { await LeaveRoomAsync(); } catch { }
            }

            await DisconnectInternalAsync();
        }

        private async Task DisconnectInternalAsync()
        {
            _cts?.Cancel();

            if (_ws?.State == WebSocketState.Open)
            {
                try
                {
                    await _ws.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Déconnexion",
                        CancellationToken.None);
                }
                catch { }
            }
        }

        // ── Réception / reconnexion ──────────────────────────────────────────────

        private async Task ReceiveLoopAsync(ClientWebSocket ws, CancellationToken token)
        {
            var buffer = new byte[4096];

            try
            {
                while (!token.IsCancellationRequested && ws.State == WebSocketState.Open)
                {
                    var sb = new StringBuilder();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            HandleDisconnection(ws);
                            return;
                        }

                        sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    while (!result.EndOfMessage);

                    HandleMessage(sb.ToString());
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (WebSocketException)
            {
                // Une boucle annulée volontairement (nouvelle connexion, déconnexion) ne doit pas relancer une reconnexion
                if (!token.IsCancellationRequested)
                    HandleDisconnection(ws);
            }
        }

        private void HandleDisconnection(ClientWebSocket source)
        {
            // Une ancienne connexion (déjà remplacée par une nouvelle) ne doit rien déclencher
            if (!ReferenceEquals(source, _ws)) return;

            OnDisconnected?.Invoke();

            // Pas de reconnexion si la déconnexion est voulue ou si le serveur a refusé l'authentification
            if (_intentionalDisconnect || _authRejected) return;

            // Une seule boucle de reconnexion à la fois (Close + exception peuvent arriver pour la même coupure)
            if (Interlocked.CompareExchange(ref _reconnecting, 1, 0) != 0) return;

            _ = Task.Run(async () =>
            {
                try { await AutoReconnectLoopAsync(); }
                finally { Interlocked.Exchange(ref _reconnecting, 0); }
            });
        }

        private async Task AutoReconnectLoopAsync()
        {
            int attempt = 0;

            while (!_intentionalDisconnect && attempt < MaxReconnectAttempts)
            {
                attempt++;
                int delaySeconds = Math.Min(MaxReconnectDelaySeconds, (int)Math.Pow(2, Math.Min(attempt, 6)));
                OnReconnecting?.Invoke(attempt);

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

                if (_intentionalDisconnect) return;

                try
                {
                    await ConnectAsync();

                    if (_currentRoomCode != null && _lastUsername != null)
                    {
                        if (_isHost)
                            await RejoinRoomAsync(_currentRoomCode, _lastUsername, _deviceSerial ?? "");
                        else
                            await JoinRoomAsync(_currentRoomCode, _lastUsername, _deviceSerial ?? "");
                    }

                    OnReconnected?.Invoke();
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    // Rejeté par le serveur (appId invalide/désactivé, version dépréciée) :
                    // inutile de réessayer, OnAuthRejected a déjà été déclenché dans ConnectAsync.
                    return;
                }
                catch
                {
                    // Échec réseau : on retente à la prochaine itération.
                }
            }

            if (!_intentionalDisconnect && !IsConnected)
                OnError?.Invoke("Impossible de se reconnecter au serveur.");
        }

        private void HandleMessage(string json)
        {
            try
            {
                var obj = JObject.Parse(json);
                var type = obj["type"]?.ToString();

                switch (type)
                {
                    case "auth_ok":
                        _authTcs?.TrySetResult(new AuthResult
                        {
                            Authorized = true,
                            Deprecated = obj.Value<bool?>("deprecated") ?? false,
                            Certified = obj.Value<bool?>("certified") ?? false,
                            Reason = obj.Value<string>("reason")
                        });
                        break;

                    case "auth_rejected":
                        _authRejected = true;
                        var authRejected = JsonConvert.DeserializeObject<AuthResponse>(json);
                        string rejectReason = authRejected?.Reason ?? "Application non autorisée à utiliser ce service.";
                        var rejectResult = new AuthResult { Authorized = false, Reason = rejectReason };

                        // Pendant le handshake : on complète l'attente. Sinon (révocation en cours de session) : on prévient l'appli.
                        if (_authTcs == null || !_authTcs.TrySetResult(rejectResult))
                            OnAuthRejected?.Invoke(rejectReason);
                        break;

                    case "room_created":
                        _currentRoomCode = obj["code"]?.ToString();
                        OnRoomCreated?.Invoke(_currentRoomCode ?? "");
                        break;

                    case "room_joined":
                        var joined = JsonConvert.DeserializeObject<RoomJoinedResponse>(json);
                        if (joined != null)
                        {
                            _currentRoomCode = joined.Code;
                            HostCertified = joined.HostCertified;
                            OnHostCertification?.Invoke(HostCertified);
                            OnRoomJoined?.Invoke(joined);
                        }
                        break;

                    case "state":
                        var state = JsonConvert.DeserializeObject<StateUpdate>(json);
                        if (state != null) OnStateReceived?.Invoke(state);
                        break;

                    case "guest_joined":
                        var gj = JsonConvert.DeserializeObject<GuestChangedResponse>(json);
                        if (gj != null) OnGuestJoined?.Invoke(gj);
                        break;

                    case "guest_left":
                        var gl = JsonConvert.DeserializeObject<GuestChangedResponse>(json);
                        if (gl != null) OnGuestLeft?.Invoke(gl);
                        break;

                    case "host_left":
                        _currentRoomCode = null;
                        HostCertified = null;
                        OnHostLeft?.Invoke();
                        break;

                    case "error":
                        var err = JsonConvert.DeserializeObject<ErrorResponse>(json);
                        if (err != null) OnError?.Invoke(err.Message);
                        break;

                    case "pong":
                        break;
                }
            }
            catch (JsonException ex)
            {
                OnError?.Invoke($"Erreur de parsing JSON : {ex.Message}");
            }
        }

        private async Task PingLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), token);
                    if (IsConnected)
                    {
                        try { await SendJsonAsync(new PingMessage()); }
                        catch { }
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task SendJsonAsync<T>(T obj) where T : notnull
            => await SendJsonInternalAsync(obj, injectIds: true);

        private async Task SendJsonInternalAsync<T>(T obj, bool injectIds) where T : notnull
        {
            if (_ws == null || _ws.State != WebSocketState.Open)
                throw new InvalidOperationException("WebSocket non connecté. La reconnexion automatique est en cours si elle a été déclenchée.");

            JObject jObj = JObject.FromObject(obj);

            if (injectIds)
            {
                jObj["appId"] = _appId;
                jObj["versionId"] = VERSION_ID;
            }

            var json = jObj.ToString(Formatting.None);
            var bytes = Encoding.UTF8.GetBytes(json);

            await _sendLock.WaitAsync();
            try
            {
                await _ws!.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken: _cts?.Token ?? CancellationToken.None);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public void Dispose()
        {
            _intentionalDisconnect = true;
            _cts?.Cancel();
            _ws?.Dispose();
            _cts?.Dispose();
            _sendLock.Dispose();
        }
    }
}
