namespace AyuSync.Agent.Core;

public sealed class SyncPreferences
{
    public string SyncServerURL { get; set; }
    public string SyncServerToken { get; set; }
    public bool SyncEnabled { get; set; }
    public bool UseSecureConnection { get; set; }
}
