# OSC-2608280e9e Design — 页面仪表盘 Widget 协议

适用前端：Arco Design Vue（https://arco.design/vue/docs/start）；迷你图表用已接入的 ECharts 壳（`web/src/core/utils/echartsTheme.ts`），**不**用 VTable 做指标卡。不涉及 FlowGram。`.vue` 薄 script，业务进同目录 `useXxx.ts` / `core/utils`。

图标：IconPark，新图标须先在 https://iconpark.oceanengine.com/official 确认存在再写入 `web/src/core/utils/iconRegistry.ts`。

## 1. 冻结与加法

| 符号 | 冻结 |
|------|------|
| `GetPage` / `Search` / `SearchData` / `CreateWhere` | 不改签名 |
| `Index` / `GetChartData` / `OnGetChartData` | 不改签名；默认仍 `[]` |
| `DataField.Fill` / `EnableFieldValidation` | 不改 |
| CubeNC `NewLife.CubeNC/Widgets/IWidget` | **不引用、不改** |
| Cube.Vue `ListChartDialog` | 不改 |
| `GET /Cube/Automation/Entities` 默认 `permission=update` | 不改默认值（可另加 `detail` 分支，但仪表盘选表必须走 `/Cube/Widget/Sources`） |

加法：Cube.xml 一列、WebAPI 新控制器、api-core 类型、ArcoVue 新目录 `features/widget/`。禁止为聚合再造 SQL 引擎或平行 `GetList2`。

## 2. 三层模型

| 层 | 职责 | 存贮 |
|----|------|------|
| Surface | `insight` 本号；`workbench` 后续 | 本号只实现 insight |
| Instance | 配置 JSON，不含查询结果 | `ViewProfile.DashboardJson` |
| Runtime | Host 拉数、降级、注册表 | 无持久化 |

读取顺序（实体仪表盘）：个人 `DashboardJson` 有效 → 整份采用；否则模板（UserId=0）；否则空（不渲染槽，与今日双关隐藏一致）。**不做** widget-id 字段级合并。

「有效」定义见 §4.3。

## 3. 实例 schema

权威 TypeScript 放入 `packages/api-core/src/widget.ts`（或 `types.ts` 末尾），ArcoVue 只 re-export，禁止再定义一份冲突形状。

```ts
export type WidgetKind = 'metricCard' | 'miniChart' | 'miniKanban' | 'legacyChart' | string
export type WidgetProvider = 'entity.aggregate' | 'entity.list' | 'named'
export type MeasureFn = 'count' | 'sum' | 'avg' | 'min' | 'max'
export type ChartType = 'sparkline' | 'line' | 'bar' | 'pie'

export interface WidgetLayout {
  w: 3 | 4 | 6 | 12
  h?: 1 | 2 | 3 | 4
  order: number
}

export interface WidgetInstance {
  id: string                 // 非空，个人配置内唯一，建议 ulid/nanoid
  kind: WidgetKind
  title: string              // 1–40 字；空则用 Catalog 标题或「未命名」
  layout: WidgetLayout
  source: {
    provider: WidgetProvider
    typePath?: string        // entity.* 必填，规范化 Admin/User（无前导 /）
    widgetName?: string      // named 必填
  }
  query: {
    measure?: { fn: MeasureFn; field?: string }
    groupBy?: string
    timeField?: string
    buckets?: number         // 默认 12，合法 1–24
    limit?: number           // 默认 30，合法 1–50
    mapping?: { groupField?: string; titleField?: string; imageField?: string }
    linkFilter?: { hostField: string; sourceField: string }[]
    extraFilter?: ViewFilter // 与 OSC-0015 同构 logic all|any
  }
  style?: {
    icon?: string
    color?: string           // 预置：blue|green|cyan|orange|red|purple|grey
    chartType?: ChartType
    clickUrl?: string
  }
}

export interface DashboardConfig {
  version: 1
  widgets: WidgetInstance[]
}
```

