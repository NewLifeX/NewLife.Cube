# OSC-260903e2a4 Design — 实体部件查询条件与宿主联动

## 0. 代码基线（2026-09-03 核对）

| 事实 | 位置 |
| --- | --- |
| `WidgetInstance.query.extraFilter?: WidgetFilter`（`{logic:'all'\|'any', conditions:[{field,op,value}]}`） | `packages/api-core/src/widget.ts` |
| dashboard JSON 归一/序列化对 `query` 原样透传（未知键保留） | `widget.ts parseDashboardJson/normalizeWidget/serializeDashboardJson` |
| 取数把 `q.extraFilter` 带进 Query body | `web/src/features/widget/useWidgetQuery.ts buildQueryBody` |
| 后端已把 `ExtraFilter` AND 进 Where（租户/DataPermission 之后，白名单=search∪list，越权/不可下推 400） | `NewLife.Cube/Widgets/WidgetQueryService.cs BuildWhere` |
| `AutomationFilter.TryBuildWhere` 上限：总 ≤10、any ≤5；SQL 支持 eq/neq/isNull/notNull/gt/gte/lt/lte/contains/notContains/startsWith/endsWith/after/before | `NewLife.Cube/Automation/AutomationFilter.cs` |
| 查询时间窗：Log/分表实体无时间条件自动收窄近 N 天（`TryGetTimeWindow`，条件命中则跳过） | `WidgetQueryService.Execute` / `AutomationFilter.TryGetTimeWindow` |
| 保存端 `DashboardJson.TryNormalize(json,user,checkSources,surface)`，不校验 extraFilter | `NewLife.Cube/Widgets/DashboardJson.cs` |
| insight 保存：`/Cube/ViewProfile` PUT 与模板 PUT 都调 `TryNormalize`（默认 insight） | `Controllers/CubeController.cs` |
| 工作台保存：`/Cube/Workbench`（个人）/`Role/{id}`/`Named{/slug}`，`surface=workbench` | `Widgets/WorkbenchResolver/RoleStore/NamedStore` + `Controllers/WorkbenchController.cs` |
| Widget 配置抽屉（kind/source/fields 三步、draft 重建 query、编辑时丢 extraFilter） | `web/src/features/widget/WidgetConfigDrawer.vue` + `useWidgetConfigDrawer.ts` |
| 查询/条件组件（OSC-0015/260830a1b2）：`FilterBuilderPopover` + `useFilterBuilderPopover` + 纯函数 `core/utils/filterBuilder.ts` + `viewProfile.normalizeFilter` | `web/src/views/crud/` + `core/utils/` |
| 页面字段候选 = `searchFields ∪ listFields`（`filterFields`，与后端白名单对齐） | `web/src/views/crud/listContext.ts` L418 |
| 宿主上下文：`useWidgetQuery` 从 `hostFilter`（页面 viewFilter）等值条件取 `hostValues` 供 linkFilter | `useWidgetQuery.ts hostValuesFromFilter` |
| 未联动判定 `isUnlinkedWidget`：跨实体且无 linkFilter | `web/src/features/widget/legacy.ts` |
| 跨实体源字段富化元数据加载器（GetPage→GetFields→Meta→枚举/LOV 富化） | `web/src/features/widget/listFieldMeta.ts` |

**核心缺口（本号要补）**：配置 UI 无入口；`save()` 重建 query 丢 extraFilter；保存端无校验；`$host` 值形态与解析不存在；`AutomationFilter.Unwrap` 会把对象值当 JSON 文本（`ReadFilterValue`/STJ `JsonElement.ToString()`），不能静默放行。

## 1. 协议：部件查询条件值形态

持久化载体 = `WidgetInstance.query.extraFilter`（不变）。条件值（`conditions[].value`）两类：

```jsonc
// 字面量（现状，后端 Unwrap 兼容）
{ "field": "Status",   "op": "eq", "value": 10 }
{ "field": "CreateTime","op": "after", "value": "2026-09-01T00:00:00" }

// 宿主字段引用（本号新增；仅实体页洞察槽可保存）
{ "field": "RoleId", "op": "eq", "value": { "$host": "RoleId" } }
```

