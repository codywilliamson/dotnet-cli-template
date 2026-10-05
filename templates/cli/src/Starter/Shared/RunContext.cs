using System.Diagnostics;
using Starter.Cli;
using Starter.Shared.Output;

namespace Starter.Shared;

// everything a command needs from the outside world, built once per run
public sealed class RunContext(CliEnvironment env, OutputMode mode, IReporter reporter)
{
    readonly Stopwatch _clock = Stopwatch.StartNew();

    public CliEnvironment Env => env;

    public OutputMode Mode => mode;

    public IReporter Reporter => reporter;

    public TimeSpan Elapsed => _clock.Elapsed;
}
