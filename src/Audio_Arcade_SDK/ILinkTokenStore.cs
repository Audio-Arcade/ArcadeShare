namespace ArcadeShare
{
    /// <summary>
    /// Stockage sécurisé du jeton de liaison, à fournir par l'application
    /// (DPAPI sous Windows, PasswordVault sous UWP/Xbox, Keystore sous Android…).
    /// Le jeton ne doit jamais être stocké en clair.
    /// </summary>
    public interface ILinkTokenStore
    {
        string? Load();
        void Save(string token);
        void Clear();
    }
}
