using System.Collections.Generic;
using System.Linq;
using LTWProtocolConverter.Models;
using LTWProtocolConverter.Utilities;

namespace LTWProtocolConverter.Converters;

public sealed class LittleTreeToApicoreConverter
{
    public IEnumerable<ApicoreFileResult> Convert(WallpaperSource source, WallpaperSplitOptions options)
    {
        var presetIndex = source.Parameters.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var api in source.Apis)
        {
            switch (api.Format)
            {
                case "static_list" when api.StaticList is not null:
                    foreach (var result in ConvertStaticList(api, options))
                    {
                        yield return result;
                    }
                    break;
                case "static_dict" when api.StaticDict is not null:
                    foreach (var result in ConvertStaticDict(api, options))
                    {
                        yield return result;
                    }
                    break;
                default:
                    yield return ConvertDynamicApi(api, presetIndex, options);
                    break;
            }
        }
    }

    private static IEnumerable<ApicoreFileResult> ConvertStaticList(WallpaperApi api, WallpaperSplitOptions options)
    {
        if (api.StaticList is null || api.StaticList.Urls.Count == 0)
        {
            yield break;
        }

        for (var index = 0; index < api.StaticList.Urls.Count; index++)
        {
            var url = api.StaticList.Urls[index];
            yield return BuildDirectImageConfig(api, url, options, index + 1);
        }
    }

    private static IEnumerable<ApicoreFileResult> ConvertStaticDict(WallpaperApi api, WallpaperSplitOptions options)
    {
        if (api.StaticDict is null || api.StaticDict.Items.Count == 0)
        {
            yield break;
        }

        for (var index = 0; index < api.StaticDict.Items.Count; index++)
        {
            var item = api.StaticDict.Items[index];
            yield return BuildDirectImageConfig(api, item.Url, options, index + 1);
        }
    }

    private static ApicoreFileResult BuildDirectImageConfig(WallpaperApi api, string url, WallpaperSplitOptions options, int order)
    {
        var fileName = ComposeFileName(api, options, order);
        var config = new ApicoreConfig
        {
            FriendlyName = order == 1 ? api.Name : $"{api.Name} #{order}",
            Link = url,
            Func = "GET",
            Version = ApicoreConstants.CurrentVersion,
            Parameters = new List<ApicoreParameter>(),
            Response = new ApicoreResponse
            {
                Image = new ApicoreImageConfig
                {
                    ContentType = "URL",
                    Path = string.Empty,
                    IsList = false,
                    IsBase64 = false
                },
                Others = new List<ApicoreOtherGroup>()
            }
        };

        return new ApicoreFileResult(fileName, config);
    }

    private static ApicoreFileResult ConvertDynamicApi(
        WallpaperApi api,
        IReadOnlyDictionary<string, WallpaperParameterPreset> presetIndex,
        WallpaperSplitOptions options)
    {
        var fileName = ComposeFileName(api, options, null);
        var parameters = BuildParameters(api, presetIndex);
        var response = BuildResponse(api);

        var config = new ApicoreConfig
        {
            FriendlyName = api.Name,
            Link = api.Url ?? string.Empty,
            Func = api.Method,
            Intro = api.Description,
            Version = ApicoreConstants.CurrentVersion,
            Parameters = parameters,
            Response = response
        };

        return new ApicoreFileResult(fileName, config);
    }

    private static List<ApicoreParameter> BuildParameters(
        WallpaperApi api,
        IReadOnlyDictionary<string, WallpaperParameterPreset> presetIndex)
    {
        if (api.ParamPresetId is null && api.CategoryParamMapping.Count == 0)
        {
            return new List<ApicoreParameter>();
        }

        var presetId = api.ParamPresetId ?? api.CategoryParamMapping.Values.FirstOrDefault();
        if (presetId is null || !presetIndex.TryGetValue(presetId, out var preset))
        {
            return new List<ApicoreParameter>();
        }

        var parameters = new List<ApicoreParameter>();
        foreach (var option in preset.Options)
        {
            parameters.Add(new ApicoreParameter
            {
                FriendlyName = option.Label ?? option.Key,
                Required = false,
                Type = option.Type switch
                {
                    "boolean" => "boolean",
                    "choice" => "enum",
                    _ => "string"
                },
                Value = option.Default ?? option.Choices?.FirstOrDefault() ?? string.Empty
            });
        }

        return parameters;
    }

    private static ApicoreResponse BuildResponse(WallpaperApi api)
    {
        var image = new ApicoreImageConfig();
        var others = new List<ApicoreOtherGroup>();

        switch (api.Format)
        {
            case "image_url":
                image = new ApicoreImageConfig { ContentType = "URL" };
                break;
            case "image_raw":
                image = new ApicoreImageConfig { ContentType = "BINARY" };
                break;
            case "image_base64":
                image = new ApicoreImageConfig { ContentType = "BASE64", IsBase64 = true };
                break;
            case "static_list":
            case "static_dict":
                image = new ApicoreImageConfig { ContentType = "BINARY" };
                break;
            default:
                var pointer = api.FieldMapping?.Image ?? string.Empty;
                image = new ApicoreImageConfig
                {
                    ContentType = "URL",
                    Path = TextUtilities.FromJsonPointer(pointer),
                    IsList = api.Multi?.Enabled ?? false
                };

                var metadata = new List<ApicoreOtherData>();
                if (!string.IsNullOrWhiteSpace(api.FieldMapping?.Title))
                {
                    metadata.Add(new ApicoreOtherData
                    {
                        FriendlyName = "title",
                        Path = TextUtilities.FromJsonPointer(api.FieldMapping!.Title)
                    });
                }
                if (!string.IsNullOrWhiteSpace(api.FieldMapping?.Description))
                {
                    metadata.Add(new ApicoreOtherData
                    {
                        FriendlyName = "description",
                        Path = TextUtilities.FromJsonPointer(api.FieldMapping!.Description)
                    });
                }
                if (!string.IsNullOrWhiteSpace(api.FieldMapping?.Copyright))
                {
                    metadata.Add(new ApicoreOtherData
                    {
                        FriendlyName = "copyright",
                        Path = TextUtilities.FromJsonPointer(api.FieldMapping!.Copyright)
                    });
                }
                if (metadata.Count > 0)
                {
                    others.Add(new ApicoreOtherGroup
                    {
                        FriendlyName = "metadata",
                        Data = metadata
                    });
                }
                break;
        }

        return new ApicoreResponse
        {
            Image = image,
            Others = others
        };
    }

    private static string ComposeFileName(WallpaperApi api, WallpaperSplitOptions options, int? order)
    {
        var slug = TextUtilities.Slugify(api.Name, "api");
        var suffix = order.HasValue ? $"_{order.Value:D2}" : string.Empty;
        var prefix = options.OutputPrefix;
        return $"{prefix}_{slug}{suffix}.json";
    }
}
