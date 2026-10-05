using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Starter.E2E.Harness;

namespace Starter.E2E.Mcp;

// json-rpc over stdio against `starter mcp`
public class McpTests
{
    [Test]
    public async Task Full_conversation()
    {
        using var tree = TempTree.Create();
        await using var mcp = McpSession.Start(tree.Root);

        var init = await mcp.RequestAsync("initialize", InitializeParams());
        await Assert.That(init["result"]!["serverInfo"]!["name"]!.GetValue<string>()).IsEqualTo("starter");
        await mcp.NotifyAsync("notifications/initialized");

        var list = await mcp.RequestAsync("tools/list");
        var names = list["result"]!["tools"]!.AsArray().Select(t => t!["name"]!.GetValue<string>());
        await Assert.That(names).IsEquivalentTo(["stats"]);

        var stats = await mcp.CallAsync("stats", new JsonObject { ["dir"] = "." });
        var structured = stats["result"]!["structuredContent"]!;
        await Assert.That(structured["files"]!.GetValue<int>()).IsEqualTo(TempTree.FILES);
        await Assert.That(structured["bytes"]!.GetValue<int>()).IsEqualTo(TempTree.BYTES);

        await Assert.That(await mcp.CloseAndWaitAsync()).IsEqualTo(0);
    }

    [Test]
    [Arguments("""{"name":"no_such_tool","arguments":{}}""")]
    [Arguments("""{"name":"stats","arguments":{}}""")]
    [Arguments("""{"name":"stats","arguments":{"dir":"definitely-not-a-directory"}}""")]
    [Arguments("""{"name":"stats","arguments":{"dir":5}}""")]
    [Arguments("""{"name":"stats","arguments":{"dir":null}}""")]
    [Arguments("""{"name":"stats","arguments":{"dir":".","extra":1}}""")]
    public async Task Unknown_tools_and_bad_args_are_invalid_params(string callParams)
    {
        using var tree = TempTree.Create();
        await using var mcp = await McpSession.StartInitializedAsync(tree.Root);

        var response = await mcp.RequestAsync("tools/call", JsonNode.Parse(callParams)!.AsObject());
        await Assert.That(response["error"]!["code"]!.GetValue<int>()).IsEqualTo(-32602);
    }

    [Test]
    public async Task Exits_cleanly_when_stdin_closes()
    {
        using var tree = TempTree.Create();
        await using var mcp = await McpSession.StartInitializedAsync(tree.Root);
        await Assert.That(await mcp.CloseAndWaitAsync()).IsEqualTo(0);
    }

    static JsonObject InitializeParams() => new()
    {
        ["protocolVersion"] = "2025-06-18",
        ["capabilities"] = new JsonObject(),
        ["clientInfo"] = new JsonObject { ["name"] = "e2e", ["version"] = "0" },
    };

    sealed class McpSession : IAsyncDisposable
    {
        readonly Process _process;
        int _nextId;

        McpSession(Process process) => _process = process;

        public static McpSession Start(string workingDirectory)
        {
            var psi = PrefixProcess.StartInfo(["mcp"]);
            psi.WorkingDirectory = workingDirectory;
            psi.RedirectStandardInput = true;
            psi.StandardInputEncoding = new UTF8Encoding(false);
            psi.StandardOutputEncoding = Encoding.UTF8;
            return new McpSession(Process.Start(psi)!);
        }

        public static async Task<McpSession> StartInitializedAsync(string workingDirectory)
        {
            var session = Start(workingDirectory);
            await session.RequestAsync("initialize", InitializeParams());
            await session.NotifyAsync("notifications/initialized");
            return session;
        }

        public Task<JsonNode> CallAsync(string tool, JsonObject arguments) =>
            RequestAsync("tools/call", new JsonObject { ["name"] = tool, ["arguments"] = arguments });

        public async Task<JsonNode> RequestAsync(string method, JsonObject? parameters = null)
        {
            var id = ++_nextId;
            await WriteAsync(new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["method"] = method, ["params"] = parameters });
            using var cts = new CancellationTokenSource(PrefixProcess.Timeout);
            while (true)
            {
                var line = await _process.StandardOutput.ReadLineAsync(cts.Token)
                    ?? throw new InvalidOperationException($"server closed stdout: {await _process.StandardError.ReadToEndAsync()}");
                var message = JsonNode.Parse(line)!;
                // skip notifications and anything that is not our answer
                if (message["id"]?.GetValueKind() == JsonValueKind.Number && message["id"]!.GetValue<int>() == id)
                {
                    return message;
                }
            }
        }

        public Task NotifyAsync(string method) => WriteAsync(new JsonObject { ["jsonrpc"] = "2.0", ["method"] = method });

        async Task WriteAsync(JsonObject message)
        {
            await _process.StandardInput.WriteLineAsync(message.ToJsonString());
            await _process.StandardInput.FlushAsync();
        }

        public async Task<int> CloseAndWaitAsync()
        {
            _process.StandardInput.Close();
            using var cts = new CancellationTokenSource(PrefixProcess.Timeout);
            await _process.WaitForExitAsync(cts.Token);
            return _process.ExitCode;
        }

        // close stdin and let it exit; a killed process can hold its cwd handle
        // past exit on windows, which breaks the temp dir cleanup
        public async ValueTask DisposeAsync()
        {
            if (!_process.HasExited)
            {
                try
                {
                    await CloseAndWaitAsync();
                }
                catch (OperationCanceledException)
                {
                    _process.Kill(entireProcessTree: true);
                    await _process.WaitForExitAsync();
                }
            }
            _process.Dispose();
        }
    }
}
