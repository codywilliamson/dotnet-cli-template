using Starter.Shared;
using Starter.Shared.Output;
using Starter.Shared.Processes;

namespace Starter.Features.Doctor;

public sealed record DoctorOptions;

public sealed class DoctorCommand(RunContext run)
{
    sealed record ToolCheck(string File, string Fix);

    static readonly ToolCheck[] Tools =
    [
        new("dotnet", "install the .NET SDK from https://dot.net"),
        new("git", "install git from https://git-scm.com"),
    ];

    public async Task<ExitCode> RunAsync(DoctorOptions options, CancellationToken ct)
    {
        var reporter = run.Reporter;
        reporter.Title("doctor", [new Fact("cwd", run.Env.CurrentDirectory)]);

        var checklist = new Checklist(reporter);
        foreach (var tool in Tools)
        {
            await checklist.CheckAsync(tool.File, scope => CheckToolAsync(tool, scope, ct));
        }

        var ok = checklist.Problems == 0;
        reporter.Finish(ok, ok ? "all checks passed" : $"{checklist.Problems} problem(s) found", run.Elapsed);
        return ok ? ExitCode.Success : ExitCode.Failed;
    }

    async Task CheckToolAsync(ToolCheck tool, StepScope scope, CancellationToken ct)
    {
        var result = await ProcessRunner.RunAsync(tool.File, ["--version"], ct, onLine: run.Reporter.Trace);
        if (!result.Succeeded)
        {
            throw new PrefixException($"{tool.File} is not usable: {result.Tail()}", tool.Fix);
        }

        scope.Detail = result.Stdout.Trim();
    }
}
