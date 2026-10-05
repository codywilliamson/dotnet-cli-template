using Starter.Shared;

namespace Starter.Mcp;

public sealed record McpOptions;

public sealed class McpCommand(RunContext run)
{
    public async Task<ExitCode> RunAsync(McpOptions options, CancellationToken ct)
    {
        var tools = new McpTools(run.Env.CurrentDirectory);
        await new PrefixMcpServer(tools, run.Env.Version).RunAsync(ct);
        return ExitCode.Success;
    }
}
