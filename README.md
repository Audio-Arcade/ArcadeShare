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

---

## ⚠️ Accès bêta — À lire avant d'intégrer

> Si un accès bêta est actif, ne l'intégrez pas dans une version stable de votre application. Les versions bêta font l'objet de correctifs réguliers afin d'atteindre le niveau de qualité requis par le SDK. Intégrer une bêta en production peut provoquer des bugs imprévus pour vos utilisateurs. Attendez toujours une version stable avant tout déploiement en production.

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
8. [Référence des méthodes](#7-référence-des-méthodes)
9. [Référence des événements](#8-référence-des-événements)

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

> ⚠️ Ne modifie pas la valeur de `versionId` dans le code du SDK : le serveur connaît la correspondance réelle entre cet identifiant et la version publiée, et refuse une version dépréciée ou inconnue.

---

## 1. Installation

Télécharge d'abord le fichier `ArcadeShareServer.dll` :

**[⬇ Télécharger ArcadeShareServer.dll](https://github.com/Audio-Arcade/Audio_Arcade_SDK/releases/download/V2/ArcadeShareServer.dll)**

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

await client.ConnectAsync();
await client.CreateRoomAsync(
    username:     "Mon Pseudo",
    deviceSerial: GetDeviceSerial()
);
_arcadeShareServer = client;
```

> ℹ️ La reconnexion après une coupure réseau est **automatique** (8 tentatives, délai croissant) : n'ajoute pas ta propre logique dans `OnDisconnected`. Après une reconnexion, `OnRoomCreated` est déclenché à nouveau pour la même room : affiche ton message « Cast démarré » une seule fois.

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
private string _guestCurrentTitle   = "";
private string _guestTempFile       = null;
private bool   _guestIsLoading      = false;
```

> ℹ️ Pendant que `isGuest` est vrai, bloque la lecture locale (lecture d'un morceau, suivant, précédent, cast) : sinon l'utilisateur écoute sa musique en même temps que celle de l'hôte.

### Rejoindre une room

```csharp
var client = ArcadeShareConfig.CreateClient();
_arcadeShareServer = client;   // dès maintenant : isGuest est vrai pendant la connexion

client.OnRoomJoined += info =>
    Console.WriteLine($"Connecté chez {info.HostName}");

client.OnStateReceived += state =>
    AppliquerEtatDistant(state, roomCode);

client.OnHostLeft += () =>
{
    _arcadeShareServer = null;
    client.Dispose();
    StopGuestPlayback();
};

client.OnError += msg => Console.WriteLine(msg);   // room introuvable, room pleine...

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

    // Nouvelle piste détectée → télécharger
    bool fileChanged = !string.IsNullOrEmpty(state.CurrentFile)
                    && state.CurrentFile != _guestCurrentTitle;

    if (fileChanged && !_guestIsLoading)
    {
        _guestPausedManually = false;
        _guestIsLoading      = true;
        string fileToLoad    = state.CurrentFile;

        _ = Task.Run(async () =>
        {
            try
            {
                bool ok = await LoadGuestMusicAsync(roomCode, state.Position, state.IsPlaying);
                if (ok) _guestCurrentTitle = fileToLoad;   // sinon : nouvel essai au prochain état (2 s)
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

> ⚠️ Mémorise le fichier courant (`_guestCurrentTitle`) **seulement après un téléchargement réussi**. Sinon, un seul échec réseau empêcherait définitivement de réessayer pour cette piste.

### Téléchargement et lecture de la musique

```csharp
private async Task<bool> LoadGuestMusicAsync(string roomCode, double startPos, bool shouldPlay)
{
    // Le SDK gère le téléchargement, l'extension et le nommage du fichier temporaire
    using var client = ArcadeShareConfig.CreateClient();
    string? tempPath = await client.DownloadRoomMusicAsync(roomCode, Path.GetTempPath());
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
    _guestCurrentTitle = "";
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

> ℹ️ Le fichier est aussi uploadé au démarrage du cast si une musique est déjà en cours (dans `OnRoomCreated`), pour que les invités qui se connectent reçoivent la bonne piste sans attendre le prochain `PlayAt`.

---

## 7. Référence des méthodes

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
| `DownloadRoomMusicAsync(roomCode, destDir)` | Télécharge la musique courante de la room (retourne le chemin, ou `null`) | Invité |
| `LeaveRoomAsync()` | Quitte la room (room conservée) | Tous |
| `DisconnectAsync()` | Déconnexion propre (désactive la reconnexion automatique) | Tous |
| `Dispose()` | Libère les ressources | Tous |

### Propriétés publiques

| Propriété | Type | Description |
|---|---|---|
| `IsConnected` | `bool` | Connexion WebSocket active |
| `CurrentRoomCode` | `string?` | Code de la room en cours |
| `IsHost` | `bool` | Vrai si le client est hôte (devient vrai après `CreateRoomAsync` ou `RejoinRoomAsync`) |

---

## 8. Référence des événements

| Événement | Paramètre | Déclenché quand |
|---|---|---|
| `OnConnected` | — | Connexion établie et application authentifiée |
| `OnAuthRejected` | `string reason` | Le serveur refuse l'application (appId inconnu ou désactivé, version inconnue) |
| `OnDeprecatedVersion` | `string message` | Version du SDK dépréciée (la connexion fonctionne, mise à jour conseillée) |
| `OnRoomCreated` | `string code` | Room créée ou reprise (aussi après une reconnexion) |
| `OnRoomJoined` | `RoomJoinedResponse` | Invité connecté à la room |
| `OnStateReceived` | `StateUpdate` | Nouvel état reçu de l'hôte |
| `OnGuestJoined` | `GuestChangedResponse` | Un invité rejoint |
| `OnGuestLeft` | `GuestChangedResponse` | Un invité quitte |
| `OnHostLeft` | — | L'hôte a quitté ou est déconnecté |
| `OnError` | `string message` | Erreur (room pleine, code invalide, reconnexion impossible...) |
| `OnDisconnected` | — | Connexion perdue (la reconnexion automatique démarre) |
| `OnReconnecting` | `int attempt` | À chaque tentative de reconnexion automatique |
| `OnReconnected` | — | Reconnexion réussie, room restaurée |

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
