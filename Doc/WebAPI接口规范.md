# 第36章 WebAPI接口规范

> 本章介绍魔方 WebAPI 的接口规范，包括统一响应格式、标准 CRUD 接口、分页排序、异常处理等。

---

## 36.1 统一响应格式（ApiResponse）

### 响应结构

魔方 WebAPI 统一使用以下响应格式：

```json
{
    "code": 0,
    "message": "操作成功",
    "data": { },
    "page": { },
    "stat": { }
}
```

### 字段说明

| 字段 | 类型 | 说明 |
|------|------|------|
| `code` | Int32 | 状态码，0 表示成功，非 0 表示错误 |
| `message` | String | 提示消息 |
| `data` | Object | 返回数据 |
| `page` | Object | 分页信息（列表接口） |
| `stat` | Object | 统计信息（可选） |

### 状态码定义

| 状态码 | 说明 |
|--------|------|
| 0 | 成功 |
| 400 | 参数错误 |
| 401 | 未登录或登录超时 |
| 403 | 无权限 |
| 404 | 资源不存在 |
| 500 | 服务器内部错误 |

### 响应示例

**成功响应**
```json
{
    "code": 0,
    "message": "查询成功",
    "data": [
        { "id": 1, "name": "张三" },
        { "id": 2, "name": "李四" }
    ],
    "page": {
        "pageIndex": 1,
        "pageSize": 20,
        "totalCount": 100
    }
}
```

**错误响应**
```json
{
    "code": 401,
    "message": "没有登录或登录超时！"
}
```

---

## 36.2 标准 CRUD 接口

### GET Index（列表查询）

**请求**
```
GET /api/student?pageIndex=1&pageSize=20&sort=Id&desc=true&classId=1&Q=张
```

**参数**
| 参数 | 类型 | 说明 |
|------|------|------|
| pageIndex | Int32 | 页码，从1开始 |
| pageSize | Int32 | 每页大小，默认20 |
| sort | String | 排序字段 |
| desc | Boolean | 是否降序 |
| Q | String | 搜索关键词 |
| 其他 | * | 业务筛选参数 |

**响应**
```json
{
    "code": 0,
    "data": [
        {
            "id": 1,
            "name": "张三",
            "classId": 1,
            "className": "一年级1班"
        }
    ],
    "page": {
        "pageIndex": 1,
        "pageSize": 20,
        "totalCount": 100,
        "pageCount": 5
    }
}
```

### GET Detail（详情）

**请求**
```
GET /api/student/1
```

**响应**
```json
{
    "code": 0,
    "data": {
        "id": 1,
        "name": "张三",
        "classId": 1,
        "className": "一年级1班",
        "birthday": "2010-01-01",
        "remark": "优秀学生"
    }
}
```

### POST Insert（新增）

**请求**
```
POST /api/student
Content-Type: application/json

{
    "name": "张三",
    "classId": 1,
    "birthday": "2010-01-01"
}
```

**响应**
```json
{
    "code": 0,
    "message": "添加成功！",
    "data": {
        "id": 1,
        "name": "张三",
        "classId": 1
    }
}
```

### PUT Update（修改）

**请求**
```
PUT /api/student/1
Content-Type: application/json

{
    "id": 1,
    "name": "张三（修改）",
    "classId": 2
}
```

**响应**
```json
{
    "code": 0,
    "message": "保存成功！"
}
```

### DELETE Delete（删除）

**请求**
```
DELETE /api/student/1
```

**响应**
```json
{
    "code": 0,
    "message": "删除成功！"
}
```

---

## 36.3 分页与排序

### Pager 分页器

```csharp
/// <summary>分页参数</summary>
public class Pager
{
    /// <summary>页码，从1开始</summary>
    public Int32 PageIndex { get; set; } = 1;
    
    /// <summary>每页大小</summary>
    public Int32 PageSize { get; set; } = 20;
    
    /// <summary>排序字段</summary>
    public String Sort { get; set; }
    
    /// <summary>是否降序</summary>
    public Boolean Desc { get; set; }
    
    /// <summary>总记录数</summary>
    public Int64 TotalCount { get; set; }
    
    /// <summary>总页数</summary>
    public Int32 PageCount => (Int32)Math.Ceiling((Double)TotalCount / PageSize);
}
```

