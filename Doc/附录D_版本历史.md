# 附录D 版本历史

> 本附录记录魔方的主要版本变更和升级指南。

---

## D.1 主要版本变更

### 第三代魔方 (NewLife.Cube)

前后端分离架构，NuGet 包名 `NewLife.Cube`。

#### v6.x（当前版本）

**v6.0** - 2024年

- ?? 新增：前后端分离架构支持
- ?? 新增：标准 WebAPI 接口规范
- ?? 新增：JWT Token 认证
- ?? 新增：多租户架构支持
- ?? 新增：数据权限控制
- ?? 新增：ECharts 图表集成
- ?? 优化：性能大幅提升
- ?? 优化：API 响应格式标准化

### 第二代魔方 (NewLife.CubeNC)

MVC 服务端渲染，NuGet 包名 `NewLife.Cube.Core`。

#### v6.x（当前版本）

**v6.2** - 2024年

- ?? 新增：支持 .NET 9/10
- ?? 新增：日期范围选择控件优化
- ?? 新增：多租户架构完善
- ?? 优化：安全性增强
- ?? 优化：性能调优

**v6.1** - 2024年

- ?? 新增：支持 .NET 8
- ?? 新增：LayuiAdmin 皮肤
- ?? 新增：Metronic8 皮肤
- ?? 优化：菜单管理优化
- ?? 修复：若干 Bug

**v6.0** - 2023年

- ?? 新增：支持 .NET 7
- ?? 新增：数据权限功能
- ?? 新增：定时作业管理
- ?? 优化：权限系统重构
- ?? 优化：日志系统优化

#### v5.x

**v5.0** - 2022年

- ?? 新增：支持 .NET 6
- ?? 新增：Tabler 皮肤
- ?? 新增：OAuth 登录增强
- ?? 优化：实体控制器重构
- ?? 优化：视图体系优化

#### v4.x

**v4.0** - 2021年

- ?? 新增：支持 .NET 5
- ?? 新增：ElementUI 皮肤
- ?? 新增：SSO 单点登录
- ?? 优化：API 接口规范化

#### v3.x

**v3.0** - 2020年

- ?? 新增：支持 .NET Core 3.1
- ?? 新增：Metronic 皮肤
- ?? 优化：从 ASP.NET Core 2.x 升级

#### v2.x

**v2.0** - 2019年

- ?? 新增：支持 ASP.NET Core 2.x
- ?? 新增：AdminLTE 皮肤
- ?? 迁移：从 .NET Framework 迁移到 .NET Core

### 第一代魔方 (NewLife.Cube)

基于 ASP.NET MVC，已停止维护。

---

## D.2 升级指南

### 从 v5.x 升级到 v6.x

#### 1. 更新 NuGet 包

```xml
<!-- 更新前 -->
<PackageReference Include="NewLife.Cube.Core" Version="5.*" />

<!-- 更新后 -->
<PackageReference Include="NewLife.Cube.Core" Version="6.*" />
```

#### 2. 更新目标框架

```xml
<!-- 推荐使用 .NET 8 或更高版本 -->
<TargetFramework>net8.0</TargetFramework>
```

#### 3. 配置变更

```csharp
// v5.x
services.AddCube();

// v6.x（保持兼容）
services.AddCube();

// 或使用新的配置方式
services.AddCube(options =>
{
    options.EnableMultiTenant = true;
    options.EnableDataPermission = true;
});
```

#### 4. 破坏性变更

| 变更项 | v5.x | v6.x | 处理方式 |
|--------|------|------|----------|
| 用户令牌 | 单一 Token | JWT + RefreshToken | 更新前端令牌处理 |
| 权限检查 | EntityAuthorize | EntityAuthorize + DataPermission | 添加数据权限配置 |
| 日志格式 | 旧格式 | 标准化 JSON | 更新日志解析 |

### 从 v4.x 升级到 v5.x

#### 1. 更新 NuGet 包

```xml
<PackageReference Include="NewLife.Cube.Core" Version="5.*" />
```

#### 2. 更新目标框架

```xml
<TargetFramework>net6.0</TargetFramework>
```

#### 3. Program.cs 变更

