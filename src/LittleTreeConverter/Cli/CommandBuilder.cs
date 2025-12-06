using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LittleTreeConverter.Converters;
using LittleTreeConverter.Models;
using LittleTreeConverter.Parsers;
using LittleTreeConverter.Serialization;

namespace LittleTreeConverter.Cli;

public static class CommandBuilder
{
    public static RootCommand Build()
    {
        var root = new RootCommand("Little Tree 壁纸源与 APICORE 转换工具");
        root.AddCommand(BuildMergeCommand());
        root.AddCommand(BuildSplitCommand());
        return root;
    }

    private static Command BuildMergeCommand()
    {
        var command = new Command("merge", "将多个 APICORE JSON 合并为一个小树壁纸源 TOML");

        var inputOption = new Option<FileInfo[]>(
            aliases: new[] { "-i", "--input" },
            description: "APICORE JSON 文件路径，可传入多个")
        { IsRequired = true, AllowMultipleArgumentsPerToken = true };

        var outputOption = new Option<FileInfo>(
            aliases: new[] { "-o", "--output" },
            description: "输出 TOML 文件路径",
            getDefaultValue: () => new FileInfo(Path.Combine(Environment.CurrentDirectory, "wallpaper-source.toml")));

        var identifierOption = new Option<string>("--identifier", "壁纸源 identifier") { IsRequired = true };
        var nameOption = new Option<string>("--name", "壁纸源名称") { IsRequired = true };
        var versionOption = new Option<string>("--version", "壁纸源版本") { IsRequired = true };
        var descriptionOption = new Option<string?>("--description", () => null, "可选描述");
        var detailsOption = new Option<string?>("--details", () => null, "Markdown 详情");
        var logoOption = new Option<string?>("--logo", () => null, "logo URL 或 base64");
        var footerOption = new Option<string?>("--footer", () => null, "底部提示文案");
        var refreshOption = new Option<int>("--refresh", () => 0, "refresh_interval_seconds 数值");
        var skipSslOption = new Option<bool>("--skip-ssl", () => false, "是否跳过 SSL 验证");
        var categoryOption = new Option<string>("--category", () => "APICORE", "分类一级名称");
        var subcategoryOption = new Option<string>("--subcategory", () => "General", "分类二级名称");

        command.AddOption(inputOption);
        command.AddOption(outputOption);
        command.AddOption(identifierOption);
        command.AddOption(nameOption);
        command.AddOption(versionOption);
        command.AddOption(descriptionOption);
        command.AddOption(detailsOption);
        command.AddOption(logoOption);
        command.AddOption(footerOption);
        command.AddOption(refreshOption);
        command.AddOption(skipSslOption);
        command.AddOption(categoryOption);
        command.AddOption(subcategoryOption);

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var inputs = ctx.ParseResult.GetValueForOption(inputOption)!;
            var output = ctx.ParseResult.GetValueForOption(outputOption)!;
            var metadata = new WallpaperMetadata(
                ctx.ParseResult.GetValueForOption(identifierOption)!,
                ctx.ParseResult.GetValueForOption(nameOption)!,
                ctx.ParseResult.GetValueForOption(versionOption)!,
                ctx.ParseResult.GetValueForOption(descriptionOption),
                ctx.ParseResult.GetValueForOption(detailsOption),
                ctx.ParseResult.GetValueForOption(logoOption),
                ctx.ParseResult.GetValueForOption(skipSslOption),
                ctx.ParseResult.GetValueForOption(refreshOption),
                ctx.ParseResult.GetValueForOption(footerOption));

            var mergeOptions = new ApicoreMergeOptions
            {
                Metadata = metadata,
                CategoryRoot = ctx.ParseResult.GetValueForOption(categoryOption)!,
                DefaultSubcategory = ctx.ParseResult.GetValueForOption(subcategoryOption)!
            };

            await MergeAsync(inputs, output, mergeOptions, ctx.GetCancellationToken());
        });

        return command;
    }

    private static Command BuildSplitCommand()
    {
        var command = new Command("split", "将一个小树壁纸源拆分为多个 APICORE JSON 文件");

        var inputOption = new Option<FileInfo>(new[] { "-i", "--input" }, "壁纸源 TOML 文件")
        { IsRequired = true };
        var outputDirOption = new Option<DirectoryInfo>(new[] { "-o", "--output-dir" }, "APICORE 输出目录")
        {
            IsRequired = true
        };
        var prefixOption = new Option<string>("--prefix", () => "apicore", "输出文件名前缀");

        command.AddOption(inputOption);
        command.AddOption(outputDirOption);
        command.AddOption(prefixOption);

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var input = ctx.ParseResult.GetValueForOption(inputOption)!;
            var outputDir = ctx.ParseResult.GetValueForOption(outputDirOption)!;
            var splitOptions = new WallpaperSplitOptions
            {
                OutputPrefix = ctx.ParseResult.GetValueForOption(prefixOption)!,
                IncludeCategoryInName = false
            };

            await SplitAsync(input, outputDir, splitOptions, ctx.GetCancellationToken());
        });

        return command;
    }

    private static async Task MergeAsync(
        IEnumerable<FileInfo> inputs,
        FileInfo output,
        ApicoreMergeOptions options,
        CancellationToken cancellationToken)
    {
        var reader = new ApicoreReader();
        var converter = new ApicoreToLittleTreeConverter();
        var writer = new WallpaperTomlWriter();

        var configs = inputs.Select(file => reader.Load(file.FullName)).ToList();
        var source = converter.Convert(configs, options);
        var toml = writer.Write(source);

        Directory.CreateDirectory(output.DirectoryName ?? Environment.CurrentDirectory);
        await File.WriteAllTextAsync(output.FullName, toml, Encoding.UTF8, cancellationToken);
        Console.WriteLine($"已生成壁纸源：{output.FullName}");
    }

    private static async Task SplitAsync(
        FileInfo input,
        DirectoryInfo outputDir,
        WallpaperSplitOptions options,
        CancellationToken cancellationToken)
    {
        var reader = new LittleTreeReader();
        var converter = new LittleTreeToApicoreConverter();
        var writer = new ApicoreJsonWriter();

        var source = reader.Load(input.FullName);
        var results = converter.Convert(source, options).ToList();

        Directory.CreateDirectory(outputDir.FullName);
        foreach (var result in results)
        {
            var path = Path.Combine(outputDir.FullName, result.FileName);
            var json = writer.Write(result.Config);
            await File.WriteAllTextAsync(path, json, Encoding.UTF8, cancellationToken);
            Console.WriteLine($"已生成 APICORE：{path}");
        }

        if (results.Count == 0)
        {
            Console.WriteLine("未找到任何可导出的 API");
        }
    }

}
