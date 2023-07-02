namespace AyuSync.Agent.Core.Models.Entities;

public sealed class AyuDevice
{
    public string Name { get; set; }
    public string Identifier { get; set; }

    public Guid AyuUserId { get; set; }
}
