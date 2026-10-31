# 第6章 字段元数据

> 本章介绍魔方的字段元数据系统，它是页面字段展示和控件渲染的核心机制。
> 
> 通过字段元数据，可以灵活控制列表、表单、详情页的字段展示方式。

---

## 6.1 字段元数据接口 GetFields

### kind 参数说明

魔方通过 `GetFields` 方法获取不同场景的字段配置：

```csharp
protected override FieldCollection GetFields(String kind)
{
    var fields = base.GetFields(kind);
    
    switch (kind)
    {
        case "List":        // 列表页字段
            fields.RemoveField("Remark");
            break;
        case "Detail":      // 详情页字段
            break;
        case "AddForm":     // 添加表单字段
            fields.RemoveField("Id", "CreateTime");
            break;
        case "EditForm":    // 编辑表单字段
            break;
    }
    
    return fields;
}
```

**kind 参数值**：

| 值 | 说明 | 使用场景 |
|-----|------|----------|
| List | 列表字段 | 数据列表页 |
| Detail | 详情字段 | 数据详情页 |
| AddForm | 添加表单字段 | 新增数据页 |
| EditForm | 编辑表单字段 | 修改数据页 |

---

## 6.2 ListField（列表字段）

列表字段控制数据表格的列显示：

```csharp
static ProductController()
{
    // 获取列表字段集合
    var list = ListFields;
    
    // 配置名称字段
    var name = list.GetField("Name") as ListField;
    if (name != null)
    {
        name.DisplayName = "产品名称";
        name.Width = "200px";
        name.Align = "left";
        name.HeaderText = "产品名称";
    }
    
    // 配置价格字段（右对齐，带格式化）
    var price = list.GetField("Price") as ListField;
    if (price != null)
    {
        price.Align = "right";
        price.Format = "N2";  // 保留2位小数
    }
    
    // 配置状态字段（使用数据源显示文本）
    var status = list.GetField("Status") as ListField;
    if (status != null)
    {
        status.DataSource = e => ((ProductStatus)(e as Product).Status).GetDescription();
    }
}
```

### ListField 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| Name | String | 字段名称 |
| DisplayName | String | 显示名称 |
| Width | String | 列宽度（如 "100px"、"10%"） |
| Align | String | 对齐方式（left/center/right） |
| Format | String | 格式化字符串 |
| Url | String | 链接 URL 模板 |
| Target | String | 链接目标（_blank/_self） |
| DataSource | Func | 数据源函数 |
| MapField | String | 映射字段名 |
| Sortable | Boolean | 是否可排序 |

### 添加自定义列

```csharp
static ProductController()
{
    // 添加计算列
    ListFields.AddDataField(new ListField
    {
        Name = "TotalAmount",
        DisplayName = "总金额",
        DataSource = e => 
        {
            var p = e as Product;
            return (p.Price * p.Stock).ToString("N2");
        }
    });
    
    // 添加操作按钮列
    ListFields.AddDataField(new LinkField
    {
        Name = "Actions",
        DisplayName = "操作",
        Url = "/Admin/Product/Detail/{Id}",
        Text = "查看"
    });
}
```

---

## 6.3 FormField（表单字段）

表单字段控制添加/编辑表单的输入控件：

```csharp
static ProductController()
{
    // 配置添加表单字段
    var addFields = AddFormFields;
    
    // 名称字段（文本框）
    var name = addFields.GetField("Name") as FormField;
    if (name != null)
    {
        name.DisplayName = "产品名称";
        name.Required = true;
        name.Placeholder = "请输入产品名称";
        name.MaxLength = 50;
    }
    
    // 描述字段（多行文本）
    var desc = addFields.GetField("Description") as FormField;
    if (desc != null)
    {
        desc.Type = "textarea";
        desc.Rows = 5;
    }
    
    // 分类字段（下拉选择）
    var category = addFields.GetField("CategoryId") as FormField;
    if (category != null)
    {
        category.Type = "select";
        category.DataSource = e => Category.FindAllWithCache()
            .Select(c => new { c.Id, c.Name })
            .ToDictionary(c => c.Id.ToString(), c => c.Name);
    }
}
```

### FormField 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| Name | String | 字段名称 |
| DisplayName | String | 显示名称 |
| Type | String | 控件类型 |
| Required | Boolean | 是否必填 |
| ReadOnly | Boolean | 是否只读 |
| Placeholder | String | 占位提示文本 |
| MaxLength | Int32 | 最大长度 |
| Min | Object | 最小值 |
| Max | Object | 最大值 |
| Rows | Int32 | 文本域行数 |
| DataSource | Func | 数据源函数（用于下拉等） |
| DefaultValue | Object | 默认值 |
| HelpText | String | 帮助提示文本 |

