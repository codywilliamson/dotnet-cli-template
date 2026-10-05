using Starter.Shared;
using Starter.Shared.Output;

namespace Starter.Features.Stats;

public sealed record StatsOptions(string Root, int Limit);

public sealed class StatsCommand(RunContext run)
{
    public Task<ExitCode> RunAsync(StatsOptions options, CancellationToken ct)
    {
        var dir = Path.GetFullPath(options.Root, run.Env.CurrentDirectory);
        if (!Directory.Exists(dir))
        {
            throw PrefixException.Usage($"directory '{options.Root}' does not exist", $"try: {AppInfo.NAME} stats .");
        }

        var result = DirectoryStats.Scan(options.Root, dir, options.Limit, ct);
        StatsWriters.For(run.Mode, dir).Write(result, run.Elapsed);

        run.Summary = $"{result.Files} files, {Durations.Size(result.Bytes)}";
        if (result.Truncated && run.Mode != OutputMode.Json)
        {
            run.Reporter.Info($"showing {result.Largest.Count} of {result.Files} files, pass --limit 0 for all");
        }
        return Task.FromResult(ExitCode.Success);
    }
}
