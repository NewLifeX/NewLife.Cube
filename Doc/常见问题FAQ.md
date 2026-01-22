# 第41章 常见问题（FAQ）

> 本章收集魔方开发中的常见问题和解答。

---

## 41.1 如何隐藏字段？

### 问题

在列表页或表单页隐藏某些字段。

### 解答

在控制器静态构造器中配置：

```csharp
public class StudentController : EntityController<Student>
{
    static StudentController()
    {
        // 列表页隐藏字段
        ListFields.RemoveField("Password", "Remark");
        
        // 添加表单隐藏字段
        AddFormFields.RemoveField("CreateTime", "UpdateTime");
        
        // 编辑表单隐藏字段
        EditFormFields.RemoveField("CreateUser", "CreateTime");
        
        // 详情页隐藏字段
        DetailFields.RemoveField("Password");
    }
}
```

### 使用 FieldCollection

```csharp
static StudentController()
{
    // 获取字段集合
    var list = ListFields;
    
    // 通过字段名获取并设置不可见
    var field = list.GetField("Password");
    if (field != null) field.Visible = false;
    
    // 或者直接使用索引器
    list["Remark"].Visible = false;
}
```

---

## 41.2 如何自定义统计？

### 问题

在列表页底部显示自定义统计信息。

### 解答

重写 `Stat` 属性：

```csharp
public class OrderController : EntityController<Order>
{
    /// <summary>统计</summary>
    protected override IDictionary<String, Object> Stat
    {
        get
        {
            var p = Session[CacheKey] as Pager ?? new Pager();
            
            // 统计总金额和订单数
            var stat = Order.SearchStat(p);
            
            return new Dictionary<String, Object>
            {
                ["总金额"] = stat.TotalAmount.ToString("N2"),
                ["订单数"] = stat.OrderCount,
                ["平均金额"] = stat.AvgAmount.ToString("N2")
            };
        }
    }
}
```

### 实体层统计方法

```csharp
// Order.Biz.cs
public partial class Order
{
    public class OrderStat
    {
        public Decimal TotalAmount { get; set; }
        public Int32 OrderCount { get; set; }
        public Decimal AvgAmount => OrderCount > 0 ? TotalAmount / OrderCount : 0;
    }
    
    public static OrderStat SearchStat(Pager p)
    {
        var exp = new WhereExpression();
        // 添加查询条件...
        
        var stat = new OrderStat
        {
            TotalAmount = FindSum(_.Amount, exp),
            OrderCount = FindCount(exp)
        };
        
        return stat;
    }
}
```

---

## 41.3 如何集成自定义前端？

### 问题

使用 Vue/React 等前端框架替换魔方默认界面。

### 解答

#### 1. 使用 WebAPI 版魔方

```csharp
// 安装 NewLife.Cube（WebAPI 版）
builder.Services.AddCube();
app.UseCube();
```

#### 2. 前端调用 API

```javascript
// 登录
const login = async (username, password) => {
    const response = await fetch('/Admin/User/Login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password })
    });
    return response.json();
};

// 获取列表
const getList = async (page, size) => {
    const response = await fetch(`/Admin/Student?pageIndex=${page}&pageSize=${size}`);
    return response.json();
};
```

#### 3. 嵌入前端到 MVC 项目

```csharp
// Program.cs
app.UseStaticFiles(); // 静态文件（前端打包后的文件）
app.UseSpaStaticFiles();

app.UseSpa(spa =>
{
    spa.Options.SourcePath = "ClientApp";
    
    if (env.IsDevelopment())
    {
        spa.UseVueCli(npmScript: "serve");
    }
});
```

---

## 41.4 如何扩展 OAuth 登录？

### 问题

添加新的第三方登录方式。

### 解答

#### 1. 创建 OAuth 客户端

```csharp
/// <summary>自定义OAuth客户端</summary>
public class MyOAuthClient : OAuthClient
{
    public override String Name => "MyProvider";
    
    protected override String AuthUrl => "https://my.provider.com/oauth/authorize";
    protected override String AccessUrl => "https://my.provider.com/oauth/token";
    protected override String UserUrl => "https://my.provider.com/api/user";
    
    protected override void OnGetInfo(IDictionary<String, String> dic)
    {
        base.OnGetInfo(dic);
        
        // 解析用户信息
        OpenID = dic["id"];
        Name = dic["name"];
        NickName = dic["nickname"];
        Avatar = dic["avatar"];
    }
}
```

