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
                └── index.vue      ← 唯一需要创建的文件
```

> **框架自动完成：** 路由注册、菜单加载、侧边栏渲染均由框架自动处理，不要手工注册路由。

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

### 第四步：创建页面组件（唯一任务）

**本框架路由和 CRUD 由后端自动驱动。** 你的唯一任务就是创建页面组件文件。

#### 默认列表页（无需创建前端文件）

若只需默认的表格列表 + 弹窗新增/编辑，**无需创建任何前端文件**，后端 Controller 创建完成后刷新即可访问。

#### 自定义页面（需创建 index.vue）

若需要**自定义页面**（非标准 CRUD 布局、自定义交互、看板、图表等），只需在以下路径创建 `index.vue`：

```
apps/{app-name}/src/views/{area}/{controller}/index.vue
```

创建此文件后，**框架会自动加载并渲染此组件**，无需做以下任何操作：
- ❌ 不要注册路由（框架自动注册 `/{area}/{controller}/{action}/{id?}`）
- ❌ 不要修改 `routes.ts` 或 `routes/index.ts`
- ❌ 不要配置菜单（后端 `[Menu]` 特性控制）
- ❌ 不要修改 `main.ts`

#### 必须先提供原型参考

创建自定义页面前，必须先提供原型参考：

- 原型 HTML 文件（推荐）
- 页面截图或设计稿
- 详细描述页面布局和交互

根据原型直接实现为 Vue 组件即可。

### 自定义页面对接后端 API

自定义页面中可通过 `usePageApi(area, controller)` composable 快速对接后端 CRUD API，无需为每个模块手写请求逻辑。

> **前提**：前端项目需在 `package.json` 中添加 `"@cube/api-core": "workspace:*"` 依赖，根目录 `pnpm-workspace.yaml` 包含 `- "Cube/packages/*"`。参考 SmartMES 项目的实现。

#### 全局 API 实例

框架提供全局 `cubeApi` 实例（位于 `src/cubeApi.ts`），基于 `@cube/api-core` 的 `createCubeApi()` 创建，自动处理 Token 注入、401 跳转、响应拦截、字段名归一化等。

```ts
import { createCubeApi } from '@cube/api-core';

const cubeApi = createCubeApi({
  baseURL: import.meta.env.DEV ? '/base-api' : (import.meta.env.VITE_API_URL ?? ''),
  onUnauthorized: () => { window.location.href = '/'; },
});
```

#### 通用 CRUD Composable

通过 `usePageApi(area, controller)` 创建页面级 API 对象，内置路径拼接逻辑：

| 方法                   | 功能                                 | 对应后端接口                            |
| ---------------------- | ------------------------------------ | --------------------------------------- |
| `getPage()`            | 获取页面元数据（字段配置、页面设置） | GET `/{area}/{controller}/GetPage`      |
| `getFields(kind)`      | 获取指定类型的字段列表               | GET `/{area}/{controller}/GetFields`    |
| `getList(params)`      | 分页列表查询                         | GET `/{area}/{controller}`              |
| `getDetail(id)`        | 查看详情                             | GET `/{area}/{controller}/Detail`       |
| `add(data)`            | 新增                                 | POST `/{area}/{controller}`             |
| `update(data)`         | 编辑                                 | PUT `/{area}/{controller}`              |
| `remove(id)`           | 删除单条                             | DELETE `/{area}/{controller}`           |
| `deleteSelect(keys)`   | 批量删除                             | DELETE `/{area}/{controller}`           |
| `uploadFile(file)`     | 上传文件                             | POST `/{area}/{controller}/UploadFile`  |
| `importFile(file)`     | 导入文件                             | POST `/{area}/{controller}/ImportFile`  |
| `getExportUrl(format)` | 获取导出下载 URL                     | 直接返回 URL                            |
| `getChartData()`       | 获取图表数据                         | GET `/{area}/{controller}/GetChartData` |

#### 在页面中使用

```vue
<script setup lang="ts">
import { ref, onMounted } from "vue";
import { ElMessage, ElMessageBox } from "element-plus";
import { usePageApi } from "@/composables/usePageApi";

// 传入区域和控制器名，即得完整 CRUD（路径自动拼接为 /{Area}/{Controller}）
const api = usePageApi("Demo", "Demo");

const list = ref<Record<string, unknown>[]>([]);
const loading = ref(false);
const total = ref(0);
const currentPage = ref(1);
const pageSize = ref(20);

/** 获取分页列表 */
async function fetchList() {
  loading.value = true;
  try {
    const res = await api.getList({
      pageIndex: currentPage.value - 1,
      pageSize: pageSize.value,
    });
    list.value = res.data ?? [];
    total.value = res.page?.totalCount ?? 0;
  } finally {
    loading.value = false;
  }
}

