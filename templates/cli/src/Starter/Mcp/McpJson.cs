using System.Text.Json;
using System.Text.Json.Serialization;
using Starter.Features.Stats;

namespace Starter.Mcp;

[JsonSerializable(typeof(StatsArgs))]
[JsonSerializable(typeof(IDictionary<string, JsonElement>))]
[JsonSerializable(typeof(StatsResult))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    RespectNullableAnnotations = true)]
partial class McpJson : JsonSerializerContext;
