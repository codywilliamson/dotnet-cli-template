#!/usr/bin/env dotnet
#:package XenoAtom.Terminal@2.2.0
#:property InvariantGlobalization=true

using System.Diagnostics;
using System.Text;
using XenoAtom.Terminal;

const int EXIT_FAILED = 1;
const int EXIT_USAGE = 2;

if (args.Length == 0)
{
    Console.Error.WriteLine("usage: tool <dir>");
    return EXIT_USAGE;
}

Console.OutputEncoding = new UTF8Encoding(false);
var color = !Console.IsOutputRedirected;
// beside the script when run as `dotnet run`, beside the exe once published
var scriptDir = AppContext.GetData("EntryPointFileDirectoryPath") as string ?? AppContext.BaseDirectory;
var dir = Path.GetFullPath(args[0]);
var failed = false;

Step("find dir", () => Directory.Exists(dir) ? dir : throw new DirectoryNotFoundException($"no such directory: {dir}"));
if (!failed)
{
    Step("count files", () => $"{Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Count()} files");
}
Step("read notes", () =>
{
    var notes = Path.Combine(scriptDir, "notes.txt");
    return File.Exists(notes) ? $"{File.ReadAllLines(notes).Length} lines" : "none";
});

return failed ? EXIT_FAILED : 0;

void Step(string name, Func<string> work)
{
    var clock = Stopwatch.StartNew();
    try
    {
        var detail = work();
        Print(true, name, clock.Elapsed, detail);
    }
    catch (Exception e) when (e is IOException or UnauthorizedAccessException)
    {
        failed = true;
        Print(false, name, clock.Elapsed, e.Message);
    }
}

void Print(bool ok, string name, TimeSpan elapsed, string detail)
{
    var mark = ok ? "✓" : "✗";
    var time = elapsed.TotalSeconds < 1 ? $"{elapsed.TotalMilliseconds:0}ms" : $"{elapsed.TotalSeconds:0.0}s";
    if (color)
    {
        var tint = ok ? "green" : "red";
        Terminal.WriteMarkupLine($"[{tint}]{mark}[/] {name} [dim]{time}  {detail}[/]");
    }
    else
    {
        Console.WriteLine($"{mark} {name} {time}  {detail}");
    }
}
