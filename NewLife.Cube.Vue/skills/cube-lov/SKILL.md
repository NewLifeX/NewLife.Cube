---
name: cube-lov
description: LOV（List of Values）值集系统使用指南。涵盖后端配置、前端组件、API 封装、枚举自动注册全链路。当用户说"配置值集"、"使用LovSelect"、"添加LOV"、"值集选择"、"枚举下拉"、"使用LOV"时使用。
---

# cube-lov

Cube LOV（List of Values）值集系统使用指南。值集用于统一管理枚举型和列表型下拉选项，贯穿后端定义 → 前端渲染 → 列翻译全链路。

## 核心原则

- **值集码只出现在后端**：前端通过 GetPage 元数据「发现」lovCode，不硬编码
- **枚举自动注册**：启动时扫描配置的命名空间，自动注册所有 C# 枚举为值集
- **两种类型**：ENUM（枚举型，options 内联）和 LIST（列表型，代理查询）
- **完全限定名**：LovCode 格式 `Enum.{FullNamespace}.{EnumName}`，如 `Enum.SmartMES.Data.ProcessCard.ProcessCardStatus`
- **代理 vs 直连**：LIST 型值集可取数自两种方式。`proxyRequest=true` 时由后端 `/Admin/Lov/ListData` 经 `ILovListDataProxy` 代理转发外部数据源（隐藏真实地址、规避跨域）；`proxyRequest=false`（默认）时前端直接请求 `requestUrl`。当 `requestUrl` 以 `/` 开头（同源同应用接口，如角色列表）时，前端强制直连、禁止代理。

## 架构概览

```
后端 (C#)                             前端 (Vue 3)
══════════════                       ══════════════

LovAutoRegisterService               LovSelect.vue
  ├─ 启动时扫描枚举                     ├─ code prop → Meta API
  └─ 自动写入 LovDefinition            ├─ ENUM → el-select 下拉
                                      └─ LIST → LovSelectTable 弹窗
LovController
  ├─ Meta API     ◄──── GET ──────   fetchLovMeta()
  ├─ ListData API ◄──── POST ──────  fetchLovListData()
  └─ BatchLabel   ◄──── POST ──────  fetchBatchLabel()

ILovListDataProxy (默认 DefaultLovListDataProxy)
  └─ ListData 经此接口转发外部请求（HTTP 客户端），可经 IOC 覆盖自定义实现

前端取数分支（LovSelectTable.fetchListData）：
  ├─ shouldDirectRequest(config) == true  → fetchLovListDataDirect() 前端直连 requestUrl
  └─ false                                → fetchLovListData() 走后端 /Admin/Lov/ListData 代理
  （requestUrl 以 / 开头 → 强制直连）

Controller 静态构造器                  LovSelectTable.vue
  └─ 设置字段 LovCode                  ├─ 弹窗内搜索栏
       └─ GetPage 响应携带 lovCode     ├─ 数据表格 + 分页
                                      └─ 列值自动翻译
```

## 配置步骤

### 第一步：启用枚举自动注册

在 `Program.cs` 配置枚举扫描命名空间：

```csharp
// SmartMES.Web/Program.cs
builder.Services.AddCubeLov(config =>
{
    config.ScanNamespace("SmartMES.Data");
    config.ScanNamespace("SmartMES.Core");
});
```

启动时自动扫描指定命名空间下的所有 `public enum`，生成 LovCode = `Enum.{FullNamespace}.{EnumName}`，并同步枚举值到 `LovEnumItem` 表。

日志输出示例：
```
Lov: 检测到枚举 SmartMES.Data.ProcessCard.ProcessCardStatus → LovCode=Enum.SmartMES.Data.ProcessCard.ProcessCardStatus
Lov: 自动注册值集 Enum.SmartMES.Data.ProcessCard.ProcessCardStatus
```

### 第二步：在 Controller 中为字段配置 LovCode

在静态构造器中，为需要值集渲染的字段设置 `LovCode`：

```csharp
// SmartMES.Web/Areas/ProcessCard/Controllers/ProcessCardController.cs
static ProcessCardController()
{
    // ... 已有的字段配置 ...

    // LOV 值集配置：状态字段（通过类型 FullName 自动生成 LovCode，避免硬编码）
    SearchFields.GetField(_.Status).LovCode = $"Enum.{typeof(ProcessCardStatus).FullName}";

    // 字段类型：搜索字段 / 列表字段 / 表单字段均可
    ListFields.GetField(_.Status).LovCode = $"Enum.{typeof(ProcessCardStatus).FullName}";
    AddFormFields.GetField(_.Status).LovCode = $"Enum.{typeof(ProcessCardStatus).FullName}";
    EditFormFields.GetField(_.Status).LovCode = $"Enum.{typeof(ProcessCardStatus).FullName}";
    DetailFields.GetField(_.Status).LovCode = $"Enum.{typeof(ProcessCardStatus).FullName}";
}
```

