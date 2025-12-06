namespace LTWProtocolConverter.Models;

public sealed record ApicoreFileResult(string FileName, ApicoreConfig Config);

public sealed record WallpaperFileResult(string FileName, WallpaperSource Source);
