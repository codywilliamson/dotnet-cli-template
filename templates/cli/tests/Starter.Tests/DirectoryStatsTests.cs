using Starter.Features.Stats;

namespace Starter.Tests;

public class DirectoryStatsTests
{
    [Test]
    public async Task Counts_files_in_nested_directories()
    {
        using var dir = TempDir.Create();
        dir.Write("a.txt", "12345");
        dir.Write("sub/deeper/b.txt", "1234567");

        var result = DirectoryStats.Scan(".", dir.Path, DirectoryStats.DEFAULT_LIMIT, CancellationToken.None);

        await Assert.That(result.Files).IsEqualTo(2);
        await Assert.That(result.Bytes).IsEqualTo(12);
        await Assert.That(result.Dir).IsEqualTo(".");
        await Assert.That(result.Truncated).IsFalse();
    }

    [Test]
    public async Task Largest_is_sorted_with_relative_paths()
    {
        using var dir = TempDir.Create();
        dir.Write("b.txt", "12");
        dir.Write("a.txt", "12");
        dir.Write("sub/big.txt", "123456");

        var result = DirectoryStats.Scan(".", dir.Path, 0, CancellationToken.None);

        await Assert.That(result.Largest.Count).IsEqualTo(3);
        await Assert.That(result.Largest[0].Path).IsEqualTo("sub/big.txt");
        await Assert.That(result.Largest[1].Path).IsEqualTo("a.txt");
    }

    [Test]
    public async Task Limit_truncates_and_says_so()
    {
        using var dir = TempDir.Create();
        dir.Write("a.txt", "1");
        dir.Write("b.txt", "12");
        dir.Write("c.txt", "123");

        var result = DirectoryStats.Scan(".", dir.Path, 2, CancellationToken.None);

        await Assert.That(result.Files).IsEqualTo(3);
        await Assert.That(result.Largest.Count).IsEqualTo(2);
        await Assert.That(result.Largest[0].Path).IsEqualTo("c.txt");
        await Assert.That(result.Truncated).IsTrue();
    }

    [Test]
    public async Task Empty_directory_is_zero()
    {
        using var dir = TempDir.Create();

        var result = DirectoryStats.Scan(".", dir.Path, DirectoryStats.DEFAULT_LIMIT, CancellationToken.None);

        await Assert.That(result.Files).IsEqualTo(0);
        await Assert.That(result.Bytes).IsEqualTo(0);
    }

    [Test]
    public async Task Cancelled_token_stops_the_scan()
    {
        using var dir = TempDir.Create();
        dir.Write("a.txt", "x");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.That(() => DirectoryStats.Scan(".", dir.Path, 0, cts.Token)).Throws<OperationCanceledException>();
    }
}
