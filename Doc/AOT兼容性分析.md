# NewLife.Cube AOT 兼容性现状与边界

> 基线：NewLife.Core 12 / NewLife.Cube 6.15｜评估方式：**NativeAOT 真实编译 + 运行实测**（win-x64，.NET 10）｜冒烟工程：`AotSmoke/`

## 1. 现状摘要

- **非 MVC 基座实测通过**：`AotSmoke` 16 个场景 JIT / NativeAOT 双通，覆盖本地化查表、`ApiResponse` 包装与 STJ 源生成序列化、格式校验、威胁检测、浏览器特性解析、空富文本判断、页面设置。
- **多语言词表 AOT 安全**：`Locales/*.yml` 在编译期生成压缩字节表，运行期只做字典查找，不依赖 `ResourceManager` 与卫星程序集。
- **不承诺整站 AOT**：魔方以 ASP.NET Core MVC + Razor 视图为核心，MVC/Razor 官方不支持 NativeAOT；插件扫描、动态控制器发现、XCode 动态映射同样属于动态能力边界。
- **CI 防回归**：`.github/workflows/aot.yml` 对冒烟工程执行分析器零告警（IL2026/IL2075/IL3050 等）与多 RID 原生发布运行；win-x64 / linux-x64 阻断，其余平台观察期。

## 2. 支持矩阵

| 能力 | JIT | NativeAOT | 说明 |
|------|-----|-----------|------|
| 本地化词表与 `Localization.Translate` | ✅ | ✅ | 编译期表 + 字典查找 |
| `ApiResponse` 包装 | ✅ | ✅ | 序列化需配合 STJ 源生成上下文 |
| 数据校验、威胁检测、UA 解析、空富文本 | ✅ | ✅ | 纯托管逻辑 |
| `PageSetting` 等配置模型 | ✅ | ✅ | 不含配置提供者初始化 |
| MVC 控制器 / Razor 视图 | ✅ | ❌ | 框架不支持 |
| 主题/插件动态加载（`ModuleManager`） | ✅ | ⚠️ 边界 | `Assembly.Load`、`Type.GetType`、构造器反射 |
| XCode 实体动态映射与高级查询 | ✅ | ⚠️ 边界 | 依赖 Model.xml 与反射元数据 |
| 动态控制器发现、`ControllerFactory` 反射 | ✅ | ❌ | 运行时类型扫描 |

## 3. 已知分析器告警分类

库整体开启 `IsAotCompatible` 时约 110 条告警，集中在：

| 类别 | 代表位置 | 处置 |
|------|---------|------|
| 插件/类型动态加载（IL2026/IL2057/IL2072） | `Modules/ModuleManager.cs`、`Services/LovRegistry.cs` | 设计性动态能力，边界方法声明 `[RequiresUnreferencedCode]` 或运行期守卫 |
| 构造器/成员反射（IL2075/IL2090/IL2091） | `Membership/MenuHelper.cs`、`ViewModels/*`、`EntityController*` | 元数据驱动能力，AOT 下需改用静态注册或源生成 |
| `Assembly.Location`（IL3000） | `Extensions/CubeEmbeddedFileProvider.cs`、`Areas/Admin/Controllers/IndexController.cs` | 改用 `AppContext.BaseDirectory` |
| 动态代码（IL3050） | `Services/ExternalAuthService.cs`、`ViewModels/ExpandField.cs` | 表达式/动态调用，AOT 下必须关闭对应功能 |

> 这些告警不是“待修 bug”，而是 MVC 框架与动态扩展能力的设计边界。整站 AOT 不在承诺范围内；对 AOT 有要求的宿主应只使用非 MVC 基座能力。

## 4. 下游使用指引

1. 以 NativeAOT 发布时，引用 `NewLife.Cube` 仅使用 `ApiResponseHelper`、`ValidFormatHelper`、`ThreatDetector`、`UserAgentParser`、`EmptyHtmlHelper`、`PageSetting`、`Localization` 等非 MVC 能力。
2. JSON 序列化使用 `JsonSerializerContext` 源生成；冒烟工程已给出 `ApiResponse<String>` 示例。
3. 若必须使用 MVC/Razor，请使用 JIT 发布（默认 `dotnet publish`）。
4. 遇到 IL2026/IL3050 告警：先确认调用链是否在 AOT 主路径；是则改用泛型/静态入口，否则在边界方法显式声明裁剪语义。

## 5. 冒烟场景

| # | 场景 | 断言 |
|---|------|------|
| 1-4 | 本地化：zh-Hans / zh-Hant / en / 未知语言回退 | 译文与回退正确 |
| 5-7 | `ApiResponse` 成功/失败包装 + STJ 源生成序列化 | Code/Message/Data 正确 |
| 8-10 | 邮箱、手机号格式校验 | 合法/非法判定正确 |
| 11-12 | 威胁检测命中与放行 | SQL 注入命中、正常请求不误报 |
| 13 | 浏览器特性解析 | 解析成功 |
| 14-15 | 空富文本判断 | 空段落判空、正常文本不误判 |
| 16 | 页面设置克隆 | 返回独立实例 |

运行：

```bash
dotnet run -c Release --project AotSmoke/AotSmoke.csproj          # JIT 基线
dotnet publish -c Release -r win-x64 -o AotSmoke/bin/aot-out      # 原生发布
./AotSmoke/bin/aot-out/AotSmoke.exe                              # 运行冒烟
```
