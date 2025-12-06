using System.Collections.Generic;
using LittleTreeConverter.Models;
using Tomlyn;
using Tomlyn.Model;

namespace LittleTreeConverter.Serialization;

public sealed class WallpaperTomlWriter
{
    public string Write(WallpaperSource source)
    {
        var root = new TomlTable
        {
            ["scheme"] = source.Scheme,
            ["identifier"] = source.Metadata.Identifier,
            ["name"] = source.Metadata.Name,
            ["version"] = source.Metadata.Version,
            ["refresh_interval_seconds"] = source.Metadata.RefreshIntervalSeconds,
            ["skip_ssl_verify"] = source.Metadata.SkipSslVerify
        };

        if (!string.IsNullOrWhiteSpace(source.Metadata.Description))
        {
            root["description"] = source.Metadata.Description;
        }
        if (!string.IsNullOrWhiteSpace(source.Metadata.Details))
        {
            root["details"] = source.Metadata.Details;
        }
        if (!string.IsNullOrWhiteSpace(source.Metadata.Logo))
        {
            root["logo"] = source.Metadata.Logo;
        }
        if (!string.IsNullOrWhiteSpace(source.Metadata.FooterText))
        {
            root["footer_text"] = source.Metadata.FooterText;
        }

        root["categories"] = BuildCategories(source.Categories);
        root["parameters"] = BuildParameters(source.Parameters);
        root["apis"] = BuildApis(source.Apis);

        return Toml.FromModel(root);
    }

    private static TomlTableArray BuildCategories(IEnumerable<WallpaperCategory> categories)
    {
        var array = new TomlTableArray();
        foreach (var category in categories)
        {
            var table = new TomlTable
            {
                ["id"] = category.Id,
                ["name"] = category.Name,
                ["category"] = category.Category
            };
            if (!string.IsNullOrWhiteSpace(category.Subcategory))
            {
                table["subcategory"] = category.Subcategory;
            }
            if (!string.IsNullOrWhiteSpace(category.Subsubcategory))
            {
                table["subsubcategory"] = category.Subsubcategory;
            }
            array.Add(table);
        }
        return array;
    }

    private static TomlTableArray BuildParameters(IEnumerable<WallpaperParameterPreset> presets)
    {
        var array = new TomlTableArray();
        foreach (var preset in presets)
        {
            var table = new TomlTable
            {
                ["id"] = preset.Id
            };
            if (!string.IsNullOrWhiteSpace(preset.Description))
            {
                table["description"] = preset.Description;
            }
            if (preset.Options.Count > 0)
            {
                var optionsArray = new TomlTableArray();
                foreach (var option in preset.Options)
                {
                    var optionTable = new TomlTable
                    {
                        ["key"] = option.Key,
                        ["type"] = option.Type
                    };
                    if (option.Default is not null)
                    {
                        optionTable["default"] = option.Default;
                    }
                    if (!string.IsNullOrWhiteSpace(option.Label))
                    {
                        optionTable["label"] = option.Label;
                    }
                    if (!string.IsNullOrWhiteSpace(option.Description))
                    {
                        optionTable["description"] = option.Description;
                    }
                    if (option.Choices is not null && option.Choices.Count > 0)
                    {
                        optionTable["choices"] = CreateArray(option.Choices);
                    }
                    if (!string.IsNullOrWhiteSpace(option.Placeholder))
                    {
                        optionTable["placeholder"] = option.Placeholder;
                    }
                    if (option.Hidden)
                    {
                        optionTable["hidden"] = true;
                    }
                    optionsArray.Add(optionTable);
                }
                table["options"] = optionsArray;
            }
            array.Add(table);
        }
        return array;
    }

    private static TomlTableArray BuildApis(IEnumerable<WallpaperApi> apis)
    {
        var array = new TomlTableArray();
        foreach (var api in apis)
        {
            var table = new TomlTable
            {
                ["name"] = api.Name,
                ["format"] = api.Format
            };
            if (!string.IsNullOrWhiteSpace(api.Url))
            {
                table["url"] = api.Url;
            }
            if (!string.IsNullOrWhiteSpace(api.Method))
            {
                table["method"] = api.Method;
            }
            if (!string.IsNullOrWhiteSpace(api.Description))
            {
                table["description"] = api.Description;
            }
            if (!string.IsNullOrWhiteSpace(api.FooterText))
            {
                table["footer_text"] = api.FooterText;
            }
            if (api.CategoryIds.Count == 1)
            {
                table["category_ids"] = api.CategoryIds[0];
            }
            else if (api.CategoryIds.Count > 1)
            {
                table["category_ids"] = CreateArray(api.CategoryIds);
            }
            if (api.CategoryParamMapping.Count > 0)
            {
                var mapping = new TomlTable();
                foreach (var kvp in api.CategoryParamMapping)
                {
                    mapping[kvp.Key] = kvp.Value;
                }
                table["category_param_mapping"] = mapping;
            }
            if (!string.IsNullOrWhiteSpace(api.ParamPresetId))
            {
                table["param_preset_id"] = api.ParamPresetId;
            }
            if (api.Multi is not null && api.Multi.Enabled)
            {
                table["multi"] = new TomlTable
                {
                    ["enabled"] = true,
                    ["items_path"] = api.Multi.ItemsPath
                };
            }
            if (api.FieldMapping is not null)
            {
                var mapping = new TomlTable
                {
                    ["image"] = api.FieldMapping.Image
                };
                if (!string.IsNullOrWhiteSpace(api.FieldMapping.Title))
                {
                    mapping["title"] = api.FieldMapping.Title;
                }
                if (!string.IsNullOrWhiteSpace(api.FieldMapping.Description))
                {
                    mapping["description"] = api.FieldMapping.Description;
                }
                if (!string.IsNullOrWhiteSpace(api.FieldMapping.Copyright))
                {
                    mapping["copyright"] = api.FieldMapping.Copyright;
                }
                table["field_mapping"] = mapping;
            }
            if (api.StaticList is not null && api.StaticList.Urls.Count > 0)
            {
                table["static_list"] = new TomlTable
                {
                    ["urls"] = CreateArray(api.StaticList.Urls)
                };
            }
            if (api.StaticDict is not null && api.StaticDict.Items.Count > 0)
            {
                var itemsArray = new TomlTableArray();
                foreach (var item in api.StaticDict.Items)
                {
                    var itemTable = new TomlTable
                    {
                        ["url"] = item.Url
                    };
                    if (!string.IsNullOrWhiteSpace(item.Title))
                    {
                        itemTable["title"] = item.Title;
                    }
                    if (!string.IsNullOrWhiteSpace(item.Description))
                    {
                        itemTable["description"] = item.Description;
                    }
                    itemsArray.Add(itemTable);
                }
                table["static_dict"] = new TomlTable { ["items"] = itemsArray };
            }

            array.Add(table);
        }
        return array;
    }

    private static TomlArray CreateArray(IEnumerable<string> values)
    {
        var array = new TomlArray();
        foreach (var value in values)
        {
            array.Add(value);
        }
        return array;
    }
}
