using System.Text.Json.Serialization;
using Starter.Features.Stats;

namespace Starter.Shared.Json;

// every json shape the cli prints goes through this source-generated context
[JsonSerializable(typeof(StatsResult))]
[JsonSerializable(typeof(ErrorLine))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class PrefixJson : JsonSerializerContext;