### 3.1 归一化顺序（前后端各做一遍，以后端为准）

1. JSON 解析失败或根不是 object → 视为未配置（GET 当 null；PUT 400）。
2. `version !== 1` → PUT 400。GET 旧未知 version 当未配置，不抛。
3. `widgets` 缺省 → `[]`。非数组 → PUT 400。
4. 条数 >12 → PUT 400。
5. `id` 空或重复 → PUT 400。
6. `layout.w` 不在 {3,4,6,12} → 归一为 3。`order` 非有限数 → 按数组下标。保存前按 `order` 升序重排并重写为 0..n-1。
7. `source.provider` 非法 → PUT 400。
8. `entity.*` 缺 `typePath` → PUT 400。`typePath` 用 `AutomationPaths.NormalizeTypePath`。
9. `named` 缺 `widgetName` → PUT 400。
10. `count` 允许无 `field`；`sum|avg|min|max` 必须有 `field` 且为源实体数值列（GetPage/工厂字段，非 PK/二进制/密码）。
11. `miniChart`：`bar|pie` 必须 `groupBy`；`sparkline|line` 必须 `timeField`。
12. `miniKanban`：`mapping.groupField` 与 `titleField` 必填；provider 必须 `entity.list`。
13. 剥离查询结果：若客户端误传 `data`/`value`/`items`/`rows` 键则删除后保存。
14. 未知顶层/实例键：**保留**（前向兼容），但函数不可能出现在 JSON。
15. 整份 UTF-8 字节 >64KiB → PUT 400。

### 3.2 kind × provider 合法矩阵

| kind | 允许 provider | 非法时 |
|------|----------------|--------|
| metricCard | `entity.aggregate`、`named` | PUT 400 |
| miniChart | `entity.aggregate` | PUT 400 |
| miniKanban | `entity.list` | **洞察槽本号禁用**（PUT 400「页面仪表盘不支持迷你看板」；Catalog/Host 剔除）；工作台另号 |
| legacyChart | 无（仅迁移合成，禁止 PUT 新编） | PUT 400 |
| 已注册业务 kind | 其 `registerWidget.providers` / Catalog | 未知 kind **允许保存**（占位卡）；Query 不替它拉实体数 |

### 3.3 空配置 vs 显式清空

| GET 个人列 | 行为 |
|------------|------|
| null / 空白 | 继承模板 DashboardJson |
| `{"version":1,"widgets":[]}` | 用户清空，**不**继承，槽隐藏（可「添加部件」） |
| PUT `dashboardJson: ""` | 清除个人域，下次 GET 继承模板（与 FiltersJson 空串同构） |

模板 PUT：`dashboardJson` null 不覆盖；空壳 `widgets:[]` 或空串清除模板域。

## 4. 后端文件地图（P1）

| 文件 | 计划 | 不动 |
|------|------|------|
| `NewLife.Cube/Entity/Cube.xml` ViewProfile | 增列 `DashboardJson` String Length=-1，说明「页面仪表盘。JSON：version+widgets」 | 其它列、索引 |
| `Entity/视图配置.cs` + `Models/ViewProfileModel.cs` | **xcode 生成**，禁止手写骨架 | — |
| `Entity/视图配置.Biz.cs` | `UpsertForUser` 拷贝 `DashboardJson`（null 不覆盖）；`SaveGlobalTemplate` 增加 `dashboardJson` 参数；`DeleteGlobalTemplate` / `DeleteGlobalFormJson` 的 hasContent/hasOther **计入** DashboardJson；空壳判定 `widgets` 空数组 | FormJson 全局唯一逻辑 |
| `Controllers/CubeController.cs` | GET 个人空则填模板 `DashboardJson`；PUT 个人走校验器；`ViewProfileTemplate` PUT/GET 接受 `dashboardJson`（仅管理员，与 views/filters 同域） | FormJson 仍管理员全局唯一 |
| **新建** `Controllers/WidgetController.cs` | `[Route("Cube/Widget")]`，鉴权同 `AutomationController`（登录/token） | 不放进 CubeNC Link 文件 |
| **新建** `Widgets/ICubeWidget.cs`、`CubeWidgetAttribute.cs`、`CubeWidgetManager.cs`、`WidgetQueryService.cs`、`DashboardJson.cs` | 扫描、聚合、校验 | 禁止 `using NewLife.CubeNC` |
| `NewLife.Cube.Tests` | `Osc260828WidgetTests` | — |

