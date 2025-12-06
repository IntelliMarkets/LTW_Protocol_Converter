using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LTWProtocolConverter.Models;

public static class ApicoreConstants
{
    public const string CurrentVersion = "1.0";
}

public sealed record ApicoreConfig
{
    [JsonPropertyName("friendly_name")]
    public string FriendlyName { get; init; } = string.Empty;

    [JsonPropertyName("intro")]
    public string? Intro { get; init; }

    [JsonPropertyName("icon")]
    public string? Icon { get; init; }

    [JsonPropertyName("link")]
    public string Link { get; init; } = string.Empty;

    [JsonPropertyName("func")]
    public string Func { get; init; } = "GET";

    [JsonPropertyName("APICORE_version")]
    public string Version { get; init; } = ApicoreConstants.CurrentVersion;

    [JsonPropertyName("parameters")]
    public List<ApicoreParameter> Parameters { get; init; } = [];

    [JsonPropertyName("response")]
    public ApicoreResponse Response { get; init; } = new();
}

public sealed record ApicoreParameter
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "string";

    [JsonPropertyName("required")]
    public bool Required { get; init; }

    [JsonPropertyName("value")]
    public object? Value { get; init; }

    [JsonPropertyName("friendly_name")]
    public string FriendlyName { get; init; } = string.Empty;

    [JsonPropertyName("min_value")]
    public double? MinValue { get; init; }

    [JsonPropertyName("max_value")]
    public double? MaxValue { get; init; }

    [JsonPropertyName("split_str")]
    public string? Split { get; init; }
}

public sealed record ApicoreResponse
{
    [JsonPropertyName("image")]
    public ApicoreImageConfig Image { get; init; } = new();

    [JsonPropertyName("others")]
    public List<ApicoreOtherGroup> Others { get; init; } = [];
}

public sealed record ApicoreImageConfig
{
    [JsonPropertyName("content_type")]
    public string ContentType { get; init; } = "URL";

    [JsonPropertyName("path")]
    public string? Path { get; init; }

    [JsonPropertyName("is_list")]
    public bool IsList { get; init; }

    [JsonPropertyName("is_base64")]
    public bool IsBase64 { get; init; }
}

public sealed record ApicoreOtherGroup
{
    [JsonPropertyName("friendly_name")]
    public string FriendlyName { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public List<ApicoreOtherData> Data { get; init; } = [];
}

public sealed record ApicoreOtherData
{
    [JsonPropertyName("friendly_name")]
    public string FriendlyName { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;
}
