namespace AyuSync.Agent.Core.Models.Requests;

public sealed class ForceSyncRequest
{
    public long UserId { get; set; }
    public long FromDate { get; set; } // in UTC
}
