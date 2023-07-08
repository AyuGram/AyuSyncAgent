#region

using System.IO.Pipes;
using System.Security.Principal;
using Serilog;

#endregion

namespace AyuSync.Agent.Core;

public sealed class PipeServerWrapper
{
    private readonly string _name;
    private readonly PipeDirection _direction;
    private PipeStream? _namedPipe;
    public PipeWrapper Wrapper { get; private set; }

    public PipeServerWrapper(string name, PipeDirection direction)
    {
        _name = name;
        _direction = direction;
    }

    public async Task Create(CancellationToken cancellationToken = default)
    {
        if (_namedPipe?.IsConnected == true)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_direction == PipeDirection.In)
                {
                    _namedPipe = new NamedPipeServerStream(_name, PipeDirection.InOut);
                }
                else
                {
                    var pipe = new NamedPipeClientStream(".", _name, PipeDirection.Out); // pipe не переиспользуется
                    await pipe.ConnectAsync(cancellationToken);

                    _namedPipe = pipe;
                }

                Wrapper = new PipeWrapper(_namedPipe);

                Log.Information("Created named pipe {Name}", _name);

                return;
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to create named pipe {Name}", _name);
                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}
