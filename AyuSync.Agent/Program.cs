#region

using System.Runtime.InteropServices;
using System.Text.Json;
using AyuSync.Agent.Core;
using Serilog;

#endregion

Log.Logger = new LoggerConfiguration()
             .MinimumLevel.Debug()
             .Enrich.FromLogContext()
             .WriteTo.Console()
             .CreateLogger();

Log.Information("Starting AyuSync Agent, running on {OS}", RuntimeInformation.OSDescription);

if (args.Length != 1)
{
    Log.Error("Invalid arguments, need settings path");
    return;
}

var server = new PipeServerWrapper("AyuSyncAgent");
await server.Create();

SyncService? syncService = null;

async Task CreateSyncService()
{
    var settingsPath = args[0];
    await using var f = File.OpenRead(settingsPath);

    SyncPreferences? preferences;
    try
    {
        preferences = await JsonSerializer.DeserializeAsync<SyncPreferences>(f);
    }
    catch (Exception e)
    {
        Log.Error(e, "Error while deserializing settings");
        Environment.Exit(1);
        return;
    }

    if (preferences == null)
    {
        Log.Error("Settings is null");
        Environment.Exit(1);
        return;
    }

    syncService = new SyncService(preferences);
    await syncService.StartAsync();
}

await CreateSyncService();

Log.Information("AyuSync Agent started");

while (true)
{
    try
    {
        var received = await server.Wrapper.ReceiveAsync<string>();

        if (string.IsNullOrWhiteSpace(received))
        {
            Log.Warning("Received empty data");
            continue;
        }

        Log.Debug("Received {Data}", received);

        switch (received)
        {
            case "reload":
                if (syncService != null)
                {
                    await syncService.StopAsync();
                }

                await CreateSyncService();

                break;
            case "stop":
                Environment.Exit(0);

                break;
            default:
                Log.Warning("Unknown command {Command}", received);

                break;
        }
    }
    catch (Exception e)
    {
        Log.Error(e, "Error while receiving or executing command");
    }

    await Task.Delay(500);
}
