# Cube 协作指令

本指令面向新生命团队（NewLife）开源 Web 快速开发框架 Cube（魔方），帮助 Copilot 在 .NET 项目中正确使用 Cube 进行 Web 管理后台开发。

---

## 1. Cube 定位与边界

### 1.1 技术栈定位

```
NewLife.Core（基础库）
       ↓
   NewLife.XCode（数据中间件）→ xcode.instructions.md
       ↓
   NewLife.Cube（Web 快速开发框架）← 本指令
```

### 1.2 职责边界

| 层级 | 职责 | 指令文件 |
|------|------|---------|
| **NewLife.XCode** | 数据建模、ORM、实体增删改查 | `xcode.instructions.md` |
| **NewLife.Cube** | Web 管理后台、控制器、视图、权限、菜单 | 本指令 |

### 1.3 本指令覆盖范围

**包含**：
- Web 控制器开发与定制
- 列表页、表单页、详情页配置
- 菜单与权限管理
- 魔方区域（Area）管理

**不包含**（由 xcode.instructions.md 负责）：
- 数据模型设计（Model.xml）
- 实体类生成
- 数据库 CRUD 底层操作

---

## 2. 快速开始

### 2.1 创建 Cube 项目

```powershell
# 安装模板
dotnet new install NewLife.Templates

# 创建 Web 项目
dotnet new cube -n {系统名}Web

# 添加数据类库引用
dotnet add reference ../Zero.Data/Zero.Data.csproj
```

### 2.2 自动生成控制器

在 Model.xml 中配置 `CubeOutput` 后，执行 `xcode` 会自动生成：
- 区域类（`*Area.cs`）
- 控制器（`*Controller.cs`）

```xml
<Option>
  <DisplayName>订单管理</DisplayName>
  <CubeOutput>../../OrderWeb/Areas/Order</CubeOutput>
</Option>
```

---

## 3. 控制器开发

### 3.1 控制器基类

| 基类 | 用途 |
|------|------|
| `EntityController<T>` | 标准 CRUD 控制器 |
| `ReadOnlyEntityController<T>` | 只读控制器（日志表等） |

### 3.2 控制器结构

```csharp
[Menu(100, true, Icon = "fa-table")]
[OrderArea]
public class OrderController : EntityController<Order>
{
    static OrderController()
    {
        // 列表字段配置
        ListFields.RemoveCreateField().RemoveRemarkField();
        
        // 添加自定义列
        //{
        //    var df = ListFields.AddListField("details", null, "Remark");
        //    df.DisplayName = "查看明细";
        //    df.Url = "OrderDetail?orderId={Id}";
        //}
    }

    /// <summary>高级搜索</summary>
    protected override IEnumerable<Order> Search(Pager p)
    {
        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();
        var key = p["Q"] + "";

        return Order.Search(start, end, key, p);
    }
}
```

### 3.3 菜单配置

```csharp
[Menu(100, true, Icon = "fa-table")]
```

| 参数 | 说明 |
|------|------|
| 第一个参数 | 菜单排序（数字越大越靠前） |
| 第二个参数 | 是否可见 |
| `Icon` | Font Awesome 图标 |

---

## 4. 列表页配置

### 4.1 ListFields 配置

```csharp
static OrderController()
{
    // 移除字段
    ListFields.RemoveField("Id", "CreateUser");
    ListFields.RemoveCreateField();  // 移除创建者/创建时间
    ListFields.RemoveRemarkField();  // 移除备注
    
    // 添加链接列
    {
        var df = ListFields.GetField("OrderNo") as ListField;
        df.Url = "Order/Detail?id={Id}";
        df.Target = "_blank";
    }
    
    // 添加操作列
    {
        var df = ListFields.AddListField("details", null, "Remark");
        df.DisplayName = "查看明细";
        df.Url = "OrderDetail?orderId={Id}";
        df.DataVisible = e => (e as Order).Details > 0;
    }
}
```

### 4.2 搜索配置

通过 Model.xml 中的 `ShowIn` 属性控制字段是否出现在搜索区。

---

## 5. 表单页配置

### 5.1 FormFields 配置

```csharp
static OrderController()
{
    // 添加表单只读
    {
        var df = AddFormFields.GetField("OrderNo");
        df.ReadOnly = true;
    }
}
```

### 5.2 字段分组

通过 Model.xml 中的 `Category` 属性控制字段分组。

---

## 6. 区域（Area）管理

### 6.1 区域类

```csharp
[DisplayName("订单管理")]
public class OrderArea : AreaBase
{
    public OrderArea() : base(nameof(OrderArea).TrimEnd("Area")) { }
}
```

### 6.2 目录结构

```
Areas/
└── Order/
    ├── OrderArea.cs
    └── Controllers/
        ├── OrderController.cs
        └── OrderDetailController.cs
```

---

## 7. 权限管理

### 7.1 内置权限

Cube 内置用户、角色、菜单、权限管理，支持：
- 基于角色的访问控制（RBAC）
- 菜单级权限
- 按钮级权限（增删改查导出）

### 7.2 权限检查

```csharp
// 控制器中检查权限
if (!HasPermission("OrderExport"))
{
    throw new InvalidOperationException("无导出权限");
}
```

---

## 8. 常用扩展

### 8.1 自定义搜索

```csharp
protected override IEnumerable<Order> Search(Pager p)
{
    var status = p["status"].ToInt(-1);
    var start = p["dtStart"].ToDateTime();
    var end = p["dtEnd"].ToDateTime();
    var key = p["Q"] + "";

    var where = new WhereExpression();
    if (status >= 0) where &= Order._.Status == status;
    where &= Order._.CreateTime.Between(start, end);
    if (!key.IsNullOrEmpty()) where &= Order._.OrderNo.Contains(key) | Order._.Remark.Contains(key);

    return Order.FindAll(where, p);
}
```

### 8.2 导出定制

```csharp
protected override IEnumerable<Order> ExportData(Pager p)
{
    p.PageSize = 10000;  // 导出数量限制
    return Search(p);
}
```

### 8.3 表单保存前处理

```csharp
protected override Int32 OnInsert(Order entity)
{
    entity.OrderNo = GenerateOrderNo();
    return base.OnInsert(entity);
}

protected override Int32 OnUpdate(Order entity)
{
    // 更新前校验
    return base.OnUpdate(entity);
}
```

---

## 9. 与 XCode 的协作

1. **数据模型**：在 `xcode.instructions.md` 指导下设计 Model.xml
2. **控制器生成**：配置 `CubeOutput` 后执行 `xcode` 自动生成
3. **定制开发**：在生成的控制器基础上进行业务定制
4. **字段显示**：通过 Model.xml 的 `ShowIn`、`ItemType`、`Category` 控制

---

## 10. Copilot 行为指引

### 10.1 控制器开发时

1. 继承正确的基类（`EntityController` 或 `ReadOnlyEntityController`）
2. 在静态构造函数中配置 `ListFields`
3. 重写 `Search` 方法实现高级搜索

### 10.2 边界意识

- 数据模型和实体操作 → `xcode.instructions.md`
- Web 控制器和界面 → 本指令
- 基础编码规范 → `copilot-instructions.md`

### 10.3 生成代码后

1. 检查控制器是否正确引用实体类
2. 检查区域类是否正确注册
3. 运行项目验证菜单和页面是否正常显示
