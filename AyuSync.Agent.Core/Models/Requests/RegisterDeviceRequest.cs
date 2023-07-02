namespace AyuSync.Agent.Core.Models.Requests;

public sealed class RegisterDeviceRequest
{
    public string Name { get; set; }
    public string Identifier { get; set; }
}
