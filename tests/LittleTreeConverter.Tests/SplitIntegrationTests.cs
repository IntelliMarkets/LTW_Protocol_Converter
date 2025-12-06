using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Tomlyn;
using Tomlyn.Model;
using LittleTreeConverter.Converters;
using LittleTreeConverter.Models;
using LittleTreeConverter.Parsers;

namespace LittleTreeConverter.Tests;

public class SplitIntegrationTests
{
    [Fact]
    public void SplitProducesEntriesForImageRawAndStaticList()
    {
        const string toml = """
scheme = "littletree_wallpaper_source_v2"
identifier = "test_source"
name = "测试源"
version = "1.0.0"

[[categories]]
id = "image_raw_cat"
name = "Image Raw"
category = "测试"

[[apis]]
name = "Raw API"
url = "https://example.com/raw"
format = "image_raw"
category_ids = ["image_raw_cat"]

[[apis]]
name = "静态列表"
format = "static_list"
category_ids = ["image_raw_cat"]

  [apis.static_list]
  urls = [
    "https://example.com/a.jpg",
    "https://example.com/b.jpg"
  ]
""";

        var tempFile = Path.Combine(Path.GetTempPath(), $"split-test-{Guid.NewGuid():N}.toml");
        File.WriteAllText(tempFile, toml, Encoding.UTF8);

        try
        {
            var document = Toml.Parse(File.ReadAllText(tempFile));
            var table = document.ToModel();
            Assert.True(table.ContainsKey("apis"));
            var apisRaw = Assert.IsType<TomlTableArray>(table["apis"]);
            Assert.NotEmpty(apisRaw);
            Assert.IsType<TomlTable>(apisRaw[0]);
                var probe = new List<string>();
                foreach (var apiTable in apisRaw.OfType<TomlTable>())
                {
                    probe.Add(Convert.ToString(apiTable["name"]) ?? string.Empty);
                }
                Assert.Contains("Raw API", probe);

            var reader = new LittleTreeReader();
            var source = reader.Load(tempFile);
            Assert.NotEmpty(source.Apis);
            var converter = new LittleTreeToApicoreConverter();
            var results = converter.Convert(source, new WallpaperSplitOptions()).ToList();

            Assert.Equal(3, results.Count);
            Assert.Single(results, r => r.Config.Link == "https://example.com/raw");
            Assert.Contains(results, r => r.Config.Link == "https://example.com/a.jpg");
            Assert.Contains(results, r => r.Config.Link == "https://example.com/b.jpg");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
