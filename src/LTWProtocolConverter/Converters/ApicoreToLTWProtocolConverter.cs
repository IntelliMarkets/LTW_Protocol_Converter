using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using LTWProtocolConverter.Models;
using LTWProtocolConverter.Utilities;

namespace LTWProtocolConverter.Converters;

public sealed class ApicoreToLTWProtocolConverter
{
    public WallpaperSource Convert(IEnumerable<ApicoreConfig> configs, ApicoreMergeOptions options)
    {
        var list = configs.ToList();
        if (list.Count == 0)
        {
            throw new InvalidOperationException("至少需要提供一个 APICORE 文件");
        }

        var categories = new List<WallpaperCategory>();
        var parameters = new List<WallpaperParameterPreset>();
        var apis = new List<WallpaperApi>();
        var categoryIndex = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var config in list)
        {
            var slug = TextUtilities.NormalizeIdentifier(config.FriendlyName, "api");
            var categoryId = $"cat_{slug}";
            if (categoryIndex.Add(categoryId))
            {
                categories.Add(new WallpaperCategory(
                    categoryId,
                    config.FriendlyName,
                    options.CategoryRoot,
                    options.DefaultSubcategory));
            }

            var preset = BuildPreset(config, slug);
            var presetId = preset?.Id;
            if (preset is not null && parameters.All(p => !p.Id.Equals(preset.Id, StringComparison.OrdinalIgnoreCase)))
            {
                parameters.Add(preset);
            }

            var (format, fieldMapping, multi) = BuildFormat(config.Response.Image);

            apis.Add(new WallpaperApi
            {
                Name = config.FriendlyName,
                Format = format,
                Url = config.Link,
                Method = config.Func.ToUpperInvariant(),
                Description = config.Intro,
                CategoryIds = new List<string> { categoryId },
                ParamPresetId = presetId,
                FieldMapping = fieldMapping,
                Multi = multi
            });
        }

        return new WallpaperSource
        {
            Scheme = "littletree_wallpaper_source_v2",
            Metadata = options.Metadata,
            Categories = categories,
            Parameters = parameters,
            Apis = apis
        };
    }

    private static WallpaperParameterPreset? BuildPreset(ApicoreConfig config, string slug)
    {
        if (config.Parameters.Count == 0)
        {
            return null;
        }

        var options = new List<WallpaperParameterOption>();
        for (var index = 0; index < config.Parameters.Count; index++)
        {
            var parameter = config.Parameters[index];
            var key = TextUtilities.Slugify(parameter.FriendlyName.Replace(' ', '_'), $"param_{index}");
            var (type, choices) = MapParameterType(parameter);
            options.Add(new WallpaperParameterOption
            {
                Key = key,
                Type = type,
                Label = parameter.FriendlyName,
                Description = null,
                Default = ConvertScalar(parameter.Value),
                Choices = choices,
                Hidden = false
            });
        }

        return new WallpaperParameterPreset
        {
            Id = $"preset_{slug}",
            Description = config.Intro,
            Options = options
        };
    }

    private static (string Type, List<string>? Choices) MapParameterType(ApicoreParameter parameter)
    {
        return parameter.Type switch
        {
            "boolean" => ("boolean", null),
            "enum" => ("choice", ExtractChoices(parameter.Value)),
            "list" => ("choice", ExtractChoices(parameter.Value)),
            _ => ("text", null)
        };
    }

    private static List<string>? ExtractChoices(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                return element.EnumerateArray().Select(item => item.ToString()).ToList();
            }
            return new List<string> { element.ToString() };
        }

        if (value is IEnumerable<object?> enumerable)
        {
            return enumerable.Where(v => v is not null).Select(v => System.Convert.ToString(v) ?? string.Empty).ToList();
        }

        return new List<string> { System.Convert.ToString(value) ?? string.Empty };
    }

    private static object? ConvertScalar(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var i) ? i : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Array => element.EnumerateArray().Select(e => e.ToString()).ToList(),
                JsonValueKind.Object => element.ToString(),
                _ => null
            };
        }

        return value;
    }

    private static (string Format, WallpaperFieldMapping? FieldMapping, WallpaperMultiConfig? Multi) BuildFormat(ApicoreImageConfig image)
    {
        if (string.IsNullOrWhiteSpace(image.Path))
        {
            return ("image_url", null, null);
        }

        var pointer = TextUtilities.ToJsonPointer(image.Path);
        if (string.IsNullOrEmpty(pointer))
        {
            return ("image_url", null, null);
        }

        WallpaperMultiConfig? multi = null;
        if (image.IsList)
        {
            multi = new WallpaperMultiConfig
            {
                Enabled = true,
                ItemsPath = TrimPointer(pointer)
            };
        }

        var mapping = new WallpaperFieldMapping
        {
            Image = pointer
        };

        return ("json", mapping, multi);
    }

    private static string TrimPointer(string pointer)
    {
        var lastSlash = pointer.LastIndexOf('/');
        if (lastSlash <= 0)
        {
            return pointer;
        }

        return pointer[..lastSlash];
    }
}
