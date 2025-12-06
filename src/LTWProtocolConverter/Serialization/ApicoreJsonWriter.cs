using System.Text.Json;
using System.Text.Json.Serialization;
using LTWProtocolConverter.Models;

namespace LTWProtocolConverter.Serialization;

public sealed class ApicoreJsonWriter
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string Write(ApicoreConfig config)
        => JsonSerializer.Serialize(config, _options);
}
