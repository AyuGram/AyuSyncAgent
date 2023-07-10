#region

using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Serilog;

#endregion

namespace AyuSync.Agent.Core;

public sealed class PipeWrapper
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly PipeStream _pipe;

    public PipeWrapper(PipeStream pipe)
    {
        _pipe = pipe;
    }

    public async Task<bool?> SendAsync<T>(T obj, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.Serialize(obj);

        Log.Debug("Sending {Length} bytes of data, type {Type}", data.Length, typeof(T).Name);

        return await SendAsync(data, cancellationToken);
    }

    public async Task<bool?> SendAsync(string json, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        var data = Encoding.UTF8.GetBytes(json);
        var length = BitConverter.GetBytes(data.Length);

        Log.Debug("Sending {Length} bytes of data", data.Length);

        try
        {
            await _pipe.WriteAsync(length, cancellationToken);
            await _pipe.FlushAsync(cancellationToken);

            await _pipe.WriteAsync(data, cancellationToken);
            await _pipe.FlushAsync(cancellationToken);
        }
        catch (InvalidOperationException e)
        {
            Log.Error(e, "Error while sending data, need recreate pipe");
            return null;
        }
        catch (IOException e)
        {
            Log.Error(e, "Error while sending data, need recreate pipe");
            return null;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error while sending data: {Message}", e.Message);
            return false;
        }
        finally
        {
            _lock.Release();
        }

        return true;
    }

    public async Task<string> ReceiveAsync()
    {
        var lengthBuffer = new byte[sizeof(int)];
        // ReSharper disable once MustUseReturnValue
        await _pipe.ReadAsync(lengthBuffer);
        var length = BitConverter.ToInt32(lengthBuffer);

        Log.Debug("Receiving {Length} bytes of data", length);

        var dataBuffer = new byte[length];
        // ReSharper disable once MustUseReturnValue
        await _pipe.ReadAsync(dataBuffer);
        var data = Encoding.UTF8.GetString(dataBuffer);

        return data;
    }

    public async Task<T?> ReceiveAsync<T>()
    {
        var data = await ReceiveAsync();
        return JsonSerializer.Deserialize<T>(data);
    }

    public async Task WaitForAnyConnection()
    {
        if (_pipe is not NamedPipeServerStream server)
        {
            return;
        }

        await server.WaitForConnectionAsync();
    }
}
