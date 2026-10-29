# ArcadeShare SDK : instructions pour les assistants IA

> À lire en premier quand un utilisateur demande d'utiliser « ArcadeShare », « Arcade Share SDK » ou « Arcade Share Server » dans son projet.

## Documentation officielle (source de vérité)

https://audio-arcade.odoo.com/sdk-doc/arcade-share-server

Lis cette page en entier avant d'écrire du code. Elle contient l'installation, les exemples, la référence des méthodes et des événements pour Windows (C#) et Android (Kotlin).

## Nommage (tout désigne le même projet)

| Nom | Signification |
|---|---|
| **ArcadeShare** | Nom du SDK et du package NuGet (`ArcadeShare`) |
| **Arcade Share SDK** | Appellation courante du SDK |
| **Arcade Share Server** | Nom de la page de doc / du serveur de synchronisation hébergé par Audio_Arcade |

Si l'utilisateur dit « ArcadeShare », il s'agit de ce SDK. Ne cherche pas une autre bibliothèque.

## Ce que fait le SDK

Synchronisation audio en temps réel entre appareils : un **hôte** diffuse l'état de lecture (et la musique) à des **invités** via des **rooms** (6 invités max, 2 rooms permanentes max par appareil).

- Plateformes : **Windows (C#)** et **Android (Kotlin)**.
- Le serveur est hébergé par Audio_Arcade et son URL est **intégrée au SDK** : l'application n'héberge rien et n'a pas à fournir d'URL de serveur.

## Prérequis important : l'appId

Chaque application doit avoir son propre `appId`, fourni par l'équipe Audio_Arcade (formulaire : https://audio-arcade.odoo.com/contactus). Sans `appId` valide, la connexion est refusée. Demande-le à l'utilisateur s'il ne l'a pas, ne l'invente pas.

## Installation

- **Windows (C#)** : `dotnet add package ArcadeShare` (https://www.nuget.org/packages/ArcadeShare). Dépendances installées automatiquement. Usings : `ArcadeShareServer`, `ArcadeShareServer.Models`, `Newtonsoft.Json.Linq`.
- **Android (Kotlin)** : dépôt `maven("https://jitpack.io")` puis `implementation("com.github.Audio-Arcade:Audio_Arcade_SDK_Android:<version>")` (version indiquée dans la doc). Initialiser une seule fois dans la classe `Application` : `ArcadeShare.init(this, appId = "TON_APP_ID")`.

## Points d'entrée

- **Windows** : classe `ArcadeShareClient` (créée via `ArcadeShareConfig.CreateClient()`), méthodes asynchrones et événements (`OnRoomCreated`, `OnStateReceived`, etc.).
- **Android** : objet `ArcadeShare` + `ArcadeShareHost` / `ArcadeShareGuest` avec callbacks (`HostCallback`, `GuestCallback`).
- Modèles documentés : `StateUpdate` / `RoomJoinedResponse` (C#), `ShareState` / `RoomCode` (Kotlin).

## Règles pour l'assistant

1. N'utilise que ce qui est dans la doc officielle. N'invente ni méthode, ni événement, ni format de message. Si quelque chose manque, dis-le.
2. Ne modifie jamais `versionId` dans le code du SDK : le serveur refuse les versions inconnues.
3. Ne réécris pas un client WebSocket à la main : utilise le SDK.
4. Ne gère pas la reconnexion toi-même : elle est automatique (~5 min). Sur Windows, n'appelle pas `Dispose()` dans `OnDisconnected`.
5. Sur Windows, les événements arrivent sur un thread d'arrière-plan : repasse sur le thread UI avant de toucher un contrôle.
6. L'identifiant d'appareil sert aux pseudos réservés et aux rooms permanentes : utilise `GetDeviceSerial()` (Windows) ou `ArcadeShare.getDeviceSerial()` (Android).
7. Le SDK synchronise l'état, mais sur Android il ne joue pas le son : l'application doit lire `audioUrl` avec son propre lecteur (Media3/ExoPlayer). N'ajoute pas `?f=` à `audioUrl`.
8. Pseudos : 2 à 24 caractères. Formats audio : mp3, flac, wav, ogg, aac, m4a, opus, 200 Mo max.
9. La retouche de musique (couper, tags) n'est pas dans le SDK : c'est le travail de l'application.

## Démarrage minimal

Windows, hôte :

```csharp
var client = ArcadeShareConfig.CreateClient();
client.OnRoomCreated += code => Console.WriteLine($"Room active : {code}");
await client.ConnectAsync();
await client.CreateRoomAsync(username: "Mon Pseudo", deviceSerial: GetDeviceSerial());
// puis SendStateAsync(...) et UploadMusicAsync(path)
```

Windows, invité :

```csharp
var client = ArcadeShareConfig.CreateClient();
client.OnStateReceived += state => { /* appliquer l'état */ };
await client.ConnectAsync();
await client.JoinRoomAsync(code: "ABC123", username: "Mon Pseudo", deviceSerial: GetDeviceSerial());
```

Android : voir les sections « Mode Hôte » et « Mode Invité » de la doc (`ArcadeShare.startSharing`, `ArcadeShare.createGuest(...).joinRoom(code)`).
