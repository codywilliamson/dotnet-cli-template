using Starter.Shared.Processes;

namespace Starter.Tests;

public class ProcessRunnerTests
{
    [Test]
    public async Task Missing_executable_returns_127_instead_of_throwing()
    {
        var result = await ProcessRunner.RunAsync("definitely-not-a-real-exe-9f3a", [], CancellationToken.None);

        await Assert.That(result.ExitCode).IsEqualTo(ProcessRunner.NOT_FOUND_EXIT_CODE);
        await Assert.That(result.Stderr).Contains("could not start");
    }

    [Test]
    public async Task Runs_a_real_tool_and_captures_stdout()
    {
        var result = await ProcessRunner.RunAsync("dotnet", ["--version"], CancellationToken.None);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.Stdout.Trim()).IsNotEmpty();
    }
}
