using Starter.Cli;

namespace Starter.Tests;

static class TestEnv
{
    public static CliEnvironment Create(bool stdoutRedirected = false, params (string Name, string Value)[] variables)
    {
        var map = variables.ToDictionary(v => v.Name, v => v.Value);
        return new CliEnvironment(
            "/work",
            stdoutRedirected,
            StdinRedirected: false,
            name => map.GetValueOrDefault(name),
            TimeProvider.System,
            "0.0.0");
    }
}
