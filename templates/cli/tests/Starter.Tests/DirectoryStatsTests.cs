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

        var result = DirectoryStats.Scan(dir.Path, CancellationToken.None);

        await Assert.That(result.Files).IsEqualTo(2);
        await Assert.That(result.Bytes).IsEqualTo(12);
        await Assert.That(result.Dir).IsEqualTo(dir.Path);
    }

    [Test]
    public async Task Empty_directory_is_zero()
    {
        using var dir = TempDir.Create();

        var result = DirectoryStats.Scan(dir.Path, CancellationToken.None);

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

        await Assert.That(() => DirectoryStats.Scan(dir.Path, cts.Token)).Throws<OperationCanceledException>();
    }
}