规则：
- `$host` 标记是**单键对象** `{ "$host": "<宿主实体字段名>" }`；键名常量 `$host` 写入 api-core 与后端各一份（文档登记，避免漂移）。
- `$host` 值的含义：取数时用「宿主页当前筛选上下文中等值条件的值」替换；该值来自**请求里的 `HostFilter`**（页面 viewFilter，与 `linkFilter` 的 `hostValues` 同一语义来源）。
- 其余任何**对象**值（非 `$host` 单键标记）在保存与查询两端都拒绝（400），防止对象被 `Unwrap` 静默变成 JSON 文本匹配。
- 字面量/数组值、`isNull/notNull` 无值条件与现状完全一致，零改动。

### 宿主引用解析源（权威定义）

| 场景 | 解析 |
| --- | --- |
| 洞察槽、跨实体部件（源 ≠ 宿主） | `req.HostFilter`（宿主页 viewFilter JSON）中 `field == $host字段 && op == eq` 且值非空 → 取该值；命中即「已应用宿主上下文」 |
| 洞察槽、同源部件（源 == 宿主） | `HostFilter` 已整体 AND 到同表，无需 `$host`（UI 不推荐，但不禁止） |
| 工作台 `/home`、角色模板、命名工作台 | 无 `HostFilter`/`hostTypePath` → `$host` 不可解析；保存端 400、前端不提供入口 |
| 宿主上下文缺失（无该 eq 条件） | 该 `$host` 条件**跳过**；若部件只剩 `$host` 条件 → 无约束 → 响应 `hostFilterApplied=false`（前端走既有「未联动」角标） |

> 备注：不扩 `hostValues` 键集合——`$host` 解析直接用请求里已下发的完整 `HostFilter`，服务端自取，避免前端逐条件回传值的双份状态。（`linkFilter` 的 `hostValues` 机制保留不动。）

## 2. 后端改动

### 2.1 `NewLife.Cube/Widgets/DashboardJson.cs`（保存端校验）

新增重载并把现有实现改走带参版本：

```csharp
public static Boolean TryNormalize(String json, IUser user, Boolean checkSources, String surface,
    String hostTypePath, out String normalized, out String error)
// hostTypePath：insight=当前 ViewProfile typePath（个人/模板均传）；workbench 域传 null
// 既有 4 参/3 参重载保持，宿主=null 转发（向后兼容单测调用）
```

`NormalizeWidget` 内对 `entity.*` 部件在收尾阶段调用新私有 `ValidateWidgetFilter(w, sourceTypePath, hostTypePath, out error)`：

1. 读 `w["query"]?["extraFilter"]`；无/空 conditions → 通过（且不强写键）。
2. 解析为节点后校验：
   - `logic` ∈ `{all, any}`（缺省 all）；`conditions` 非数组 → 400「部件查询条件无效」。
   - 数量：总 >10 → 400「筛选条件过多」；`any` 且 >5 → 400「OR 条件过多」（与 AutomationFilter 一致）。
   - 每条：`field` 非空且属于**源实体** `search ∪ list` 白名单（复用 `WidgetQueryService.BuildAllowedField` 提取为内部 helper `WidgetQueryService.AllowedNames(fact)` 或本文件内联 `FieldCollection(fact, Search|List)`），否则 400「部件查询条件含未授权字段 X」；`op` ∈ SQL 白名单否则 400。
   - `value`：JsonValue/数组/空 → 放行；JsonObject 必须恰为单键 `$host` 且值非空字符串 → `hostTypePath` 为空（工作台域）→ 400「工作台不支持宿主引用」；宿主字段 ∈ 宿主实体字段（大小写不敏感，可仅查实体工厂字段名）→ 否则 400「未知宿主字段 X」。其余对象值 → 400「部件查询条件值无效」。
3. 校验在**剥离 data/rows 之后、重排之前**执行；失败返回 false 与文案（保存侧 400，前端 Message 提示）。
4. 源实体解析失败（未知 typePath，仅当有 extraFilter 时）→ 400「未知实体」（有 extraFilter 才要求解析，避免无谓扫描）。

