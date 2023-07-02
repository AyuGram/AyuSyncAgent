#region

using System.IO.Pipes;
using Serilog;

#endregion

namespace AyuSync.Agent.Core;

public sealed class PipeServerWrapper
{
    private readonly string _name;
    private NamedPipeServerStream? _namedPipe;
    public PipeWrapper Wrapper { get; private set; }

    public PipeServerWrapper(string name)
    {
        _name = name;
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
                _namedPipe = new NamedPipeServerStream(_name);
                Wrapper = new PipeWrapper(_namedPipe);

                Log.Information("Created named pipe {Name}", _name);

                return;
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to create named pipe");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}