CubeNC：`视图配置*.cs` 已 Link，生成列自动进入 MVC 实体；**不**向 CubeNC 增加 `/Cube/Widget` Action。MVC 工作台仍用 Razor Widget。

### 4.1 HTTP 契约

| Action | 方法 | 权限 | 成功 |
|--------|------|------|------|
| `Sources` | GET | 登录 | `{ typePath, displayName, name }[]`，仅 `AutomationAuth.HasPermission(user, typePath, Detail)` |
| `Catalog` | GET | 登录 | `{ kinds: 平台 kind 元数据[], named: { name, title, kind, cols, adminOnly, surfaces }[] }`。named 过滤：AdminOnly 仅系统角色；`Permission` 角色名不匹配则隐藏 |
| `Query` | POST | 登录 + 目标 typePath **Detail** | 见下 |
| `Data` | GET `?name=` | 登录 + 该 named 可见 | `ICubeWidget.GetData(ctx)` JSON |

未登录 401。目标无 Detail：Query **403** 且 body `{ code:403, message }`（前端 Host 收成锁卡，不跳登录页）。Sources 不含该表。

**Query body：**

```json
{
  "mode": "aggregate | list",
  "typePath": "Admin/User",
  "measure": { "fn": "count", "field": null },
  "groupBy": null,
  "timeField": null,
  "buckets": 12,
  "limit": 30,
  "extraFilter": { "logic": "all", "conditions": [] },
  "hostTypePath": "Admin/Log",
  "hostFilter": { "logic": "all", "conditions": [] },
  "linkFilter": [{ "hostField": "Category", "sourceField": "Name" }],
  "hostValues": {}
}
```

`hostValues`：仅包含 `linkFilter.hostField` 的当前值（标量）。服务端 **忽略** 未在 mapping 中声明的键。

**Query 响应：**

- `mode=aggregate`：`{ value, items: [{ key, label, value }], hostFilterApplied: boolean }`。无 `rows`。
- `mode=list`：`{ rows: [ { 字段: 值 } ], hostFilterApplied }`。只含 GetPage **list** 分区字段（及主键），最多 `limit` 条。

### 4.2 Query 服务端步骤（穷尽）

1. 规范化 `typePath`；`EntityPageRegistry` 反查实体 Type，失败 → 400。
2. `HasPermission(Detail)` 否 → 403。
3. 取 `IEntityFactory`。构造 Where：
   - 租户：与 `ReadOnlyEntityController2.CreateWhere` 同等（无租户则 `1=0` 失败关闭，照现码）。
   - `DataPermissionAttribute`：按 typePath 找控制器 Type（菜单 FullName / 路由），无特性则不加；有则 `Valid` 通过才跳过表达式，否则 AND 表达式。找不到控制器 → **失败关闭 403**（禁止裸查）。
   - `extraFilter`：`AutomationFilter.TryBuildWhere`；无法翻译 → **400**「筛选无法下推」，禁止内存假聚合。
   - 宿主筛选：若 `hostTypePath` 规范化后 **等于** `typePath`，将 `hostFilter` TryBuildWhere 后 AND。若 **不等**：`linkFilter` 为空 → **不** AND 宿主条件，`hostFilterApplied=false`。非空则逐条：两边字段必须存在且类型兼容（数值/字符串/布尔/日期），用 `hostValues[hostField]` 生成源字段等值（或 in）条件 AND；缺值则该条 mapping 跳过；全部跳过则 `hostFilterApplied=false`。
