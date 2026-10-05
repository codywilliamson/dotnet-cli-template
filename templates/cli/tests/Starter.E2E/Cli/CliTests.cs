using System.Text.Json;
using Starter.E2E.Harness;

namespace Starter.E2E.Cli;

// drives the published native exe, docs/specs/cli.md is the contract
public class CliTests
{
    const byte ESCAPE = 0x1b;

    [Test]
    public async Task Help_exits_zero_and_lists_the_commands()
    {
        var run = await PrefixProcess.RunAsync("--help");

        await Assert.That(run.ExitCode).IsEqualTo(0);
        await Assert.That(run.Stdout + run.Stderr).Contains("stats");
        await Assert.That(run.Stdout + run.Stderr).Contains("doctor");
    }

    [Test]
    public async Task Version_prints_a_version()
    {
        var run = await PrefixProcess.RunAsync("--version");

        await Assert.That(run.ExitCode).IsEqualTo(0);
        await Assert.That(run.Stdout.Trim()).Matches(@"^\d+\.\d+\.\d+");
    }

    [Test]
    public async Task Stats_plain_has_no_escape_bytes()
    {
        using var tree = TempTree.Create();
        var run = await PrefixProcess.RunAsync("stats", tree.Root, "--plain");

        await Assert.That(run.ExitCode).IsEqualTo(0);
        await Assert.That(run.Lines).Contains($"files {TempTree.FILES}");
        await Assert.That(run.Lines).Contains($"bytes {TempTree.BYTES}");
        await Assert.That(run.StdoutBytes.Contains(ESCAPE)).IsFalse();
    }

    [Test]
    public async Task Stats_json_is_one_parsable_object()
    {
        using var tree = TempTree.Create();
        var run = await PrefixProcess.RunAsync("stats", tree.Root, "--json");

        await Assert.That(run.ExitCode).IsEqualTo(0);
        await Assert.That(run.Lines.Length).IsEqualTo(1);
        using var json = JsonDocument.Parse(run.Lines[0]);
        await Assert.That(json.RootElement.GetProperty("files").GetInt64()).IsEqualTo(TempTree.FILES);
        await Assert.That(json.RootElement.GetProperty("bytes").GetInt64()).IsEqualTo(TempTree.BYTES);
        await Assert.That(json.RootElement.GetProperty("dir").GetString()).IsEqualTo(tree.Root);
    }

    [Test]
    public async Task Stats_on_a_missing_directory_is_a_usage_error_with_a_hint()
    {
        var run = await PrefixProcess.RunAsync("stats", "definitely-not-a-directory");

        await Assert.That(run.ExitCode).IsEqualTo(2);
        await Assert.That(run.Stdout).IsEmpty();
        await Assert.That(run.Stderr).Contains("does not exist");
        await Assert.That(run.Stderr).Contains("hint:");
    }

    [Test]
    public async Task Doctor_reports_each_check_without_escape_bytes()
    {
        var run = await PrefixProcess.RunAsync("doctor");

        await Assert.That(run.ExitCode).IsEqualTo(0);
        await Assert.That(run.Stdout).Contains("ok dotnet");
        await Assert.That(run.StdoutBytes.Contains(ESCAPE)).IsFalse();
    }

    [Test]
    public async Task Unknown_command_is_a_usage_error()
    {
        var run = await PrefixProcess.RunAsync("frobnicate");

        await Assert.That(run.ExitCode).IsEqualTo(2);
    }
}
