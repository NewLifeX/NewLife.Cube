# 模块与皮肤（扩展包）

魔方采用**扩展包**模型：下游把功能做成独立的 NuGet 包，引用到最终项目后由魔方自动发现并装配，不需要在数据库登记，也不需要宿主手写加载代码。

扩展包分为两条正交的轴：

| 类型 | 契约 | 作用 | 数量 |
|------|------|------|------|
| 功能模块 | `IModule`（可选 `IModuleEndpoint`） | 后端能力：服务、中间件、端点、实体、菜单、作业、部件 | 可叠加多个 |
| 前端皮肤 | `ICubeSkin` | 前端外观：`index.html` 与静态资源 | 同时只启用一个 |

> 模块决定"能做什么"，皮肤决定"长什么样"。一个包可以同时携带模块与皮肤，例如皮肤需要自己的设置控制器时。

## 1. 事实源边界

| 事实 | 事实源 |
|------|--------|
| 装了哪些扩展 | 程序集引用（`csproj`） |
| 是否启用 | 引用即启用 |
| 选哪套皮肤 | 设置项 `CubeSetting.SpaSkin`，保存后刷新即生效 |
| 功能细粒度开关 | 各模块自己的设置项，语义由模块定义 |

**没有插件表。** 程序集引用是唯一的安装事实源，数据库不参与"加载哪些代码"的决策。

## 2. 功能模块

### 2.1 契约

```csharp
public interface IModule
{
    /// <summary>添加模块。注册依赖注入服务，在 AddCube 阶段调用</summary>
    void Add(IServiceCollection services);

    /// <summary>使用模块。配置中间件，在 UseCube 阶段、路由注册之前调用</summary>
    void Use(IApplicationBuilder app, IWebHostEnvironment env);
}

/// <summary>需要注册路由或终结点映射的模块额外实现</summary>
public interface IModuleEndpoint
{
    void Map(IEndpointRouteBuilder endpoints);
}
```

`Use` 一定在路由注册之前调用，模块中间件对后续所有请求生效；`Map` 在路由内部、SPA 回退之前调用，模块端点优先于前端 History 路由回退。

需要比 `Use` 更早位置的中间件（例如在认证之前）时，模块可以在 `Add` 中注册 ASP.NET Core 标准的 `IStartupFilter`。

### 2.2 声明元数据

```csharp
[Module("WeChat", DisplayName = "微信公众号", Order = 100)]
public class WeChatModule : IModule, IModuleEndpoint
{
    public void Add(IServiceCollection services) => services.AddSingleton<IWeChatService, WeChatService>();

    public void Use(IApplicationBuilder app, IWebHostEnvironment env) { }

    public void Map(IEndpointRouteBuilder endpoints) => endpoints.MapPost("/wechat/callback", ...);
}
```

| 属性 | 说明 |
|------|------|
| `Name` | 模块标识，缺省取类型名去掉 `Module`/`Service`/`Adapter` 后缀 |
| `DisplayName` | 显示名称，用于日志与诊断 |
| `Order` | 装配顺序，升序，缺省 0；相同顺序按名称排序 |

### 2.3 自动发现

`ModuleManager` 使用 `AssemblyX.FindAllPlugins` 扫描所有引用了魔方的程序集，实例化 `IModule` 实现并缓存结果：

- 只加载一次，可重复调用；
- 单个模块构造或装配异常只影响自身，记录日志后继续；
- 重名模块保留装配顺序靠前的一个，并记录警告。

宿主只需要：

```csharp
services.AddCube();

var app = builder.Build();
app.UseCube(builder.Environment);
app.MapControllers();
```

### 2.4 显式注册

裁剪/AOT 或单元测试需要固定清单时，可以显式注册：

```csharp
services.AddCubeModule<WeChatModule>();
```

显式注册与自动发现会合并去重，同一个模块只装配一次。

### 2.5 适配器

`IAdapter` 用于把魔方的字段集合编码成前端需要的页面配置，按名称匹配：

```csharp
[Module("QuickVue")]
public class QuickVueAdapter : IAdapter { ... }
```

启动时自动发现所有适配器；`DefaultAdapter` 由核心始终注册，`PageService` 不会再因为找不到适配器而退化。

## 3. 前端皮肤

皮肤包只提供 `index.html` 与静态资源，实现 `ICubeSkin`：

```csharp
internal sealed class VueSkin : ICubeSkin
{
    public String Name => "Vue";
    public String DisplayName => "Vue3 + Element Plus";
    public IFileProvider CreateFileProvider() => new CubeEmbeddedFileProvider(typeof(VueSkin).Assembly, "NewLife.Cube.Vue.wwwroot");
}
```

- `UseCube` 自动发现已引用的皮肤包并注册静态资源与 SPA 回退，用户项目无需额外调用；
- 当前皮肤由 `CubeSetting.SpaSkin` 每次请求实时解析，管理后台保存后刷新即生效；
- 多个皮肤包可以同时引用，只有当前皮肤的资源与回退生效；
- `UseVue` / `UseReact` / `UseCubeSkin` 保留为强制指定入口。

详见《[内置前端皮肤](Api/内置前端皮肤.md)》。

> 第二代 MVC 主题皮肤（AdminLTE/Tabler 等）已冻结，继续使用 `app.UseAdminLTE(env)` 等显式入口注册，不纳入 `ICubeSkin`。

## 4. 开发一个扩展包

以对接企业微信为例：

```
NewLife.Cube.WeCom/
├── WeComModule.cs            // IModule + IModuleEndpoint
├── Entity/                   // XCode 实体，启动自动建表
├── Areas/WeCom/
│   ├── WeComArea.cs          // AreaBase，控制器所在命名空间自动生成菜单
│   └── Controllers/          // EntityController<T>，自动 CRUD 与权限
├── Jobs/                     // ICubeJob，自动发现
├── Widgets/                  // IWidget，自动发现
└── Locales/zh-Hans.yml       // 可选，模块自带的译文
```

宿主引用包后即可获得表、菜单、权限、接口与后台页面，无需修改 `Program.cs`。

实体 CRUD 页面由 `/meta` 自描述元数据驱动，通用列表/表单/详情页零前端开发即可使用；需要自定义交互的页面再按皮肤规范单独实现。

## 5. 常见问题

### 5.1 如何停用某个扩展？

- 部署层：不引用（或从发布目录移除）对应包；
- 运行层：模块自己读取设置项，在 `Add`/`Use` 中跳过装配。这类开关需要重启进程。

不建议在管理后台提供"加载/卸载代码"的开关：已注册的服务、中间件和路由无法在运行期安全卸载。

### 5.2 还能不能丢一个 DLL 进目录就加载？

不支持。动态 DLL 热插拔需要自建清单、处理控制器发现与视图编译，且绕过包管理与版本约束。需要免编译交付时，使用私有 NuGet 源。

### 5.3 原来的「应用插件」菜单去哪了？

`AppModule` 表与「应用插件」页已删除。旧数据库中的表会成为孤表，不影响运行，确认无需回滚后可手工删除；旧菜单行会由菜单可用性检查自动隐藏。

### 5.4 老皮肤包实现了 `IModule` 怎么办？

改为实现 `ICubeSkin`，并删除 `IModule` 里的静态文件注册，否则会与皮肤系统重复注册。

## 6. 约束

- 插件之间不互相编译引用；需要协作时依赖只含接口的契约包，或通过依赖注入解析接口。
- `IModule` 签名保持稳定；新增能力使用可选接口（如 `IModuleEndpoint`）。
- 自动发现依赖运行时反射，NativeAOT/裁剪场景必须改用 `AddCubeModule<T>()` 显式注册。
