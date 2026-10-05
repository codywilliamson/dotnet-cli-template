using System.IO.Enumeration;

namespace Starter.Features.Stats;

public sealed record StatsResult(string Dir, long Files, long Bytes);

// pure logic shared by the cli command and the mcp tool
public static class DirectoryStats
{
    static readonly EnumerationOptions Options = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        // junctions and symlinks can loop
        AttributesToSkip = FileAttributes.ReparsePoint,
    };

    public static StatsResult Scan(string dir, CancellationToken ct)
    {
        // entry.Length reads straight from the directory listing, no FileInfo per file
        var lengths = new FileSystemEnumerable<long>(dir, (ref FileSystemEntry entry) => entry.Length, Options)
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory,
        };

        long files = 0;
        long bytes = 0;
        foreach (var length in lengths)
        {
            ct.ThrowIfCancellationRequested();
            files++;
            bytes += length;
        }
        return new StatsResult(dir, files, bytes);
    }
}
