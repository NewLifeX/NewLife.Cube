---
name: cube-add-page
description: 根据后端 NewLife.Cube 控制器定义，在前端项目创建页面组件。当用户说"新增页面"、"创建页面"、"添加功能页面"、"给 XX 模块加页面"时使用。
---

# cube-add-page

Cube 前端新增页面技能。根据后端控制器定义，在前端对应应用目录创建页面组件，并配置菜单图标、列表展示字段等。

## 核心原则

> **只需创建页面组件文件，不需要关心页面如何注册和渲染。**
> 本框架会自动加载页面、自动注册路由，无需手工配置。

## 技能触发

当需要新增或修改前端页面时使用，例如：
- 后端已有 Controller，前端需要创建对应页面
- 需要配置菜单图标、列表展示字段
- 需要自定义列表页或表单页的行为

## 前置条件

1. **应用是否存在**：先查看前端 `apps/` 目录，按「页面目录结构」判断该区域是独立成应用（情形 B）还是归在某个综合 app 下（情形 A），确认目标 app 目录已存在
2. **若应用未创建**：先调用 `cube-add-app` 技能创建应用
3. **若控制器未创建**：先基于 Model 实体创建 Controller

## 输入参数

| 参数             | 说明                                     | 示例                                    |
| ---------------- | ---------------------------------------- | --------------------------------------- |
| `controllerName` | 控制器名称（PascalCase）                 | `Product`, `Device`, `Alarm`            |
| `area`           | 业务区域（与后端 Area 名一致）           | `Basic`, `Device`, `Demo`              |
| `entityName`     | 实体名称（可选，默认同 controllerName）  | `Product`                               |
| `menuIcon`       | 菜单图标（Element Plus 图标名）          | `Files`, `Setting`, `User`              |
| `listFields`     | 列表页要展示的字段数组（可选，默认全部） | `["Id", "Name", "Code", "Enable"]`      |
| `detailUrl`      | 详情页 URL 模板（可选）                  | `"/{area}/{controller}/Detail?id={Id}"` |

## 页面目录结构

页面位置由**后端控制器所属的区域（Area）** 和**前端 `apps/` 目录的部署形态**共同决定。

1. 确定页面所属的后端 `Area`（区域）和 `Controller`（控制器）。
2. 查看前端 `apps/` 目录，按下列**两种情形**取对应的路径：

### 情形 A：多个区域共用一个应用（或 apps 下只有一个 app）

`views` 下**先按区域、再按控制器**分两级文件夹：

```
{前端项目}/apps/{app-name}/src/views/{area}/{controller}/index.vue
```

**示例**：Admin 区域、User 控制器，应用为 `cube-admin`
→ `apps/cube-admin/src/views/admin/user/index.vue`

### 情形 B：一个区域一个应用（区域即应用）

`views` 下**只有控制器一级**文件夹：

```
{前端项目}/apps/{area-app}/src/views/{controller}/index.vue
```

**示例**：Admin 区域、User 控制器，对应应用为 `admin`
→ `apps/admin/src/views/user/index.vue`

> **判断口诀**：`apps/` 里区域是"文件夹"还是"应用名"？是文件夹 → 情形 A（两级）；是应用名 → 情形 B（一级）。

## 工作流程

### 第一步：确认准备

1. 确认后端 Controller 已创建，继承 `EntityController<TEntity>`
2. 确认 Area 已注册（继承 `AreaBase`，构造函数传入 Area 名）
3. 确认前端应用目录已存在（`apps/{app-name}/`），不存在则调用 `cube-add-app`

### 第二步：配置菜单图标

在后端 Controller 上通过 `[Menu]` 特性设置图标：

```csharp
[Menu(30, true, Icon = "Files")]  // Icon 为 Element Plus 图标名
public class DemoController : EntityController<DemoEntity>
```

> 图标也可通过 Cube 后台 → 菜单管理修改。常用图标：`Files`、`Setting`、`User`、`List`、`Document`、`DataBoard`、`Coin`、`Clock`。

