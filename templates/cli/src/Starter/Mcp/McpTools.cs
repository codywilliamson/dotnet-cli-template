using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using Starter.Features.Stats;
using Tool = ModelContextProtocol.Protocol.Tool;

namespace Starter.Mcp;

public sealed class McpTools(string workingDirectory)
{
    public static readonly List<Tool> Definitions =
    [
        new()
        {
            Name = "stats",
            Description = "Count the files under a directory and add up their size.",
            InputSchema = ToolSchema.For(McpJson.Default.StatsArgs),
        },
    ];

    public CallToolResult Call(string name, IDictionary<string, JsonElement>? arguments, CancellationToken ct)
    {
        switch (name)
        {
            case "stats":
                return Stats(Parse(arguments, McpJson.Default.StatsArgs), ct);
            default:
                throw InvalidParams($"unknown tool '{name}'");
        }
    }

    CallToolResult Stats(StatsArgs args, CancellationToken ct)
    {
        var dir = Path.GetFullPath(args.Dir, workingDirectory);
        if (!Directory.Exists(dir))
        {
            throw InvalidParams($"directory '{args.Dir}' does not exist");
        }

        var result = DirectoryStats.Scan(args.Dir, dir, DirectoryStats.DEFAULT_LIMIT, ct);
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = $"{result.Files} files, {result.Bytes} bytes in {args.Dir}" }],
            StructuredContent = JsonSerializer.SerializeToElement(result, McpJson.Default.StatsResult),
        };
    }

    static T Parse<T>(IDictionary<string, JsonElement>? arguments, JsonTypeInfo<T> typeInfo)
    {
        var json = arguments is null
            ? "{}"u8.ToArray()
            : JsonSerializer.SerializeToUtf8Bytes(arguments, McpJson.Default.IDictionaryStringJsonElement);
        try
        {
            return JsonSerializer.Deserialize(json, typeInfo) ?? throw InvalidParams("arguments must be an object");
        }
        catch (JsonException e)
        {
            throw InvalidParams(e.Message);
        }
    }

    static McpProtocolException InvalidParams(string message) => new(message, McpErrorCode.InvalidParams);
}