调用点传参：
- `CubeController.ViewProfile`（PUT）与 `ViewProfileTemplate`（PUT/POST 别名）：`TryNormalize(..., SurfaceInsight, typePath, ...)`。
- `WorkbenchController` 个人/Role/Named 各 PUT：`TryNormalize(..., SurfaceWorkbench, hostTypePath: null, ...)`（surface 语义不变）。
- 既有 XUnit 直接调用 3/4 参重载（默认 null 宿主）不受影响；新增带宿主单测。

> 说明：保存端白名单校验在**有 extraFilter 时**才解析实体/字段，代价可忽略（配置保存低频）；字段日后漂移（列被移除/改名）仍由查询端 400 兜底并显示错误卡。

### 2.2 `NewLife.Cube/Widgets/WidgetQueryService.cs`（取数解析）

`BuildWhere` 中 `ExtraFilter` 分支改造（`AutomationFilter.TryBuildWhere` **不改**）：

```csharp
if (HasConditions(req.ExtraFilter))
{
    // 1) $host 预解析：把「宿主 eq 值」写入克隆条件；失败/无值返回 false 的条件标记跳过
    var (extra, hostRefResolved, hostRefFailed) = ResolveHostRefs(req.ExtraFilter, req.HostFilter);
    // 2) 非法对象值（非 $host 标记）→ 400（防 JSON 文本字面匹配）
    // 3) 剩余字面条件 TryBuildWhere；有 $host 成功应用 → hostApplied = true（跨源）
    // 4) 全部 $host 失败且无数值条件 → hostApplied = false（未联动语义，同 linkFilter 缺值）
    exp = ...（原 AND 逻辑保留）
}
```

`ResolveHostRefs` 语义（内部 helper，不改 DTO）：
- 遍历 `req.ExtraFilter.Conditions`；对值形如 `$host` 的条件：在 `req.HostFilter`（须先 `HasConditions` 判定）的 conditions 中找 `field == $host字段 && op ∈ {eq}` 且值非空 → 用该值替换克隆条件的 value；找不到 → 该条件从克隆集中剔除并记 failed。
- 非 `$host` 的对象值（JsonElement ValueKind.Object 且键 ≠ `$host`）→ 抛 `ApiException(400, "筛选值无效")`。
- 返回：克隆后的 `ViewFilterDto`（可能为空 conditions）+ `appliedCount`（成功解析条数）+ `skippedCount`。

`hostFilterApplied` 落点（保持单一返回值语义）：
- 跨源且至少一个 `$host` 解析成功（无论是否另有 linkFilter）→ `hostApplied = true`。
- 跨源、仅 `$host` 且全部失败 → `hostApplied = false`（前端「未联动」）。
- 同源：`HostFilter` AND 已置 true（现状）；`$host` 一般不使用，不干扰。
- 空 `$host` 剔除后仍有字面条件 → 字面正常 AND，`hostApplied` 由上述规则定（存在成功 `$host` 才 true；若无 → 前端按「声明了联动但本次未应用」显示未联动角标——与 linkFilter 缺值一致）。

时间窗注意：`TryGetTimeWindow(fact, req.ExtraFilter, …)` 沿用**原始** ExtraFilter 判断是否已含时间条件（`$host` 引用时间字段不视为已含时间条件——保持现状逻辑，不引入新行为）。

### 2.3 无需改
- `AutomationFilter` / `ViewFilterDto` / `FilterJson.DeserializeFilter`（工作流与自动化继续使用，无宿主概念）。
- `WidgetQueryRequest` 结构（`ExtraFilter` 里嵌 `$host` 对象由 STJ 落成 JsonElement，`ResolveHostRefs` 负责解释）。
- 聚合/列表/分组执行路径。

## 3. 前端复用设计与接线

### 3.1 复用清单（不复制）

