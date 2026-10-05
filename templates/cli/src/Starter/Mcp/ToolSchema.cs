using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;

namespace Starter.Mcp;

static class ToolSchema
{
    static readonly JsonSchemaExporterOptions ExporterOptions = new()
    {
        // otherwise the root object and array items come out nullable
        TreatNullObliviousAsNonNullable = true,
        TransformSchemaNode = CopyDescriptions,
    };

    public static JsonElement For<T>(JsonTypeInfo<T> typeInfo) =>
        JsonElement.Parse(JsonSchemaExporter.GetJsonSchemaAsNode(typeInfo, ExporterOptions).ToJsonString());

    static JsonNode CopyDescriptions(JsonSchemaExporterContext ctx, JsonNode schema)
    {
        if (ctx.PropertyInfo?.AttributeProvider is not { } provider || schema is not JsonObject node)
        {
            return schema;
        }

        foreach (var attribute in provider.GetCustomAttributes(inherit: false))
        {
            if (attribute is DescriptionAttribute description)
            {
                node["description"] = description.Description;
            }
        }
        return node;
    }
}