> `typeof(TEnum).FullName` 会自动生成完全限定名如 `SmartMES.Data.ProcessCard.ProcessCardStatus`，最终 LovCode = `Enum.SmartMES.Data.ProcessCard.ProcessCardStatus`。

### 第三步：前端使用 LovSelect 组件

**方式 A：直接使用（已知 lovCode 的页面）**

```vue
<script setup lang="ts">
import LovSelect from '@newlifex/cube-vue/core/components/LovSelect.vue';
import { ref } from 'vue';

const filterStatus = ref('');
</script>

<template>
  <LovSelect
    code="Enum.SmartMES.Data.ProcessCard.ProcessCardStatus"
    v-model="filterStatus"
    placeholder="全部状态"
    style="width: 140px"
    clearable
  />
</template>
```

**方式 B：通过 GetPage 元数据驱动（推荐的「不硬编码」方式）**

```vue
<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { usePageApi } from '@/composables/usePageApi';
import LovSelect from '@newlifex/cube-vue/core/components/LovSelect.vue';

const api = usePageApi("AreaName", "ControllerName");
const filterStatus = ref('');

const pageMeta = ref<{ search?: Array<{ name: string; lovCode?: string }> } | null>(null);

async function fetchPageMeta() {
  try {
    const res = await api.getAction('GetPage');
    pageMeta.value = (res as any)?.data ?? null;
  } catch {
    pageMeta.value = null;
  }
}

const hasLovStatus = computed(() =>
  pageMeta.value?.search?.some(f => f.name === 'status' && f.lovCode) ?? false
);

const statusLovCode = computed(() => {
  const field = pageMeta.value?.search?.find(f => f.name === 'status');
  return field?.lovCode ?? '';
});

onMounted(() => { fetchPageMeta(); });
</script>

<template>
  <LovSelect
    v-if="hasLovStatus"
    :code="statusLovCode"
    v-model="filterStatus"
    placeholder="全部状态"
    clearable
  />
  <!-- 降级：GetPage 失败时使用硬编码 -->
  <el-select v-else v-model="filterStatus" placeholder="全部状态">
    <el-option label="草稿" :value="0" />
    <el-option label="已发布" :value="3" />
  </el-select>
</template>
```

### 第四步：列表型值集用 [LovList] 特性自动注册

除手动在后台配置 LIST 型值集外，可直接在控制器 Action 上标注 `[LovList]` 特性，让 `LovAutoRegisterService` 在启动初始化枚举时一并自动注册列表型值集（Type=LIST, Source=AUTO，含数据源配置、表格列、搜索字段）。

```csharp
// CubeDemo/Areas/Test/Controllers/TestFieldController.cs
[LovList(
    LovCode = "List.CubeDemo.Role",
    Name = "角色",
    RequestUrl = "/Test/TestField/RoleList",   // 同应用接口（以 / 开头）→ 前端直连，不代理
    Method = "GET",
    Pageable = false,
    ValueField = "id",
    LabelField = "name",
    DataPath = "data",                          // 从响应 { data:[...], total:N } 中取数组
    TotalPath = "total",
    ProxyRequest = false,                       // 同应用接口不代理
    Columns = new[] { "id:编号:80:left", "name:角色名:200:left" },
    SearchFields = new[] { "name:角色名:input:BODY:false" }
)]
[HttpGet]
public Object RoleList()
{
    var list = Role.FindAll();
    // 显式字典投影，确保序列化键为小驼峰 id/name，与 ValueField/LabelField 对齐
    var data = list.Select(r => new Dictionary<String, Object>
    {
        ["id"] = r.ID,
        ["name"] = r.Name,
    }).ToList();
    return new { Data = (Object)data, Total = data.Count };
}
```

约定：
- `LovCode` 必须以 `List.` 开头；`Source=AUTO` 的值集启动时自动同步，手工管理（`MANUAL`）的不覆盖。
- `Columns` 格式 `"Field:Title:Width:Align"`（Width/Align 可省略）；`SearchFields` 格式 `"Field:Title:ComponentType:ParamType:Required"`（Required 可省略）。
- `RequestUrl` 指向同应用接口（以 `/` 开头）时，`ProxyRequest` 应为 `false`，前端直连、不暴露也无需后端代理。
- 字段侧与普通 LIST 值集完全一致：设置 `LovCode = "List.CubeDemo.Role"` 即可。

## 后端 LovController API

所有 API 由 Cube 框架的 `LovController` 提供，路由前缀 `/Admin/Lov/`。

