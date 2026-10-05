using System.Diagnostics;
using XenoAtom.Ansi;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Prompts;

namespace Starter.Shared.Output;

// a real terminal: live spinner per running step, static result lines once it settles.
// only built when stdout is a terminal, because live widgets leak frames into redirected output
public sealed class PrettyReporter(bool live, bool verbose, bool canPrompt) : IReporter
{
    static readonly string[] SpinnerFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];
    const int SPINNER_FRAME_MS = 80;
    const string CARRIAGE_RETURN = "\r";

    public bool CanPrompt => canPrompt;

    public void Title(string title, IReadOnlyList<Fact> facts)
    {
        Out($"[bold cyan]●[/] [bold]{Escape(title)}[/]");
        foreach (var fact in facts)
        {
            Out($"  [dim]{Escape(fact.Label),-12}[/] {Escape(fact.Value)}");
        }
        Out(" ");
    }

    public async Task<T> StepAsync<T>(string name, Func<StepScope, Task<T>> work)
    {
        var scope = new StepScope();
        var clock = Stopwatch.StartNew();
        var task = work(scope);

        if (live && !task.IsCompleted)
        {
            // one markup line sized to its text; a stack stretches to the terminal width
            await Terminal.LiveAsync(new Markup(() => RunningLine(name, scope, clock)),
                () => task.IsCompleted ? TerminalLoopResult.Stop : TerminalLoopResult.Continue);
            // the cleared live region leaves the cursor where its text ended
            Terminal.Write(CARRIAGE_RETURN);
        }
        else if (!task.IsCompleted)
        {
            Out($"[dim]▸ {Escape(name)}…[/]");
        }

        try
        {
            var result = await task;
            if (scope.SkipReason is { } reason)
            {
                Out($"[dim]○ {Escape(name)}  {Escape(reason)}[/]");
            }
            else
            {
                var detail = scope.Detail is null ? "" : $"  [dim]{Escape(scope.Detail)}[/]";
                Out($"[green]✓[/] {Escape(name)} [dim]{Durations.Format(clock.Elapsed)}[/]{detail}");
            }

            foreach (var note in scope.Notes)
            {
                Out($"    [dim]{Escape(note)}[/]");
            }
            return result;
        }
        catch
        {
            Out($"[red]✗[/] {Escape(name)} [dim]{Durations.Format(clock.Elapsed)}[/]");
            throw;
        }
    }

    public void Info(string message) => Err($"[cyan]i[/] {Escape(message)}");

    public void Warn(string message) => Err($"[yellow]![/] [yellow]{Escape(message)}[/]");

    public void Error(string message, string? hint)
    {
        Err($"[red bold]✗ {Escape(message)}[/]");
        if (hint is not null)
        {
            Err($"  [dim]hint:[/] {Escape(hint)}");
        }
    }

    public void Trace(string line)
    {
        if (verbose)
        {
            Err($"    [dim]│ {Escape(line)}[/]");
        }
    }

    public bool Confirm(string question)
    {
        if (!canPrompt)
        {
            return false;
        }

        Out(" ");
        return Terminal.Prompt(new ConfirmationPrompt(new Markup($"[bold]{Escape(question)}[/]")).Default(false));
    }

    public void Finish(bool success, string headline, TimeSpan elapsed)
    {
        Out(" ");
        var mark = success ? "[green bold]✓" : "[red bold]✗";
        Out($"{mark} {Escape(headline)}[/] [dim]in {Durations.Format(elapsed)}[/]");
    }

    static string RunningLine(string name, StepScope scope, Stopwatch clock)
    {
        var frame = SpinnerFrames[(int)(clock.ElapsedMilliseconds / SPINNER_FRAME_MS) % SpinnerFrames.Length];
        var detail = scope.Detail is null ? "" : $" {Escape(scope.Detail)}";
        return $"[cyan]{frame}[/] {Escape(name)} [dim]{Durations.Format(clock.Elapsed)}{detail}[/]";
    }

    // the string overloads, so holes are markup we already escaped rather than auto-escaped text
    static void Out(string markup) => Terminal.WriteMarkupLine(markup);

    static void Err(string markup) => Terminal.WriteErrorAtomic(writer => writer.WriteLine(AnsiMarkup.Render(markup)));

    static string Escape(string text) =>
        text.Replace("[", "[[", StringComparison.Ordinal).Replace("]", "]]", StringComparison.Ordinal);
}
