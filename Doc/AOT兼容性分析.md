# NewLife.Cube AOT 兼容性现状与边界

> 基线：NewLife.Core 12 / NewLife.Cube 6.15｜评估方式：**NativeAOT 真实编译 + 运行实测**（win-x64，.NET 10）｜冒烟工程：`AotSmoke/`

## 1. 现状摘要

- **非 MVC 基座实测通过**：`AotSmoke` 18 个场景 JIT / NativeAOT 双通，覆盖多语言查表（中/繁/英/德/印地/回退）、`ApiResponse` 包装与 STJ 源生成序列化、格式校验、威胁检测、浏览器特性解析、空富文本判断、页面设置。
- **多语言词表 AOT 安全**：`Locales/*.yml` 在编译期生成压缩字节表，运行期只做字典查找，不依赖 `ResourceManager` 与卫星程序集。
- **不承诺整站 AOT**：魔方以 ASP.NET Core MVC + Razor 视图为核心，MVC/Razor 官方不支持 NativeAOT；插件扫描、动态控制器发现、XCode 动态映射同样属于动态能力边界。
- **CI 防回归**：`.github/workflows/aot.yml` 对冒烟工程执行分析器零告警（IL2026/IL2075/IL3050 等）与多 RID 原生发布运行；win-x64 / linux-x64 阻断，其余平台观察期。多 RID 与指令集覆盖矩阵见 §6。

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
| 1-6 | 本地化：zh-Hans / zh-Hant / en / de / hi / 未知语言回退 | 译文与回退正确 |
| 7-9 | `ApiResponse` 成功/失败包装 + STJ 源生成序列化 | Code/Message/Data 正确 |
| 10-12 | 邮箱、手机号格式校验 | 合法/非法判定正确 |
| 13-14 | 威胁检测命中与放行 | SQL 注入命中、正常请求不误报 |
| 15 | 浏览器特性解析 | 解析成功 |
| 16-17 | 空富文本判断 | 空段落判空、正常文本不误判 |
| 18 | 页面设置克隆 | 返回独立实例 |

运行：

```bash
dotnet run -c Release --project AotSmoke/AotSmoke.csproj          # JIT 基线
dotnet publish -c Release -r win-x64 -o AotSmoke/bin/aot-out      # 原生发布
./AotSmoke/bin/aot-out/AotSmoke.exe                              # 运行冒烟
```

## 6. 多 RID 与指令集覆盖

> 定位：魔方是 Web 平台，**不覆盖 Android / iOS 等应用模型目标**（没有对应的测试宿主）；服务端指令集则要真机或仿真跑过。
> 分层：x64 / arm64 原生 runner 跑单测与 E2E；其余指令集跑「JIT 冒烟 + AOT 原生发布」。参照 NewLife.Core 的同一套配方。

### 6.1 覆盖矩阵

| 层级 | 目标 | 单测 / E2E | AOT 冒烟 | 载体 | 状态 |
|---|---|---|---|---|---|
| T1 主流 | win-x64 | ✔ 单测 + E2E | ✔ | windows-latest | **阻断门禁** |
| T1 主流 | linux-x64 | ✔ 单测 + E2E | ✔ | ubuntu-latest | **阻断门禁** |
| T1 主流 | linux-arm64 | ✔ 单测 | ✔ | ubuntu-24.04-arm | 观察期 |
| T1 主流 | win-arm64 | ✔ 单测 | ✔ | windows-11-arm | 观察期 |
| T1 主流 | osx-arm64 / osx-x64 | ✔ 单测（arm64 另跑 E2E） | ✔ | macos-latest / macos-15-intel | 观察期 |
| T1 主流 | linux-musl-x64 / arm64 | —（数据库驱动按 glibc 原生件分发） | ✔ alpine 容器 | ubuntu(-arm) + alpine:3.20 | 观察期 |
| T2 边缘 | win-x86 | —（32 位宿主无托管 runner） | ✔ 交叉发布 + WoW64 运行 | windows-latest | 观察期容错 |
| T2 边缘 | linux-arm（32 位） | 指令集 JIT 冒烟 | — | QEMU + arm32 容器 + dotnet-install | 观察期容错 |
| T2 边缘 | linux-riscv64 | 指令集 JIT 冒烟 | —（无 runtime pack / AOT 后端） | QEMU + 社区 SDK | 观察期容错 |
| T2 边缘 | linux-s390x | 指令集 JIT 冒烟 | —（无 AOT 后端） | QEMU + Ubuntu 官方运行时 | 观察期容错 |
| T3 LA64 | linux-loongarch64 | 指令集 JIT 冒烟（人工触发） | — | QEMU + 龙芯 SDK | 观察期容错 |

