using Newtonsoft.Json;

namespace ArcadeShareServer.Models
{
    public class CreateRoomMessage
    {
        [JsonProperty("type")] public string Type { get; } = "create_room";
        [JsonProperty("username")] public string Username { get; set; } = "";
        [JsonProperty("deviceSerial")] public string DeviceSerial { get; set; } = "";
    }

    public class JoinRoomMessage
    {
        [JsonProperty("type")] public string Type { get; } = "join_room";
        [JsonProperty("code")] public string Code { get; set; } = "";
        [JsonProperty("username")] public string Username { get; set; } = "";
        [JsonProperty("deviceSerial")] public string DeviceSerial { get; set; } = "";
    }

    public class RejoinRoomMessage
    {
        [JsonProperty("type")] public string Type { get; } = "rejoin_room";
        [JsonProperty("code")] public string Code { get; set; } = "";
        [JsonProperty("username")] public string Username { get; set; } = "";
        [JsonProperty("deviceSerial")] public string DeviceSerial { get; set; } = "";
    }

    public class StateMessage
    {
        [JsonProperty("type")] public string Type { get; } = "state";
        [JsonProperty("code")] public string Code { get; set; } = "";
        [JsonProperty("position")] public double Position { get; set; }
        [JsonProperty("isPlaying")] public bool IsPlaying { get; set; }
        [JsonProperty("title")] public string Title { get; set; } = "";
        [JsonProperty("artist")] public string Artist { get; set; } = "";
        [JsonProperty("artworkUrl")] public string? ArtworkUrl { get; set; }
        [JsonProperty("duration")] public double Duration { get; set; }
        [JsonProperty("nextTitle")] public string? NextTitle { get; set; }
    }

    public class LeaveRoomMessage
    {
        [JsonProperty("type")] public string Type { get; } = "leave_room";
        [JsonProperty("code")] public string Code { get; set; } = "";
    }

    public class DeleteRoomMessage
    {
        [JsonProperty("type")] public string Type { get; } = "delete_room";
    }

    public class MusicEndedMessage
    {
        [JsonProperty("type")] public string Type { get; } = "music_ended";
    }

    public class PingMessage
    {
        [JsonProperty("type")] public string Type { get; } = "ping";
    }

    /// <summary>Handshake d'authentification envoyé juste après la connexion WebSocket.</summary>
    public class AuthMessage
    {
        [JsonProperty("type")] public string Type { get; } = "auth";
        [JsonProperty("appId")] public string AppId { get; set; } = "";
        [JsonProperty("versionId")] public string VersionId { get; set; } = "";
    }

    // ─── SERVEUR → CLIENT ───────────────────────────────────────────────────────

    public class RoomCreatedResponse
    {
        [JsonProperty("type")] public string Type { get; set; } = "";
        [JsonProperty("code")] public string Code { get; set; } = "";
        [JsonProperty("guestCount")] public int GuestCount { get; set; }
    }

    public class RoomJoinedResponse
    {
        [JsonProperty("type")] public string Type { get; set; } = "";
        [JsonProperty("code")] public string Code { get; set; } = "";
        [JsonProperty("hostCertified")] public bool? HostCertified { get; set; }
        [JsonProperty("guestCount")] public int GuestCount { get; set; }
        [JsonProperty("hostName")] public string HostName { get; set; } = "";
        [JsonProperty("lastState")] public StateUpdate? LastState { get; set; }
    }

    public class StateUpdate
    {
        [JsonProperty("type")] public string Type { get; set; } = "";
        [JsonProperty("position")] public double Position { get; set; }
        [JsonProperty("isPlaying")] public bool IsPlaying { get; set; }
        [JsonProperty("title")] public string Title { get; set; } = "";
        [JsonProperty("artist")] public string Artist { get; set; } = "";
        [JsonProperty("artworkUrl")] public string? ArtworkUrl { get; set; }
        [JsonProperty("duration")] public double Duration { get; set; }
        [JsonProperty("nextTitle")] public string? NextTitle { get; set; }
        [JsonProperty("timestamp")] public long Timestamp { get; set; }
        [JsonProperty("currentFile")] public string CurrentFile { get; set; } = "";
    }

    public class GuestChangedResponse
    {
        [JsonProperty("type")] public string Type { get; set; } = "";
        [JsonProperty("guestCount")] public int GuestCount { get; set; }
        [JsonProperty("username")] public string Username { get; set; } = "";
    }

    public class ErrorResponse
    {
        [JsonProperty("type")] public string Type { get; set; } = "";
        [JsonProperty("message")] public string Message { get; set; } = "";
    }

    /// <summary>Réponse au handshake d'authentification.</summary>
    public class AuthResponse
    {
        [JsonProperty("type")] public string Type { get; set; } = "";
        [JsonProperty("authorized")] public bool Authorized { get; set; }
        [JsonProperty("certified")] public bool Certified { get; set; }
        [JsonProperty("deprecated")] public bool Deprecated { get; set; }
        [JsonProperty("reason")] public string? Reason { get; set; }
    }

    public class SimpleResponse
    {
        [JsonProperty("type")] public string Type { get; set; } = "";
    }
}