#### 2. 注册 OAuth 客户端

```csharp
// Startup 中注册
OAuthClient.Register("MyProvider", () => new MyOAuthClient());
```

#### 3. 配置 OAuth

```json
{
  "OAuth": {
    "MyProvider": {
      "AppId": "your_app_id",
      "Secret": "your_secret",
      "Scope": "user_info"
    }
  }
}
```

---

## 41.5 如何实现多租户？

### 问题

在单个应用中支持多个租户数据隔离。

### 解答

参考 [多租户架构](多租户架构.md) 章节，关键步骤：

#### 1. 实体添加租户字段

```csharp
public partial class Student : Entity<Student>
{
    /// <summary>租户</summary>
    public Int32 TenantId { get; set; }
}
```

#### 2. 实现租户过滤

```csharp
// 查询时自动添加租户条件
public static IList<Student> Search(String name, Pager p)
{
    var exp = new WhereExpression();
    
    // 添加租户过滤
    var tenantId = ManageProvider.User?.Extends["TenantId"].ToInt();
    if (tenantId > 0) exp &= _.TenantId == tenantId;
    
    if (!name.IsNullOrEmpty()) exp &= _.Name.Contains(name);
    
    return FindAll(exp, p);
}
```

#### 3. 新增时自动设置租户

```csharp
protected override Int32 OnInsert()
{
    if (TenantId <= 0)
    {
        TenantId = ManageProvider.User?.Extends["TenantId"].ToInt() ?? 0;
    }
    
    return base.OnInsert();
}
```

---

## 41.6 如何自定义导出格式？

### 问题

自定义 Excel 导出的格式或内容。

### 解答

#### 1. 重写 ExportExcel 方法

```csharp
public class StudentController : EntityController<Student>
{
    protected override IActionResult ExportExcel()
    {
        // 获取数据
        var list = Student.FindAll();
        
        // 自定义导出列
        var columns = new List<String>
        {
            "Id", "Name", "ClassName", "Score", "CreateTime"
        };
        
        // 使用 ExcelWriter
        using var ms = new MemoryStream();
        var writer = new ExcelWriter(ms);
        
        // 写入标题行
        writer.WriteRow(new[] { "编号", "姓名", "班级", "分数", "创建时间" });
        
        // 写入数据行
        foreach (var item in list)
        {
            writer.WriteRow(new Object[]
            {
                item.Id,
                item.Name,
                item.ClassName,
                item.Score,
                item.CreateTime.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }
        
        writer.Finish();
        ms.Position = 0;
        
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"学生列表_{DateTime.Now:yyyyMMdd}.xlsx");
    }
}
```

#### 2. 使用 GetExportData 自定义数据

```csharp
protected override IEnumerable<Student> GetExportData()
{
    // 自定义导出数据
    return Student.FindAll().Where(e => e.Status == 1);
}
```

---

## 41.7 如何添加自定义操作按钮？

### 问题

在列表页工具栏或行内添加自定义按钮。

### 解答

#### 1. 添加工具栏按钮

```csharp
static StudentController()
{
    // 添加工具栏按钮
    ListFields.AddAction("导入成绩", "ImportScore", "btn-info");
    ListFields.AddAction("批量分班", "BatchAssign", "btn-warning");
}

// 对应的 Action
[HttpPost]
[EntityAuthorize(PermissionFlags.Update)]
public ActionResult ImportScore(HttpPostedFileBase file)
{
    // 处理导入逻辑
    return Json(new { code = 0, message = "导入成功" });
}
```

#### 2. 添加行内按钮

```csharp
static StudentController()
{
    // 添加行内操作按钮
    var list = ListFields;
    list.AddRowAction("查看成绩", "Score/{Id}", "btn-sm btn-info");
    list.AddRowAction("转班", "Transfer/{Id}", "btn-sm btn-warning", "确定要转班吗？");
}
```

---

## 41.8 如何处理文件上传？

### 问题

实现文件上传和存储。

### 解答

#### 1. 表单配置

```csharp
static StudentController()
{
    // 设置字段为文件上传
    var form = AddFormFields;
    form["Avatar"].Type = "file";
    form["Avatar"].ItemType = "image"; // 图片预览
}
```

#### 2. 处理上传