- **阻断门禁**：win-x64、linux-x64 不带 `continue-on-error`；其余 job 一律观察期，连续稳定全绿后逐项转阻断。
- **触发分层**：linux-arm / linux-riscv64 / linux-s390x 冒烟随 push/PR 执行；linux-loongarch64 仅 `workflow_dispatch` 人工触发（龙芯 SDK 240MB、QEMU 较慢，与 NewLife.Core 同策略）。
- **AOT 矩阵**（`aot.yml`）：win-x64 / linux-x64 阻断；win-arm64、linux-arm64、osx 双架构、linux-musl-x64 / musl-arm64、win-x86 观察期。
- **为什么非 x64/arm64 不跑单测 / E2E**：魔方的集成用例依赖 SQLite 原生驱动与 Chromium，两者都要按目标架构单独取件，CI 无对应渠道；这些指令集改为在容器内跑两个冒烟件——`AotSmoke`（18 个纯托管场景：本地化、`ApiResponse` 序列化、格式校验、威胁检测、UA 解析、页面设置）与 `WebSmoke`（启动 Kestrel、动态端口绑定并完成一次 HTTP 往返，验证 Web 宿主栈）。两者均由宿主 x64 SDK 构建（IL 与 RID 无关），容器内只提供目标架构运行时。生产部署到这些指令集时应改用 MySQL / PostgreSQL / SqlServer 等托管驱动，或自备对应架构的 SQLite 原生库。

### 6.2 指令集取舍

| 平台 | 运行时渠道（已核实） | 独特价值 | 结论 |
|---|---|---|---|
| linux-riscv64 | 社区构建 [filipnavara/dotnet-riscv](https://github.com/filipnavara/dotnet-riscv) 跟踪 dotnet VMR 全量构建，已发 `10.0.113`（glibc / musl 两档）；官方不发 runtime pack | 新 JIT 后端 + 弱内存模型；国产硬件主线；版本与仓库 net10 对齐 | 已接入（观察期） |
| linux-s390x | .NET 10 官方支持清单在 RHEL / CentOS Stream 明确列出 s390x；Ubuntu 24.04 `noble-updates` 提供 `dotnet-runtime-10.0` / `aspnetcore-runtime-10.0` | **大端**：验证多语言压缩词表、序列化与协议解析中的字节序假设 | 已接入（观察期） |
| linux-loongarch64 | 龙芯 [loongson/dotnet](https://github.com/loongson/dotnet) 提供 VMR 源码构建的 SDK（glibc / musl 两档） | 国产指令集主线；官方无 runner / runtime pack | 已接入（人工触发） |
| linux-arm（32 位） | 官方 `dotnet-sdk-10.0.x-linux-arm.tar.gz` 与 `aspnetcore-runtime-10.0-linux-arm.tar.gz` 均在发布 | 边缘网关 / 嵌入式 Linux | 已接入（观察期） |
| win-x86 | 官方 NativeAOT 目标，WoW64 可直接运行 | 32 位 Windows 存量环境 | 已接入（观察期） |
| Android / iOS | 应用模型目标，与 Web 平台无关 | — | **不做**（与 NewLife.Core 的移动端矩阵区分） |
| tvOS / macCatalyst / WASM | 应用模型类目标，魔方无对应宿主 | — | 不做 |
| linux-ppc64le | QEMU 下 .NET 的 `(Int32)double` 转换错值（NewLife.Core 实测），仿真结果不可信；无托管 runner | 与大端场景重叠 | 不做（需真机再议） |
| MIPS64 | .NET（Core/5+）从未支持，龙芯已迁移到 LoongArch | 无新增覆盖面 | **不做，不再讨论** |

### 6.3 本地复跑

QEMU job 的完整命令见 `.github/workflows/test.yml`，本地有 Docker 时可直接执行对应 `docker run` 段；容器内均为「宿主构建 IL + 目标架构运行时执行」，无需目标架构 SDK 完整可用。
