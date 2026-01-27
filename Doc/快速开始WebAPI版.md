# 第35章 快速开始（WebAPI版）

> 本章介绍第三代魔方（WebAPI版）的快速入门，包括项目创建、配置和与 MVC 版的区别。

---

## 35.1 创建 WebAPI 项目

### 使用 Visual Studio 创建

1. **新建项目**
   - 打开 Visual Studio 2022
   - 选择"创建新项目"
   - 选择"ASP.NET Core Web API"模板
   - 设置项目名称，如 `MyApp.WebApi`
   - 选择 .NET 8.0（或更高版本）
   - 勾选"启用 OpenAPI 支持"

2. **安装 NuGet 包**
   ```
   Install-Package NewLife.Cube
   ```

3. **配置 Program.cs**
   ```csharp
   using NewLife.Cube;
   
   var builder = WebApplication.CreateBuilder(args);
   
   // 添加控制器
   builder.Services.AddControllers();
   
   // 添加魔方 WebAPI
   builder.Services.AddCube();
   
   // 添加 Swagger
   builder.Services.AddEndpointsApiExplorer();
   builder.Services.AddSwaggerGen();
   
   var app = builder.Build();
   
   // 使用魔方
   app.UseCube(app.Environment);
   
   if (app.Environment.IsDevelopment())
   {
       app.UseSwagger();
       app.UseSwaggerUI();
   }
   
   app.UseHttpsRedirection();
   app.UseAuthorization();
   app.MapControllers();
   
   app.Run();
   ```

### 使用命令行创建

```bash
# 创建 WebAPI 项目
dotnet new webapi -n MyApp.WebApi

# 进入项目目录
cd MyApp.WebApi

# 添加魔方包
dotnet add package NewLife.Cube

# 运行项目
dotnet run
```

---

## 35.2 与 MVC 版的区别

### 包引用对比

| 版本 | NuGet 包 | 说明 |
|------|----------|------|
| MVC 版（第二代） | `NewLife.Cube.Core` | 服务端渲染，包含完整视图 |
| WebAPI 版（第三代） | `NewLife.Cube` | 前后端分离，只提供 API |

### 功能对比

| 功能 | MVC 版 | WebAPI 版 |
|------|--------|-----------|
| 视图渲染 | ? Razor 视图 | ? 无视图 |
| 管理界面 | ? 内置 | ? 需配合前端 |
| API 接口 | ? 支持 | ? 主要功能 |
| Swagger | ? 需额外配置 | ? 推荐使用 |
| 前端技术 | 服务端渲染 | Vue/React/Angular |

### 控制器基类对比

```csharp
// MVC 版：继承 EntityController
public class StudentController : EntityController<Student>
{
    // 同时支持视图和 API
}

// WebAPI 版：继承 EntityController 或 ApiController
[Route("api/[controller]")]
[ApiController]
public class StudentController : EntityController<Student>
{
    // 只返回 JSON
}
```

---

## 35.3 配置说明

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Membership": "Data Source=..\\Data\\Membership.db;Provider=SQLite",
    "Cube": "MapTo=Membership",
    "Log": "MapTo=Membership"
  },
  "CubeSetting": {
    "EnableTenant": false,
    "TokenExpire": 7200,
    "JwtSecret": "your-jwt-secret-key-here",
    "CorsOrigins": "*"
  }
}
```

### 跨域配置

```csharp
// 在 AddCube 中已自动配置 CORS
// 可通过 CubeSetting.CorsOrigins 设置允许的来源

// 自定义跨域
builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 使用
app.UseCors("MyPolicy");
```

---

## 35.4 配合 Swagger 文档

### 安装 Swagger 包

```bash
dotnet add package NewLife.Cube.Swagger
```

### 配置 Swagger

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
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MyApp API V1");
});

app.MapControllers();
app.Run();
```

### Swagger 区域分组

魔方会自动按区域对 API 进行分组：

```csharp
// Admin 区域的控制器会分到 Admin 组
[AdminArea]
[Route("api/admin/[controller]")]
public class UserController : EntityController<User> { }

// School 区域的控制器会分到 School 组
[SchoolArea]
[Route("api/school/[controller]")]
public class StudentController : EntityController<Student> { }
```

---

## 35.5 前后端分离架构

### 架构图

