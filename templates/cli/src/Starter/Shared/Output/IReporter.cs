namespace Starter.Shared.Output;

public sealed record Fact(string Label, string Value);

// what a running step can say about itself
public sealed class StepScope
{
    readonly List<string> _notes = [];

    public string? Detail { get; set; }

    public string? SkipReason { get; private set; }

    public IReadOnlyList<string> Notes => _notes;

    public void Note(string line) => _notes.Add(line);

    public void Skip(string reason) => SkipReason = reason;
}

// step tools talk to this and never to the console, so every output mode stays one implementation
public interface IReporter
{
    bool CanPrompt { get; }

    void Title(string title, IReadOnlyList<Fact> facts);

    Task<T> StepAsync<T>(string name, Func<StepScope, Task<T>> work);

    void Info(string message);

    void Warn(string message);

    void Error(string message, string? hint);

    // streamed child output, shown with --verbose only
    void Trace(string line);

    // false when !CanPrompt
    bool Confirm(string question);

    void Finish(bool success, string headline, TimeSpan elapsed);
}

public static class ReporterExtensions
{
    public static Task StepAsync(this IReporter reporter, string name, Func<StepScope, Task> work) =>
        reporter.StepAsync<bool>(name, async scope =>
        {
            await work(scope);
            return true;
        });
}
