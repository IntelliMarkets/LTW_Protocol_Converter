using LittleTreeConverter.Utilities;

namespace LittleTreeConverter.Tests;

public class TextUtilitiesTests
{
    [Theory]
    [InlineData("data.items[0].url", "/data/items/url")]
    [InlineData("items", "/items")]
    [InlineData("items[].value", "/items/value")]
    public void ToJsonPointer_NormalizesIndexer(string input, string expected)
    {
        var pointer = TextUtilities.ToJsonPointer(input);
        Assert.Equal(expected, pointer);
    }

    [Theory]
    [InlineData("/data/items/url", "data.items.url")]
    [InlineData("/items", "items")]
    public void FromJsonPointer_RestoresDotPath(string pointer, string expected)
    {
        var path = TextUtilities.FromJsonPointer(pointer);
        Assert.Equal(expected, path);
    }
}
