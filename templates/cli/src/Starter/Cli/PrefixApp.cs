using System.Text.Json;
using Starter.Features.Doctor;
using Starter.Features.Stats;
//#if (mcp)
using Starter.Mcp;
//#endif
using Starter.Shared;
using Starter.Shared.Json;
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

    public bool Agent { get; set; }

    public bool Verbose { get; set; }
}

public sealed class PrefixApp(CliEnvironment env, CancellationToken ct)
{
    const string BLANK_LINE = "";
    ExitCode? _result;

    public async Task<ExitCode> RunAsync(string[] args)
    {
        var app = new CommandApp(AppInfo.NAME)
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
            Skill(),
        };
        app.Add(Help(app));
        app.Add(BLANK_LINE);
        foreach (var line in HelpText.RootExamples)
        {
            app.Add(line);
        }

        var parserExit = await app.RunAsync(args);
        // the parser reports bad args as 1, which is our "failed"; commands always set _result
        return _result ?? (parserExit == 0 ? ExitCode.Success : ExitCode.Usage);
    }

    Command Stats()
    {
        var dir = new List<string>();
        string? limit = null;
        return Define("stats", "Count the files under a directory and list the largest",
            [
                new Opt("<dir>", "Directory to scan", dir),
                new Opt("limit=", "List the largest {N} files, 0 for all (default 20)", v => limit = v),
            ],
            (run, token) => new StatsCommand(run).RunAsync(new StatsOptions(dir[0], ParseLimit(limit)), token),
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

    Command Skill() => Define("skill", "Print the agent skill (SKILL.md) to stdout",
        [],
        (_, token) => new SkillCommand().RunAsync(new SkillOptions(), token));

    // long help: examples first, then the command's own --help
    Command Help(CommandApp app)
    {
        var target = new List<string>();
        return new Command("help", "Long help for a command, examples first")
        {
            new HelpOption(),
            { "<command>?", "Command name", target },
            (ctx, _) =>
            {
                _result = ExitCode.Success;
                if (target.Count == 0)
                {
                    app.ShowHelp(ctx.RunConfig);
                    return ValueTask.FromResult((int)_result);
                }

                var command = app.OfType<Command>().FirstOrDefault(c => c.Name == target[0]);
                if (command is null)
                {
                    ctx.Error.WriteLine($"{AppInfo.NAME} help: unknown command '{target[0]}'");
                    ctx.Error.WriteLine($"  hint: try: {AppInfo.NAME} --help");
                    _result = ExitCode.Usage;
                    return ValueTask.FromResult((int)_result);
                }

                Console.Out.WriteLine("Examples:");
                foreach (var line in HelpText.For(command.Name))
                {
                    Console.Out.WriteLine($"  {line}");
                }
                Console.Out.WriteLine();
                command.ShowHelp(ctx.RunConfig);
                return ValueTask.FromResult((int)_result);
            },
        };
    }

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

        new Opt("agent", "Agent output: plain, never prompts, summary line on stderr", _ => common.Agent = true).AddTo(command);
        new Opt("plain", "Plain output even on a terminal", _ => common.Plain = true).AddTo(command);
        new Opt("verbose", "Show child process output on stderr", _ => common.Verbose = true).AddTo(command);
        command.Add(async (_, _) =>
        {
            _result = await Execute(name, common, action);
            return (int)_result;
        });
        return command;
    }

    // the single error boundary: expected failures print message plus hint, a stack trace means a bug
    async Task<ExitCode> Execute(string name, CommonArgs common, Func<RunContext, CancellationToken, Task<ExitCode>> action)
    {
        var mode = OutputModes.Detect(common.Json, common.Plain, common.Agent, env);
        var reporter = OutputModes.Create(mode, common.Verbose, env);
        var run = new RunContext(name, env, mode, reporter);
        try
        {
            var exitCode = await action(run, ct);
            WriteAgentFooter(run);
            return exitCode;
        }
        catch (PrefixException e)
        {
            WriteError(mode, reporter, e.Message, e.Hint, e.ExitCode);
            return e.ExitCode;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            WriteError(mode, reporter, "cancelled", null, ExitCode.Aborted);
            return ExitCode.Aborted;
        }
    }

    // one line an agent can read last: `stats: 1234 files, 56.7 MB, 12ms`
    static void WriteAgentFooter(RunContext run)
    {
        if (run.Mode == OutputMode.Agent && run.Summary is { } summary)
        {
            Console.Error.WriteLine($"{run.Command}: {summary}, {Durations.Format(run.Elapsed)}");
        }
    }

    static void WriteError(OutputMode mode, IReporter reporter, string message, string? hint, ExitCode exitCode)
    {
        if (mode == OutputMode.Json)
        {
            Console.Error.WriteLine(JsonSerializer.Serialize(new ErrorLine(message, hint, (int)exitCode), PrefixJson.Default.ErrorLine));
            return;
        }

        reporter.Error($"{AppInfo.NAME}: {message}", hint);
    }

    static int ParseLimit(string? text)
    {
        if (text is null)
        {
            return DirectoryStats.DEFAULT_LIMIT;
        }

        if (!int.TryParse(text, out var limit) || limit < 0)
        {
            throw PrefixException.Usage($"invalid limit '{text}'", $"try: {AppInfo.NAME} stats . --limit 0");
        }
        return limit;
    }
}