```
┌─────────────────┐     ┌─────────────────┐
│   前端应用      │     │   后端 API      │
│   Vue/React     │ ←→  │   NewLife.Cube  │
│   localhost:3000│     │   localhost:5000│
└─────────────────┘     └─────────────────┘
         ↓                      ↓
    ┌─────────────────────────────────┐
    │           数据库                │
    │     SQLite/MySQL/MSSQL          │
    └─────────────────────────────────┘
```

### 开发流程

1. **后端开发**
   - 创建实体类
   - 创建控制器
   - 配置路由
   - 编写业务逻辑

2. **前端开发**
   - 调用后端 API
   - 获取字段元数据
   - 动态渲染表单
   - 处理 CRUD 操作

### 典型 API 交互

```javascript
// 前端调用示例（JavaScript）

// 1. 登录获取令牌
const loginResult = await fetch('/api/user/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: 'admin', password: '123456' })
});
const { accessToken } = await loginResult.json();

// 2. 获取字段元数据
const fieldsResult = await fetch('/api/student/getfields?kind=1', {
    headers: { 'Authorization': `Bearer ${accessToken}` }
});
const fields = await fieldsResult.json();

// 3. 获取数据列表
const listResult = await fetch('/api/student?pageIndex=1&pageSize=20', {
    headers: { 'Authorization': `Bearer ${accessToken}` }
});
const { data, page } = await listResult.json();

// 4. 添加数据
const addResult = await fetch('/api/student', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${accessToken}`
    },
    body: JSON.stringify({ name: '张三', classId: 1 })
});
```

---

## 35.6 示例项目 CubeDemo

### 项目结构

```
CubeDemo/
├── Controllers/
│   ├── HomeController.cs
│   └── Areas/
│       └── School/
│           └── Controllers/
│               ├── StudentController.cs
│               └── ClassController.cs
├── Data/
│   ├── Student.cs
│   └── Class.cs
├── Models/
│   └── StudentInput.cs
├── Program.cs
├── appsettings.json
└── CubeDemo.csproj
```

### 核心代码

**Program.cs**
```csharp
using NewLife.Cube;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCube();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCube(app.Environment);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
```

**StudentController.cs**
```csharp
using NewLife.Cube;
using NewLife.Web;
using XCode.Membership;

namespace CubeDemo.Areas.School.Controllers;

[SchoolArea]
[Route("api/school/[controller]")]
[ApiController]
public class StudentController : EntityController<Student>
{
    static StudentController()
    {
        ListFields.RemoveField("Password", "Remark");
        ListFields.AddField("ClassName", "ClassId");
    }
    
    protected override IEnumerable<Student> Search(Pager p)
    {
        var classId = p["classId"].ToInt(-1);
        var name = p["Q"];
        
        return Student.Search(classId, name, p);
    }
}
```

### 运行示例

```bash
# 克隆示例项目
git clone https://github.com/NewLifeX/NewLife.Cube.git

# 进入 WebAPI 示例目录
cd NewLife.Cube/CubeDemo

# 运行
dotnet run

# 访问 Swagger
# http://localhost:5000/swagger
```

---

## 35.7 认证与授权

### JWT 认证

```csharp
// WebAPI 版默认使用 JWT 认证

// 登录接口
[HttpPost("login")]
public ActionResult Login([FromBody] LoginModel model)
{
    var result = _userService.Login(model, HttpContext);
    return Json(result);
    // 返回：{ accessToken: "xxx", refreshToken: "xxx" }
}

// 需要认证的接口
[EntityAuthorize(PermissionFlags.Detail)]
[HttpGet]
public ActionResult Index(Pager p)
{
    // 需要携带 Authorization: Bearer {token}
    var list = Search(p);
    return Json(0, null, list, new { page = p });
}
```

### 令牌传递方式

```csharp
// 1. Header 方式（推荐）
Authorization: Bearer {token}

// 2. Query 方式
/api/student?token={token}

// 3. Cookie 方式
Cookie: Token={token}
```

---

## 本章小结

本章介绍了 WebAPI 版魔方的快速入门：

1. **项目创建**：使用 VS 或命令行创建 WebAPI 项目
2. **与 MVC 版的区别**：前后端分离，只提供 API
3. **配置说明**：连接字符串、跨域、JWT
4. **Swagger 集成**：API 文档自动生成
5. **前后端分离架构**：典型的开发流程

WebAPI 版魔方适合构建现代化的前后端分离应用。

---

**下一章**：[WebAPI接口规范](WebAPI接口规范.md) - 了解魔方 API 的标准规范
