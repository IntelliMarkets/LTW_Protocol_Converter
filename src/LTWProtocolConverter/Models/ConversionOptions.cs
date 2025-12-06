namespace LTWProtocolConverter.Models;

public sealed record ApicoreMergeOptions
{
    public WallpaperMetadata Metadata { get; init; } = new("auto_identifier", "Auto Name", "1.0.0");
    public string CategoryRoot { get; init; } = "APICORE";
    public string DefaultSubcategory { get; init; } = "General";
}

public sealed record WallpaperSplitOptions
{
    public string OutputPrefix { get; init; } = "apicore";
    public bool IncludeCategoryInName { get; init; } = true;
}