4. `mode=aggregate`：
   - `count` 且无 group/time：`FindCount(where)` → `value`。
   - `sum|avg|min|max`：字段白名单（数值、非 PK、非密码/二进制）；用工厂聚合（SelectStat 或等价 Group 查询），禁止拼接用户字符串进 SQL。
   - `groupBy`：TopN=`min(limit,20)`，`items` 按 value 降序。
   - `timeField`：按日历日 `yyyy-MM-dd`，最近 `buckets`（≤24）天；当前库方言无法日期截断 → 400「不支持时间分桶」。
5. `mode=list`：`FindAll(where, 默认排序, null, 0, limit)`，limit 封顶 50。
6. 超时/异常：500，不返回半截行。单测覆盖 400/403 路径。

**禁止：** 请求体出现 `sql`/`script`/`join` 键 → 400。`measure.field` / `groupBy` / `timeField` 必须是工厂字段名（大小写不敏感，写入用元数据 Name）。

### 4.3 ICubeWidget

```csharp
public interface ICubeWidget
{
    Object GetData(WidgetContext ctx);
}

public sealed class WidgetContext
{
    public IUser User { get; init; }
    public ViewFilterDto HostFilter { get; init; }
    public String HostTypePath { get; init; }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class CubeWidgetAttribute : Attribute
{
    public String Name { get; }
    public String Title { get; }
    public String Kind { get; set; } = "metricCard"; // 渲染提示
    public Int32 Cols { get; set; } = 3;
    public Boolean AdminOnly { get; set; }
    public String Permission { get; set; } // 角色名逗号
    public String Surfaces { get; set; } = "insight,workbench";
}
```

扫描：启动时扫已加载程序集中 `ICubeWidget` + 特性（对标 CubeNC WidgetManager 的发现方式，**新类型新命名空间** `NewLife.Cube.Widgets`）。本号 **不**实现 InboxUnread 等业务 named（Catalog 可以是空 named 列表 + 平台 kinds）。提供 **一个测试用内部 widget** 仅测试项目可见，或单测直接 new Manager 注入假实现。

`Data?name=`：找不到或不可见 → 404。GetData 抛错 → 500，Host 单卡错误。

### 4.4 DashboardJson 保存鉴权

个人 PUT：每个 `entity.*` 部件的 `typePath` 必须当前用户 Detail，否则 **400** 并指出 `id`。named 必须在 Catalog 对该用户可见。`legacyChart` 禁止出现在 PUT body（迁移只存在内存合成，不写回）。

模板 PUT：管理员；不校验「所有用户」的 Detail（模板可含普通用户没有的表；运行时锁卡）。

## 5. api-core 文件地图（P2）

| 文件 | 计划 | 不动 |
|------|------|------|
| `packages/api-core/src/types.ts` | `ViewProfileModel.dashboardJson?: string \| null` | 其它字段 |
| `packages/api-core/src/widget.ts` | schema 类型 + `parseDashboardJson` / `serializeDashboardJson`（纯函数，供前后端测试也可在前端再包一层） | — |
| `packages/api-core/src/api.ts` | `createWidgetApi`：`sources` / `catalog` / `query` / `data` | `getChartData` 签名 |
| `packages/api-core/src/cube.ts` | 挂 `cubeApi.widget` | — |
| api-core 单测 | 归一化 / 64KB / 重复 id | — |

## 6. ArcoVue 文件地图（P3–P4）

