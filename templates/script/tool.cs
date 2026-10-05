#!/usr/bin/env dotnet
#:package XenoAtom.Terminal@2.2.0
#:property InvariantGlobalization=true

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using XenoAtom.Terminal;

const int EXIT_FAILED = 1;
const int EXIT_USAGE = 2;

var flags = args.Where(a => a.StartsWith("--", StringComparison.Ordinal)).ToHashSet();
var positional = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();
if (positional.Length != 1 || flags.Contains("--help"))
{
    Console.Error.WriteLine("usage: tool <dir> [--json] [--agent]");
    Console.Error.WriteLine("example: tool . --json");
    Console.Error.WriteLine("exit codes: 0 ok, 1 a step failed, 2 usage");
    return EXIT_USAGE;
}

Console.OutputEncoding = new UTF8Encoding(false);
var json = flags.Contains("--json");
// agent env vars, --agent and redirected stdout all mean plain text: no color, no prompts, ever
var agent = flags.Contains("--agent") || new[] { "CLAUDECODE", "CODEX_SANDBOX", "CODEX_SANDBOX_NETWORK_DISABLED" }
    .Any(name => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)));
var color = !json && !agent && !Console.IsOutputRedirected;
// beside the script when run as `dotnet run`, beside the exe once published
var scriptDir = AppContext.GetData("EntryPointFileDirectoryPath") as string ?? AppContext.BaseDirectory;
var dir = Path.GetFullPath(positional[0]);
var total = Stopwatch.StartNew();
var steps = 0;
var failed = 0;

Step("find dir", () => Directory.Exists(dir) ? dir : throw new DirectoryNotFoundException($"no such directory: {dir}"));
if (failed == 0)
{
    Step("count files", () => $"{Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Count()} files");
}
Step("read notes", () =>
{
    var notes = Path.Combine(scriptDir, "notes.txt");
    return File.Exists(notes) ? $"{File.ReadAllLines(notes).Length} lines" : "none";
});

// the agent footer goes last, on stderr
if (agent)
{
    Console.Error.WriteLine($"tool: {steps - failed} of {steps} steps ok, {total.ElapsedMilliseconds}ms");
}
return failed == 0 ? 0 : EXIT_FAILED;

void Step(string name, Func<string> work)
{
    var clock = Stopwatch.StartNew();
    steps++;
    try
    {
        Print(new StepResult(name, true, clock.ElapsedMilliseconds, work()));
    }
    catch (Exception e) when (e is IOException or UnauthorizedAccessException)
    {
        failed++;
        Print(new StepResult(name, false, clock.ElapsedMilliseconds, e.Message));
    }
}

void Print(StepResult result)
{
    if (json)
    {
        Console.WriteLine(JsonSerializer.Serialize(result, ScriptJson.Default.StepResult));
        return;
    }

    var mark = result.Ok ? "✓" : "✗";
    if (color)
    {
        Terminal.WriteMarkupLine($"[{(result.Ok ? "green" : "red")}]{mark}[/] {result.Step} [dim]{result.Ms}ms  {result.Detail}[/]");
    }
    else
    {
        Console.WriteLine($"{mark} {result.Step} {result.Ms}ms  {result.Detail}");
    }
}

// one json line per step, source-generated so the script stays AOT clean
record StepResult(string Step, bool Ok, long Ms, string Detail);

[JsonSerializable(typeof(StepResult))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
partial class ScriptJson : JsonSerializerContext;