| 复用件 | 用途 | 改动 |
| --- | --- | --- |
| `views/crud/FilterBuilderPopover.vue` | 条件编辑弹层（标题「查询」）整体嵌入配置抽屉 | 加可选 props（见 3.3）；`apply` 事件回填 |
| `useFilterBuilderPopover.ts` | draft/条件行/值控件逻辑 | 加 `$host` 行值来源处理 |
| `core/utils/filterBuilder.ts` | draft↔filter 纯函数、字段类别/操作符矩阵 | **零改动** |
| `core/utils/viewProfile.ts normalizeFilter` | 读侧归一 | 新增容忍 `$host` 值的归一（值对象不丢弃） |
| `features/widget/listFieldMeta.ts` | 源实体字段富化加载 | 扩展 search∪list（见 3.4） |
| `features/widget/legacy.ts isUnlinkedWidget` + `useWidgetQuery` | 未联动判定 | 扩展（见 3.5） |

抽屉内嵌方式：在「字段与样式」步骤中「查询条件」`a-form-item` 内放 `FilterBuilderPopover`（锚点按钮「编辑条件（N）」+ 摘要），`FilterBuilderPopover` 自带 popover 弹层、无状态、`apply` 时把 ViewFilter 写回 draft；保持 Widget 特性不 import `useListQuery`（既有 widget.spec 约束）。

### 3.2 `WidgetConfigDrawer` / `useWidgetConfigDrawer`

- `draft` 增 `extraFilter: ViewFilter | null`（编辑还原：`normalizeFilter(editing.query?.extraFilter)`，空 conditions 视为 null；新建 null）。
- 「字段与样式」步骤（`!isNamed` 且已选 typePath 后）新增「查询条件」区：
  - 摘要：无 → 「无查询条件（显示源实体全部匹配数据）」；有 → `N 条条件（且/或）` + 可读首条示例。
  - 「编辑条件」打开 `FilterBuilderPopover`（modelValue=draft.extraFilter、fields=源实体 filter 候选、hostFields=宿主候选或空、showSaveView=false）；`@apply` 写回 draft；「清除」置 null。
- `save()` 实体分支：`query.extraFilter = normalizeDraftFilter(draft.extraFilter)`（空 conditions → undefined 不落键）；**同时保留**既有 measure/groupBy/timeField/limit/mapping/linkFilter 重建逻辑（把 extraFilter 作为独立合并项，避免重建丢条件——修复现状编辑即丢的缺陷）。`named` 分支不改（平台部件无查询）。
- 越权字段提示依赖保存端 400（现有 Message.error 链路），候选字段已白名单化，正常路径不会触发。

### 3.3 `FilterBuilderPopover` 可选扩展（向后兼容）

新增两个 props（缺省即今日行为，DefaultList 零改动）：

```ts
/** 宿主字段候选；非空时每行值来源可切「固定值 / 宿主字段」，值= { $host: fieldName } */
hostFields?: FieldMeta[]
/** 是否显示「保存条件到此视图」按钮；部件配置传 false（默认 true 保持列表页语义） */
showSaveView?: boolean
```

- 行渲染：`hostFields?.length` 时在值控件前加 `a-radio-group size=mini`（固定值 / 宿主字段，inline 小控件）；选「宿主字段」时值控件替换为 `a-select`（选项=hostFields 的 displayName/name），`op` 为 `isNull/notNull` 时该切换禁用（无值）。
- 值写入 `draftToFilter` 前保持 `{ $host: fieldName }` 对象（`filterBuilder.draftToFilter` 原样透传 value；`hostFields` 逻辑仅在组件层）。
- `save` 语义：`showSaveView=false` 时不渲染「保存条件到此视图」按钮与相关禁用态（canSave 照传 false）。
- 组件仅依赖 core/utils + `LovSelect`，无 DefaultList 私有状态 → widget 特性引用不产生模块环（`DefaultList → InsightPanel → WidgetHost → WidgetConfigDrawer → FilterBuilderPopover` 是单向的，FilterBuilderPopover 不反向引用）。

### 3.4 字段候选加载

