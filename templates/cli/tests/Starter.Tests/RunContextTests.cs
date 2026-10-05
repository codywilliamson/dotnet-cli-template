using Starter.Shared;
using Starter.Shared.Output;

namespace Starter.Tests;

public class RunContextTests
{
    static RunContext Create(IReporter reporter) => new("clean", TestEnv.Create(), OutputMode.Agent, reporter);

    [Test]
    public async Task Confirm_without_a_terminal_fails_fast_and_names_the_flag()
    {
        var run = Create(new PlainReporter(verbose: false));

        var error = await Assert.That(() => run.Confirm("delete everything", yes: false)).Throws<PrefixException>();

        await Assert.That(error!.ExitCode).IsEqualTo(ExitCode.Usage);
        await Assert.That(error.Hint).Contains("--yes");
    }

    [Test]
    public async Task Confirm_with_yes_never_asks()
    {
        var run = Create(new PlainReporter(verbose: false));

        run.Confirm("delete everything", yes: true);

        await Assert.That(run.Command).IsEqualTo("clean");
    }
}
