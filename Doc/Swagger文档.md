# 第37章 Swagger 文档

> 本章介绍如何在魔方 WebAPI 项目中集成 Swagger 文档，包括区域分组、接口配置等。

---

## 37.1 引入 NewLife.Cube.Swagger

### 安装 NuGet 包

```bash
dotnet add package NewLife.Cube.Swagger
```

### 配置服务

```csharp
using NewLife.Cube.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCube();

// 添加魔方 Swagger
builder.Services.AddCubeSwagger();

var app = builder.Build();

app.UseCube(app.Environment);

// 使用 Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();
```

---

## 37.2 区域自动分组

### 自动分组机制

魔方会根据控制器所在区域自动分组：

```csharp
// Admin 区域
[AdminArea]
[Route("api/admin/[controller]")]
public class UserController : EntityController<User> { }
// 自动分到 Admin 组

// School 区域
[SchoolArea]
[Route("api/school/[controller]")]
public class StudentController : EntityController<Student> { }
// 自动分到 School 组

// 无区域
[Route("api/[controller]")]
public class HomeController : ControllerBase { }
// 自动分到默认组
```

### Swagger UI 显示

```
┌─────────────────────────────────┐
│ API 文档                        │
├─────────────────────────────────┤
│ ▼ Admin                         │
│   ├── User                      │
│   ├── Role                      │
│   └── Menu                      │
├─────────────────────────────────┤
│ ▼ School                        │
│   ├── Student                   │
│   └── Class                     │
├─────────────────────────────────┤
│ ▼ Default                       │
│   └── Home                      │
└─────────────────────────────────┘
```

---

## 37.3 接口文档配置

### 基本配置

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "我的应用 API",
        Version = "v1",
        Description = "基于魔方快速开发平台的 API 接口",
        Contact = new OpenApiContact
        {
            Name = "技术支持",
            Email = "support@example.com",
            Url = new Uri("https://example.com")
        }
    });
    
    // 添加 XML 注释
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});
```

### 启用 XML 注释

在项目文件中添加：

```xml
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

### JWT 认证配置

```csharp
builder.Services.AddSwaggerGen(options =>
{
    // 添加 JWT 认证
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT 授权头，格式：Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<String>()
        }
    });
});
```

---

## 37.4 测试接口

### Swagger UI 测试

1. 启动应用后访问 `/swagger`
2. 展开需要测试的接口
3. 点击 "Try it out" 按钮
4. 填写参数
5. 点击 "Execute" 执行

### 认证测试流程

1. 先调用登录接口获取 Token
2. 点击页面顶部的 "Authorize" 按钮
3. 输入 `Bearer {your_token}`
4. 点击 "Authorize" 确认
5. 后续请求自动携带认证头

### 示例请求

```bash
# 使用 curl 测试
curl -X GET "http://localhost:5000/api/student?pageIndex=1&pageSize=20" \
     -H "Authorization: Bearer your_token_here"
```

---

## 37.5 自定义 Swagger 配置

### 隐藏接口

```csharp
/// <summary>内部接口，不在 Swagger 显示</summary>
[ApiExplorerSettings(IgnoreApi = true)]
[HttpGet("internal")]
public ActionResult InternalApi() { }
```

### 接口分组

```csharp
/// <summary>V2 版本接口</summary>
[ApiExplorerSettings(GroupName = "v2")]
[HttpGet]
public ActionResult GetV2() { }
```

### 自定义响应示例

```csharp
/// <summary>获取学生列表</summary>
/// <response code="200">返回学生列表</response>
/// <response code="401">未登录</response>
[HttpGet]
[ProducesResponseType(typeof(ApiResult<List<Student>>), 200)]
[ProducesResponseType(typeof(ApiResult), 401)]
public ActionResult Index(Pager p) { }
```

---

## 本章小结

本章介绍了 Swagger 文档的配置：

1. **引入包**：`NewLife.Cube.Swagger`
2. **区域分组**：自动按区域分组显示
3. **接口配置**：XML 注释、JWT 认证
4. **测试接口**：使用 Swagger UI 测试

Swagger 是前后端分离开发的重要工具。

---

**下一章**：[前端对接指南](前端对接指南.md) - 了解前端如何对接魔方 API
