# NewLife.CubeNC MVC E2E 自动化测试

> 适用范围：第二代魔方（NewLife.CubeNC），启动项目 CubeSSO，监听 http://localhost:8080

---

## 快速开始

### 前置条件

| 工具 | 最低版本 | 说明 |
|------|---------|------|
| .NET SDK | 10.0 | 编译 + 运行脚本 |
| Playwright | 随 NuGet 包安装 | 浏览器自动化 |
| Chromium | 随 playwright install 安装 | 默认测试浏览器 |

### 一键运行

```bash
# 在仓库根目录执行，构建 + 启动 + 测试 + 生成报告，全流程自动完成
dotnet run Test/e2e-mvc.cs
```

执行后自动完成：
1. 清空并重建 `Bin/E2ETest/Run-{yyyyMMdd-HHmmss}/` 测试输出目录
2. 编译并 publish CubeSSO 到输出目录（独立纯净环境）
3. 后台启动 CubeSSO 进程，等待 http://localhost:8080 就绪
4. 调用 `dotnet test` 运行 `Test/E2EMvcTest/` 中全部测试
5. 生成 HTML 报告 + TRX 报告
6. 清理后台进程

### 查看报告

```
Bin/E2ETest/Run-20260523-143022/
├── report.html          # 人类可读 HTML 报告（含失败截图）
├── results.trx          # 机器可读 TRX 报告（CI/Copilot 解析）
├── screenshots/         # 失败测试截图
└── app/                 # CubeSSO 运行目录
    └── Data/            # SQLite 数据库（测试后可直接查询验证）
```

### 仅运行特定分组

```bash
# 仅运行认证测试（Session A）
dotnet test Test/E2EMvcTest/ --filter Category=Auth

# 仅运行菜单导航测试（Session B）
dotnet test Test/E2EMvcTest/ --filter Category=Menu

# 仅运行用户信息测试（Session C）
dotnet test Test/E2EMvcTest/ --filter Category=UserProfile
```

### 手动安装 Playwright 浏览器（首次使用）

```bash
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium
```

---

## 目录导航

| 文档 | 说明 |
|------|------|
| [E2E测试规划.md](E2E测试规划.md) | 技术选型、架构设计、会话策略、报告格式 |
| [测试用例清单.md](测试用例清单.md) | 全部测试用例，含优先级、验证手段 |

## 相关路径

| 路径 | 说明 |
|------|------|
| `Test/E2EMvcTest/` | Playwright + xUnit 测试项目 |
| `Test/e2e-mvc.cs` | .NET 10 编排脚本（构建/启动/测试/报告） |
| `CubeSSO/` | 被测启动项目 |
| `Bin/E2ETest/` | 测试输出目录（运行时自动创建） |
| `XUnitTest/` | 现有单元测试（API 级别，与 E2E 隔离） |
