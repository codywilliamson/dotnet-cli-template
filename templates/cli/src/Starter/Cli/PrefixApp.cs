using Starter.Features.Doctor;
using Starter.Features.Stats;
//#if (mcp)
using Starter.Mcp;
//#endif
using Starter.Shared;
using Starter.Shared.Output;
using XenoAtom.CommandLine;

namespace Starter.Cli;

// one option or positional, added through the matching Command.Add overload
sealed record Opt(string Prototype, string Description, Action<string?>? OnValue = null, List<string>? Values = null)
{
    public Opt(string prototype, string description, List<string> values) : this(prototype, description, null, values) { }

    public void AddTo(Command command)
    {
        if (Values is not null)
        {
            command.Add(Prototype, Description, Values);
        }
        else
        {
            command.Add(Prototype, Description, OnValue!);
        }
    }
}

// options every command accepts
sealed class CommonArgs
{
    public bool Plain { get; set; }

    public bool Json { get; set; }

    public bool Verbose { get; set; }
}

public sealed class PrefixApp(CliEnvironment env, CancellationToken ct)
{
    public const string NAME = "starter";

    const string BLANK_LINE = "";
    ExitCode? _result;

    public async Task<ExitCode> RunAsync(string[] args)
    {
        var app = new CommandApp(NAME)
        {
            new CommandUsage("Usage: {NAME} <command> [options]"),
            BLANK_LINE,
            "Describe what this tool does in one line.",
            BLANK_LINE,
            new HelpOption(),
            new VersionOption(env.Version),
            BLANK_LINE,
            Stats(),
            Doctor(),
//#if (mcp)
            Mcp(),
//#endif
            BLANK_LINE,
            "Examples:",
            "  starter stats .",
            "  starter stats src --json",
        };

        var parserExit = await app.RunAsync(args);
        // the parser reports bad args as 1, which is our "failed"; commands always set _result
        return _result ?? (parserExit == 0 ? ExitCode.Success : ExitCode.Usage);
    }

    Command Stats()
    {
        var dir = new List<string>();
        return Define("stats", "Count the files under a directory and add up their size",
            [new Opt("<dir>", "Directory to scan", dir)],
            (run, token) => new StatsCommand(run).RunAsync(new StatsOptions(dir[0]), token),
            json: true);
    }

    Command Doctor() => Define("doctor", "Check that the tools this one needs are installed",
        [],
        (run, token) => new DoctorCommand(run).RunAsync(new DoctorOptions(), token));
//#if (mcp)

    Command Mcp() => Define("mcp", "Run the stdio MCP server",
        [],
        (run, token) => new McpCommand(run).RunAsync(new McpOptions(), token));
//#endif

    Command Define(string name, string description, IEnumerable<Opt> options, Func<RunContext, CancellationToken, Task<ExitCode>> action, bool json = false)
    {
        var common = new CommonArgs();
        var command = new Command(name, description) { new HelpOption() };
        foreach (var option in options)
        {
            option.AddTo(command);
        }

        if (json)
        {
            new Opt("json", "One JSON object on stdout", _ => common.Json = true).AddTo(command);
        }

        new Opt("plain", "Plain output even on a terminal", _ => common.Plain = true).AddTo(command);
        new Opt("verbose", "Show child process output", _ => common.Verbose = true).AddTo(command);
        command.Add(async (_, _) =>
        {
            _result = await Execute(common, action);
            return (int)_result;
        });
        return command;
    }

    // the single error boundary: expected failures print message plus hint, a stack trace means a bug
    async Task<ExitCode> Execute(CommonArgs common, Func<RunContext, CancellationToken, Task<ExitCode>> action)
    {
        var mode = OutputModes.Detect(common.Json, common.Plain, env);
        var reporter = OutputModes.Create(mode, common.Verbose, env);
        try
        {
            return await action(new RunContext(env, mode, reporter), ct);
        }
        catch (PrefixException e)
        {
            reporter.Error($"{NAME}: {e.Message}", e.Hint);
            return e.ExitCode;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            reporter.Error($"{NAME}: cancelled", null);
            return ExitCode.Aborted;
        }
    }
}
