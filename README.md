# Audio_Arcade_SDK

## Arcade Share Server

> Partagez votre musique à distance, en temps réel, sans réseau commun — et gardez le contrôle total de votre session.

📱 **Android** &nbsp;·&nbsp; 🪟 **Windows** &nbsp;·&nbsp; 🐧 ~~Linux~~ *(à venir)*

---

## 📋 Présentation

**Arcade Share Server** est un SDK qui intègre la fonctionnalité de *cast amélioré* d'Audio_Arcade directement dans vos applications musicales. Il permet le partage de musique à distance, de façon synchronisée, **sans que les auditeurs aient besoin d'être sur la même connexion internet**. Vous restez le maître de la session : les auditeurs ne peuvent que mettre en pause ou reprendre la lecture de leur côté uniquement.

> ℹ️ **À noter :** le code de session fonctionne uniquement **de vous vers un autre appareil**. Il n'est pas possible d'écouter votre propre musique via ce code sur le même compte — le partage est conçu pour être partagé avec d'autres personnes, pas avec vous-même.

---

## ✨ Fonctionnalités clés

| | |
|---|---|
| 🛰️ **Sans réseau commun** | Hôte et auditeurs n'ont pas besoin de partager la même connexion. Le SDK gère tout automatiquement. |
| 🔄 **Synchronisation temps réel** | Tout le monde écoute la même chose au même moment, grâce à la synchronisation côté serveur. |
| 👑 **Contrôle maître** | L'hôte garde la main sur tout. Les auditeurs peuvent uniquement pause / reprendre de leur côté. |

---

## 🔐 Permissions par rôle

| Rôle | Pause / Reprise | Changer de piste | File d'attente | Volume | Stopper la session | Quitter la session |
|---|---|---|---|---|---|---|
| 👑 Hôte | ✅ Oui | ✅ Oui | ✅ Oui | ✅ Oui | ✅ Oui | ❌ Non |
| 🎧 Auditeur | ⚠️ Local uniquement | ❌ Non | ❌ Non | ✅ Oui | ❌ Non | ✅ Oui |

---

## 🖥️ Plateformes supportées

| Plateforme | Statut |
|---|---|
| 📱 Android | ✅ Disponible |
| 🪟 Windows | ✅ Disponible |
| 🐧 Linux | 🚧 En développement |

---

## 🛡️ Sécurité & Certification

> 🚨 **Ne partagez jamais votre code de session avec une application non certifiée**
>
> Ne saisissez ou ne partagez jamais un code de lecture dans une application qui n'a pas obtenu la certification officielle Audio_Arcade. Une application non certifiée peut intercepter votre session, accéder à vos données ou compromettre votre compte. **Vérifiez toujours la certification avant d'utiliser le SDK dans une application tierce.** Si une application vous demande ce code sans être certifiée, refusez et signalez-la immédiatement.

