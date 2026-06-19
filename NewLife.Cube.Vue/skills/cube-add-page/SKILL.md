---
name: cube-add-page
description: 根据后端 NewLife.Cube 控制器定义，在前端项目创建页面组件。当用户说"新增页面"、"创建页面"、"添加功能页面"、"给 XX 模块加页面"时使用。
---

# cube-add-page

Cube 前端新增页面技能。根据后端控制器定义，在前端对应应用目录创建页面组件，并配置菜单图标、列表展示字段等。

## 技能触发

当需要新增或修改前端页面时使用，例如：
- 后端已有 Controller，前端需要创建对应页面
- 需要配置菜单图标、列表展示字段
- 需要自定义列表页或表单页的行为

## 前置条件

1. **应用是否存在**：一个后端 Area 对应一个前端应用（在 `{前端项目}/apps/{app-name}/`）
2. **若应用未创建**：先调用 `cube-add-app` 技能创建应用
3. **若控制器未创建**：先基于 Model 实体创建 Controller

## 输入参数

| 参数             | 说明                                     | 示例                                    |
| ---------------- | ---------------------------------------- | --------------------------------------- |
| `controllerName` | 控制器名称（PascalCase）                 | `Product`, `Device`, `Alarm`            |
| `area`           | 业务区域（与后端 Area 名一致）           | `Basic`, `Device`, `Demo`               |
| `entityName`     | 实体名称（可选，默认同 controllerName）  | `Product`                               |
| `menuIcon`       | 菜单图标（Element Plus 图标名）          | `Files`, `Setting`, `User`              |
| `listFields`     | 列表页要展示的字段数组（可选，默认全部） | `["Id", "Name", "Code", "Enable"]`      |
| `detailUrl`      | 详情页 URL 模板（可选）                  | `"/{area}/{controller}/Detail?id={Id}"` |

## 页面目录结构

每个页面在前端应用目录下创建（路径规则：`{area}/{controller}/index.vue`）：

```
{前端项目}/apps/{app-name}/
└── src/
    └── views/
        └── {area}/
            └── {controller}/
                └── index.vue      ← 页面组件
```

> 路由由框架根据后端菜单自动注册 `/{area}/{controller}/{action}/{id?}`，无需手工添加。

## 工作流程

### 第一步：确认准备

1. 确认后端 Controller 已创建，继承 `EntityController<TEntity>`
2. 确认 Area 已注册（继承 `AreaBase`，构造函数传入 Area 名，基类自动调用 `RegisterArea`）
3. 确认前端应用目录已存在（`apps/{app-name}/`），不存在则调用 `cube-add-app`

### 第二步：配置菜单图标

在后端 Controller 上通过 `[Menu]` 特性设置图标：

```csharp
[Menu(30, true, Icon = "Files")]  // Icon 为 Element Plus 图标名
public class DemoController : EntityController<DemoEntity>
```

图标设置后可通过 Cube 后台管理 → 菜单管理 → 修改 `Icon` 字段覆盖。

> 初始图标推荐对应功能的 Element Plus 图标名，如 `Files`、`Setting`、`User`、`List`、`Document`、`DataBoard`、`Coin`、`Clock` 等。

### 第三步：配置列表展示字段

在 Controller 静态构造函数中配置 `ListFields`：

```csharp
static ProductController()
{
    // 1. 清空默认字段，只保留需要的
    var list = ListFields;
    list.Clear();
    var allows = new[] { "Id", "Name", "Code", "Category", "Enable", "Version", "UpdateTime" };
    foreach (var item in allows)
        list.AddListField(item);

    // 2. 设置名称列的详情链接
    var df = ListFields.GetField("Name") as ListField;
    df.Url = "/{area}/{controller}/Detail?id={Id}";
    df.Target = "_blank";

    // 3. 添加自定义操作列（如日志）
    var logField = ListFields.AddListField("Log", "UpdateTime");
    logField.DisplayName = "日志";
    logField.Url = "/Admin/Log?category={实体名}&linkId={Id}";
}
```

**常用 ListFields 方法：**

| 方法                                        | 说明             | 示例                                           |
| ------------------------------------------- | ---------------- | ---------------------------------------------- |
| `list.Clear()`                              | 清空所有字段     | `list.Clear()`                                 |
| `list.AddListField(field)`                  | 添加字段         | `list.AddListField("Name")`                    |
| `ListFields.RemoveField(fields)`            | 移除字段         | `ListFields.RemoveField("Creator", "Updater")` |
| `ListFields.RemoveCreateField()`            | 移除创建审计字段 | `ListFields.RemoveCreateField()`               |
| `ListFields.RemoveUpdateField()`            | 移除更新审计字段 | `ListFields.RemoveUpdateField()`               |
| `ListFields.GetField(name)`                 | 获取字段引用     | `ListFields.GetField("Name") as ListField`     |
| `ListFields.AddListField(name, afterField)` | 在指定字段后追加 | `ListFields.AddListField("Log", "UpdateTime")` |