---

## 6.4 SearchField（搜索字段）

搜索字段控制列表页的查询条件：

```csharp
static ProductController()
{
    // 添加搜索字段
    var search = ListFields;
    
    // 名称搜索（文本框）
    search.AddSearchField("Name", "名称");
    
    // 状态搜索（下拉选择）
    search.AddSearchField("Status", "状态", SearchFieldType.Select)
        .DataSource = e => typeof(ProductStatus).GetDescription();
    
    // 日期范围搜索
    search.AddSearchField("CreateTime", "创建时间", SearchFieldType.DateRange);
    
    // 分类搜索（关联下拉）
    search.AddSearchField("CategoryId", "分类", SearchFieldType.Select)
        .DataSource = e => Category.FindAllWithCache()
            .ToDictionary(c => c.Id.ToString(), c => c.Name);
}
```

### SearchFieldType 枚举

| 类型 | 说明 | 渲染控件 |
|------|------|----------|
| Text | 文本搜索 | 文本输入框 |
| Select | 下拉选择 | 下拉列表 |
| DateRange | 日期范围 | 日期选择器 |
| Number | 数值范围 | 数值输入框 |
| Boolean | 布尔值 | 是/否选择 |

---

## 6.5 日期选择控件

> 详细教程：https://newlifex.com/cube/cube_daterange

### _DateRange.cshtml 视图

魔方内置日期范围选择视图：

```html
<!-- 使用日期范围部分视图 -->
@Html.Partial("_DateRange", new { 
    name = "CreateTime",
    startValue = ViewBag.dtStart,
    endValue = ViewBag.dtEnd
})
```

### 日期时间查询

在控制器中处理日期查询：

```csharp
protected override IEnumerable<Product> Search(Pager p)
{
    // 获取日期范围参数
    var dtStart = p["dtStart"].ToDateTime();
    var dtEnd = p["dtEnd"].ToDateTime();
    
    // 构建查询
    var exp = new WhereExpression();
    
    if (dtStart > DateTime.MinValue)
        exp &= Product._.CreateTime >= dtStart;
    
    if (dtEnd > DateTime.MinValue)
        exp &= Product._.CreateTime < dtEnd.AddDays(1);  // 包含结束日期当天
    
    return Product.FindAll(exp, p);
}
```

### 预设日期范围

```csharp
// 快捷日期选择
// 今天、昨天、最近7天、最近30天、本月、上月
```

---

## 6.6 字段类型与控件映射

> **一条映射，全皮肤生效**：`ItemType`（含 `FormField.Type` 写法）到控件的映射统一收敛在共享前端包 `@newlifex/cube` 的 `resolveWidget`，与 MVC `_Form_Item` / `_Form_String` / `_Form_Editor` 同口径。八个 SPA 皮肤（NaiveUI/ArcoVue/TDesign/Vuetify/MUI/Shadcn/Svelte/Angular）只负责把抽象 widget 映射到自己的组件库，**不要在皮肤里另写一套 ItemType 判断**；新增 ItemType 时改共享包并补 `packages/cube/tests/resolve-widget.test.cjs`，`tests/skin-compliance.test.cjs` 会校验每个皮肤的控件分支是否齐全。Vue 皮肤在此之上保留了一层更细的控件词表（`mapSelect`/`mapInput`/`multiSelect`/`timeSpan`/`area`），其判定口径与共享引擎一致，**数据换算与地区接口一律复用共享实现**（`timeSpan*`、`AreaCascade`）。
>
> 覆盖范围：文本类（text/textarea/password/email/phone/url/color/code/json/icon/html/markdown）、附件类（image/avatar/file/files/attachment 及 `image-*`/`file-*` 前缀）、选择类（select/radio/singleSelect/multipleSelect/checkbox/lovTable/映射 Map 字段）、日期类（date/datetime/time）、数值类（number/percent/money/decimal(p,s)/GMK）、地区类（area/area1~4 级联）、时长类（`TimeSpan`/`TimeSpan:<格式>`）。

### 文本框 / 文本域

```csharp
// 单行文本框
var field = new FormField
{
    Name = "Name",
    Type = "text",
    MaxLength = 100
};

// 多行文本域
var field = new FormField
{
    Name = "Description",
    Type = "textarea",
    Rows = 5
};

// 密码框
var field = new FormField
{
    Name = "Password",
    Type = "password"
};
```

