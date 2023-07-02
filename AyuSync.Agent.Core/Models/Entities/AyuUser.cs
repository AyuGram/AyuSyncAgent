namespace AyuSync.Agent.Core.Models.Entities;

public sealed class AyuUser
{
    public Guid Id { get; set; }

    public string AccessToken { get; set; }

    public DateTime? MVPUntil { get; set; }
    public ICollection<AyuDevice> Devices { get; set; }
}
