using System.Diagnostics;

namespace Starter.Shared.Output;

// redirected output, CI logs and agents: no escape bytes, one line per event, stable prefixes.
// the run's report goes to stdout; warnings, errors and child output go to stderr
public sealed class PlainReporter(bool verbose) : IReporter
{
    public bool CanPrompt => false;

    public void Title(string title, IReadOnlyList<Fact> facts)
    {
        Console.WriteLine(title);
        foreach (var fact in facts)
        {
            Console.WriteLine($"  {fact.Label,-12} {fact.Value}");
        }
    }

    public async Task<T> StepAsync<T>(string name, Func<StepScope, Task<T>> work)
    {
        var scope = new StepScope();
        var clock = Stopwatch.StartNew();
        Console.WriteLine($"> {name}");
        try
        {
            var result = await work(scope);
            if (scope.SkipReason is { } reason)
            {
                Console.WriteLine($"- {name} (skipped: {reason})");
            }
            else
            {
                var detail = scope.Detail is null ? "" : $"  {scope.Detail}";
                Console.WriteLine($"ok {name} ({Durations.Format(clock.Elapsed)}){detail}");
            }

            foreach (var note in scope.Notes)
            {
                Console.WriteLine($"     {note}");
            }
            return result;
        }
        catch
        {
            Console.WriteLine($"FAILED {name} ({Durations.Format(clock.Elapsed)})");
            throw;
        }
    }

    public void Info(string message) => Console.Error.WriteLine(message);

    public void Warn(string message) => Console.Error.WriteLine($"warning: {message}");

    public void Error(string message, string? hint)
    {
        Console.Error.WriteLine(message);
        if (hint is not null)
        {
            Console.Error.WriteLine($"  hint: {hint}");
        }
    }

    public void Trace(string line)
    {
        if (verbose)
        {
            Console.Error.WriteLine($"    | {line}");
        }
    }

    public bool Confirm(string question) => false;

    public void Finish(bool success, string headline, TimeSpan elapsed) =>
        Console.WriteLine($"{(success ? "done" : "failed")}: {headline} ({Durations.Format(elapsed)})");
}