| 接口 | 方法 | 地址 | 用途 |
|------|------|------|------|
| Meta | GET | `/Admin/Lov/Meta?lovCode=xxx` | 获取值集元数据 |
| ListData | POST | `/Admin/Lov/ListData` | 列表型值集代理查询 |
| BatchLabel | POST | `/Admin/Lov/BatchLabel` | 批量值翻译 |

### Meta 响应结构

```json
{
  "meta": [
    {
      "lovCode": "Enum.SmartMES.Data.ProcessCard.ProcessCardStatus",
      "type": "ENUM",
      "name": "工艺卡状态",
      "options": [
        { "value": "0", "label": "草稿" },
        { "value": "3", "label": "已发布" }
      ]
    }
  ],
  "inlineEnums": {
    "Enum.SmartMES.Data.ProcessCard.EnableStatus": [
      { "value": "0", "label": "禁用" },
      { "value": "1", "label": "启用" }
    ]
  }
}
```

### ListData 请求/响应

`ListData` 仅用于**代理请求**（`proxyRequest=true`）。前端直连（`proxyRequest=false` 或 `requestUrl` 以 `/` 开头）时不会调用此接口，而是由 `fetchLovListDataDirect()` 直接请求 `requestUrl`。

```json
// POST /Admin/Lov/ListData
// Request:
{ "lovCode": "List.User", "params": { "name": "张" }, "pageNum": 1, "pageSize": 20 }
// Response:
{ "data": [{ "id": 1, "name": "张三" }], "total": 1 }
```

> 代理转发逻辑封装在 `ILovListDataProxy` 接口，默认实现 `DefaultLovListDataProxy` 以 `IHttpClientFactory` 发起 GET/POST 请求，支持分页参数、固定参数（`FixedParams`）与 `DataPath`/`TotalPath` 路径解析。使用者可通过 `services.AddCubeLov(...)` 注册自定义 `ILovListDataProxy` 实现来替换默认转发行为。

### BatchLabel 请求/响应

```json
// POST /Admin/Lov/BatchLabel
// Request:
{ "lovCode": "Enum.Status", "values": ["0", "1", "2"] }
// Response:
{ "0": "草稿", "1": "试模中", "2": "试模合格待审批" }
```

## 前端类型定义

完整类型定义位于 `@newlifex/cube-vue/core/types/lov.ts`：

```typescript
import type {
  LovEnumOption,        // 枚举选项 { value, label, extra? }
  LovListConfig,        // 列表数据源配置
  LovSearchField,       // 搜索字段配置
  LovTableColumn,       // 表格列配置
  LovMetaItem,          // 值集元数据联合类型
  LovEnumMeta,          // ENUM 类型元数据
  LovListMeta,          // LIST 类型元数据
  LovMetaResponse,      // Meta 接口完整响应
  LovListDataRequest,   // ListData 请求参数
  LovListDataResponse,  // ListData 响应
  LovBatchLabelRequest, // BatchLabel 请求参数
  LovBatchLabelResponse,// BatchLabel 响应
} from '@newlifex/cube-vue/core/types/lov';
```

## 前端 API 封装

位于 `@newlifex/cube-vue/core/utils/lov-api.ts`：

```typescript
import { fetchLovMeta, fetchLovListData, fetchBatchLabel, resolveLovType } from '@newlifex/cube-vue/core/utils/lov-api';

// 获取值集元数据
const meta = await fetchLovMeta('Enum.SmartMES.Data.ProcessCard.ProcessCardStatus');

// 列表型数据查询
const data = await fetchLovListData({ lovCode: 'List.User', params: { name: '张' } });

// 批量翻译
const labels = await fetchBatchLabel({ lovCode: 'Enum.Status', values: ['0','1','2'] });

// 解析 LovCode 类型
resolveLovType('Enum.xxx')  // => 'ENUM'
resolveLovType('List.xxx')  // => 'LIST'
```

## 组件 Props

### LovSelect

| Prop | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `code` | `string` | — | 值集编码（必填） |
| `modelValue` | `string \| number` | — | v-model 值 |
| `placeholder` | `string` | `'请选择'` | 占位文本 |
| `clearable` | `boolean` | `true` | 是否可清除 |
| `disabled` | `boolean` | `false` | 是否禁用 |
| `size` | `'large'\|'default'\|'small'` | — | 尺寸 |

### LovSelectTable（弹窗）

| Prop | 类型 | 说明 |
|------|------|------|
| `dialogVisible` | `boolean` | 弹窗显示状态（v-model） |
| `lovCode` | `string` | 值集编码 |
| `lovMeta` | `LovListMeta \| null` | 列表型元数据 |
| `inlineEnums` | `Record<string, LovEnumOption[]>` | 内联枚举 |
| `translateCache` | `Map<string, string>` | 翻译缓存 |

## 数据模型

