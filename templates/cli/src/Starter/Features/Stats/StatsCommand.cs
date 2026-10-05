using Starter.Shared;

namespace Starter.Features.Stats;

public sealed record StatsOptions(string Root);

public sealed class StatsCommand(RunContext run)
{
    public Task<ExitCode> RunAsync(StatsOptions options, CancellationToken ct)
    {
        var dir = Path.GetFullPath(options.Root, run.Env.CurrentDirectory);
        if (!Directory.Exists(dir))
        {
            throw PrefixException.Usage($"directory '{options.Root}' does not exist", "pass an existing directory, e.g. starter stats .");
        }

        var result = DirectoryStats.Scan(dir, ct);
        StatsWriters.For(run.Mode).Write(result, run.Elapsed);
        return Task.FromResult(ExitCode.Success);
    }
}