**ListField 常用属性：**

| 属性          | 说明         | 示例                                |
| ------------- | ------------ | ----------------------------------- |
| `Url`         | 点击跳转链接 | `"/Area/Controller/Detail?id={Id}"` |
| `Target`      | 链接打开方式 | `"_blank"` / `"_frame"`             |
| `DisplayName` | 显示名称     | `df.DisplayName = "操作"`           |
| `Align`       | 对齐方式     | `df.Align = "center"`               |
| `Width`       | 列宽         | `df.Width = 200`                    |
| `Header`      | 表头标题     | `df.Header = "名称"`                |

### 第四步：提供原型参考（先设计，后实现）

本框架路由和 CRUD 由后端自动驱动，新增页面若只需默认列表页（表格 + 弹窗新增/编辑），**无需创建任何前端文件**，后端 Controller 创建完成后刷新即可访问。

若需要**自定义页面**（非标准 CRUD 布局、自定义交互、看板、图表等），必须先提供原型参考：

- 原型 HTML 文件（推荐）
- 页面截图或设计稿
- 详细描述页面布局和交互

根据原型设计出 HTML 原型文件，确认后再实现为 Vue 组件。

> 前端视图组件路径：`apps/{app-name}/src/views/{area}/{controller}/index.vue`
> 路由由框架根据后端菜单自动注册 `/{area}/{controller}/{action}/{id?}`，无需手工添加。

### 第五步：确认路由生效

- 框架在 `CubeService.cs` 中自动注册路由：`/{area}/{controller}/{action}/{id?}`
- 无需在前端 `routes/` 中手工添加路由
- 后端菜单数据自动同步到前端 `menuStore`，侧边栏自动显示

## 完整示例

### 后端 Controller（已创建）

```csharp
namespace MyApp.Web.Areas.Demo.Controllers;

[Menu(30, true, Icon = "Files")]           // 菜单图标
[DemoArea]                                  // Area 注册
public class DemoController : EntityController<DemoEntity>
{
    static DemoController()
    {
        // 列表展示字段
        var list = ListFields;
        list.Clear();
        var allows = new[] { "Id", "Name", "Code", "Category", "Enable", "Version", "UpdateTime" };
        foreach (var item in allows)
            list.AddListField(item);

        // 名称列跳转详情
        var df = ListFields.GetField("Name") as ListField;
        df.Url = "/Demo/Demo/Detail?id={Id}";
        df.Target = "_blank";

        // 日志列
        var logField = ListFields.AddListField("Log", "UpdateTime");
        logField.DisplayName = "日志";
        logField.Url = "/Admin/Log?category=示例&linkId={Id}";
    }
}
```

### 操作说明

| 项       | 位置                                                      | 说明                         |
| -------- | --------------------------------------------------------- | ---------------------------- |
| 菜单图标 | Controller `[Menu(Icon = "Files")]`                       | 使用 Element Plus 图标名     |
| 列表字段 | Controller 静态构造函数 `ListFields`                      | 增删改字段及配置链接         |
| 页面组件 | `apps/{app-name}/src/views/{area}/{controller}/index.vue` | 默认 CRUD 无需创建，自定义页面需提供原型 |
| 路由     | 框架自动注册                                              | 无需手工配置                 |

## 注意事项

1. **图标名**必须是 Element Plus 图标 PascalCase 名称（如 `Files`、`Setting`、`User`、`DataBoard`、`Coin`），不可用 `fa-` 旧格式
2. **字段名**必须与实体属性名一致，大小写敏感
3. **Area 注册**：Controller 必须加上 `[XxxArea]` 特性（即 Area 类名），Area 类继承 `AreaBase` 即可，基类构造函数自动注册，无需手动调用 `RegisterArea`
4. **路由自动注册**：前端不需要在 `routes/index.ts` 中写路由，框架从后端菜单数据自动注册
5. **后端路由**：`/{area}/{controller}/{action}/{id?}`，Cube 自动注册
6. **新增/编辑**默认通过弹窗（对话框）打开，无需注册独立前端路由

## 字段配置速查

### ListFields

| 场景         | 代码                                                 |
| ------------ | ---------------------------------------------------- |
| 移除审计字段 | `ListFields.RemoveCreateField().RemoveUpdateField()` |
| 移除指定字段 | `ListFields.RemoveField("CreatorId", "UpdaterId")`   |
| 清空并自定义 | `list.Clear(); foreach(...) list.AddListField(item)` |
| 添加链接列   | `var df = ListFields.AddListField(name, afterField)` |
| 设置详情 URL | `df.Url = "/{area}/{controller}/Detail?id={Id}"`     |
| 设置目标窗口 | `df.Target = "_blank"`                               |

### AddFormFields / EditFormFields

| 场景         | 代码                                                    |
| ------------ | ------------------------------------------------------- |
| 移除审计字段 | `AddFormFields.RemoveCreateField().RemoveUpdateField()` |
| 移除指定字段 | `EditFormFields.RemoveField("Remark", "CreatorId")`     |