/** 新增 */
async function handleAdd(data: Record<string, unknown>) {
  await api.add(data);
  ElMessage.success("新增成功");
  await fetchList();
}

/** 编辑 */
async function handleEdit(data: Record<string, unknown>) {
  await api.update(data);
  ElMessage.success("更新成功");
  await fetchList();
}

/** 删除（带确认弹窗） */
async function handleDelete(row: Record<string, unknown>) {
  try {
    await ElMessageBox.confirm(
      `确定要删除 "${row.name ?? row.Name}" 吗？`,
      "确认删除",
      { confirmButtonText: "删除", cancelButtonText: "取消", type: "warning" }
    );
    const id = (row.id ?? row.Id) as number;
    await api.remove(id);
    ElMessage.success("删除成功");
    await fetchList();
  } catch (err: any) {
    if (err !== "cancel") ElMessage.error(err?.message || "删除失败");
  }
}

onMounted(() => fetchList());
</script>
```

> **关键设计**：`usePageApi` composable 是唯一的 API 入口，所有自定义页面都通过它调用后端。这样既不需要为每个模块创建 `api/xxx.ts` 文件，也保持了统一的错误处理和 Token 管理。

### 第五步：刷新验证

- 确保后端项目（SmartMES.Web）正在运行
- 刷新浏览器页面，框架会自动加载新页面
- 页面路径为 `/{area}/{controller}`，无需手动输入路由配置

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

### 自定义页面组件

只需在 `apps/{app-name}/src/views/{area}/{controller}/index.vue` 创建 Vue 组件，框架自动加载。

```vue
<template>
  <div>
    <el-table :data="list" v-loading="loading" stripe>
      <el-table-column prop="name" label="名称" />
      <el-table-column prop="code" label="编码" />
      <el-table-column label="操作">
        <template #default="{ row }">
          <el-button link type="primary" @click="handleEdit(row)">编辑</el-button>
          <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import { ElMessage } from "element-plus";
import { usePageApi } from "@/composables/usePageApi";

const api = usePageApi("Demo", "Demo");
const list = ref<Record<string, unknown>[]>([]);
const loading = ref(false);

async function fetchList() {
  loading.value = true;
  try {
    const res = await api.getList({ pageIndex: 0, pageSize: 20 });
    list.value = res.data ?? [];
  } finally {
    loading.value = false;
  }
}

async function handleEdit(row: Record<string, unknown>) {
  await api.update(row);
  ElMessage.success("更新成功");
  await fetchList();
}

async function handleDelete(row: Record<string, unknown>) {
  await api.remove(row.id as number);
  ElMessage.success("删除成功");
  await fetchList();
}

onMounted(() => fetchList());
</script>
```

### 操作说明

| 项       | 位置                                                      | 说明                                         |
| -------- | --------------------------------------------------------- | -------------------------------------------- |
| 菜单图标 | Controller `[Menu(Icon = "Files")]`                       | 使用 Element Plus 图标名                     |
| 列表字段 | Controller 静态构造函数 `ListFields`                      | 增删改字段及配置链接                         |
| API 对接 | 页面内 `usePageApi(area, controller)`                     | 传入区域+控制器名即得完整 CRUD，无需手写请求 |
| 页面组件 | `apps/{app-name}/src/views/{area}/{controller}/index.vue` | 默认 CRUD 无需创建，自定义页面需提供原型     |
| 路由     | **框架自动注册，不要手工配置**                            | 无需关心                                     |

## 注意事项

1. **图标名**必须是 Element Plus 图标 PascalCase 名称（如 `Files`、`Setting`、`User`、`DataBoard`、`Coin`），不可用 `fa-` 旧格式
2. **字段名**必须与实体属性名一致，大小写敏感
3. **Area 注册**：Controller 必须加上 `[XxxArea]` 特性（即 Area 类名），Area 类继承 `AreaBase` 即可，基类构造函数自动注册，无需手动调用 `RegisterArea`
4. **路由由框架自动注册**：前端不要在 `routes.ts` 中写路由，不要修改 `main.ts`，只需创建 views 目录下的 `index.vue`
5. **页面自动加载**：框架扫描 `apps/*/src/views/{area}/{controller}/index.vue` 并自动匹配后端菜单路由
6. **新增/编辑**默认通过弹窗（对话框）打开，无需注册独立前端路由
7. **不需要手工注册任何东西**：只需创建 `index.vue` 文件，刷新浏览器即可看到效果
8. **API 调用**：自定义页面通过 `usePageApi(area, controller)` 对接后端，该 composable 包装了 `@cube/api-core` 的通用 CRUD 方法，**不需要为每个模块建 `api/xxx.ts` 文件**
9. **分页参数**：后端分页从 0 开始，`getList` 需传 `pageIndex: page - 1`；`totalCount` 在 `res.page.totalCount` 中

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