| 表 | 说明 | 关键字段 |
|----|------|----------|
| `LovDefinition` | 值集定义 | LovCode(200), Name, Type(ENUM/LIST), ValueField, LabelField, Source(AUTO/MANUAL), Enabled |
| `LovEnumItem` | 枚举值 | LovDefId, Value, Label, Sort, Enabled, Extra |
| `LovListConfig` | 列表数据源配置 | LovDefId, RequestUrl, Method, Pageable, DataPath, TotalPath, ProxyRequest, FixedParams |
| `LovSearchField` | 列表搜索字段 | LovDefId, Field, Title, ComponentType, RefLovCode(200) |
| `LovTableColumn` | 列表表格列 | LovDefId, Field, Title, Width, Align, Sortable, RefLovCode(200), FormatType |

## 常见场景

### 场景 1：枚举型值集（搜索栏状态下拉）

```
后端：SearchFields.GetField(_.Status).LovCode = $"Enum.{typeof(ProcessCardStatus).FullName}";
前端：<LovSelect code="Enum.xxx" v-model="filterStatus" />
→ 渲染为 el-select 下拉，选项从 Meta API 获取
```

### 场景 2：枚举型值集（列表列翻译）

```
后端：ListFields.GetField(_.Status).LovCode = $"Enum.{typeof(ProcessCardStatus).FullName}";
前端：GetPage 返回 list[].lovCode → 调用 BatchLabel 翻译列值
→ 列表中状态列显示中文标签而非数字
```

### 场景 3：列表型值集（选择用户/部门）

```
后端：配置 LovListConfig（请求地址、分页参数）+ LovSearchField + LovTableColumn
前端：<LovSelect code="List.User" v-model="userId" />
→ 渲染为只读输入框+搜索按钮，点击弹出 LovSelectTable 弹窗
→ 弹窗内支持搜索、分页、列翻译
```

### 场景 4：通过 GetPage 元数据自动适配

```
后端：仅配置 SearchFields.GetField(_.Status).LovCode = "Enum.xxx"
前端：onMounted → GET GetPage → 检测 search[].lovCode → 有则渲染 LovSelect
→ 值集码只出现在后端，前端完全动态适配
```

### 场景 5：列表型值集 + [LovList] 特性 + 代理/直连

**同应用接口（直连，推荐）**

```
后端：控制器 Action 标注 [LovList(LovCode="List.CubeDemo.Role", RequestUrl="/Test/TestField/RoleList", ProxyRequest=false)]
      → 启动自动注册 LIST 值集（含配置/列/搜索字段）
前端：字段 LovCode="List.CubeDemo.Role" → 弹窗直连 /Test/TestField/RoleList（requestUrl 以 / 开头强制直连）
→ 同应用数据无需后端代理，地址对前端可见且无跨域
```

**外部接口（代理）**

```
后端：[LovList(LovCode="List.Xxx", RequestUrl="https://external/api", ProxyRequest=true)]
前端：shouldDirectRequest=false → fetchLovListData() → 后端 /Admin/Lov/ListData → ILovListDataProxy 转发
→ 外部地址隐藏、规避跨域；可 IOC 覆盖 DefaultLovListDataProxy 自定义转发
```

## 注意事项

1. **LovCode 长度**：完全限定名可能超过 50 字符（`Enum.SmartMES.Data.ProcessCard.ProcessCardStatus` 为 52 字符），数据库列已设为 `Length=200`
2. **启动顺序**：LovAutoRegisterService 在应用启动时运行，需在用到值集前完成
3. **Source 字段**：`AUTO` 为自动注册，启动时会同步枚举成员；`MANUAL` 为手工管理，启动时不做修改
4. **LovSelect 异步加载**：组件已通过 `watch(code)` 监听 code 变化，支持 GetPage 晚于组件挂载的场景
5. **降级策略**：GetPage 失败或 lovCode 不存在时，保留原有硬编码渲染
6. **ProxyRequest 与 `/` 校验**：`requestUrl` 以 `/` 开头（同源同应用）时前端强制直连、禁用代理，即使 `proxyRequest=true` 也会被忽略（后端配置页也会禁用该开关）。只有跨域或需隐藏地址的外部接口才启用 `proxyRequest=true`
7. **自定义代理实现**：通过 `services.AddCubeLov(...)` 用 `TryAddSingleton<ILovListDataProxy, 自定义实现>()` 覆盖默认 `DefaultLovListDataProxy`（HTTP 客户端转发）；默认实现以 `TryAddSingleton` 注册，便于使用者覆盖
8. **[LovList] 自动注册**：特性标注在控制器 Action 上，随枚举初始化一并扫描注册；`LovCode` 须以 `List.` 开头，手工管理（`MANUAL`）的值集不会被覆盖
