using System.Text.Json;
using Starter.Shared.Json;
using Starter.Shared.Output;
using XenoAtom.Terminal;

namespace Starter.Features.Stats;

static class StatsWriters
{
    public static IResultWriter<StatsResult> For(OutputMode mode) => mode switch
    {
        OutputMode.Json => new StatsJsonWriter(),
        OutputMode.Pretty => new StatsPrettyWriter(),
        OutputMode.Agent => new StatsPlainWriter(footer: true),
        _ => new StatsPlainWriter(footer: false),
    };
}

sealed class StatsJsonWriter : IResultWriter<StatsResult>
{
    public void Write(StatsResult result, TimeSpan elapsed) =>
        Console.Out.WriteLine(JsonSerializer.Serialize(result, PrefixJson.Default.StatsResult));
}

sealed class StatsPlainWriter(bool footer) : IResultWriter<StatsResult>
{
    public void Write(StatsResult result, TimeSpan elapsed)
    {
        Console.Out.WriteLine($"dir {result.Dir}");
        Console.Out.WriteLine($"files {result.Files}");
        Console.Out.WriteLine($"bytes {result.Bytes}");
        if (footer)
        {
            Console.Out.WriteLine($"{result.Files} files, {Durations.Size(result.Bytes)}, {Durations.Format(elapsed)}");
        }
    }
}

// writes through Terminal so NO_COLOR and capability detection apply
sealed class StatsPrettyWriter : IResultWriter<StatsResult>
{
    public void Write(StatsResult result, TimeSpan elapsed)
    {
        Terminal.WriteMarkup($"[dim]{"dir",-7}[/] ");
        Terminal.BeginLink(new Uri(result.Dir).AbsoluteUri);
        Terminal.Write(result.Dir);
        Terminal.EndLink();
        Terminal.WriteLine();
        Terminal.WriteMarkupLine($"[dim]{"files",-7}[/] [bold]{result.Files:N0}[/]");
        Terminal.WriteMarkupLine($"[dim]{"size",-7}[/] [bold]{Durations.Size(result.Bytes)}[/] [dim]{Durations.Format(elapsed)}[/]");
    }
}