### 分页响应格式

```json
{
    "page": {
        "pageIndex": 1,
        "pageSize": 20,
        "totalCount": 100,
        "pageCount": 5,
        "sort": "Id",
        "desc": true
    }
}
```

### 排序参数

```
# 单字段排序
?sort=Name&desc=false

# 复合排序（使用 OrderBy 参数）
?orderBy=ClassId,Name desc
```

---

## 36.4 搜索与过滤

### 通用搜索参数

| 参数 | 说明 | 示例 |
|------|------|------|
| Q | 关键词搜索 | `?Q=张三` |
| dtStart | 开始时间 | `?dtStart=2024-01-01` |
| dtEnd | 结束时间 | `?dtEnd=2024-12-31` |

### 字段过滤

```
# 精确匹配
?classId=1

# 模糊匹配（需后端支持）
?name=张

# 多值匹配
?status=1,2,3
```

### 高级搜索

```csharp
// 控制器中实现高级搜索
protected override IEnumerable<Student> Search(Pager p)
{
    var classId = p["classId"].ToInt(-1);
    var name = p["Q"];
    var start = p["dtStart"].ToDateTime();
    var end = p["dtEnd"].ToDateTime();
    var status = p["status"];
    
    var exp = new WhereExpression();
    
    if (classId > 0) exp &= Student._.ClassId == classId;
    if (!name.IsNullOrEmpty()) exp &= Student._.Name.Contains(name);
    if (start > DateTime.MinValue) exp &= Student._.CreateTime >= start;
    if (end > DateTime.MinValue) exp &= Student._.CreateTime < end.AddDays(1);
    if (!status.IsNullOrEmpty())
    {
        var arr = status.SplitAsInt();
        exp &= Student._.Status.In(arr);
    }
    
    return Student.FindAll(exp, p);
}
```

---

## 36.5 ApiFilter 过滤器

### 功能说明

`ApiFilterAttribute` 自动包装响应结果和异常：

```csharp
[ApiFilter]
[Route("api/[controller]")]
public class MyController : ControllerBase
{
    [HttpGet]
    public Object GetData()
    {
        return new { name = "test" };
    }
    // 自动包装为：{ code: 0, data: { name: "test" } }
    
    [HttpGet("error")]
    public Object GetError()
    {
        throw new Exception("出错了");
    }
    // 自动包装为：{ code: 500, message: "出错了" }
}
```

### 过滤器实现

```csharp
public sealed class ApiFilterAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // 加载访问令牌
        var token = context.HttpContext.LoadToken();
        context.HttpContext.Items["Token"] = token;
    }
    
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is ObjectResult obj)
        {
            context.Result = new JsonResult(new
            {
                code = obj.StatusCode ?? 0,
                data = obj.Value
            });
        }
        
        if (context.Exception != null && !context.ExceptionHandled)
        {
            var ex = context.Exception.GetTrue();
            context.Result = new JsonResult(new
            {
                code = ex is ApiException ae ? ae.Code : 500,
                message = ex.Message
            });
            context.ExceptionHandled = true;
        }
    }
}
```

---

## 36.6 异常处理与错误码（CubeCode）

### 错误码枚举

```csharp
/// <summary>魔方错误码</summary>
public enum CubeCode
{
    /// <summary>成功</summary>
    Ok = 0,
    
    /// <summary>参数错误</summary>
    BadRequest = 400,
    
    /// <summary>未授权</summary>
    Unauthorized = 401,
    
    /// <summary>禁止访问</summary>
    Forbidden = 403,
    
    /// <summary>未找到</summary>
    NotFound = 404,
    
    /// <summary>服务器错误</summary>
    InternalError = 500,
    
    /// <summary>验证失败</summary>
    ValidationFailed = 1001,
    
    /// <summary>数据已存在</summary>
    DataExists = 1002,
    
    /// <summary>数据不存在</summary>
    DataNotFound = 1003,
    
    /// <summary>操作失败</summary>
    OperationFailed = 1004
}
```

