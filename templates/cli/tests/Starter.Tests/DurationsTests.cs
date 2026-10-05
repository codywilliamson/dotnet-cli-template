using Starter.Shared.Output;

namespace Starter.Tests;

public class DurationsTests
{
    [Test]
    [Arguments(250, "250ms")]
    [Arguments(1500, "1.5s")]
    [Arguments(64_000, "1m04s")]
    public async Task Format_picks_the_unit(int milliseconds, string expected)
    {
        await Assert.That(Durations.Format(TimeSpan.FromMilliseconds(milliseconds))).IsEqualTo(expected);
    }

    [Test]
    [Arguments(512L, "512 B")]
    [Arguments(1536L, "1.5 KB")]
    [Arguments(5L * 1024 * 1024, "5.0 MB")]
    public async Task Size_picks_the_unit(long bytes, string expected)
    {
        await Assert.That(Durations.Size(bytes)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(30, "just now")]
    [Arguments(5 * 60, "5m ago")]
    [Arguments(3 * 3600, "3h ago")]
    [Arguments(5 * 86400, "5d ago")]
    public async Task Age_reads_naturally(int seconds, string expected)
    {
        await Assert.That(Durations.Age(TimeSpan.FromSeconds(seconds))).IsEqualTo(expected);
    }
}
