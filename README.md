# LTWProtocolConverter

LTWProtocolConverter 是一个基于 .NET 的命令行工具，可在 APICORE JSON 配置与小树壁纸源（Little Tree Wallpaper Next v2.0）TOML 文件之间进行双向转换。

> [!TIP]
> 您可以在 https://github.com/IntelliMarkets/LTW_Protocol_Converter/actions 获取最新的开发构建

## 功能

- **merge**：将多个 APICORE JSON 按照约定合并为一个壁纸源（内置 APICORE Schema，无需额外文件）。
- **split**：将一个壁纸源文件拆分为多个 APICORE JSON，静态类型（`static_list`/`static_dict`）会展开为多份配置，直接以图片链接填充 `link` 字段并移除参数。
- 自动在工具内部使用内置 APICORE Schema 做输入校验，无需准备外部 schema 文件。

## 快速开始

```powershell
# 进入仓库根目录
pwsh -Command "dotnet run --project src/LTWProtocolConverter -- --help"
```

### 合并 APICORE → 壁纸源

```powershell
dotnet run --project src/LTWProtocolConverter -- merge `
  -i apis/bing.json apis/wallhaven.json `
  --identifier littletree_auto `
  --name "示例壁纸源" `
  --version 1.0.0 `
  --category APICORE `
  --subcategory General `
  -o build/wallpaper.toml
```

### 拆分 壁纸源 → APICORE

```powershell
dotnet run --project src/LTWProtocolConverter -- split `
  -i samples/wallpaper.toml `
  -o build/apicore `
  --prefix wallpaper
```

## 约定

- 合并时按照 APICORE `friendly_name` 生成分类与 API 名称，统一一级分类/二级分类可通过 `--category` 与 `--subcategory` 设定。
- APICORE 参数类型自动映射到壁纸源参数：`boolean`→`boolean`，`enum/list`→`choice`，其他类型→`text`。
- 拆分时，如果 API 使用参数预设则取 `param_preset_id`（若缺失则取 `category_param_mapping` 中的首个预设）。
- `static_list`、`static_dict` 会被展开为多份 APICORE JSON，`link` 直接指向图片地址，`parameters` 留空。