> **密码只写不读**：`ItemType = "password"` 或字段名以 `Password` 结尾即视为敏感字段。`/meta` 不会把它下发到列表、详情与搜索；新增/编辑表单保留遮蔽输入，编辑时取值为空、留空提交即保持原密码。实体接口返回前统一清空取值，导出也跳过该列。密码有专用修改接口的页面可再把字段从 `EditFormFields` 移除（如用户管理）。

### 下拉选择 / 单选 / 多选

```csharp
// 下拉选择
var field = new FormField
{
    Name = "CategoryId",
    Type = "select",
    DataSource = e => Category.FindAllWithCache()
        .ToDictionary(c => c.Id.ToString(), c => c.Name)
};

// 单选按钮组
var field = new FormField
{
    Name = "Gender",
    Type = "radio",
    DataSource = e => new Dictionary<String, String>
    {
        ["1"] = "男",
        ["2"] = "女"
    }
};

// 多选复选框
var field = new FormField
{
    Name = "Tags",
    Type = "checkbox",
    DataSource = e => Tag.FindAllWithCache()
        .ToDictionary(t => t.Id.ToString(), t => t.Name)
};
```

> **映射字段（Map）**：`RoleName` 这类通过 `Map` 指向 `RoleID` 的虚拟字段，`/meta` 会下发 `mapField`（关联键）、`mapTypeName`（键类型）与 `dataSource`（关联表选项）；前端在字段原位置渲染下拉，提交的是关联键 `RoleID`，列表仍显示名称。控制器设置 `DataSource` 时优先使用（可过滤/排序），未设置时由 `MapProvider` 自动取关联表：行数不超过 `CubeSetting.MaxDropDownList` 才下发，整型键自动补「无」选项，超过该上限则退回键值输入（与 MVC `_Form_Map` 同口径）。关联表很大时给该字段配 `LovCode` 即可改走远程 LOV（同样提交关联键，支持远程搜索与分页）。
>
> **多选字典**：`dataSource` 配合 `multiple`（`ItemType=multipleSelect`，或字段名以 `s` 结尾）渲染多选下拉，提交时前端把数组拼成逗号串（如角色组 `RoleIds`）；搜索区同样按多选渲染并把数组拼成逗号串提交。

### 日期时间选择器

```csharp
// 日期选择
var field = new FormField
{
    Name = "BirthDate",
    Type = "date"
};

// 日期时间选择
var field = new FormField
{
    Name = "CreateTime",
    Type = "datetime"
};

// 时间选择
var field = new FormField
{
    Name = "StartTime",
    Type = "time"
};
```

> **纯日期**：日期字段应显式声明 `ItemType = "date"`。此时表单用纯日期选择器、列表/详情只显示 `yyyy-MM-dd`、搜索用日期区间，不再出现时分秒；生日、班次日期等只到年月日的字段都应如此声明。

### 时长（TimeSpan）

```csharp
// 累计在线时长（秒）
var field = new FormField
{
    Name = "OnlineTime",
    Type = "TimeSpan"
};
```

> **时长按存储值输入**：`ItemType = "TimeSpan"` 的字段本身仍是整数（或小数），单位从字段描述推断——描述含「毫秒/ms」按毫秒、含「小时」按小时、含「分」按分钟，其余按秒（与列表格式化同一口径）。Vue 表单渲染为「时 : 分 : 秒」分栏输入：小时不限位数（可超 24 小时），分/秒限 0~59，毫秒单位的秒保留 3 位小数，对外提交的仍是原始存储值，不做单位换算歧义。`Type = "TimeSpan"`（CLR 时间类型）与时长不是一回事，后者请一律用 `ItemType` 声明。
>
> 八个 SPA 皮肤共用同一份换算（`@newlifex/cube` 的 `timeSpanUnitOfDescription`/`splitTimeSpan`/`joinTimeSpan`）：单位推断、分段回显、进位规则完全一致，列表格式化 `formatTimeSpanCell` 也复用它，因此不会出现「列表显示 6 小时、编辑框按秒回显」。

### 地区级联（area / area1~4）

```csharp
// 省市区三级（默认）：省 → 市 → 区
var field = new FormField { Name = "AreaId", Type = "area" };

// 省略层级时的等价写法与显式层级
var province = new FormField { Name = "ProvinceId", Type = "area1" }; // 只到省
var city = new FormField { Name = "CityId", Type = "area2" };         // 省 → 市
var district = new FormField { Name = "DistrictId", Type = "area3" }; // 省 → 市 → 区
var street = new FormField { Name = "StreetId", Type = "area4" };     // 省 → 市 → 区 → 街道
```

