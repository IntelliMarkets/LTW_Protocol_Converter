using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using LittleTreeConverter.Models;
using LittleTreeConverter.Resources;

namespace LittleTreeConverter.Parsers;

public sealed class ApicoreReader
{
    private readonly JsonSchema _schema;
    private readonly JsonSerializerOptions _serializerOptions;

    public ApicoreReader()
    {
        _schema = JsonSchema.FromText(SchemaDefinitions.ApicoreSchema);
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }

    public ApicoreConfig Load(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var node = JsonNode.Parse(json) ?? throw new InvalidOperationException("无法解析 APICORE JSON");
        var result = _schema.Evaluate(node, new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (!result.IsValid)
        {
            var errors = result.Details is null
                ? Array.Empty<string>()
                : result.Details
                    .Where(d => d.HasErrors && d.Errors is not null)
                    .SelectMany(d => d.Errors!)
                    .Select(e => $"- {e.Key}: {e.Value}")
                    .ToArray();
            var message = errors.Length == 0 ? "未知错误" : string.Join(Environment.NewLine, errors);
            throw new InvalidOperationException($"文件 {filePath} 未通过 APICORE schema 校验:\n{message}");
        }

        var config = JsonSerializer.Deserialize<ApicoreConfig>(json, _serializerOptions);
        if (config is null)
        {
            throw new InvalidOperationException($"无法读取 APICORE 文件: {filePath}");
        }

        return config;
    }
}