**[✅ Voir les applications certifiées](https://audio-arcade.odoo.com/sdk-doc/arcade-share-server/certified-apps)**

> 🔎 **Depuis la V3, le SDK expose la certification.** Ton application peut savoir si elle est elle-même certifiée, consulter le statut public d'une application à partir de son nom et, côté invité, savoir si l'application de l'hôte est certifiée. Rien n'est bloqué : c'est une information à afficher à l'utilisateur (voir [section 7](#7-certification-de-lapplication)).

---

# 🪟 Documentation Windows — C#

> SDK de synchronisation audio en temps réel entre appareils. L'URL du serveur est intégrée au SDK : ton application n'a plus qu'à fournir son `appId`.

---

## 📑 Sommaire

1. [Authentification (appId)](#-authentification-appid)
2. [Installation](#1-installation)
3. [ID de l'appareil](#2-id-de-lappareil)
4. [Mode Hôte — Cast](#3-mode-hôte--cast)
5. [Rooms permanentes](#4-rooms-permanentes)
6. [Mode Invité — Live](#5-mode-invité--live)
7. [Envoi de musique](#6-envoi-de-musique)
8. [Certification de l'application](#7-certification-de-lapplication)
9. [Référence des méthodes](#8-référence-des-méthodes)
10. [Référence des événements](#9-référence-des-événements)

---

## 🔑 Authentification (appId)

Chaque application qui utilise ArcadeShare doit posséder son propre `appId`, fourni par l'équipe Audio_Arcade ([formulaire de contact](https://audio-arcade.odoo.com/contactus)). Il permet d'autoriser ou de révoquer l'accès application par application.

| Élément | Qui le fournit | Rôle |
|---|---|---|
| `appId` | Toi (obtenu auprès d'Audio_Arcade) | Identifie ton application auprès du serveur |
| `versionId` | Le SDK (intégré, ne pas modifier) | Identifie la version du SDK utilisée |

| Situation | Comportement sous Windows |
|---|---|
| `appId` inconnu ou désactivé | `OnAuthRejected` + exception `UnauthorizedAccessException` à `ConnectAsync()` |
| Version du SDK dépréciée (fonctionne encore) | `OnDeprecatedVersion` |
| Version du SDK inconnue | Connexion refusée : télécharge la dernière version du SDK |
| Application non certifiée | Aucun blocage : `IsCertified` vaut `false` (voir section 7) |

> ⚠️ Ne modifie pas la valeur de `versionId` dans le code du SDK : le serveur connaît la correspondance réelle entre cet identifiant et la version publiée, et refuse une version dépréciée ou inconnue.

---

## 1. Installation

Télécharge d'abord le fichier `ArcadeShareServer.dll` :

**[⬇ Télécharger ArcadeShareServer.dll](https://github.com/Audio-Arcade/Audio_Arcade_SDK/releases/download/V3/ArcadeShareServer.dll)**

Ajoute le fichier suivant à ton projet Visual Studio :

| Fichier | Rôle |
|---|---|
| `ArcadeShareServer.dll` | Client WebSocket compilé (URL du serveur intégrée) |

Clic droit sur **Dépendances** → **Ajouter une référence** → **Parcourir** → sélectionne `ArcadeShareServer.dll`.

Installe ensuite ces packages NuGet (clic droit sur le projet → *Gérer les packages NuGet*) :

| Package | Version |
|---|---|
| Newtonsoft.Json | >= 13.0 |
| System.Management | >= 8.0 |

Ajoute ces usings en haut de tes fichiers :

```csharp
using ArcadeShareServer;
using ArcadeShareServer.Models;
using Newtonsoft.Json.Linq;
```

### Créer un client

Le constructeur exige ton `appId`. Le plus simple est de le centraliser dans une petite classe :

```csharp
internal static class ArcadeShareConfig
{
    public const string AppId = "TON_APP_ID";   // fourni par Audio_Arcade

    public static ArcadeShareClient CreateClient() => new ArcadeShareClient(AppId);
}
```

> ⚠️ Le SDK utilise `Newtonsoft.Json` en interne (pas `System.Text.Json`) : les objets renvoyés par le SDK (ex. liste de rooms) sont des `JObject` / `JToken`, à manipuler avec l'indexeur `["clé"]` comme montré dans les exemples ci-dessous.

> ℹ️ Les pseudos font entre **2 et 24 caractères**. Tu peux demander le pseudo une seule fois à l'utilisateur et le mémoriser dans les paramètres de ton application.

---

## 2. ID de l'appareil

ArcadeShare identifie chaque appareil par son UUID matériel. Ajoute cette méthode dans ta classe principale :

```csharp
private static string GetDeviceSerial()
{
    try
    {
        using var searcher = new System.Management.ManagementObjectSearcher(
            "SELECT UUID FROM Win32_ComputerSystemProduct");

        foreach (System.Management.ManagementObject obj in searcher.Get())
            return obj["UUID"]?.ToString() ?? Environment.MachineName;
    }
    catch { }

    return Environment.MachineName;
}
```

> ℹ️ Cette méthode retourne un identifiant stable même après redémarrage. C'est cet ID qui permet d'accéder aux pseudos réservés et aux rooms permanentes.

---

## 3. Mode Hôte — Cast

L'hôte diffuse l'état de lecture à tous ses invités en temps réel.

### Déclarer les champs nécessaires

```csharp
private ArcadeShareClient _arcadeShareServer = null;
private List<ArcadeShareClient> _permanentClients = new();
private bool isCasting = false;
```

### Démarrer le cast

```csharp
var client = ArcadeShareConfig.CreateClient();

client.OnRoomCreated += code =>
{
    isCasting = true;
    Console.WriteLine($"Room active : {code}");
    StartStateLoop(); // démarrer l'envoi d'état
};

client.OnGuestJoined += info =>
    Console.WriteLine($"{info.Username} a rejoint ({info.GuestCount} invité(s))");

client.OnGuestLeft += info =>
    Console.WriteLine($"{info.Username} est parti ({info.GuestCount} invité(s))");

client.OnAuthRejected += reason => Console.WriteLine(reason);
client.OnDeprecatedVersion += msg => Console.WriteLine(msg);

client.OnCertificationChanged += certified =>      // true / false, informatif (voir section 7)
    Console.WriteLine($"Application certifiée : {certified}");

client.OnReconnectFailed += () => { /* reconnexion abandonnée : arrête le cast */ };

await client.ConnectAsync();
await client.CreateRoomAsync(
    username:     "Mon Pseudo",
    deviceSerial: GetDeviceSerial()
);
_arcadeShareServer = client;
```

> ℹ️ La reconnexion après une coupure réseau est **automatique** (jusqu'à 25 tentatives, délai croissant plafonné à 15 s, soit environ 5 minutes — la durée pendant laquelle le serveur conserve la room de l'hôte) : n'ajoute pas ta propre logique dans `OnDisconnected`. Après une reconnexion, `OnRoomCreated` est déclenché à nouveau pour la même room : affiche ton message « Cast démarré » une seule fois. Si toutes les tentatives échouent, `OnReconnectFailed` est déclenché, puis `OnError`.

### Boucle d'envoi d'état (toutes les 2 secondes)

```csharp
private void StartStateLoop()
{
    _ = Task.Run(async () =>
    {
        while (_arcadeShareServer != null && _arcadeShareServer.IsConnected && isCasting)
        {
            try
            {
                await _arcadeShareServer.SendStateAsync(
                    position:  audioFile.CurrentTime.TotalSeconds,
                    isPlaying: !isPaused,
                    title:     trackName,
                    artist:    "",
                    duration:  audioFile.TotalTime.TotalSeconds
                );
            }
            catch { }

            await Task.Delay(2000);
        }
    });
}
```

### Arrêter le cast

```csharp
await _arcadeShareServer.DisconnectAsync();
_arcadeShareServer.Dispose();
_arcadeShareServer = null;
isCasting = false;
```

> ⚠️ Chaque room accepte au maximum **6 invités**. Au-delà, toute nouvelle tentative de connexion est refusée par le serveur (l'invité reçoit `OnError`).

---

## 4. Rooms permanentes

Une room permanente a un code fixe qui ne change jamais. L'hôte la crée une seule fois et la réactive à chaque cast. Maximum **2 rooms** par appareil.

> ℹ️ Toute la logique réseau (création, listing, suppression) est encapsulée dans `ArcadeShareClient` — aucun appel HTTP manuel n'est nécessaire côté application, et ces trois méthodes n'exigent pas de `ConnectAsync()`.

### Créer une room permanente

À appeler depuis l'interface des paramètres — une seule fois :

```csharp
using var client = ArcadeShareConfig.CreateClient();

var (ok, code, error) = await client.CreatePermanentRoomAsync(
    username:     "Mon Pseudo",
    deviceSerial: GetDeviceSerial()
);

if (ok) Console.WriteLine($"Room créée : {code}");   // afficher le code à l'utilisateur
else    Console.WriteLine($"Refusé : {error}");       // ex. « Maximum 2 rooms permanentes atteint »
```

### Activer toutes les rooms permanentes au démarrage du cast

```csharp
private async Task<bool> StartPermanentCastAsync(string username, string deviceSerial)
{
    using var bootstrapClient = ArcadeShareConfig.CreateClient();
    var rooms = await bootstrapClient.GetMyRoomsAsync(deviceSerial);

    if (rooms.Count == 0) return false;

    foreach (var roomInfo in rooms)
    {
        string code = roomInfo["code"]?.ToString() ?? "";
        if (string.IsNullOrEmpty(code)) continue;

        var client = ArcadeShareConfig.CreateClient();

        client.OnRoomCreated += c => Console.WriteLine($"Room {c} active");
        // ajouter OnGuestJoined, OnGuestLeft, OnError...

        _permanentClients.Add(client);   // avant la connexion : l'événement « room prête » arrive peu après

        await client.ConnectAsync();
        await client.RejoinRoomAsync(code, username, deviceSerial);
    }

    _arcadeShareServer = _permanentClients.FirstOrDefault();
    return _permanentClients.Count > 0;
}
```

> ℹ️ Avec plusieurs rooms, envoie l'état (`SendStateAsync`) et la musique (`UploadMusicAsync`) à **chaque** client de la liste, pas seulement au premier.

### Supprimer une room permanente

```csharp
using var client = ArcadeShareConfig.CreateClient();
bool ok = await client.DeletePermanentRoomAsync(code, GetDeviceSerial());
```

---

## 5. Mode Invité — Live

L'invité rejoint une room avec son code. La musique est téléchargée automatiquement et la lecture se synchronise avec l'hôte.

### Champs nécessaires

```csharp
private ArcadeShareClient _arcadeShareServer = null;

// Invité = connecté à la room d'un autre hôte
private bool isGuest => _arcadeShareServer != null && !_arcadeShareServer.IsHost;

private bool   _guestPausedManually = false;
private string _guestCurrentKey     = "";
private string _guestTempFile       = null;
private bool   _guestIsLoading      = false;
```

> ℹ️ Pendant que `isGuest` est vrai, bloque la lecture locale (lecture d'un morceau, suivant, précédent, cast) : sinon l'utilisateur écoute sa musique en même temps que celle de l'hôte.

### Rejoindre une room

```csharp
var client = ArcadeShareConfig.CreateClient();
_arcadeShareServer = client;   // dès maintenant : isGuest est vrai pendant la connexion

client.OnRoomJoined += info =>
    Console.WriteLine($"Connecté chez {info.HostName}");   // info.HostCertified : true / false / null (voir section 7)

client.OnStateReceived += state =>
    AppliquerEtatDistant(state, roomCode);

client.OnHostLeft += () =>
{
    _arcadeShareServer = null;
    client.Dispose();
    StopGuestPlayback();
};

client.OnError += msg => Console.WriteLine(msg);   // room introuvable, room pleine...

// Coupure réseau : le client se reconnecte tout seul et rejoint à nouveau la room.
// N'appelle PAS client.Dispose() dans OnDisconnected : cela annulerait la reconnexion.
client.OnReconnecting += attempt => Console.WriteLine($"Reconnexion… ({attempt})");
client.OnReconnectFailed += () =>      // abandon : quitte la room côté application
{
    _arcadeShareServer = null;
    client.Dispose();
    StopGuestPlayback();
};

await client.ConnectAsync();
await client.JoinRoomAsync(
    code:         "ABC123",
    username:     "Mon Pseudo",
    deviceSerial: GetDeviceSerial()
);
```

### Synchronisation de l'état reçu

```csharp
private void AppliquerEtatDistant(StateUpdate state, string roomCode)
{
    if (!isGuest) return;   // état arrivé après avoir quitté la room

    if (!string.IsNullOrEmpty(state.Title))
    {
        string newName = string.IsNullOrEmpty(state.Artist)
            ? state.Title : $"{state.Artist} - {state.Title}";
        if (trackName != newName) { trackName = newName; scrollPos = 0; }
    }

    if (string.IsNullOrEmpty(state.CurrentFile)) return;   // l'hôte n'a pas (encore) de musique

    // Clé = room + fichier : évite de confondre deux rooms qui diffusent un fichier de même nom
    string key = $"{roomCode}|{state.CurrentFile}";

    // Nouvelle piste détectée → télécharger (un seul téléchargement à la fois)
    if (key != _guestCurrentKey)
    {
        if (_guestIsLoading) return;   // le prochain état (≈ 2 s) refera la comparaison

        _guestPausedManually = false;
        _guestIsLoading      = true;

        _ = Task.Run(async () =>
        {
            try
            {
                bool ok = await LoadGuestMusicAsync(roomCode, state.Position, state.IsPlaying, state.CurrentFile);
                if (ok) _guestCurrentKey = key;   // sinon : nouvel essai au prochain état (2 s)
            }
            finally { _guestIsLoading = false; }
        });
        return;
    }

    // Synchroniser la position (si écart > 2 secondes)
    if (audioFile != null && !_guestPausedManually)
    {
        var diff = Math.Abs((audioFile.CurrentTime - TimeSpan.FromSeconds(state.Position)).TotalSeconds);
        if (diff > 2.0) audioFile.CurrentTime = TimeSpan.FromSeconds(state.Position);

        if (outputDevice != null)
        {
            if (state.IsPlaying && isPaused)       { outputDevice.Play();  isPaused = false; }
            else if (!state.IsPlaying && !isPaused) { outputDevice.Pause(); isPaused = true; }
        }
    }
}
```

> ⚠️ Mémorise le fichier courant (`_guestCurrentKey`) **seulement après un téléchargement réussi**. Sinon, un seul échec réseau empêcherait définitivement de réessayer pour cette piste.

### Téléchargement et lecture de la musique

```csharp
private async Task<bool> LoadGuestMusicAsync(string roomCode, double startPos, bool shouldPlay, string currentFile)
{
    // Le SDK gère le téléchargement, l'extension et le nommage du fichier temporaire.
    // currentFile (StateUpdate.CurrentFile) évite de réutiliser un cache périmé pour une nouvelle piste.
    using var client = ArcadeShareConfig.CreateClient();
    string? tempPath = await client.DownloadRoomMusicAsync(
        roomCode, Path.GetTempPath(), currentFile: currentFile);
    if (tempPath == null) return false;   // échec : le son en cours n'est pas coupé

    BeginInvoke(() =>
    {
        outputDevice?.Stop(); outputDevice?.Dispose(); outputDevice = null;
        audioFile?.Dispose(); audioFile = null;

        if (_guestTempFile != null) { try { File.Delete(_guestTempFile); } catch { } }
        _guestTempFile = tempPath;

        audioFile    = new NAudio.Wave.AudioFileReader(tempPath);
        outputDevice = new NAudio.Wave.WaveOut();
        outputDevice.Init(audioFile);
        audioFile.Volume      = Volume.Value / 100f;
        audioFile.CurrentTime = TimeSpan.FromSeconds(startPos);
        if (shouldPlay) { outputDevice.Play(); isPaused = false; }
        else              isPaused = true;
        StartTimerThread();
        UpdateButtonsState();
    });

    return true;
}
```

> ℹ️ Pour lire en streaming plutôt que télécharger, `client.GetStreamUrl(roomCode, state.CurrentFile)` donne l'URL authentifiée de la musique de la room.

### Quitter la room / arrêter la lecture invité

```csharp
// Quitter : mettre _arcadeShareServer à null AVANT, pour réactiver les boutons
var leaving = _arcadeShareServer;
_arcadeShareServer = null;
await leaving.DisconnectAsync();
leaving.Dispose();
StopGuestPlayback();

private void StopGuestPlayback()
{
    _guestCurrentKey   = "";
    _guestIsLoading    = false;
    outputDevice?.Stop(); outputDevice?.Dispose(); outputDevice = null;
    audioFile?.Dispose(); audioFile = null;
    isPaused = false;

    if (_guestTempFile != null) { try { File.Delete(_guestTempFile); } catch { } _guestTempFile = null; }

    BeginInvoke(() =>
    {
        trackName = ""; Titre.Text = "";
        Timer_Start.Text = "00:00"; Timer_end.Text = "00:00";
        progressBar.Value = 0;
        UpdateButtonsState();
    });
}
```

---

## 6. Envoi de musique

Appelle `UploadMusicAsync` dans `PlayAt()` à chaque changement de piste, vers **toutes** les rooms :

```csharp
// Dans PlayAt(), après avoir mis à jour currentFilePath
if (isCasting && _permanentClients.Count > 0 && File.Exists(currentFilePath))
{
    string path = currentFilePath;
    var targets = _permanentClients.ToList();

    _ = Task.Run(async () =>
    {
        foreach (var client in targets)
        {
            try
            {
                if (client.IsConnected && !await client.UploadMusicAsync(path))
                    Console.WriteLine("Envoi refusé par le serveur");
            }
            catch { }
        }
    });
}

// En fin de piste (optionnel — supprime le fichier côté serveur)
await _arcadeShareServer.NotifyMusicEndedAsync();
```

| Contrainte | Valeur |
|---|---|
| Formats acceptés | `.mp3` `.flac` `.wav` `.ogg` `.aac` `.m4a` `.opus` (les `.mid` sont refusés) |
| Taille maximale | 200 Mo |

| Refus du serveur | Réaction du SDK |
|---|---|
| 413 — fichier trop volumineux | `OnError("Fichier trop volumineux pour le serveur.")`, pas de nouvelle tentative |
| 415 — format non pris en charge | `OnError("Format audio non pris en charge par le serveur.")`, pas de nouvelle tentative |
| 401 / 403 — application non autorisée | `OnError("Envoi refusé par le serveur (application non autorisée).")`, pas de nouvelle tentative |
| Erreur réseau ou serveur | 3 tentatives, puis `OnError("Impossible d'envoyer la musique au serveur.")` |

> ℹ️ **V3** : `UploadMusicAsync` retourne `true` seulement si le serveur sert réellement le fichier après l'envoi (vérification automatique) ; sinon `false`, et la raison est donnée par `OnError`.

> ℹ️ Le fichier est aussi uploadé au démarrage du cast si une musique est déjà en cours (dans `OnRoomCreated`), pour que les invités qui se connectent reçoivent la bonne piste sans attendre le prochain `PlayAt`.

---

## 7. Certification de l'application

Le serveur sait si ton application est certifiée par Audio_Arcade. Cette information est **purement informative** : une application non certifiée continue de fonctionner. Elle te permet d'afficher un indicateur (accueil, paramètres, page des codes…) pour que l'utilisateur sache à qui il confie son code de session.

### Certification de ta propre application

```csharp
client.OnCertificationChanged += certified =>   // bool? : true / false, à chaque authentification réussie
{
    // mets à jour ton indicateur (repasse sur le thread de l'interface : BeginInvoke / Dispatcher)
};

bool? c = client.IsCertified;   // null tant qu'aucune connexion authentifiée n'a eu lieu
```

> ℹ️ `IsCertified` n'est connu qu'après `ConnectAsync()`. Pour afficher le statut dès l'accueil, sans client connecté, utilise `FetchCertificationStatusAsync`.

### Statut public d'une application (par son nom)

```csharp
// Méthode statique : ni connexion, ni room, ni appId nécessaires
string? status = await ArcadeShareClient.FetchCertificationStatusAsync("Audio_Arcade");
// "certified" | "partial" | "unverified" | "revoked" | null (serveur injoignable)
```

| Statut | Signification |
|---|---|
| `certified` | Application certifiée par Audio_Arcade |
| `partial` | Certification partielle |
| `unverified` | Application non vérifiée / non certifiée |
| `revoked` | Certification révoquée |
| `null` | Statut indisponible (réseau ou serveur injoignable) |

> ℹ️ Cette requête est publique et ne révèle jamais l'`appId`.

### Certification de l'application de l'hôte (invité)

```csharp
client.OnHostCertification += certified =>   // bool? : true / false / null (inconnue)
{
    // déclenché juste avant OnRoomJoined ; la même valeur est dans info.HostCertified et client.HostCertified
};
```

> ℹ️ Les événements sont déclenchés sur un thread d'arrière-plan : repasse sur le thread de l'interface avant de modifier un contrôle.

---

## 8. Référence des méthodes

| Méthode | Description | Accès |
|---|---|---|
| `new ArcadeShareClient(appId)` | Crée un client (appId obligatoire) | Tous |
| `ConnectAsync()` | Connecte le WebSocket et authentifie l'application | Tous |
| `CreateRoomAsync(username, deviceSerial)` | Crée une room via le WebSocket | Hôte |
| `CreatePermanentRoomAsync(username, deviceSerial)` | Crée une room permanente (max 2 par appareil). Retourne `(bool Ok, string? Code, string? Error)` | Hôte |
| `GetMyRoomsAsync(deviceSerial)` | Liste les rooms permanentes de l'appareil | Hôte |
| `RejoinRoomAsync(code, username, deviceSerial)` | Réactive une room permanente | Hôte |
| `DeletePermanentRoomAsync(code, deviceSerial)` | Supprime définitivement une room permanente. Retourne `bool` | Hôte |
| `SendStateAsync(position, isPlaying, title, artist, duration, ...)` | Envoie l'état de lecture | Hôte |
| `UploadMusicAsync(filePath)` | Upload le fichier audio (envoi en flux). Retourne `bool` | Hôte |
| `NotifyMusicEndedAsync()` | Signale la fin de piste | Hôte |
| `JoinRoomAsync(code, username, deviceSerial)` | Rejoint une room en tant qu'invité | Invité |
| `DownloadRoomMusicAsync(roomCode, destDir, ct, currentFile)` | Télécharge la musique courante de la room (retourne le chemin, ou `null`). `currentFile` : nom du fichier courant (`StateUpdate.CurrentFile`), optionnel | Invité |
| `GetStreamUrl(roomCode, currentFile)` | URL authentifiée de la musique de la room (lecture en streaming) | Invité |
| `ArcadeShareClient.FetchCertificationStatusAsync(appName)` | Méthode **statique** : statut public d'une application d'après son nom | Tous |
| `LeaveRoomAsync()` | Quitte la room (room conservée) | Tous |
| `DisconnectAsync()` | Déconnexion propre (désactive la reconnexion automatique) | Tous |
| `Dispose()` | Libère les ressources | Tous |

### Propriétés publiques

| Propriété | Type | Description |
|---|---|---|
| `IsConnected` | `bool` | Connexion WebSocket active |
| `CurrentRoomCode` | `string?` | Code de la room en cours |
| `IsHost` | `bool` | Vrai si le client est hôte (devient vrai après `CreateRoomAsync` ou `RejoinRoomAsync`) |
| `IsCertified` | `bool?` | Certification de ton application (`null` = pas encore connu) |
| `HostCertified` | `bool?` | (Invité) Certification de l'application de l'hôte (`null` = inconnue) |
| `ArcadeShareClient.SDK_VERSION` | `string` | Version du SDK (constante) |

---

## 9. Référence des événements

| Événement | Paramètre | Déclenché quand |
|---|---|---|
| `OnConnected` | — | Connexion établie et application authentifiée |
| `OnAuthRejected` | `string reason` | Le serveur refuse l'application (appId inconnu ou désactivé, version inconnue) |
| `OnDeprecatedVersion` | `string message` | Version du SDK dépréciée (la connexion fonctionne, mise à jour conseillée) |
| `OnRoomCreated` | `string code` | Room créée ou reprise (aussi après une reconnexion) |
| `OnRoomJoined` | `RoomJoinedResponse` | Invité connecté à la room (aussi après une reconnexion) |
| `OnStateReceived` | `StateUpdate` | Nouvel état reçu de l'hôte |
| `OnGuestJoined` | `GuestChangedResponse` | Un invité rejoint |
| `OnGuestLeft` | `GuestChangedResponse` | Un invité quitte |
| `OnHostLeft` | — | L'hôte a quitté ou est déconnecté |
| `OnError` | `string message` | Erreur (room pleine, code invalide, reconnexion impossible, fichier refusé à l'envoi...) |
| `OnDisconnected` | — | Connexion perdue (la reconnexion automatique démarre) |
| `OnReconnecting` | `int attempt` | À chaque tentative de reconnexion automatique |
| `OnReconnected` | — | Reconnexion réussie, room restaurée |
| `OnReconnectFailed` | — | Reconnexion automatique abandonnée (juste avant `OnError`) |
| `OnCertificationChanged` | `bool? certified` | Certification de ton application connue (à chaque authentification réussie) |
| `OnHostCertification` | `bool? certified` | (Invité) Certification de l'application de l'hôte, juste avant `OnRoomJoined` |

### Modèle `RoomJoinedResponse`

| Champ | Type | Description |
|---|---|---|
| `Code` | `string` | Code de la room |
| `HostName` | `string` | Pseudo de l'hôte |
| `GuestCount` | `int` | Nombre d'invités connectés |
| `HostCertified` | `bool?` | Application de l'hôte certifiée (`true`), non certifiée (`false`) ou inconnue (`null`) |
| `LastState` | `StateUpdate?` | Dernier état connu de la lecture |

### Modèle `StateUpdate`

| Champ | Type | Description |
|---|---|---|
| `Title` | `string` | Titre de la piste |
| `Artist` | `string` | Artiste |
| `Position` | `double` | Position en secondes |
| `Duration` | `double` | Durée totale en secondes |
| `IsPlaying` | `bool` | Lecture en cours |
| `CurrentFile` | `string` | Nom du fichier audio côté serveur (détection changement de piste) |
| `ArtworkUrl` | `string?` | URL de la pochette (optionnel) |
| `NextTitle` | `string?` | Titre suivant (optionnel) |
| `Timestamp` | `long` | Horodatage Unix ms |

---

<sub>ArcadeShare SDK · Windows · Audio_Arcade — La_TNT<br>Copyright © 2024–2026 Audio_Arcade. Tous droits réservés.</sub>
