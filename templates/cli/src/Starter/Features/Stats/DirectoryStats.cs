using System.IO.Enumeration;

namespace Starter.Features.Stats;

public sealed record FileEntry(string Path, long Bytes);

// dir is what the caller passed and paths are relative to it, so output is the same on every machine
public sealed record StatsResult(string Dir, long Files, long Bytes, IReadOnlyList<FileEntry> Largest, bool Truncated);

// pure logic shared by the cli command and the mcp tool
public static class DirectoryStats
{
    // limit 0 means every file
    public const int DEFAULT_LIMIT = 20;

    static readonly EnumerationOptions Options = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        // junctions and symlinks can loop
        AttributesToSkip = FileAttributes.ReparsePoint,
    };

    // smallest first, and for equal sizes the later path first, so the heap drops what sorts last
    sealed class WorstFirst : IComparer<(long Bytes, string Path)>
    {
        public int Compare((long Bytes, string Path) x, (long Bytes, string Path) y)
        {
            var bySize = x.Bytes.CompareTo(y.Bytes);
            return bySize != 0 ? bySize : string.CompareOrdinal(y.Path, x.Path);
        }
    }

    public static StatsResult Scan(string root, string dir, int limit, CancellationToken ct)
    {
        // entry.Length reads straight from the directory listing, no FileInfo per file
        var entries = new FileSystemEnumerable<FileEntry>(dir, (ref FileSystemEntry entry) => new FileEntry(entry.ToFullPath(), entry.Length), Options)
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory,
        };

        long files = 0;
        long bytes = 0;
        var top = new PriorityQueue<FileEntry, (long Bytes, string Path)>(new WorstFirst());
        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();
            files++;
            bytes += entry.Bytes;
            top.Enqueue(entry, (entry.Bytes, entry.Path));
            if (limit > 0 && top.Count > limit)
            {
                top.Dequeue();
            }
        }

        var largest = new List<FileEntry>(top.Count);
        while (top.TryDequeue(out var entry, out _))
        {
            largest.Add(entry with { Path = Path.GetRelativePath(dir, entry.Path).Replace('\\', '/') });
        }
        largest.Sort((a, b) => b.Bytes != a.Bytes ? b.Bytes.CompareTo(a.Bytes) : string.CompareOrdinal(a.Path, b.Path));
        return new StatsResult(root, files, bytes, largest, Truncated: files > largest.Count);
    }
}