| 文件 | 计划 | 不动 |
|------|------|------|
| **新建** `web/src/features/widget/registry.ts` | `registerWidget` / `getWidget` / 平台三种预注册 | 不塞进 `useSections` |
| **新建** `WidgetHost.vue` + `useWidgetHost.ts` | 并行 Query/Data；单卡失败互不影响 | 不读 layout/theme store |
| **新建** `WidgetGrid.vue` + `useWidgetGrid.ts` | 12 栅格自动流，`w`→`grid-column: span` | 无拖拽库 |
| **新建** `MetricCardWidget.vue` + use | 主值、trend、可选 sparkline、点击跳转 | — |
| **新建** `MiniChartWidget.vue` + use | 平台模板 option + `initEcharts` | 不读用户自由 option（legacy 除外） |
| **新建** `MiniKanbanWidget.vue` + use | 调 `KanbanBoard` `compact` | 不写回、无编辑按钮 |
| **新建** `WidgetConfigDrawer.vue` + use | 添加/编辑；Sources 下拉含「当前实体」 | — |
| **新建** `UnknownWidget.vue` | 未安装 kind | — |
| **新建** `LockedWidget.vue` | 403 | — |
| `features/views/KanbanBoard.vue` | 增 `compact?: boolean` 默认 false；true 时隐藏编辑/删除、降低高度 | 六视图看板行为不变 |
| `views/crud/DefaultList.vue` | InsightPanel 改为薄封装 Host | 容器顺序：洞察→Tab→视图→分页→抽屉 |
| `features/search/InsightPanel.vue` | 改为洞察表面：Host + 「添加部件」；无 widgets 且非编辑态不占高度 | 删除内联 stat chips / 用户 chartOption 主路径 |
| `stores/viewProfile.ts` | 解析/保存 `dashboardJson`；`updateDashboard` 防抖 400ms | `stateToWirePayload` 默认 **不**每次带 views 时误清 dashboard；payload 显式带 `dashboardJson` |
| `core/utils/viewProfile.ts` | `parseDashboardJson` 调 api-core 或本地包装；**不再**把 insight 当写入权威 | `NamedView.insight` 类型保留只读 |
| `views/crud/useListQuery.ts` | Host 不依赖 `statData`；可停止仅为 insight 调 GetChartData（P4：仅当合成 legacyChart/开发者图时仍调） | `loadData` 列表逻辑 |
| `views/crud/useListViews.ts` | `onFilterApply` 后通知 Host 重查（事件/inject）。列表 skipFetch **不改** | — |
| `views/crud/ViewConfigDrawer.vue` | 去掉「统计标签/固定图表」作为仪表盘主入口（可留只读提示「已迁移」） | 列/映射/筛选配置 |
| `views/crud/listContext.ts` | 提供 `hostTypePath`、`viewFilter`、`dashboard` | — |

`registerWidget` 与 Section 并列，在 `main.ts` 注册平台 kinds；`apps/{biz}` 可再注册。

### 6.1 Host inject 契约（工作台复用）

```ts
interface WidgetSurfaceContext {
  surface: 'insight' | 'workbench'
  hostTypePath?: string          // insight 必填；workbench 可空
  hostFilter: ViewFilter | null
  canEdit: boolean               // 洞察槽：已登录即可编个人；模板入口跟现有 isAdmin
  dashboard: DashboardConfig
  saveDashboard: (next: DashboardConfig) => Promise<void>
}
```

禁止 Host 读取 `useListQuery` 内部 ref；只经 inject/props。这样工作台只换 Context。

### 6.2 洞察槽 UI（详见 `ui/information-architecture.md`）

- 空且 `canEdit`：悬停工具条「添加部件」，默认高度 0。
- 有部件：12 列 CSS grid，间距 12px；密度跟随 `UserProfile.theme.density`（comfortable 卡片 padding 16 / compact 10）。
- 排序：配置抽屉内上下移动，或卡片菜单「左移/右移」（改 `order`）。**不做**拖拽画布。
- 添加上限 12，满则禁用添加并 Message。

### 6.3 筛选联动矩阵

| 源 | 宿主 viewFilter | 结果 |
|----|-----------------|------|
| 同源 entity.* | 有条件 | AND 进 Query，`hostFilterApplied=true` |
| 同源 | 空 | 只 extraFilter |
| 跨实体 + 已填 linkFilter | 有条件 | 服务端按 mapping AND |
| 跨实体 + 无 linkFilter | 任意 | 不吃宿主筛选，卡上角标「未联动」 |
| named | 任意 | 把 hostFilter 放进 WidgetContext，由 C# 决定；本号无 named 实现则 N/A |
| SearchDrawer / effectiveSearch | 任意 | **不传** Query |

