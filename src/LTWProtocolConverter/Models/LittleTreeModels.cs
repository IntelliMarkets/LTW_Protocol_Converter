using System.Collections.Generic;

namespace LTWProtocolConverter.Models;

public sealed record WallpaperMetadata(
    string Identifier,
    string Name,
    string Version,
    string? Description = null,
    string? Details = null,
    string? Logo = null,
    bool SkipSslVerify = false,
    int RefreshIntervalSeconds = 0,
    string? FooterText = null
);

public sealed record WallpaperSource
{
    public string Scheme { get; init; } = "littletree_wallpaper_source_v2";
    public WallpaperMetadata Metadata { get; init; } = new("auto_identifier", "Auto Name", "1.0.0");
    public List<WallpaperCategory> Categories { get; init; } = [];
    public List<WallpaperParameterPreset> Parameters { get; init; } = [];
    public List<WallpaperApi> Apis { get; init; } = [];
}

public sealed record WallpaperCategory
(
    string Id,
    string Name,
    string Category,
    string? Subcategory = null,
    string? Subsubcategory = null
);

public sealed record WallpaperParameterPreset
{
    public string Id { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<WallpaperParameterOption> Options { get; init; } = [];
}

public sealed record WallpaperParameterOption
{
    public string Key { get; init; } = string.Empty;
    public string Type { get; init; } = "text";
    public string? Label { get; init; }
    public string? Description { get; init; }
    public object? Default { get; init; }
    public List<string>? Choices { get; init; }
    public string? Placeholder { get; init; }
    public bool Hidden { get; init; }
}

public sealed record WallpaperFieldMapping
{
    public string Image { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Copyright { get; init; }
}

public sealed record WallpaperMultiConfig
{
    public bool Enabled { get; init; }
    public string ItemsPath { get; init; } = string.Empty;
}

public sealed record WallpaperApi
{
    public string Name { get; init; } = string.Empty;
    public string Format { get; init; } = "json";
    public string? Url { get; init; }
    public string Method { get; init; } = "GET";
    public string? Description { get; init; }
    public string? FooterText { get; init; }
    public WallpaperMultiConfig? Multi { get; init; }
    public WallpaperFieldMapping? FieldMapping { get; init; }
    public List<string> CategoryIds { get; init; } = [];
    public Dictionary<string, string> CategoryParamMapping { get; init; } = new();
    public string? ParamPresetId { get; init; }
    public StaticListPayload? StaticList { get; init; }
    public StaticDictPayload? StaticDict { get; init; }
}

public sealed record StaticListPayload
{
    public List<string> Urls { get; init; } = [];
}

public sealed record StaticDictPayload
{
    public List<StaticDictItem> Items { get; init; } = [];
}

public sealed record StaticDictItem
{
    public string Url { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? Description { get; init; }
}
