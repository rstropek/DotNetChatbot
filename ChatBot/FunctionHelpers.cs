using System.Text.Json;
using System.Text.Json.Schema;

namespace ChatBot;

public static class FunctionHelpers
{
    static readonly JsonSchemaExporterOptions exporterOptions = new()
    {
        TreatNullObliviousAsNonNullable = true,
    };

    public static BinaryData ToJsonSchema<T>() =>
        BinaryData.FromString(
            JsonSerializerOptions.Default.GetJsonSchemaAsNode(
                typeof(T),
                exporterOptions).ToString());
}