- 新增 `features/widget/listFieldMeta.ts loadEntityFilterFields(typePath)`：返回源实体 `search ∪ list` 的 `FieldMeta[]`（富化 displayName/typeName/dataSource/lovCode）——把现有 `loadFromGetPage`/`loadFromGetFields` 泛化支持 `FieldKind.Search|List`（GetPage 分区取 `search`+`list`，缺分区时 GetFields 分别兜底），复用 `mergeFieldMetas` + `enrichFieldsWithEnumDataSource/Lookup`。
- `useWidgetConfigDrawer`：
  - `hostTypePath` 存在且 `typePath == host`（insight 同源）：条件候选直接取表面注入的宿主字段候选（页面 `filterFields`，见 3.5），不发请求。
  - 其余（跨源/工作台）：`pickSource`/编辑打开时懒加载 `filterFieldsOfSource`；失败回落空列表（编辑器仅提示无候选字段）。
- 注意与既有 `sourceFields`（automation.meta 全字段，用于显示列/联动下拉）**分离**：条件编辑器只用白名单候选，避免选出后端 400 的字段。

### 3.5 表面上下文与「未联动」

- `features/widget/context.ts` `WidgetSurfaceContext` 增 `hostFilterFields?: FieldMeta[]`（宿主页 `search∪list`，来自 DefaultList 已计算的 `filterFields`）。
- `InsightPanel.vue`/`useInsightPanel.ts` 增 prop `filterFields` 透传；`DefaultList.vue` 传入 `:filter-fields="filterFields"`（复用 listContext 计算，不重复）。
- `legacy.ts isUnlinkedWidget` 扩展：跨实体且（无 linkFilter 且无 `$host` 条件）→ true；声明 linkFilter 或 `$host` → false（先视为已声明联动，查询回来再定角标）。
- `useWidgetQuery` 结果态：部件「声明联动」时 `unlinked = result?.hostFilterApplied === false`；未声明联动时保持现状（跨实体恒 true）。后端 `hostFilterApplied=true` 表示「本次确实应用了宿主上下文约束」（含 `$host` 解析成功），前端据此决定角标，避免 query 前/后闪烁。
- 工作台表面：`hostTypePath=undefined`、`hostFilterFields=[]` → 条件编辑器不显示宿主引用；保存端 400 兜底。

## 4. 保存与读取兼容

| 场景 | 行为 |
| --- | --- |
| 存量无 `extraFilter` 部件 | 归一/序列化不新增键；行为与今日一致 |
| 模板域（`ViewProfileTemplate`）含 extraFilter | 随 dashboardJson 模板整份继承（个人 > 模板既有规则不变） |
| 旧 `FilterBuilder` 视图筛选 / 预定义查询 / FiltersJson | 与部件 extraFilter 互不覆盖（不同域） |
| 列日后漂移（extraFilter 引用被移除字段） | 查询期 400 → 错误卡；用户重编辑该部件（下拉已无该字段）修正。不静默放宽 |
| 编辑器重建 | `save()` 合并保留 extraFilter（修复现状丢条件缺陷） |

## 5. 联动语义矩阵（同源/跨源 × 静态/宿主 × 上下文）

| 部件源 | 条件 | 宿主上下文 | 后端行为 | 前端 |
| --- | --- | --- | --- | --- |
| 同源 | 仅静态 | 任意 | extraFilter AND HostFilter（现状） | 无角标 |
| 同源 | 含 `$host` | 任意 | 一般不配置；若配：`$host` 从 HostFilter 解析（同表已整体约束，通常冗余） | — |
| 跨源 | 仅静态（无 link/`$host`） | 任意 | extraFilter 生效；宿主筛选不映射（现状） | 未联动角标（现状） |
| 跨源 | linkFilter（现状） | 有该 eq | 映射 AND；hostFilterApplied=true | 无角标 |
| 跨源 | linkFilter | 缺 | mapping 跳过；hostFilterApplied=false | 未联动角标（现状） |
| 跨源 | `$host`（可另带静态） | 有该 eq | `$host` 条件 AND；hostFilterApplied=true | 无角标 |
| 跨源 | 仅 `$host` | 缺该 eq | 条件跳过；无约束；hostFilterApplied=false | 未联动角标（提示“未在当前筛选下联动”） |
| 工作台 | 仅静态 | 无 | extraFilter 生效 | 无角标（工作台无宿主概念） |
| 工作台 | `$host` | 无 | 保存端 400；前端不提供入口 | — |