```csharp
// v4.x (Startup.cs 模式)
public class Startup
{
    public void ConfigureServices(IServiceCollection services) { }
    public void Configure(IApplicationBuilder app) { }
}

// v5.x (最小 API 模式)
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCube();

var app = builder.Build();
app.UseCube();
app.Run();
```

### 从 MVC 版迁移到 WebAPI 版

#### 1. 更换 NuGet 包

```xml
<!-- MVC 版 -->
<PackageReference Include="NewLife.Cube.Core" Version="6.*" />

<!-- WebAPI 版 -->
<PackageReference Include="NewLife.Cube" Version="6.*" />
```

#### 2. 调整控制器

```csharp
// MVC 版
[SchoolArea]
[Menu(0, "学生管理", Icon = "fa-users")]
public class StudentController : EntityController<Student>
{
}

// WebAPI 版
[Route("api/[controller]")]
[ApiController]
public class StudentController : EntityController<Student>
{
}
```

#### 3. 前端改造

WebAPI 版需要独立的前端项目，可选择：

- **NewLife.CubeVue**：Vue 版前端
- **NewLife.CubeAntd**：Ant Design 版前端
- **自定义前端**：参考 API 文档对接

---

## D.3 版本兼容性

### .NET 版本支持

| 魔方版本 | .NET Framework | .NET Core | .NET 5 | .NET 6 | .NET 7 | .NET 8 | .NET 9 | .NET 10 |
|----------|----------------|-----------|--------|--------|--------|--------|--------|---------|
| v6.x | - | - | - | ? | ? | ? | ? | ? |
| v5.x | - | - | ? | ? | ? | - | - | - |
| v4.x | - | ? | ? | - | - | - | - | - |
| v3.x | - | ? | - | - | - | - | - | - |
| v2.x | - | ? | - | - | - | - | - | - |
| v1.x | ? | - | - | - | - | - | - | - |

### 数据库支持

| 数据库 | 版本要求 | 说明 |
|--------|----------|------|
| SQLite | 3.x | 推荐开发测试使用 |
| MySQL | 5.7+ / 8.0+ | 推荐生产使用 |
| SQL Server | 2012+ | 推荐生产使用 |
| PostgreSQL | 10+ | 支持 |
| Oracle | 11g+ | 支持 |
| 达梦 | DM8 | 国产数据库支持 |

---

## D.4 长期支持版本 (LTS)

| 版本 | 发布日期 | 支持截止 | 状态 |
|------|----------|----------|------|
| v6.2 | 2024-12 | 2027-12 | 当前版本 |
| v6.0 | 2024-01 | 2027-01 | 活跃支持 |
| v5.0 | 2022-11 | 2025-11 | 维护中 |
| v4.x | 2021-11 | 2024-11 | 已停止支持 |

---

## D.5 更新日志格式说明

- ?? **新增**：新功能
- ?? **优化**：现有功能改进
- ?? **修复**：Bug 修复
- ?? **变更**：破坏性变更
- ?? **文档**：文档更新
- ?? **安全**：安全更新

---

## D.6 获取更新

### NuGet 更新

```bash
# 更新所有 NewLife 包
dotnet add package NewLife.Cube.Core

# 指定版本
dotnet add package NewLife.Cube.Core --version 6.2.0
```

### 源码获取

```bash
# GitHub
git clone https://github.com/NewLifeX/NewLife.Cube.git

# Gitee（国内镜像）
git clone https://gitee.com/NewLifeX/NewLife.Cube.git
```

### 关注更新

- **GitHub Releases**：https://github.com/NewLifeX/NewLife.Cube/releases
- **NuGet**：https://www.nuget.org/packages/NewLife.Cube.Core
- **官方网站**：https://newlifex.com

---

## 本附录小结

本附录提供了完整的版本历史和升级指南：

1. 主要版本变更记录
2. 各版本升级步骤
3. 版本兼容性说明
4. 长期支持版本说明
5. 更新日志格式
6. 获取更新方式

建议定期关注官方更新，及时升级以获得新功能和安全修复。

---

**下一章**：[附录E 贡献指南](附录E_贡献指南.md) - 参与开源贡献
