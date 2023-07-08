#region

using System.IO.Pipes;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reflection;
using AyuSync.Agent.Core.Models.Entities;
using AyuSync.Agent.Core.Models.Requests;
using Serilog;
using Websocket.Client;

#endregion

namespace AyuSync.Agent.Core;

public sealed class SyncService : IDisposable
{
    private readonly SyncPreferences _preferences;
    private readonly HttpClient _httpClient;
    private PipeServerWrapper _server;
    private WebsocketClient _websocket;

    private bool _usable;

    private readonly CancellationTokenSource _cts;
    private readonly CancellationToken _ct;

    public SyncService(SyncPreferences preferences)
    {
        _preferences = preferences;

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("X-APP-PACKAGE", Assembly.GetExecutingAssembly().FullName);
        _httpClient.DefaultRequestHeaders.Add("Authorization", preferences.SyncServerToken);
        _httpClient.BaseAddress = new Uri(preferences.SyncServerURL);

        _usable = true;

        _cts = new CancellationTokenSource();
        _ct = _cts.Token;
    }

    public async Task StartAsync()
    {
        if (!_usable)
        {
            throw new Exception("Don't reuse this instance");
        }

        _usable = false;

        Log.Information("Starting sync service");

        if (string.IsNullOrWhiteSpace(_preferences.SyncServerToken))
        {
            Log.Error("Token is not set");
            return;
        }

        var self = await GetSelfForConnect();

        if (self == null)
        {
            Log.Error("Token is invalid");
            return;
        }

        if (self.MVPUntil == null)
        {
            Log.Error("MVP is not active");
            return;
        }

        Log.Information("Subscription until {Until}", self.MVPUntil);

        var registerResult = await RegisterDevice();

        if (!registerResult)
        {
            Log.Error("Failed to register device");
            return;
        }

        _server = new PipeServerWrapper("AyuSync", PipeDirection.Out);
        await _server.Create(_ct);

        Log.Debug("Created named pipe");

        _websocket = CreateWebSocketClient();
        _websocket.ReconnectionHappened.Subscribe(x =>
        {
            _websocket.MessageReceived.Select(msg => Observable.FromAsync(async () =>
            {
                Log.Debug("Received message {Message}", msg.Text);
                while (!_ct.IsCancellationRequested)
                {
                    var res = await _server.Wrapper.SendAsync(msg.Text, _ct);
                    if (res == true)
                    {
                        break;
                    }

                    if (res == null)
                    {
                        await _server.Create(_ct);
                    }
                }
            })).Concat().Subscribe();
        });

        await _websocket.Start();
    }

    public async Task StopAsync()
    {
        Log.Information("Stopping sync service");

        _cts.Cancel();
        await _websocket.Stop(WebSocketCloseStatus.Empty, "");

        // Give some time to stop
        // ReSharper disable once MethodSupportsCancellation
        await Task.Delay(500);
    }

    private async Task<AyuUser?> GetSelfForConnect()
    {
        var response = await _httpClient.GetAsync("/v1/ayu/info", _ct);
        if (!response.IsSuccessStatusCode)
        {
            Log.Error("Failed to get self for connect, status {StatusCode}", response.StatusCode);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AyuUser>(cancellationToken: _ct);
    }

    private async Task<bool> RegisterDevice()
    {
        var req = new RegisterDeviceRequest
        {
            Name = AuthUtils.DeviceName,
            Identifier = AuthUtils.DeviceIdentifier
        };

        var response = await _httpClient.PostAsJsonAsync("/v1/sync/register", req, _ct);
        if (!response.IsSuccessStatusCode)
        {
            Log.Error("Failed to register device, status {StatusCode}", response.StatusCode);
            return false;
        }

        return true;
    }

    private WebsocketClient CreateWebSocketClient()
    {
        // interesting moment
        // if it's https, then after replacement it will be wss :)
        var newUrl = new Uri(_httpClient.BaseAddress!.AbsoluteUri.Replace("http", "ws"));
        var url = new Uri(newUrl, "/v1/sync/ws");

        var factory = new Func<ClientWebSocket>(() =>
        {
            var webSocket = new ClientWebSocket
            {
                Options =
                {
                    KeepAliveInterval = TimeSpan.FromSeconds(5)
                }
            };
            webSocket.Options.SetRequestHeader("Authorization", _preferences.SyncServerToken);
            webSocket.Options.SetRequestHeader("X-DEVICE-IDENTIFIER", AuthUtils.DeviceIdentifier);

            return webSocket;
        });

        var client = new WebsocketClient(url, factory);
        client.ReconnectTimeout = TimeSpan.FromSeconds(60);
        client.ErrorReconnectTimeout = TimeSpan.FromSeconds(5);

        return client;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _cts.Cancel();
        _cts.Dispose();
    }
}
