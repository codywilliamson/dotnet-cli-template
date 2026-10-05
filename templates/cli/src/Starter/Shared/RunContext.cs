using System.Diagnostics;
using Starter.Cli;
using Starter.Shared.Output;

namespace Starter.Shared;

// everything a command needs from the outside world, built once per run
public sealed class RunContext(string command, CliEnvironment env, OutputMode mode, IReporter reporter)
{
    readonly Stopwatch _clock = Stopwatch.StartNew();

    public string Command => command;

    public CliEnvironment Env => env;

    public OutputMode Mode => mode;

    public IReporter Reporter => reporter;

    public TimeSpan Elapsed => _clock.Elapsed;

    // one line for the agent footer, set by the command when it has something worth saying
    public string? Summary { get; set; }

    // never blocks: without a terminal (redirected stdin, agent mode) it fails fast and names the flag
    public void Confirm(string question, bool yes)
    {
        if (yes)
        {
            return;
        }

        if (!reporter.CanPrompt)
        {
            throw PrefixException.Usage($"{question} needs confirmation", $"try: {AppInfo.NAME} {command} --yes");
        }

        if (!reporter.Confirm(question))
        {
            throw new PrefixException("declined", exitCode: ExitCode.Declined);
        }
    }
}
