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
        await Assert.That(run.Stderr).Contains("hint: try: starter stats .");
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

    [Test]
    public async Task Agent_env_prints_plain_output_and_a_summary_footer_on_stderr()
    {
        using var tree = TempTree.Create();
        var run = await PrefixProcess.RunAsync(["stats", tree.Root], agent: true);

        await Assert.That(run.ExitCode).IsEqualTo(0);
        await Assert.That(run.StdoutBytes.Contains(ESCAPE)).IsFalse();
        await Assert.That(run.Stderr.Contains((char)ESCAPE)).IsFalse();
        await Assert.That(run.Stderr.Trim()).Matches(@"^stats: 2 files, 12 B, \d+ms$");
    }

    [Test]
    public async Task Agent_flag_works_without_the_env_var()
    {
        using var tree = TempTree.Create();
        var run = await PrefixProcess.RunAsync("stats", tree.Root, "--agent");

        await Assert.That(run.Stderr).Contains("stats: 2 files");
    }

    [Test]
    public async Task Truncated_output_names_the_flag_for_more()
    {
        using var tree = TempTree.Create();
        var run = await PrefixProcess.RunAsync(["stats", tree.Root, "--limit", "1"], agent: true);

        await Assert.That(run.Lines.Count(l => l.StartsWith("file "))).IsEqualTo(1);
        await Assert.That(run.Stderr).Contains("showing 1 of 2 files, pass --limit 0 for all");
    }

    [Test]
    public async Task Json_lists_relative_paths_largest_first()
    {
        using var tree = TempTree.Create();
        var run = await PrefixProcess.RunAsync("stats", tree.Root, "--json");

        using var json = JsonDocument.Parse(run.Lines[0]);
        var largest = json.RootElement.GetProperty("largest");
        await Assert.That(largest[0].GetProperty("path").GetString()).IsEqualTo("sub/b.txt");
        await Assert.That(largest[1].GetProperty("path").GetString()).IsEqualTo("a.txt");
        await Assert.That(json.RootElement.GetProperty("truncated").GetBoolean()).IsFalse();
    }

    [Test]
    public async Task Json_errors_are_json_on_stderr()
    {
        var run = await PrefixProcess.RunAsync("stats", "definitely-not-a-directory", "--json");

        await Assert.That(run.ExitCode).IsEqualTo(2);
        await Assert.That(run.Stdout).IsEmpty();
        using var json = JsonDocument.Parse(run.Stderr.Trim());
        await Assert.That(json.RootElement.GetProperty("error").GetString()).Contains("does not exist");
        await Assert.That(json.RootElement.GetProperty("hint").GetString()).StartsWith("try: starter stats");
        await Assert.That(json.RootElement.GetProperty("exit_code").GetInt32()).IsEqualTo(2);
    }

    [Test]
    public async Task Bad_limit_is_a_usage_error_with_a_next_command()
    {
        using var tree = TempTree.Create();
        var run = await PrefixProcess.RunAsync("stats", tree.Root, "--limit", "lots");

        await Assert.That(run.ExitCode).IsEqualTo(2);
        await Assert.That(run.Stderr).Contains("try: starter stats . --limit 0");
    }

    [Test]
    public async Task Help_has_examples_and_the_exit_codes()
    {
        var run = await PrefixProcess.RunAsync("--help");

        await Assert.That(run.Stdout).Contains("Examples:");
        await Assert.That(run.Stdout).Contains("starter stats src --json");
        await Assert.That(run.Stdout).Contains("Exit codes: 0 ok, 1 failed, 2 usage");
        await Assert.That(run.Lines.Length).IsLessThan(40);
    }

    [Test]
    public async Task Help_for_a_command_puts_examples_first()
    {
        var run = await PrefixProcess.RunAsync("help", "stats");

        await Assert.That(run.ExitCode).IsEqualTo(0);
        await Assert.That(run.Lines[0]).IsEqualTo("Examples:");
        await Assert.That(run.Stdout).Contains("--limit");
    }

    [Test]
    public async Task Skill_prints_frontmatter_and_covers_every_command_in_help()
    {
        var skill = await PrefixProcess.RunAsync("skill");
        var help = await PrefixProcess.RunAsync("--help");

        await Assert.That(skill.ExitCode).IsEqualTo(0);
        await Assert.That(skill.Stdout).StartsWith("---");
        await Assert.That(skill.Stdout).Contains("name: starter");
        await Assert.That(skill.Stdout).Contains("description: Use when");

        // two-space indent plus a name is a command line, option lines start with a dash
        var commands = help.Stdout.Split('\n')
            .Select(line => System.Text.RegularExpressions.Regex.Match(line, @"^  ([a-z][a-z-]*)\s{2,}\S"))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .ToArray();
        await Assert.That(commands).Contains("stats");
        foreach (var command in commands)
        {
            await Assert.That(skill.Stdout).Contains($"starter {command}");
        }
    }
}