### 抛出业务异常

```csharp
// 使用 ApiException
throw new ApiException(403, "无权限访问");

// 使用 XException
throw new XException("数据不存在");

// 使用 ArgumentException
throw new ArgumentException("参数不能为空", nameof(name));
```

### 异常响应格式

```json
{
    "code": 403,
    "message": "无权限访问"
}
```

---

## 36.7 WebApi 版实体修改的巧妙实现

### 问题背景

前后端分离时，前端可能只提交部分字段，如何保证只更新提交的字段？

### 解决方案

魔方的 `EntityModelBinder` 会先从数据库查询实体，再绑定提交的字段：

```csharp
// EntityModelBinder.CreateModel 方法
protected virtual Object CreateModel(ModelBindingContext bindingContext)
{
    // 对于编辑操作，先查询实体
    if (act.ActionName != "Add")
    {
        var id = bindingContext.ValueProvider.GetValue(uk.Name);
        entity = fact.FindByKeyForEdit(id.ToString());
    }
    
    return entity ?? fact.Create(true);
}
```

### 使用示例

```csharp
// 前端只提交需要修改的字段
PUT /api/student/1
{
    "id": 1,
    "name": "张三（修改）"
    // 其他字段不提交
}

// 控制器
[HttpPut("{id}")]
public ActionResult Edit(Student model)
{
    // model 已经从数据库查询出来
    // 只有 name 被修改，其他字段保持原值
    
    model.Update();
    return Ok();
}
```

### 完整流程

1. 前端提交：`{ id: 1, name: "新名字" }`
2. EntityModelBinder 根据 id 查询数据库
3. 绑定提交的 name 字段到实体
4. 实体的其他字段保持数据库原值
5. 调用 Update() 只更新有变化的字段

---

## 36.8 字段元数据接口

### GetFields 接口

```
GET /api/student/getfields?kind=1
```

**kind 参数**
| 值 | 说明 |
|----|------|
| 1 | List - 列表字段 |
| 2 | Detail - 详情字段 |
| 3 | AddForm - 添加表单字段 |
| 4 | EditForm - 编辑表单字段 |
| 5 | Search - 搜索字段 |

**响应**
```json
{
    "code": 0,
    "data": [
        {
            "name": "Id",
            "displayName": "编号",
            "type": "Int32",
            "nullable": false,
            "primaryKey": true
        },
        {
            "name": "Name",
            "displayName": "姓名",
            "type": "String",
            "nullable": false,
            "length": 50
        },
        {
            "name": "ClassId",
            "displayName": "班级",
            "type": "Int32",
            "dataSource": {
                "1": "一年级1班",
                "2": "一年级2班"
            }
        }
    ]
}
```

### 前端动态渲染

```javascript
// 根据字段元数据动态生成表单
const fields = await getFields('EditForm');

fields.forEach(field => {
    if (field.dataSource) {
        // 渲染下拉框
        renderSelect(field);
    } else if (field.type === 'DateTime') {
        // 渲染日期选择器
        renderDatePicker(field);
    } else if (field.type === 'Boolean') {
        // 渲染开关
        renderSwitch(field);
    } else {
        // 渲染文本框
        renderInput(field);
    }
});
```

---

## 本章小结

本章介绍了魔方 WebAPI 的接口规范：

1. **统一响应格式**：code/message/data/page/stat
2. **标准 CRUD 接口**：GET/POST/PUT/DELETE
3. **分页与排序**：Pager 参数
4. **搜索与过滤**：通用搜索和字段过滤
5. **ApiFilter 过滤器**：自动包装响应
6. **异常处理**：错误码和异常响应
7. **实体修改**：部分字段更新机制
8. **字段元数据**：动态表单支持

遵循这些规范，可以构建一致性良好的 API 接口。

---

**下一章**：[Swagger文档](Swagger文档.md) - 了解 Swagger 文档的配置