> **级联层级由 ItemType 决定**：`area` 等同 `area3`（省市区选到底），`area1` 只选省、`area2` 到市、`area4` 到街道；提交值是**最后一级地区编号**。数据来自魔方内置接口 `/Cube/AreaChilds?id=（0 取省）` 与 `/Cube/AreaParents?id=&isContainSelf=true`（回显路径），前端级联状态机在 `@newlifex/cube` 的 `AreaCascade`/`createAreaCascade`，八个 SPA 皮肤与 MVC 的 city-picker 行为一致。

### 文件上传 / 图片上传

```csharp
// 文件上传
var field = new FormField
{
    Name = "Attachment",
    Type = "file",
    Accept = ".pdf,.doc,.docx"
};

// 图片上传
var field = new FormField
{
    Name = "Avatar",
    Type = "image",
    Accept = "image/*"
};

// 多文件上传
var field = new FormField
{
    Name = "Files",
    Type = "files",
    Multiple = true
};
```

### 富文本编辑器

```csharp
// 富文本编辑器（使用内置编辑器）
var field = new FormField
{
    Name = "Content",
    Type = "editor"
};

// 指定编辑器类型
var field = new FormField
{
    Name = "Content",
    Type = "editor",
    EditorType = "ueditor"  // 或 "ckeditor"、"tinymce"
};
```

### 地图选点

```csharp
// 地图选点控件
var field = new FormField
{
    Name = "Location",
    Type = "map",
    MapType = "amap"  // 高德地图，或 "bmap" 百度地图
};
```

---

## 6.7 字段扩展与自定义

### 自定义字段类型

创建自定义字段类型：

```csharp
public class ColorField : FormField
{
    public ColorField()
    {
        Type = "color";
    }
    
    public override String Render(Object entity)
    {
        var value = GetValue(entity)?.ToString() ?? "#000000";
        return $"<input type=\"color\" name=\"{Name}\" value=\"{value}\" />";
    }
}

// 使用
static ProductController()
{
    AddFormFields.AddField(new ColorField
    {
        Name = "Color",
        DisplayName = "颜色"
    });
}
```

### 自定义数据源

```csharp
static ProductController()
{
    // 使用 Lambda 表达式
    var field = AddFormFields.GetField("CategoryId") as FormField;
    field.DataSource = e => 
    {
        // 根据当前用户过滤分类
        var user = ManageProvider.User;
        return Category.FindAll(Category._.TenantId == user?.TenantId)
            .ToDictionary(c => c.Id.ToString(), c => c.Name);
    };
    
    // 使用静态方法
    field.DataSource = e => GetCategoryOptions();
}

private static IDictionary<String, String> GetCategoryOptions()
{
    return Category.FindAllWithCache()
        .Where(c => c.Enable)
        .ToDictionary(c => c.Id.ToString(), c => c.Name);
}
```

### 字段联动

实现字段之间的联动效果：

```csharp
// 前端 JavaScript 实现联动
// 当省份变化时，更新城市下拉列表

static ProductController()
{
    // 省份字段
    var province = AddFormFields.GetField("ProvinceId") as FormField;
    province.OnChange = "loadCities(this.value)";
    
    // 城市字段（动态加载）
    var city = AddFormFields.GetField("CityId") as FormField;
    city.DependsOn = "ProvinceId";
}
```

---

## 字段配置优先级

字段配置的优先级从高到低：

1. **控制器静态构造函数配置**
2. **GetFields 方法返回**
3. **实体字段特性（Attribute）**
4. **字段默认行为**

```csharp
// 实体类特性配置
public class Product : Entity<Product>
{
    [DisplayName("产品名称")]
    [Required]
    [StringLength(100)]
    public String Name { get; set; }
    
    [DisplayName("价格")]
    [Range(0, 999999)]
    public Decimal Price { get; set; }
}

// 控制器配置会覆盖实体特性
static ProductController()
{
    var name = AddFormFields.GetField("Name") as FormField;
    name.DisplayName = "商品名称";  // 覆盖实体的 DisplayName
}
```

---

## 本章小结

通过本章学习，你应该掌握了：

1. **字段元数据接口**：GetFields 方法和 kind 参数
2. **ListField**：列表字段的配置和使用
3. **FormField**：表单字段的配置和控件类型
4. **SearchField**：搜索字段的配置
5. **日期选择控件**：日期范围查询的实现
6. **字段类型映射**：各种控件类型的配置方法
7. **字段扩展**：自定义字段类型和数据源

**下一步**：

- 学习 [菜单系统](PERM-菜单系统.md) 了解菜单配置
- 了解 [权限系统](PERM-权限系统.md) 的访问控制

---

## 参考资源

- [魔方日期选择控件](https://newlifex.com/cube/cube_daterange)
