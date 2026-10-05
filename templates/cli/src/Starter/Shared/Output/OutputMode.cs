using Starter.Cli;

namespace Starter.Shared.Output;

public enum OutputMode { Pretty, Plain, Agent, Json }

public static class OutputModes
{
    // GetVariable cannot enumerate, so list the exact names; CODEX_* names are added once confirmed
    static readonly string[] AgentEnvVars = ["CLAUDECODE", "CODEX_SANDBOX", "CODEX_SANDBOX_NETWORK_DISABLED"];

    public static OutputMode Detect(bool json, bool plain, bool agent, CliEnvironment env) =>
        json ? OutputMode.Json
        : plain ? OutputMode.Plain
        : agent || IsAgent(env) ? OutputMode.Agent
        : env.StdoutRedirected || !string.IsNullOrEmpty(env.GetVariable("CI")) ? OutputMode.Plain
        : OutputMode.Pretty;

    public static IReporter Create(OutputMode mode, bool verbose, CliEnvironment env) => mode switch
    {
        // live widgets fight with streamed trace lines, so verbose renders steps statically
        OutputMode.Pretty => new PrettyReporter(live: !verbose, verbose, canPrompt: !env.StdinRedirected),
        _ => new PlainReporter(verbose),
    };

    static bool IsAgent(CliEnvironment env) =>
        Array.Exists(AgentEnvVars, name => !string.IsNullOrEmpty(env.GetVariable(name)));
}