## 6. 文件级改动地图

### 6.1 后端（NewLife.Cube）

| 文件 | 改动 | 不动 |
| --- | --- | --- |
| `Widgets/DashboardJson.cs` | `TryNormalize` 增 `hostTypePath`；`ValidateWidgetFilter`；调用点传参 | version/widgets/layout/source 既有规则、4/3 参重载默认宿主 null |
| `Widgets/WidgetQueryService.cs` | `BuildWhere` ExtraFilter 分支 `$host` 预解析 + 非法对象值 400 + `hostApplied` 落点；`BuildAllowedField` 提为可复用 `AllowedNames(fact)` | 聚合/分组/时间窗、白名单语义 |
| `Controllers/CubeController.cs` | ViewProfile/Template PUT 调 `TryNormalize` 传 `typePath` | GET/其余域 |
| `Controllers/WorkbenchController.cs` | 各 PUT 调 `TryNormalize(..., null)` | 读路径、角色/命名逻辑 |
| `NewLife.Cube.Tests/Osc260903WidgetQueryTests.cs`（新） | 见 §8.1 | — |

### 6.2 api-core（packages/api-core）

| 文件 | 改动 |
| --- | --- |
| `src/widget.ts` | `WidgetFilterCondition.value` 类型注明可含 `{ $host: string }`；导出 `HOST_REF_KEY`、`isHostRefValue(v)`、`hostRefField(v)`、`hasHostRefFilter(f)`；dashboard 归一/序列化零改动（query 原样透传已支持） |
| `src/widget.spec.ts` | 纯函数 + extraFilter round-trip 保留用例 |

### 6.3 ArcoVue（web）

| 文件 | 改动 |
| --- | --- |
| `src/views/crud/FilterBuilderPopover.vue` | 加 `hostFields?`/`showSaveView?` props 与行值来源切换、按钮显隐 |
| `src/views/crud/useFilterBuilderPopover.ts` | hostFields 下拉选项、值写入 `{ $host }`、`save` 显隐逻辑 |
| `src/features/widget/WidgetConfigDrawer.vue` | 「查询条件」区 + 内嵌 FilterBuilderPopover + 摘要/清除 |
| `src/features/widget/useWidgetConfigDrawer.ts` | `draft.extraFilter` 还原/保存；save() 合并保留；候选字段加载 |
| `src/features/widget/listFieldMeta.ts` | `loadEntityFilterFields`（search∪list 泛化多源加载） |
| `src/features/widget/context.ts` | `hostFilterFields?` 注入 |
| `src/features/search/InsightPanel.vue` + `useInsightPanel.ts` | `filterFields` prop 透传表面 |
| `src/views/crud/DefaultList.vue` | `:filter-fields` 传入（读既有 computed，无逻辑改动） |
| `src/features/widget/legacy.ts` | `isUnlinkedWidget` 纳入 `hasHostRefFilter` |
| `src/features/widget/useWidgetQuery.ts` | 未联动结果判定（声明联动 → 以 `hostFilterApplied` 定角标） |
| `src/core/utils/viewProfile.ts` | `normalizeFilter` 容忍 `$host` 值对象（不丢弃），保持既有 key 归一下发 |
| `src/features/widget/useDataListWidget.ts` 等渲染器 | 零改动（消费既有 `result.rows/items/value`） |

### 6.4 文档

`Doc/功能清单.md`（DASH-1/DASH-2 备注 + 本号登记）、`Doc/Api/核心接口架构.md`（`/Cube/Widget/Query` extraFilter `$host` 说明 + DashboardJson 保存校验）、`Doc/Api/前端对接指南.md`（Widget 配置：查询条件与宿主引用消费约定）、`web/README.md`、`openspec/README.md`（如登记能力）。

## 7. 测试设计

### 7.1 XUnit（`NewLife.Cube.Tests/Osc260903WidgetQueryTests.cs`，复用 `Osc260828*` 夹具实体/控制器）