```csharp
[HttpPost]
public ActionResult Upload(IFormFile file)
{
    if (file == null || file.Length == 0)
        return Json(new { code = 1, message = "请选择文件" });
    
    // 保存文件
    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
    var path = Path.Combine("Uploads", fileName);
    
    using var stream = System.IO.File.Create(path);
    file.CopyTo(stream);
    
    return Json(new { code = 0, data = new { url = $"/Uploads/{fileName}" } });
}
```

#### 3. 使用附件服务

```csharp
// 使用魔方内置的附件服务
var att = new Attachment
{
    FileName = file.FileName,
    Size = (Int32)file.Length,
    Extension = Path.GetExtension(file.FileName),
    ContentType = file.ContentType
};

using var stream = file.OpenReadStream();
att.SaveFile(stream);
att.Insert();

return Json(new { code = 0, data = new { id = att.Id, url = att.Url } });
```

---

## 41.9 如何实现数据审批？

### 问题

实现数据的审批流程。

### 解答

#### 1. 添加审批状态

```csharp
public partial class Order : Entity<Order>
{
    /// <summary>审批状态。0待提交，1待审批，2已通过，3已拒绝</summary>
    public Int32 ApprovalStatus { get; set; }
    
    /// <summary>审批人</summary>
    public Int32 ApprovalBy { get; set; }
    
    /// <summary>审批时间</summary>
    public DateTime ApprovalTime { get; set; }
    
    /// <summary>审批备注</summary>
    public String ApprovalRemark { get; set; }
}
```

#### 2. 添加审批按钮

```csharp
static OrderController()
{
    // 行内审批按钮
    ListFields.AddRowAction("审批", "Approve/{Id}", "btn-sm btn-success");
}
```

#### 3. 审批 Action

```csharp
[HttpPost]
[EntityAuthorize(PermissionFlags.Update)]
public ActionResult Approve(Int32 id, Boolean pass, String remark)
{
    var order = Order.FindById(id);
    if (order == null) return Json(new { code = 1, message = "订单不存在" });
    
    order.ApprovalStatus = pass ? 2 : 3;
    order.ApprovalBy = ManageProvider.User.ID;
    order.ApprovalTime = DateTime.Now;
    order.ApprovalRemark = remark;
    order.Update();
    
    return Json(new { code = 0, message = pass ? "已通过" : "已拒绝" });
}
```

---

## 41.10 如何配置跨域？

### 问题

前后端分离时需要配置跨域访问。

### 解答

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
    
    options.AddPolicy("AllowSpecific", policy =>
    {
        policy.WithOrigins("https://app.example.com", "https://admin.example.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 使用
app.UseCors("AllowAll");

// 或在控制器上指定
[EnableCors("AllowSpecific")]
public class ApiController : ControllerBase { }
```

---

## 41.11 如何处理大文件下载？

### 问题

下载大文件时避免内存溢出。

### 解答

```csharp
[HttpGet("download/{id}")]
public IActionResult Download(Int32 id)
{
    var attachment = Attachment.FindById(id);
    if (attachment == null) return NotFound();
    
    var path = attachment.GetFilePath();
    if (!System.IO.File.Exists(path)) return NotFound();
    
    // 使用 FileStream 流式传输，避免将整个文件加载到内存
    var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
    
    return File(stream, attachment.ContentType, attachment.FileName);
}
```

---

## 41.12 如何调试 SQL 语句？

### 问题

查看和调试 XCode 生成的 SQL 语句。

### 解答

#### 1. 开启 SQL 日志

```csharp
// 代码中设置
XCodeSetting.Current.ShowSQL = true;
XCodeSetting.Current.SQLPath = "Log"; // SQL 日志保存目录

// 或在配置文件中
{
  "XCode": {
    "ShowSQL": true,
    "SQLPath": "Log"
  }
}
```

#### 2. 查看控制台输出

SQL 语句会输出到控制台和日志文件。

#### 3. 使用追踪

```csharp
using var span = DefaultTracer.Instance?.NewSpan("db:query");
var list = Student.FindAll();
// 追踪会记录 SQL 执行时间
```

---

## 本章小结

本章收集了魔方开发中的常见问题和解答，包括：

1. 字段隐藏与显示控制
2. 自定义统计信息
3. 前端集成方案
4. OAuth 扩展
5. 多租户实现
6. 导出格式自定义
7. 操作按钮添加
8. 文件上传处理
9. 数据审批流程
10. 跨域配置
11. 大文件下载
12. SQL 调试

遇到其他问题，可以查阅官方文档或在社区提问。

---

**下一章**：[附录A 配置参考](附录A_配置参考.md) - 完整配置说明
