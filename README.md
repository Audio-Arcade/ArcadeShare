# Audio_Arcade_SDK

## Arcade Share

> Partagez votre musique à distance, en temps réel, sans réseau commun — et gardez le contrôle total de votre session.

📱 **Android** &nbsp;·&nbsp; 🪟 **Windows** &nbsp;·&nbsp; 🐧 ~~Linux~~ *(à venir)*

**Version 4.0.0** &nbsp;·&nbsp; [📖 Documentation en ligne](https://audio-arcade.fr.eu.org/sdk)

---

## 📋 Présentation

**Arcade Share** est un SDK qui intègre la fonctionnalité de *cast amélioré* d'Audio_Arcade directement dans vos applications musicales. Il permet le partage de musique à distance, de façon synchronisée, **sans que les auditeurs aient besoin d'être sur la même connexion internet**. Vous restez le maître de la session : les auditeurs ne peuvent que mettre en pause ou reprendre la lecture de leur côté uniquement.

> ℹ️ **À noter:** le code de session fonctionne uniquement **de vous vers un autre appareil**. Il n'est pas possible d'écouter votre propre musique via ce code sur le même compte — le partage est conçu pour être partagé avec d'autres personnes, pas avec vous-même.

---

## ✨ Fonctionnalités clés

| | |
|---|---|
| 🛰️ **Sans réseau commun** | Hôte et auditeurs n'ont pas besoin de partager la même connexion. Le SDK gère tout automatiquement. |
| 🔄 **Synchronisation temps réel** | Tout le monde écoute la même chose au même moment, grâce à la synchronisation côté serveur. |
| 👑 **Contrôle maître** | L'hôte garde la main sur tout. Les auditeurs peuvent uniquement pause / reprendre de leur côté. |
| 🔗 **Compte lié et pseudo vérifié** *(V4)* | L'utilisateur lie son appareil à son compte Audio_Arcade : son pseudo réservé devient un pseudo **vérifié** et ne peut plus être usurpé. |
| ✅ **Certification** | Ton application peut être certifiée par Audio_Arcade, et le SDK expose ce statut. |

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

> 🔎 **Depuis la V3, le SDK expose la certification.** Ton application peut savoir si elle est elle-même certifiée, consulter le statut public d'une application à partir de son nom et, côté invité, savoir si l'application de l'hôte est certifiée. Rien n'est bloqué : c'est une information à afficher à l'utilisateur (voir [section 8](#8-certification-de-lapplication)).

> 🔗 **Depuis la V4, le SDK protège les pseudos.** Un pseudo réservé sur Audio_Arcade ne peut être utilisé dans une room que par son propriétaire, **vérifié** grâce à la liaison de son compte. Toute autre personne reçoit l'erreur « Nom réservé » (voir [section 7](#7-compte-audio_arcade-lié-pseudo-vérifié)).

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
8. [Compte Audio_Arcade lié (pseudo vérifié)](#7-compte-audio_arcade-lié-pseudo-vérifié)
9. [Certification de l'application](#8-certification-de-lapplication)
10. [Référence des méthodes](#9-référence-des-méthodes)
11. [Référence des événements](#10-référence-des-événements)
12. [Migration depuis la V3 et historique](#11-migration-depuis-la-v3-et-historique)

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
| Application révoquée en cours de session | `OnAuthRejected`, sans nouvelle tentative de reconnexion |
| Version du SDK dépréciée (fonctionne encore) | `OnDeprecatedVersion` |
| Version du SDK inconnue | Connexion refusée : télécharge la dernière version du SDK |
| Application non certifiée | Aucun blocage : `IsCertified` vaut `false` (voir section 8) |

> ⚠️ Ne modifie pas la valeur de `versionId` dans le code du SDK : le serveur connaît la correspondance réelle entre cet identifiant et la version publiée, et refuse une version dépréciée ou inconnue.

---

## 1. Installation

### Option A — NuGet (recommandé)

Le SDK est publié sur NuGet sous le nom `ArcadeShare`. Choisis l'une de ces méthodes :

**En ligne de commande** (dans le dossier de ton projet) :

```bash
dotnet add package ArcadeShare --version 4.0.0
```

**Dans Visual Studio** : clic droit sur le projet → *Gérer les packages NuGet* → onglet *Parcourir* → tape `ArcadeShare` → *Installer*.

**Depuis le site** : [https://www.nuget.org/packages/ArcadeShare](https://www.nuget.org/packages/ArcadeShare)

> ℹ️ Avec NuGet, les dépendances (`Newtonsoft.Json` et `System.Management`) sont installées automatiquement : tu n'as rien d'autre à ajouter.

### Option B — Fichier DLL

Télécharge d'abord le fichier `ArcadeShare.dll` :

**[⬇ Télécharger ArcadeShare.dll](https://github.com/Audio-Arcade/ArcadeShare/releases/download/v4.0.0/ArcadeShare.dll)**

Ajoute le fichier suivant à ton projet Visual Studio :

| Fichier | Rôle |
|---|---|
| `ArcadeShare.dll` | Client WebSocket compilé (URL du serveur intégrée) |

Clic droit sur **Dépendances** → **Ajouter une référence** → **Parcourir** → sélectionne `ArcadeShare.dll`.

Installe ensuite ces packages NuGet (clic droit sur le projet → *Gérer les packages NuGet*) :

| Package | Version |
|---|---|
| Newtonsoft.Json | >= 13.0 |
| System.Management | >= 8.0 |

> ⚠️ N'utilise pas en même temps le package NuGet **et** une référence vers la DLL : deux versions du SDK seraient chargées, et l'application planterait au lancement (`Could not load file or assembly 'ArcadeShare'`). Choisis l'une des deux options.

### Usings

Ajoute ces usings en haut de tes fichiers :

```csharp
using ArcadeShare;
using ArcadeShare.Models;
using Newtonsoft.Json.Linq;
```

### Créer un client

Le constructeur exige ton `appId`. Le plus simple est de le centraliser dans une petite classe :

```csharp
internal static class ArcadeShareConfig
{
    public const string AppId = "TON_APP_ID";   // fourni par Audio_Arcade

    // Sans compte lié : identique à la V3
    public static ArcadeShareClient CreateClient() => new ArcadeShareClient(AppId);

    // Avec compte lié (V4) : voir la section 7
    // public static ArcadeShareClient CreateClient() => new ArcadeShareClient(AppId, LinkStore);
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

> ℹ️ Cette méthode retourne un identifiant stable même après redémarrage. C'est cet ID qui permet d'accéder aux rooms permanentes. Pour les pseudos réservés, la méthode recommandée depuis la V4 est la **liaison de compte** (section 7).

---

## 3. Mode Hôte — Cast

L'hôte diffuse l'état de lecture à tous ses invités en temps réel.

### Déclarer les champs nécessaires

```csharp
private ArcadeShareClient _arcadeShare = null;
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
{
    // info.VerifiedHandle : pseudo vérifié de l'invité (V4), ou null
    string badge = string.IsNullOrEmpty(info.VerifiedHandle) ? "" : " ✔";
    Console.WriteLine($"{info.Username}{badge} a rejoint ({info.GuestCount} invité(s))");
};

client.OnGuestLeft += info =>
    Console.WriteLine($"{info.Username} est parti ({info.GuestCount} invité(s))");

client.OnError += msg => Console.WriteLine(msg);   // « Nom réservé », « Maximum 2 rooms permanentes atteint »…

client.OnAuthRejected += reason => Console.WriteLine(reason);
client.OnDeprecatedVersion += msg => Console.WriteLine(msg);

client.OnCertificationChanged += certified =>      // true / false, informatif (voir section 8)
    Console.WriteLine($"Application certifiée : {certified}");

client.OnReconnectFailed += () => { /* reconnexion abandonnée : arrête le cast */ };

await client.ConnectAsync();
await client.CreateRoomAsync(
    username:     "Mon Pseudo",
    deviceSerial: GetDeviceSerial()
);
_arcadeShare = client;
```

> ℹ️ `CreateRoomAsync` envoie la demande : la room n'existe vraiment qu'une fois `OnRoomCreated` reçu. Un refus (pseudo réservé, limite de rooms atteinte…) arrive par `OnError`.

> ℹ️ La reconnexion après une coupure réseau est **automatique** (jusqu'à 25 tentatives, délai croissant plafonné à 15 s, soit environ 5 minutes — la durée pendant laquelle le serveur conserve la room de l'hôte) : n'ajoute pas ta propre logique dans `OnDisconnected`. Après une reconnexion, `OnRoomCreated` est déclenché à nouveau pour la même room : affiche ton message « Cast démarré » une seule fois. Si toutes les tentatives échouent, `OnReconnectFailed` est déclenché, puis `OnError`.

### Boucle d'envoi d'état (toutes les 2 secondes)

```csharp
private void StartStateLoop()
{
    _ = Task.Run(async () =>
    {
        while (_arcadeShare != null && _arcadeShare.IsConnected && isCasting)
        {
            try
            {
                await _arcadeShare.SendStateAsync(
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
await _arcadeShare.DisconnectAsync();
_arcadeShare.Dispose();
_arcadeShare = null;
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

> ℹ️ **V4** : si un compte est lié, son jeton est joint automatiquement à la création. Une room peut ainsi porter un pseudo réservé, à condition qu'il appartienne au compte lié.

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

    _arcadeShare = _permanentClients.FirstOrDefault();
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
private ArcadeShareClient _arcadeShare = null;

// Invité = connecté à la room d'un autre hôte
private bool isGuest => _arcadeShare != null && !_arcadeShare.IsHost;

private bool   _guestPausedManually = false;
private string _guestCurrentKey     = "";
private string _guestTempFile       = null;
private bool   _guestIsLoading      = false;
```

> ℹ️ Pendant que `isGuest` est vrai, bloque la lecture locale (lecture d'un morceau, suivant, précédent, cast) : sinon l'utilisateur écoute sa musique en même temps que celle de l'hôte.

### Rejoindre une room

> ⚠️ **`JoinRoomAsync` n'attend pas la réponse du serveur** : il envoie seulement la demande. L'entrée est confirmée par `OnRoomJoined`. Un refus (code inconnu, room pleine, pseudo réservé…) arrive par `OnError`, **avant** toute confirmation. Si tu ne gères pas ce cas, l'application se croit « dans une room » qui n'existe pas, et `isGuest` reste vrai, ce qui bloque la lecture locale. Considère donc l'utilisateur dans la room **uniquement après `OnRoomJoined`**, comme ci-dessous.

```csharp
var client = ArcadeShareConfig.CreateClient();
_arcadeShare = client;   // dès maintenant : isGuest est vrai pendant la connexion

bool awaitingJoin = true;   // vrai tant que le serveur n'a pas confirmé l'entrée (1re jointure ou re-jointure)
var joinResult = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

void LeaveLocally()
{
    if (!ReferenceEquals(_arcadeShare, client)) return;   // déjà quitté
    _arcadeShare = null;
    client.Dispose();   // stoppe aussi la reconnexion automatique
    StopGuestPlayback();
}

client.OnRoomJoined += info =>
{
    awaitingJoin = false;
    joinResult.TrySetResult(null);   // entrée confirmée par le serveur

    Console.WriteLine($"Connecté chez {info.HostName}");   // info.HostCertified : true / false / null (voir section 8)
    if (!string.IsNullOrEmpty(info.HostHandle))
        Console.WriteLine($"Pseudo vérifié de l'hôte : @{info.HostHandle}");   // V4
};

client.OnStateReceived += state =>
    AppliquerEtatDistant(state, roomCode);

client.OnHostLeft += () => LeaveLocally();

client.OnError += msg =>
{
    if (awaitingJoin)
    {
        // Erreur AVANT la confirmation d'entrée : on n'est PAS dans la room.
        if (joinResult.TrySetResult(msg)) return;   // 1re jointure : le catch plus bas affiche le motif

        // Re-jointure après une coupure qui échoue (room supprimée…) : on quitte proprement
        Console.WriteLine(msg);
        LeaveLocally();
        return;
    }

    Console.WriteLine(msg);   // erreur survenue dans la room
};

// Coupure réseau : le client se reconnecte tout seul et rejoint à nouveau la room.
// N'appelle PAS client.Dispose() dans OnDisconnected : cela annulerait la reconnexion.
client.OnReconnecting += attempt =>
{
    awaitingJoin = true;   // la re-jointure doit être confirmée à nouveau
    Console.WriteLine($"Reconnexion… ({attempt})");
};
client.OnReconnectFailed += () => LeaveLocally();   // abandon : quitte la room côté application

try
{
    await client.ConnectAsync();
    await client.JoinRoomAsync(
        code:         "ABC123",
        username:     "Mon Pseudo",
        deviceSerial: GetDeviceSerial()
    );

    // Attendre la confirmation (OnRoomJoined) ou le refus (OnError), 10 secondes maximum
    var answered = await Task.WhenAny(joinResult.Task, Task.Delay(TimeSpan.FromSeconds(10)));
    if (answered != joinResult.Task)
        throw new TimeoutException("Le serveur n'a pas répondu à la demande d'entrée dans la room.");

    string? refusal = await joinResult.Task;
    if (refusal != null)
        throw new InvalidOperationException(refusal);
}
catch (Exception ex)
{
    // Pas dans la room : on nettoie, les boutons se réactivent
    if (ReferenceEquals(_arcadeShare, client)) _arcadeShare = null;
    client.Dispose();

    Console.WriteLine(ex is UnauthorizedAccessException
        ? $"Accès refusé : {ex.Message}"            // application refusée par le serveur
        : $"Impossible de rejoindre la room : {ex.Message}");
}
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
// Quitter : mettre _arcadeShare à null AVANT, pour réactiver les boutons
var leaving = _arcadeShare;
_arcadeShare = null;
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
await _arcadeShare.NotifyMusicEndedAsync();
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

> ℹ️ Depuis la V3, `UploadMusicAsync` retourne `true` seulement si le serveur sert réellement le fichier après l'envoi (vérification automatique) ; sinon `false`, et la raison est donnée par `OnError`.

> ℹ️ Le fichier est aussi uploadé au démarrage du cast si une musique est déjà en cours (dans `OnRoomCreated`), pour que les invités qui se connectent reçoivent la bonne piste sans attendre le prochain `PlayAt`.

---

## 7. Compte Audio_Arcade lié (pseudo vérifié)

*Nouveau en V4.* L'utilisateur peut lier son appareil à son **compte Audio_Arcade**. Son **pseudo réservé** devient alors un pseudo **vérifié** : il apparaît comme tel chez l'hôte et chez les invités, et **personne d'autre ne peut l'utiliser**.

### Principe

| Étape | Qui | Action |
|---|---|---|
| 1 | L'utilisateur | Se connecte avec Discord sur la page [Mon compte](https://audio-arcade.fr.eu.org/compte), réserve son pseudo, puis clique sur **Lier une application** : un code s'affiche (valable **10 minutes**, à usage unique). |
| 2 | Ton application | Envoie ce code au SDK avec `LinkAccountAsync`. Le SDK obtient un **jeton secret** et le remet à ton stockage sécurisé. |
| 3 | Le SDK | Vérifie le jeton **automatiquement** à chaque connexion, avant de créer ou de rejoindre une room. |
| 4 | L'utilisateur | Peut révoquer l'application à tout moment depuis **Applications connectées** sur son compte. À la connexion suivante, le SDK supprime le jeton local (`OnLinkRevoked`). |

**Règle de protection** : un pseudo réservé ne peut être utilisé dans une room que par son propriétaire vérifié. Toute autre personne reçoit l'erreur `Nom réservé`. Les comptes invités du site ne peuvent pas lier d'application (ils n'ont pas de pseudo réservé).

> ℹ️ Le jeton est lié à **ton application** et à **l'appareil** : volé, il est inutilisable ailleurs. Chaque appareil et chaque application a sa propre liaison, visible et révocable séparément (par exemple « Windows » et « Android » pour la même application).

### 1. Fournir un stockage sécurisé du jeton

Le SDK ne sait pas où ranger un secret : c'est ton application qui le fournit, via l'interface `ILinkTokenStore`. Le jeton ne doit **jamais** être stocké en clair. Sous Windows, DPAPI le chiffre pour le compte Windows de l'utilisateur (sur .NET 5 et plus, ajoute le package NuGet `System.Security.Cryptography.ProtectedData`) :

```csharp
using System.Security.Cryptography;
using System.Text;

public sealed class DpapiLinkTokenStore : ILinkTokenStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonApp", "link.dat");

    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("MonApp.ArcadeShare.link");

    public string? Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            byte[] raw = ProtectedData.Unprotect(File.ReadAllBytes(FilePath), Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(raw);
        }
        catch { return null; }   // fichier illisible : comme si aucune liaison n'existait
    }

    public void Save(string token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        byte[] enc = ProtectedData.Protect(Encoding.UTF8.GetBytes(token), Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(FilePath, enc);
    }

    public void Clear()
    {
        try { File.Delete(FilePath); } catch { }
    }
}
```

Passe ensuite le stockage au constructeur :

```csharp
internal static class ArcadeShareConfig
{
    public const string AppId = "TON_APP_ID";
    public static readonly ILinkTokenStore LinkStore = new DpapiLinkTokenStore();

    public static ArcadeShareClient CreateClient() => new ArcadeShareClient(AppId, LinkStore);
}
```

### 2. Lier l'appareil

À appeler depuis tes paramètres, avec le code saisi par l'utilisateur :

```csharp
using var client = ArcadeShareConfig.CreateClient();
await client.ConnectAsync();

var (ok, handle, error) = await client.LinkAccountAsync(codeSaisi, GetDeviceSerial());

if (ok)
    Console.WriteLine(handle != null
        ? $"Compte lié : @{handle}"
        : "Compte lié (aucun pseudo réservé pour l'instant)");
else
    Console.WriteLine(error ?? "Liaison impossible");   // « Code invalide ou expiré », …
```

### 3. Utiliser le pseudo vérifié

Une fois le compte lié, **la vérification est automatique** : `CreateRoomAsync`, `RejoinRoomAsync` et `JoinRoomAsync` vérifient le jeton avant d'envoyer leur message (une seule fois par connexion, y compris après une reconnexion automatique). Un serveur injoignable ou muet n'empêche jamais de continuer.

```csharp
await client.ConnectAsync();
await client.JoinRoomAsync(code, pseudo, GetDeviceSerial());   // vérifie le jeton avant d'entrer

if (client.IsLinked)
    Console.WriteLine($"Pseudo vérifié : @{client.LinkedHandle}");   // LinkedHandle peut être null
```

Pour afficher l'état de la liaison dans tes paramètres, sans entrer dans une room :

```csharp
using var client = ArcadeShareConfig.CreateClient();
bool revoked = false;
client.OnLinkRevoked += () => revoked = true;

await client.ConnectAsync();
bool linked = await client.VerifyLinkAsync(GetDeviceSerial());

// linked  == true : client.LinkedHandle (pseudo vérifié) et client.LinkedName (nom du compte)
// revoked == true : l'utilisateur a révoqué l'application, le jeton local est supprimé
// sinon           : aucun jeton, ou service indisponible (la liaison enregistrée reste valable)
```

### 4. Imposer le pseudo dans ton interface

**Recommandation** : quand un compte est lié et possède un pseudo réservé, utilise ce pseudo à la place de la saisie libre, et verrouille le champ. Mémorise localement le dernier pseudo vérifié (`client.LinkedHandle`, `client.LinkedName`) pour l'afficher sans attendre une connexion. Le serveur refuse de toute façon un pseudo réservé à qui n'en est pas propriétaire : le verrouillage n'est qu'un confort, la protection est côté serveur.

> ℹ️ La comparaison des pseudos ignore les majuscules, les accents et les séparateurs : `La_TNT`, `la-tnt` et `latnt` désignent le même pseudo.

### Plateforme déclarée

Chaque liaison indique sa plateforme, affichée dans **Applications connectées** (« Windows », « Android »…). Le SDK la détecte automatiquement (Windows, macOS, Linux). Sur une autre plateforme, définis-la toi-même avant de lier :

```csharp
client.Platform = "Windows";
```

### Récapitulatif

| Élément | Rôle |
|---|---|
| `ILinkTokenStore` | À implémenter : stockage chiffré du jeton (`Load`, `Save`, `Clear`) |
| `new ArcadeShareClient(appId, linkStore)` | Active la liaison (le deuxième argument est facultatif) |
| `LinkAccountAsync(code, deviceSerial)` | Lie l'appareil avec le code saisi par l'utilisateur |
| `VerifyLinkAsync(deviceSerial)` | Vérifie le jeton enregistré (aussi fait automatiquement) |
| `Unlink()` | Oublie la liaison sur cet appareil (pour la révoquer côté serveur : « Applications connectées » du compte) |
| `IsLinked` · `LinkedHandle` · `LinkedName` · `HasStoredLink` | État de la liaison |
| `OnLinkRevoked` | Liaison révoquée par l'utilisateur : le jeton local a été supprimé |
| `RoomJoinedResponse.HostHandle` | Pseudo vérifié de l'hôte, vu par l'invité |
| `GuestChangedResponse.VerifiedHandle` | Pseudo vérifié d'un invité, vu par l'hôte |

---

## 8. Certification de l'application

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

## 9. Référence des méthodes

| Méthode | Description | Accès |
|---|---|---|
| `new ArcadeShareClient(appId)` | Crée un client (appId obligatoire) | Tous |
| `new ArcadeShareClient(appId, linkStore)` | Crée un client avec compte lié (`ILinkTokenStore` facultatif) — **V4** | Tous |
| `ConnectAsync()` | Connecte le WebSocket et authentifie l'application | Tous |
| `CreateRoomAsync(username, deviceSerial)` | Crée une room via le WebSocket | Hôte |
| `CreatePermanentRoomAsync(username, deviceSerial)` | Crée une room permanente (max 2 par appareil). Retourne `(bool Ok, string? Code, string? Error)` | Hôte |
| `GetMyRoomsAsync(deviceSerial)` | Liste les rooms permanentes de l'appareil | Hôte |
| `RejoinRoomAsync(code, username, deviceSerial)` | Réactive une room permanente | Hôte |
| `DeletePermanentRoomAsync(code, deviceSerial)` | Supprime définitivement une room permanente. Retourne `bool` | Hôte |
| `SendStateAsync(position, isPlaying, title, artist, duration, ...)` | Envoie l'état de lecture | Hôte |
| `UploadMusicAsync(filePath)` | Upload le fichier audio (envoi en flux). Retourne `bool` | Hôte |
| `NotifyMusicEndedAsync()` | Signale la fin de piste | Hôte |
| `JoinRoomAsync(code, username, deviceSerial)` | Envoie la demande d'entrée en tant qu'invité (confirmée par `OnRoomJoined`) | Invité |
| `DownloadRoomMusicAsync(roomCode, destDir, ct, currentFile)` | Télécharge la musique courante de la room (retourne le chemin, ou `null`). `currentFile` : nom du fichier courant (`StateUpdate.CurrentFile`), optionnel | Invité |
| `GetStreamUrl(roomCode, currentFile)` | URL authentifiée de la musique de la room (lecture en streaming) | Invité |
| `LinkAccountAsync(code, deviceSerial)` | Lie l'appareil à un compte Audio_Arcade avec le code saisi. Retourne `(bool Ok, string? Handle, string? Error)` — **V4** | Tous |
| `VerifyLinkAsync(deviceSerial)` | Vérifie le jeton de liaison enregistré. Retourne `bool` — **V4** | Tous |
| `Unlink()` | Oublie la liaison sur cet appareil — **V4** | Tous |
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
| `IsLinked` | `bool` | Un compte lié a été vérifié sur la connexion en cours — **V4** |
| `LinkedHandle` | `string?` | Pseudo réservé vérifié du compte lié (`null` = aucun) — **V4** |
| `LinkedName` | `string?` | Nom du compte lié, vérifié sur la connexion en cours — **V4** |
| `HasStoredLink` | `bool` | Un jeton de liaison est enregistré sur l'appareil (pas forcément encore vérifié) — **V4** |
| `Platform` | `string` | Plateforme déclarée au serveur (détectée automatiquement, modifiable) — **V4** |
| `ArcadeShareClient.SDK_VERSION` | `string` | Version du SDK (constante) |

---

## 10. Référence des événements

| Événement | Paramètre | Déclenché quand |
|---|---|---|
| `OnConnected` | — | Connexion établie et application authentifiée |
| `OnAuthRejected` | `string reason` | Le serveur refuse l'application (appId inconnu ou désactivé, version inconnue, révocation en cours de session) |
| `OnDeprecatedVersion` | `string message` | Version du SDK dépréciée (la connexion fonctionne, mise à jour conseillée) |
| `OnRoomCreated` | `string code` | Room créée ou reprise (aussi après une reconnexion) |
| `OnRoomJoined` | `RoomJoinedResponse` | Invité connecté à la room — **c'est la confirmation d'entrée** (aussi après une reconnexion) |
| `OnStateReceived` | `StateUpdate` | Nouvel état reçu de l'hôte |
| `OnGuestJoined` | `GuestChangedResponse` | Un invité rejoint |
| `OnGuestLeft` | `GuestChangedResponse` | Un invité quitte |
| `OnHostLeft` | — | L'hôte a quitté ou est déconnecté |
| `OnError` | `string message` | Erreur (room introuvable, room pleine, nom réservé, reconnexion impossible, fichier refusé à l'envoi...). Avant la confirmation d'entrée, elle signifie que l'entrée est **refusée** |
| `OnDisconnected` | — | Connexion perdue (la reconnexion automatique démarre) |
| `OnReconnecting` | `int attempt` | À chaque tentative de reconnexion automatique |
| `OnReconnected` | — | Reconnexion réussie, room restaurée |
| `OnReconnectFailed` | — | Reconnexion automatique abandonnée (juste avant `OnError`) |
| `OnCertificationChanged` | `bool? certified` | Certification de ton application connue (à chaque authentification réussie) |
| `OnHostCertification` | `bool? certified` | (Invité) Certification de l'application de l'hôte, juste avant `OnRoomJoined` |
| `OnLinkRevoked` | — | Le compte a révoqué l'application : le jeton local est supprimé — **V4** |

### Modèle `RoomJoinedResponse`

| Champ | Type | Description |
|---|---|---|
| `Code` | `string` | Code de la room |
| `HostName` | `string` | Pseudo de l'hôte |
| `GuestCount` | `int` | Nombre d'invités connectés |
| `HostCertified` | `bool?` | Application de l'hôte certifiée (`true`), non certifiée (`false`) ou inconnue (`null`) |
| `HostHandle` | `string?` | Pseudo vérifié de l'hôte (compte lié), ou `null` — **V4** |
| `LastState` | `StateUpdate?` | Dernier état connu de la lecture |

### Modèle `GuestChangedResponse`

| Champ | Type | Description |
|---|---|---|
| `Username` | `string` | Pseudo de l'invité |
| `GuestCount` | `int` | Nombre d'invités connectés |
| `VerifiedHandle` | `string?` | Pseudo vérifié de l'invité (compte lié), ou `null` — **V4** |

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

## 11. Migration depuis la V3 et historique

### Migrer de la V3 vers la V4

**Rien ne casse** : une application compilée contre la V3 continue de fonctionner, et `new ArcadeShareClient(appId)` reste valide. Pour profiter des nouveautés :

1. Mets à jour le package : `dotnet add package ArcadeShare --version 4.0.0` (ou remplace la DLL).
2. Pour les comptes liés et les pseudos vérifiés : implémente `ILinkTokenStore` et passe-le au constructeur (section 7).
3. Si ce n'est pas déjà fait, gère la **confirmation d'entrée** à `JoinRoomAsync` avec `OnRoomJoined` / `OnError` (section 5) : un code de room invalide ne doit pas laisser l'application « dans une room ».
4. Vérifie que ton projet ne référence **qu'une seule** version du SDK (NuGet ou DLL, pas les deux).

> ℹ️ Les anciennes versions restent acceptées par le serveur tant qu'elles ne sont pas retirées. Une version dépréciée fonctionne, avec un avertissement (`OnDeprecatedVersion`).

### Historique

| Version | Nouveautés |
|---|---|
| **4.0.0** | Compte Audio_Arcade lié et pseudo vérifié (`ILinkTokenStore`, `LinkAccountAsync`, `VerifyLinkAsync`, `Unlink`, `OnLinkRevoked`). Protection des pseudos réservés. Plateforme déclarée (`Platform`). `HostHandle` et `VerifiedHandle`. Événement `OnReconnectFailed`. |
| **3.x** | Exposition de la certification (`IsCertified`, `OnCertificationChanged`, `FetchCertificationStatusAsync`, `HostCertified`). Vérification de l'envoi de la musique. Reconnexion automatique. |

---

<sub>ArcadeShare SDK · Windows · Audio_Arcade — La_TNT<br>Copyright © 2024–2026 Audio_Arcade. Tous droits réservés.</sub>
