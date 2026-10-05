using System.Text.Json;
using Starter.Shared.Json;
using Starter.Shared.Output;
using XenoAtom.Terminal;

namespace Starter.Features.Stats;

static class StatsWriters
{
    public static IResultWriter<StatsResult> For(OutputMode mode, string fullDir) => mode switch
    {
        OutputMode.Json => new StatsJsonWriter(),
        OutputMode.Pretty => new StatsPrettyWriter(fullDir),
        _ => new StatsPlainWriter(),
    };
}

sealed class StatsJsonWriter : IResultWriter<StatsResult>
{
    public void Write(StatsResult result, TimeSpan elapsed) =>
        Console.Out.WriteLine(JsonSerializer.Serialize(result, PrefixJson.Default.StatsResult));
}

// plain and agent share this, the agent footer is added by the app boundary on stderr
sealed class StatsPlainWriter : IResultWriter<StatsResult>
{
    public void Write(StatsResult result, TimeSpan elapsed)
    {
        Console.Out.WriteLine($"dir {result.Dir}");
        Console.Out.WriteLine($"files {result.Files}");
        Console.Out.WriteLine($"bytes {result.Bytes}");
        foreach (var file in result.Largest)
        {
            Console.Out.WriteLine($"file {file.Bytes} {file.Path}");
        }
    }
}

// writes through Terminal so NO_COLOR and capability detection apply
sealed class StatsPrettyWriter(string fullDir) : IResultWriter<StatsResult>
{
    public void Write(StatsResult result, TimeSpan elapsed)
    {
        Terminal.WriteMarkup($"[dim]{"dir",-7}[/] ");
        Terminal.BeginLink(new Uri(fullDir).AbsoluteUri);
        Terminal.Write(result.Dir);
        Terminal.EndLink();
        Terminal.WriteLine();
        Terminal.WriteMarkupLine($"[dim]{"files",-7}[/] [bold]{result.Files:N0}[/]");
        Terminal.WriteMarkupLine($"[dim]{"size",-7}[/] [bold]{Durations.Size(result.Bytes)}[/] [dim]{Durations.Format(elapsed)}[/]");
        foreach (var file in result.Largest)
        {
            Terminal.WriteMarkupLine($"[dim]{Durations.Size(file.Bytes),9}[/]  {file.Path}");
        }
    }
}
