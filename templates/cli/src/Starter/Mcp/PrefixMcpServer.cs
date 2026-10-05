using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Starter.Shared;

namespace Starter.Mcp;

// hand-written handlers on ModelContextProtocol.Core: no hosting, no attribute reflection
public sealed class PrefixMcpServer(McpTools tools, string version)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var options = new McpServerOptions
        {
            ServerInfo = new Implementation { Name = AppInfo.NAME, Version = version },
            Handlers = new McpServerHandlers
            {
                ListToolsHandler = (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = McpTools.Definitions }),
                CallToolHandler = async (ctx, token) =>
                {
                    var p = ctx.Params ?? throw new McpProtocolException("missing params", McpErrorCode.InvalidParams);
                    return await Task.Run(() => tools.Call(p.Name, p.Arguments, token), token);
                },
            },
        };
        await using var server = McpServer.Create(new StdioServerTransport(options), options);
        await server.RunAsync(ct);
    }
}