切换命名视图：DashboardJson **不**变；仅 `hostFilter` 变（各 NamedView.filter）→ Host 重查。

## 7. 旧 insight 合成（P4，只读）

仅当解析后个人+模板 DashboardJson 均「未配置」（null）时执行。写入 DashboardJson 后永久停止合成。

| 旧状态 | 合成 |
|--------|------|
| `showStat` 且 `statData` 有键 | 每个非空 stat 字段一张同源 `metricCard`（measure 无法从 stat 反推则用 count 一张 + 标题「统计」）。**注意**：合成卡展示仍可临时读本次 GetList.stat；用户一点「保存到仪表盘」才变成 Query 卡 |
| `showStat` 且 stat 空 | 一张 count `metricCard` 标题「记录数」 |
| `showChart` 且 `OnGetChartData` 非空 | 一张只读卡，仍走 GetChartData（不进入 kind 新编） |
| `showChart` 且 `chartOption` | `kind: legacyChart`，沿用现 `applyChartData`+当前页行；菜单仅「删除 / 升级为迷你图表」 |
| 双关 | 不合成、不占高度 |

升级：打开 ConfigDrawer 预填 miniChart bar；保存则写入 DashboardJson 且去掉该 legacy 卡。删除 legacy：写入 DashboardJson（可空数组）。

`updateInsight`：**停止**作为主写。ViewConfigDrawer 双开关隐藏或改为链到仪表盘。仍可读 `insight` 供合成。

## 8. 权限与可见性矩阵

| 输入 | 输出 |
|------|------|
| 未登录调 Widget API | 401 |
| 无 Detail 的 typePath Query | 403 → LockedWidget |
| Sources | 不含无 Detail 实体 |
| 保存含无权限 typePath | 400 |
| 模板含用户无权限的表 | GET 配置仍下发；Query 锁卡 |
| named AdminOnly + 普通用户 | Catalog 无；Data 404 |
| 未知 kind | 渲染 UnknownWidget，不请求 Query |
| 整页 apps 覆写 | 不挂 Host（与现 Insight 一样不出现） |
| Section 替换 DefaultListPage | 不强制兼容 Host |

## 9. 核心文档影响

| 文档 | 影响 |
|------|------|
| `ArcoVue企业中后台迁移方案.md` §8.5.3 / §3.1 图表行 / §10.4 #13 | 改为 Widget 协议；本号 ID |
| `Doc/功能清单.md` | SPA-7/SPA-15 测试列；拟新增 `DASH-1` 页面仪表盘 Widget |
| `Doc/Api/核心接口架构.md` | ViewProfile `dashboardJson`；`/Cube/Widget/*` |
| `Doc/Api/前端对接指南.md` | Profile 增 dashboardJson；Widget API |
| `NewLife.Cube.ArcoVue/web/README.md` | `features/widget`、`registerWidget` |
| `竞品分析报告.md` §3.1 仪表盘行 | ⚠️→ 本号目标（验收后改） |

## 10. 测试设计

后端：无权限 Sources 不含 Admin/User；Query count 与 FindCount 一致；sum 非法字段 400；跨实体无 mapping 的 hostFilter 不进入 Where；sql 键 400；DashboardJson 超 12 条 / 重复 id / 64KB 400；空串清除继承模板。

前端：parse 未知键保留；legacy 仅 null dashboard 时合成；linkFilter 空显示未联动；未知 kind 占位；serialize 按 order 重排。

构建：`dotnet build NewLife.Cube`、`NewLife.CubeNC`（Link 实体编译过）、`pnpm --filter @cube/api-core test`、`pnpm --filter @cube/arco-vue test` 与 build。
