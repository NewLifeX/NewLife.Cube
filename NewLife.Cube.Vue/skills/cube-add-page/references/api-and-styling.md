# 页面 API 对接与样式规范参考

> 从 cube-add-page SKILL.md 拆出的 ListFields 配置、自定义页面样式规范、usePageApi 使用指南、错误处理约定、完整示例和枚举处理。

## ListFields 配置

### 后端 Controller 静态构造函数配置

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

### 常用 ListFields 方法

| 方法                                        | 说明             | 示例                                           |
| ------------------------------------------- | ---------------- | ---------------------------------------------- |
| `list.Clear()`                              | 清空所有字段     | `list.Clear()`                                 |
| `list.AddListField(field)`                  | 添加字段         | `list.AddListField("Name")`                    |
| `ListFields.RemoveField(fields)`            | 移除字段         | `ListFields.RemoveField("Creator", "Updater")` |
| `ListFields.RemoveCreateField()`            | 移除创建审计字段 | `ListFields.RemoveCreateField()`               |
| `ListFields.RemoveUpdateField()`            | 移除更新审计字段 | `ListFields.RemoveUpdateField()`               |
| `ListFields.GetField(name)`                 | 获取字段引用     | `ListFields.GetField("Name") as ListField`     |
| `ListFields.AddListField(name, afterField)` | 在指定字段后追加 | `ListFields.AddListField("Log", "UpdateTime")` |

### ListField 常用属性

| 属性          | 说明         | 示例                                |
| ------------- | ------------ | ----------------------------------- |
| `Url`         | 点击跳转链接 | `"/Area/Controller/Detail?id={Id}"` |
| `Target`      | 链接打开方式 | `"_blank"` / `"_frame"`             |
| `DisplayName` | 显示名称     | `df.DisplayName = "操作"`           |
| `Align`       | 对齐方式     | `df.Align = "center"`               |
| `Width`       | 列宽         | `df.Width = 200`                    |
| `Header`      | 表头标题     | `df.Header = "名称"`                |

### 字段配置速查

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

---

## 自定义页面样式规范

创建自定义页面时，所有样式**必须**使用 Element Plus CSS token（`--el-*`）或 Cube Layout token（`--cube-layout-*`），禁止硬编码色值、自定义 CSS 变量或第三方 token 体系。

**组件优先（页面只用 el-*）：** 页面结构应优先使用 Element Plus 组件，它们自带主题样式并自动跟随明暗主题。自定义 `<style scoped>` **允许使用**，但其中颜色 / 边框 / 背景 / 圆角 / 阴影等**必须**通过 `var(--el-*)` 或 `var(--cube-layout-*)` 引用 token。

**✅ 正确写法：**
```scss
.search-bar {
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--el-border-radius-base);
  color: var(--el-text-color-primary);
}
```

**❌ 错误写法：**
```scss
.search-bar {
  background: #ffffff;           /* 硬编码 */
  border: 1px solid #ebeef5;     /* 硬编码 */
  color: #303133;                /* 硬编码 */
}
.sidebar { background: var(--bg-primary); }   /* 已废弃的自定义 token */
```

### 常用 token 速查

| 语义              | token                                | 用途                   |
| ----------------- | ------------------------------------ | ---------------------- |
| 背景色            | `var(--el-bg-color)`                 | 页面主体背景           |
| 卡片/浮层面板背景 | `var(--el-bg-color-overlay)`         | 弹窗、卡片、下拉面板   |
| 填充色            | `var(--el-fill-color-light)`         | 输入框背景、搜索栏背景 |
| 一级文字色        | `var(--el-text-color-primary)`       | 标题、正文             |
| 二级文字色        | `var(--el-text-color-regular)`       | 次要信息               |
| 三级文字色        | `var(--el-text-color-secondary)`     | 提示文字、占位符       |
| 边框色            | `var(--el-border-color)`             | 表格、卡片边框         |
| 浅边框色          | `var(--el-border-color-light)`       | 分割线、搜索栏边框     |
| 主色              | `var(--el-color-primary)`            | 按钮、链接、激活态     |
| 成功色            | `var(--el-color-success)`            | 成功状态               |
| 警告色            | `var(--el-color-warning)`            | 警告状态               |
| 危险色            | `var(--el-color-danger)`              | 错误、删除             |
| 圆角              | `var(--el-border-radius-base)`       | 卡片、弹窗圆角         |
| 小圆角            | `var(--el-border-radius-small)`      | 按钮、输入框圆角       |
| 浅阴影            | `var(--el-box-shadow-light)`         | 卡片阴影               |
| 深阴影            | `var(--el-box-shadow)`               | 下拉面板、弹窗阴影     |
| 侧边栏宽度        | `var(--cube-layout-sidebar-width)`   | 布局结构               |
| 导航栏高度        | `var(--cube-layout-nav-height)`      | 布局结构               |
| 内容区域内边距    | `var(--cube-layout-content-padding)` | 布局结构               |

---

## 自定义页面对接后端 API

自定义页面中可通过 `usePageApi(area, controller)` composable 快速对接后端 CRUD API。

> **前提**：前端项目需在 `package.json` 中添加 `"@newlifex/api-core": "workspace:*"` 依赖。

### 全局 API 实例