- DashboardJson 校验：
  1. 合法字面 extraFilter（search∪list 字段）保存通过且保留。
  2. 条件字段在源实体 search∪list 之外 → 400（文案含字段名）。
  3. 总条件 11 / any 6 → 400。
  4. insight 含合法 `$host`（宿主字段存在）→ 通过；`$host` 宿主字段不存在 → 400。
  5. workbench（hostTypePath=null）含 `$host` → 400。
  6. 值对象非 `$host` 单键（如 `{"a":1}`）→ 400。
  7. 无 extraFilter 部件归一结果与旧行为字节一致（不新增键）。
- WidgetQueryService：
  8. 静态 extraFilter：count/group/list 三种模式行集正确。
  9. `$host` 解析：跨源 + HostFilter 含该宿主字段 eq → 行过滤且 `HostFilterApplied=true`。
  10. `$host` 缺值：HostFilter 无该 eq → 条件跳过、`HostFilterApplied=false`（返回未过滤行集）。
  11. `$host` + 静态混合：静态仍生效；`HostFilterApplied` 按 `$host` 是否成功定。
  12. 非 `$host` 对象值 → 400。
  13. 与时间窗组合：Log 夹具实体静态时间条件命中 → 不注入收窄；`$host` 引用时间字段不视为时间条件（现状）。
  14. 既有 `Osc260828WidgetTests` / `Osc26082815a1WorkbenchTests` 全绿（回归门禁）。

### 7.2 Vitest（api-core）

- `isHostRefValue/hostRefField/hasHostRefFilter` 边界（标量/数组/空/多键对象）。
- `parseDashboardJson/serializeDashboardJson`：含 `$host` 与字面 extraFilter round-trip 保留；无 extraFilter 输出无该键。

### 7.3 Vitest（arco-vue）

- `isUnlinkedWidget`：跨实体含 `$host` → false；无 link 无 `$host` → true。
- `useWidgetConfigDrawer`：编辑含 extraFilter 部件 → draft 还原；save() 输出 query.extraFilter 保留；空条件不落键。
- `buildQueryBody`：extraFilter 不变式（既有 widget.spec 扩展 1 例）。
- `normalizeFilter` 对 `$host` 值对象不丢弃（新增 1 例）。

### 7.4 构建与冒烟

`dotnet build 魔方.sln`（或 NewLife.Cube + NewLife.Cube.Tests）、`npm.cmd --prefix packages/api-core run build|test`、`npm.cmd --prefix NewLife.Cube.ArcoVue\web run build|test`（vue-tsc + vitest + vite build）。手工冒烟见 verify.md。

## 8. 核心文档影响

| 文档 | 影响 |
| --- | --- |
| `Doc/功能清单.md` | DASH-1/DASH-2 增补查询条件能力与 OSC 登记 |
| `Doc/Api/核心接口架构.md` | Widget Query extraFilter `$host` 契约、DashboardJson 保存校验、ViewProfile PUT 语义 |
| `Doc/Api/前端对接指南.md` | Widget 配置消费约定（查询条件/宿主引用/候选白名单） |
| `web/README.md` | 登记实体部件查询条件能力 |

## 9. 风险

| 风险 | 缓解 |
| --- | --- |
| 条件值对象被既有 `Unwrap`/`ReadFilterValue` 静默文本化 | 保存 + 查询两端 400 显式拒绝非 `$host` 对象值 |
| 宿主引用在宿主未筛时返回未过滤数据（语义泄露面） | 与 linkFilter 缺值同款「未联动角标」；行权仍 DataPermission+租户+Detail；文档明确语义 |
| FilterBuilderPopover 扩展影响列表页 | 新增 props 可选，缺省行为不变；DefaultList 调用零改动 + 既有 Vitest 回归 |
| 保存端白名单与查询端白名单漂移 | 两端同源（FieldCollection Search∪List）；查询端为最终强制 |
| widget 特性与 views/crud 引用产生环 | FilterBuilderPopover 无反向依赖；widget.spec「不 import useListQuery」约束保留 |
| 编辑存量部件丢 extraFilter（现状缺陷） | save() 显式合并保留 + 单测覆盖 |
