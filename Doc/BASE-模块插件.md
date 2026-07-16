# 模块插件

魔方支持模块化插件架构，通过 `IModule` 和 `IAdapter` 接口实现功能的动态加载和扩展。皮肤主题（AdminLTE、Layui、Tabler 等）就是基于该机制实现的。

## 核心接口

### IModule — 模块接口

```csharp
public interface IModule
{
    /// <summary>添加模块，注册依赖注入服务</summary>
    void Add(IServiceCollection services);

    /// <summary>使用模块，配置请求管道</summary>
    void Use(IApplicationBuilder app, IWebHostEnvironment env);
}
```

模块接口对应 ASP.NET Core 的两个阶段：
- `Add()` → 在 `ConfigureServices` 阶段注册服务
- `Use()` → 在 `Configure` 阶段配置中间件

### IAdapter — 适配器接口

```csharp
public interface IAdapter
{
    /// <summary>编码配置对象</summary>
    Object Encode(Dictionary<String, Object> dic, Dictionary<ViewKinds, FieldCollection> fieldCollections);

    /// <summary>序列化配置对象</summary>
    Object Decode(Object obj);
}
```

适配器用于给前端提供合适的页面配置，将魔方的字段集合转换为前端框架所需的格式。

### ModuleAttribute — 模块标记

```csharp
[Module("AdminLTE")]
public class AdminLTEService : IModule { ... }
```

## 模块管理器

`ModuleManager` 负责模块的扫描、加载和管理：

| 方法 | 说明 |
|------|------|
| `LoadAll(services)` | 加载所有启用的模块，从 AppModule 表读取配置 |
| `ScanAllModules()` | 扫描所有程序集中的 IModule 实现 |
| `GetAdapter(name)` | 获取指定名称的适配器 |

### 加载流程

```
应用启动
  └─ CubeService.AddCube()
       └─ ModuleManager.LoadAll()
            ├─ 从 AppModule 表读取已注册模块
            ├─ 过滤启用的模块
            ├─ 动态加载 DLL 程序集
            ├─ 注册 MVC 应用部分（CompiledRazorAssemblyPart）
            └─ 调用 IModule.Add() 注册服务
```

## 内置模块

魔方项目中已有的模块实现：

| 模块 | 程序集 | 说明 |
|------|--------|------|
| AdminLTE | NewLife.Cube.AdminLTE | AdminLTE 皮肤主题 |
| Layui | NewLife.Cube.LayuiAdmin | Layui Admin 皮肤主题 |
| Tabler | NewLife.Cube.Tabler | Tabler 皮肤主题 |
| Blazor | NewLife.Cube.Blazor | Blazor Server 支持 |
| ElementUI | NewLife.Cube.ElementUI | ElementUI 前端适配 |
| Metronic8 | NewLife.Cube.Metronic8 | Metronic 8 皮肤主题 |

## 开发自定义模块

### 创建模块项目

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFrameworks>net6.0;net7.0;net8.0;net9.0;net10.0</TargetFrameworks>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="NewLife.Cube.Core" Version="6.9.*" />
  </ItemGroup>
</Project>
```

### 实现 IModule

```csharp
[Module("MyModule")]
public class MyModuleService : IModule
{
    public void Add(IServiceCollection services)
    {
        // 注册模块专属服务
        services.AddSingleton<IMyService, MyService>();
    }

    public void Use(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // 配置模块中间件
        app.UseMiddleware<MyMiddleware>();
    }
}
```

### 注册模块

模块通过 **系统管理 → 应用插件** 管理：
- 填写模块名称和程序集路径
- 启用/禁用模块
- 模块加载顺序由配置决定

## 管理后台

应用插件在管理后台 **魔方管理 → 应用插件** 中配置：
- 查看已扫描到的模块
- 启用/禁用模块
- 配置模块程序集路径
- 模块的异常隔离（单个模块加载失败不影响其它模块）
