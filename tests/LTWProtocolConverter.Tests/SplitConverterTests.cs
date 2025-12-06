using System.Collections.Generic;
using System.Linq;
using LTWProtocolConverter.Converters;
using LTWProtocolConverter.Models;

namespace LTWProtocolConverter.Tests;

public class SplitConverterTests
{
    [Fact]
    public void StaticList_ProducesOneFilePerUrl()
    {
        var source = new WallpaperSource
        {
            Apis = new List<WallpaperApi>
            {
                new WallpaperApi
                {
                    Name = "静态示例",
                    Format = "static_list",
                    StaticList = new StaticListPayload
                    {
                        Urls = new List<string>
                        {
                            "https://example.com/a.jpg",
                            "https://example.com/b.jpg"
                        }
                    }
                }
            }
        };

        var converter = new LittleTreeToApicoreConverter();
        var results = converter.Convert(source, new WallpaperSplitOptions()).ToList();

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.EndsWith(".json", r.FileName));
        Assert.Equal("https://example.com/a.jpg", results[0].Config.Link);
    }
}

