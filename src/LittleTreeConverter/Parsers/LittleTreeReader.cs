using System;
using System.Collections.Generic;
using System.Linq;
using LittleTreeConverter.Models;
using Tomlyn;
using Tomlyn.Model;

namespace LittleTreeConverter.Parsers;

public sealed class LittleTreeReader
{
    public WallpaperSource Load(string filePath)
    {
        var text = File.ReadAllText(filePath);
        var doc = Toml.Parse(text, filePath);
        if (doc.HasErrors)
        {
            var errors = string.Join(Environment.NewLine, doc.Diagnostics.Select(d => $"- {d}"));
            throw new InvalidOperationException($"TOML 解析失败:\n{errors}");
        }

        var model = doc.ToModel();
        if (model is not TomlTable table)
        {
            throw new InvalidOperationException("壁纸源文件不包含有效的 TOML 表");
        }

        var metadata = new WallpaperMetadata(
            RequireString(table, "identifier"),
            RequireString(table, "name"),
            RequireString(table, "version"),
            GetString(table, "description"),
            GetString(table, "details"),
            GetString(table, "logo"),
            table.TryGetValue("skip_ssl_verify", out var skip) && Convert.ToBoolean(skip),
            table.TryGetValue("refresh_interval_seconds", out var refresh) ? Convert.ToInt32(refresh) : 0,
            GetString(table, "footer_text"));

        var source = new WallpaperSource
        {
            Scheme = RequireString(table, "scheme"),
            Metadata = metadata,
            Categories = ReadCategories(table),
            Parameters = ReadParameters(table),
            Apis = ReadApis(table)
        };

        return source;
    }

    private static List<WallpaperCategory> ReadCategories(TomlTable table)
    {
        var categories = new List<WallpaperCategory>();
        if (!table.TryGetValue("categories", out var raw))
        {
            return categories;
        }

        foreach (var entry in EnumerateTables(raw))
        {
            categories.Add(new WallpaperCategory(
                RequireString(entry, "id"),
                RequireString(entry, "name"),
                RequireString(entry, "category"),
                GetString(entry, "subcategory"),
                GetString(entry, "subsubcategory")));
        }

        return categories;
    }

    private static List<WallpaperParameterPreset> ReadParameters(TomlTable table)
    {
        var presets = new List<WallpaperParameterPreset>();
        if (!table.TryGetValue("parameters", out var raw))
        {
            return presets;
        }

        foreach (var entry in EnumerateTables(raw))
        {
            var preset = new WallpaperParameterPreset
            {
                Id = RequireString(entry, "id"),
                Description = GetString(entry, "description"),
                Options = ReadParameterOptions(entry)
            };
            presets.Add(preset);
        }

        return presets;
    }

    private static List<WallpaperParameterOption> ReadParameterOptions(TomlTable presetTable)
    {
        if (!presetTable.TryGetValue("options", out var raw))
        {
            return new List<WallpaperParameterOption>();
        }

        var options = new List<WallpaperParameterOption>();
        foreach (var entry in EnumerateTables(raw))
        {
            options.Add(new WallpaperParameterOption
            {
                Key = RequireString(entry, "key"),
                Type = RequireString(entry, "type"),
                Label = GetString(entry, "label"),
                Description = GetString(entry, "description"),
                Default = entry.TryGetValue("default", out var defaultValue) ? defaultValue : null,
                Choices = entry.TryGetValue("choices", out var choicesRaw) && choicesRaw is TomlArray choicesArr
                    ? choicesArr.OfType<string>().ToList()
                    : null,
                Placeholder = GetString(entry, "placeholder"),
                Hidden = entry.TryGetValue("hidden", out var hiddenRaw) && Convert.ToBoolean(hiddenRaw)
            });
        }

        return options;
    }