框架提供全局 `cubeApi` 实例（位于 `src/cubeApi.ts`），基于 `@newlifex/api-core` 的 `createCubeApi()` 创建，自动处理 Token 注入、401 跳转、响应拦截、字段名归一化等。

### 通用 CRUD Composable

通过 `usePageApi(area, controller)` 创建页面级 API 对象：

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

### 在页面中使用

```vue
<script setup lang="ts">
import { ref, onMounted } from "vue";
import { ElMessage, ElMessageBox } from "element-plus";
import { usePageApi } from "@/composables/usePageApi";

const api = usePageApi("Demo", "Demo");

const list = ref<Record<string, unknown>[]>([]);
const loading = ref(false);
const total = ref(0);
const currentPage = ref(1);
const pageSize = ref(20);

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

async function handleAdd(data: Record<string, unknown>) {
  await api.add(data);
  ElMessage.success("新增成功");
  await fetchList();
}

async function handleEdit(data: Record<string, unknown>) {
  await api.update(data);
  ElMessage.success("更新成功");
  await fetchList();
}

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
    if (err !== "cancel") console.error("[Demo] 删除失败:", err);
  }
}

onMounted(() => fetchList());
</script>
```

### 错误处理约定（重要）

| 路径 | 入口 | 全局是否自动弹错 | 业务页面是否还需处理 |
| --- | --- | --- | --- |
| 默认模板页 | `@newlifex/cube-vue` 的 `request` | ✅ 是 | ❌ 不需要，catch 里只做复位/日志 |
| 自定义页 | `usePageApi` / `@newlifex/api-core` 的 `cubeApi` | ⚠️ 取决于是否挂 `onBusinessError` | 见下方说明 |

- **默认模板页**：全局拦截器已统一弹错，业务 `catch` 不要再 `ElMessage.error`，否则重复弹两次。正确写法：`catch` 只留 `console.error`，`finally` 复位 loading。
- **自定义页（usePageApi）**：默认没挂 `onBusinessError`，全局不会自动弹错。`catch` **必须**自己处理错误提示；推荐在 `src/api/index.ts` 里统一挂一次 `onBusinessError`，之后各页面 `catch` 只做复位。
- **通用铁律**：`ElMessage.success(...)` 这类**成功**提示可保留；**失败**提示只许全局一处弹。

> 删除操作例外：`ElMessageBox.confirm` 取消时 reject 的是字符串 `'cancel'`，所以 `catch` 里仍需 `if (err !== 'cancel')` 判断。

---

## 完整示例

### 后端 Controller

```csharp
namespace MyApp.Web.Areas.Demo.Controllers;

[Menu(30, true, Icon = "Files")]
[DemoArea]
public class DemoController : EntityController<DemoEntity>
{
    static DemoController()
    {
        var list = ListFields;
        list.Clear();
        var allows = new[] { "Id", "Name", "Code", "Category", "Enable", "Version", "UpdateTime" };
        foreach (var item in allows)
            list.AddListField(item);

        var df = ListFields.GetField("Name") as ListField;
        df.Url = "/Demo/Demo/Detail?id={Id}";
        df.Target = "_blank";

        var logField = ListFields.AddListField("Log", "UpdateTime");
        logField.DisplayName = "日志";
        logField.Url = "/Admin/Log?category=示例&linkId={Id}";
    }
}
```

### 自定义页面组件

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

---

## 枚举字段处理

### 自动渲染（推荐）

Cube 框架的自动表格和下拉选择已内置枚举处理：
- **表格显示**：枚举字段值为 0 且枚举定义中无对应成员时，自动显示 **"未设置"**
- **下拉选择**：枚举选项缺少 0 值成员时，自动添加 **"未设置"** 选项

> 枚举类型通过 `/Cube/Lookup?codes=Type.FullName` 接口获取。

### 自定义页面中使用

通过 `useEnumLabel` composable 统一处理枚举显示：

```ts
import { createEnumLabel, useEnumLookup } from "@/composables/useEnumLabel";
import { usePageApi } from "@/composables/usePageApi";

const api = usePageApi("Equipments", "Equipment");

// 方式一：配合 useEnumLookup 自动获取枚举并生成标签函数
const { labels, loading } = useEnumLookup(api, {
  kind: 'SmartMES.Data.Equipments.EquipmentKinds',
  status: 'SmartMES.Data.Equipments.EquipmentStatus',
});
```

```ts
// 方式二：已有选项数组时直接创建标签函数
const typeOptions = ref([{ value: 1, label: '注塑机' }, { value: 2, label: '机械手' }]);
const typeLabel = createEnumLabel(typeOptions);
```

**`createEnumLabel` 自动处理情况：**

| 输入值                      | 选项中有匹配     | 选项中无匹配 | 说明             |
| --------------------------- | ---------------- | ------------ | ---------------- |
| `null` / `undefined` / `''` | —                | `"未设置"`   | 空值统一显示     |
| `0`                         | 匹配成员的 label | `"未设置"`   | 0 且无对应成员   |
| `1`/`2`/...                 | 匹配成员的 label | 原始值字符串 | 有匹配显示 label |

> **不需要再手写** `if (v == null || v === '' || Number(v) === 0) return '未设置'` 这类重复代码。