### 第三步：配置列表展示字段

在 Controller 静态构造函数中配置 `ListFields`。常用操作：

| 场景         | 代码                                                 |
| ------------ | ---------------------------------------------------- |
| 移除审计字段 | `ListFields.RemoveCreateField().RemoveUpdateField()` |
| 清空并自定义 | `list.Clear(); foreach(...) list.AddListField(item)` |
| 设置详情链接 | `df.Url = "/{area}/{controller}/Detail?id={Id}"`     |

> 完整 ListFields 方法表、ListField 属性表、AddFormFields/EditFormFields 配置详见 [references/api-and-styling.md](references/api-and-styling.md#listfields-配置)。

### 第四步：创建页面组件（唯一任务）

**本框架路由和 CRUD 由后端自动驱动，你的唯一任务就是创建页面组件文件。**

#### 默认列表页（无需创建前端文件）

若只需标准表格列表 + 弹窗新增/编辑，**无需创建任何前端文件**，后端 Controller 创建完成后刷新即可访问。

#### 自定义页面（需创建 index.vue）

非标准 CRUD 布局、看板、图表、自定义交互等场景，按「页面目录结构」在对应路径创建 `index.vue`：

```
# 情形 A（两级）
apps/{app-name}/src/views/{area}/{controller}/index.vue
# 情形 B（一级）
apps/{area-app}/src/views/{controller}/index.vue
```

创建后**框架自动加载并渲染**，无需以下任何操作：
- ❌ 不要注册路由（框架自动注册 `/{area}/{controller}/{action}/{id?}`）
- ❌ 不要修改 `routes.ts`、`main.ts`
- ❌ 不要配置菜单（后端 `[Menu]` 特性控制）

**必须先提供原型参考**（原型 HTML / 截图 / 详细描述），再根据原型实现 Vue 组件。

**样式规范**：自定义页面**必须**使用 Element Plus CSS token（`--el-*`）或 Cube Layout token（`--cube-layout-*`），禁止硬编码色值与自定义 token。详见 [references/api-and-styling.md](references/api-and-styling.md#自定义页面样式规范)。

**API 对接**：通过 `usePageApi(area, controller)` composable 对接后端 CRUD，无需为每个模块手写 `api/xxx.ts`：

```ts
const api = usePageApi("Demo", "Demo");
const res = await api.getList({ pageIndex: 0, pageSize: 20 });
```

> 完整 usePageApi 方法表、使用示例、错误处理约定、完整示例、枚举字段处理详见 [references/api-and-styling.md](references/api-and-styling.md#自定义页面对接后端-api)。

### 第五步：刷新验证

- 确保后端项目正在运行
- 刷新浏览器，框架自动加载新页面
- 页面路径为 `/{area}/{controller}`，无需手动输入路由配置

## 注意事项

1. **图标名**必须是 Element Plus 图标 PascalCase 名称（如 `Files`、`Setting`），不可用 `fa-` 旧格式
2. **字段名**必须与实体属性名一致，大小写敏感
3. **Area 注册**：Controller 必须加上 `[XxxArea]` 特性
4. **路由由框架自动注册**：不要在 `routes.ts` 中写路由，不要修改 `main.ts`
5. **页面自动加载**：框架扫描 `apps/*/src/views/**/index.vue` 自动匹配后端菜单路由
6. **新增/编辑**默认通过弹窗打开，无需注册独立前端路由
7. **API 调用**：通过 `usePageApi(area, controller)` 对接后端，不需要为每个模块建 `api/xxx.ts`
8. **分页参数**：后端分页从 0 开始，`getList` 需传 `pageIndex: page - 1`；`totalCount` 在 `res.page.totalCount`

## 参考文件

| 文件 | 内容 |
| --- | --- |
| [references/api-and-styling.md](references/api-and-styling.md) | ListFields 配置、自定义页面样式规范、usePageApi 通用 CRUD Composable、错误处理约定、完整示例、枚举字段处理 |