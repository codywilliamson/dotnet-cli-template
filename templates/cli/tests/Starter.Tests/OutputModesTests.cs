using Starter.Shared.Output;

namespace Starter.Tests;

public class OutputModesTests
{
    [Test]
    public async Task Json_beats_everything()
    {
        var env = TestEnv.Create(stdoutRedirected: true, ("CLAUDECODE", "1"));
        await Assert.That(OutputModes.Detect(json: true, plain: true, env)).IsEqualTo(OutputMode.Json);
    }

    [Test]
    public async Task Plain_beats_agent_env()
    {
        var env = TestEnv.Create(stdoutRedirected: false, ("CLAUDECODE", "1"));
        await Assert.That(OutputModes.Detect(json: false, plain: true, env)).IsEqualTo(OutputMode.Plain);
    }

    [Test]
    [Arguments("CLAUDECODE")]
    [Arguments("CODEX_SANDBOX")]
    public async Task Agent_env_beats_redirected_stdout(string variable)
    {
        var env = TestEnv.Create(stdoutRedirected: true, (variable, "1"));
        await Assert.That(OutputModes.Detect(json: false, plain: false, env)).IsEqualTo(OutputMode.Agent);
    }

    [Test]
    public async Task Empty_agent_env_is_ignored()
    {
        var env = TestEnv.Create(stdoutRedirected: false, ("CLAUDECODE", ""));
        await Assert.That(OutputModes.Detect(json: false, plain: false, env)).IsEqualTo(OutputMode.Pretty);
    }

    [Test]
    public async Task Redirected_stdout_is_plain()
    {
        var env = TestEnv.Create(stdoutRedirected: true);
        await Assert.That(OutputModes.Detect(json: false, plain: false, env)).IsEqualTo(OutputMode.Plain);
    }

    [Test]
    public async Task Ci_is_plain_even_on_a_terminal()
    {
        var env = TestEnv.Create(stdoutRedirected: false, ("CI", "true"));
        await Assert.That(OutputModes.Detect(json: false, plain: false, env)).IsEqualTo(OutputMode.Plain);
    }

    [Test]
    public async Task Terminal_is_pretty()
    {
        var env = TestEnv.Create();
        await Assert.That(OutputModes.Detect(json: false, plain: false, env)).IsEqualTo(OutputMode.Pretty);
    }
}
