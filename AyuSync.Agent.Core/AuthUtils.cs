#region

using DeviceId;

#endregion

namespace AyuSync.Agent.Core;

public static class AuthUtils
{
    static AuthUtils()
    {
        DeviceIdentifier = new DeviceIdBuilder()
                           .AddMachineName()
                           .OnWindows(x => x.AddMachineGuid())
                           .OnLinux(x => x.AddMotherboardSerialNumber().AddCpuInfo())
                           .ToString();
    }

    public static string DeviceName => Environment.MachineName;
    public static string DeviceIdentifier { get; }
}