    private static List<WallpaperApi> ReadApis(TomlTable table)
    {
        var apis = new List<WallpaperApi>();
        if (!table.TryGetValue("apis", out var raw))
        {
            return apis;
        }

        foreach (var entry in EnumerateTables(raw))
        {
            var api = new WallpaperApi
            {
                Name = RequireString(entry, "name"),
                Format = RequireString(entry, "format"),
                Url = GetString(entry, "url"),
                Method = entry.TryGetValue("method", out var method) ? Convert.ToString(method) ?? "GET" : "GET",
                Description = GetString(entry, "description"),
                FooterText = GetString(entry, "footer_text"),
                CategoryIds = ReadStringArray(entry, "category_ids"),
                CategoryParamMapping = ReadMapping(entry, "category_param_mapping"),
                ParamPresetId = GetString(entry, "param_preset_id"),
                Multi = ReadMulti(entry),
                FieldMapping = ReadFieldMapping(entry),
                StaticList = ReadStaticList(entry),
                StaticDict = ReadStaticDict(entry)
            };

            apis.Add(api);
        }

        return apis;
    }

    private static List<string> ReadStringArray(TomlTable table, string key)
    {
        if (!table.TryGetValue(key, out var raw))
        {
            return new List<string>();
        }

        if (raw is string single)
        {
            return new List<string> { single };
        }

        if (raw is TomlArray arr)
        {
            var list = new List<string>();
            foreach (var item in arr)
            {
                var value = Convert.ToString(item);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    list.Add(value);
                }
            }
            return list;
        }

        return new List<string>();
    }

    private static Dictionary<string, string> ReadMapping(TomlTable table, string key)
    {
        if (!table.TryGetValue(key, out var raw) || raw is not TomlTable map)
        {
            return new Dictionary<string, string>();
        }

        return map.Keys.ToDictionary(k => k, k => Convert.ToString(map[k]) ?? string.Empty);
    }

    private static WallpaperMultiConfig? ReadMulti(TomlTable table)
    {
        if (!table.TryGetValue("multi", out var raw) || raw is not TomlTable multi)
        {
            return null;
        }

        return new WallpaperMultiConfig
        {
            Enabled = multi.TryGetValue("enabled", out var enabled) && Convert.ToBoolean(enabled),
            ItemsPath = GetString(multi, "items_path") ?? string.Empty
        };
    }

    private static WallpaperFieldMapping? ReadFieldMapping(TomlTable table)
    {
        if (!table.TryGetValue("field_mapping", out var raw) || raw is not TomlTable fm)
        {
            return null;
        }

        return new WallpaperFieldMapping
        {
            Image = RequireString(fm, "image"),
            Title = GetString(fm, "title"),
            Description = GetString(fm, "description"),
            Copyright = GetString(fm, "copyright")
        };
    }

    private static StaticListPayload? ReadStaticList(TomlTable table)
    {
        if (!table.TryGetValue("static_list", out var raw) || raw is not TomlTable sl)
        {
            return null;
        }

        var urls = new List<string>();
        if (sl.TryGetValue("urls", out var urlsRaw) && urlsRaw is TomlArray arr)
        {
            foreach (var item in arr)
            {
                var value = Convert.ToString(item);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    urls.Add(value);
                }
            }
        }

        return new StaticListPayload { Urls = urls };
    }

    private static StaticDictPayload? ReadStaticDict(TomlTable table)
    {
        if (!table.TryGetValue("static_dict", out var raw) || raw is not TomlTable sd)
        {
            return null;
        }

        if (!sd.TryGetValue("items", out var itemsRaw))
        {
            return new StaticDictPayload();
        }

        var items = new List<StaticDictItem>();
        foreach (var itemTable in EnumerateTables(itemsRaw))
        {
            items.Add(new StaticDictItem
            {
                Url = RequireString(itemTable, "url"),
                Title = GetString(itemTable, "title"),
                Description = GetString(itemTable, "description")
            });
        }

        return new StaticDictPayload { Items = items };
    }

    private static string RequireString(TomlTable table, string key)
    {
        if (!table.TryGetValue(key, out var value) || value is not string str || string.IsNullOrWhiteSpace(str))
        {
            throw new InvalidOperationException($"缺少必要字段: {key}");
        }

        return str;
    }

    private static string? GetString(TomlTable table, string key)
        => table.TryGetValue(key, out var value) ? Convert.ToString(value) : null;

    private static IEnumerable<TomlTable> EnumerateTables(object? raw)
    {
        return raw switch
        {
            TomlTableArray tableArray => tableArray.Cast<TomlTable>(),
            TomlArray arr => arr.OfType<TomlTable>(),
            _ => Enumerable.Empty<TomlTable>()
        };
    }
}
