# OSC-0016 Design — 通用查询与预定义查询

## 0. 适用框架与官方资料

| 场景 | 框架 | 官方资料 |
| --- | --- | --- |
| 下拉按钮/菜单/弹窗/表单控件（QueryComboButton、Q/日期控件） | Arco Design Vue | https://arco.design/vue/docs/start （`Dropdown`/`Doption`/`Modal`/`RangePicker`/`DatePicker`/`InputNumber`/`TimePicker`/`Popconfirm` 组件文档；实现前必须查阅对应组件官方 API，不得凭印象补造 props/emits） |
| 多维数据视图 | 本号**不触及** VTable | — |

后端为 NewLife.Cube / NewLife.CubeNC（C#，`<LangVersion>latest</LangVersion>`，类型名用 .NET 正式名 `String`/`Int32`/`Boolean`）。

## 1. 目标与契约边界

在**不改动 `Search(Pager)` 查询逻辑**（`NewLife.Cube/Common/ReadOnlyEntityController2.cs` L63-98：`Q`→`SearchWhereByKeys`、`dtStart/dtEnd`→`MasterTime.Between`、字段名=值→`field.Equal`）的前提下：

- **后端**只扩展四处元数据/LOV 链路：ViewProfile 增列 `QueriesJson`；`FieldCollection` Search 分支为 Map 字段填充候选；`LovAutoRegisterService`/`LovController` 支持 `Entity.` 内部实体值集；GetPage setting 增补 MasterTime 信息。
- **前端**在 `QueryInsightPanel` 内补齐保留参数控件与查询组合按钮；range 控件改单值等值；预定义查询走 ViewProfile 持久化。

**与既有机制的职责分离**：
- 搜索面板（本号）= 服务端等值查询，参数并入 `effectiveSearch` → GetList。
- OSC-0015 筛选构建器 = 纯前端过滤，存 `NamedView.filter`，不动。
- 「保存到此视图」（OSC-0012 FiltersJson）= 命名视图默认搜索，不动；本号的预定义查询是**独立新域**，三者并存互不覆盖。

## 2. 文件级改动地图

### 2.1 后端

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `NewLife.Cube/Entity/Cube.xml`（ViewProfile 表，约 L754-780） | `FiltersJson` 列后新增 `<Column Name="QueriesJson" DataType="String" Length="-1" Description="预定义查询。JSON" />` | 其余列、`UserId,TypePath` 唯一索引 |
| `NewLife.Cube/Entity/视图配置.cs` | xcode 重新生成：新增 `QueriesJson` 属性与字段名常量 `__.QueriesJson` | 生成代码其余部分 |
| `NewLife.Cube/Entity/Models/ViewProfileModel.cs` | `FiltersJson` 属性后新增 `public String QueriesJson { get; set; }`（含 `/// <summary>预定义查询。JSON</summary>`） | 其余属性 |
| `NewLife.Cube/Entity/视图配置.Biz.cs` `UpsertForUser` | 在 `FiltersJson` 赋值行后追加 `if (model.QueriesJson != null) entity.QueriesJson = model.QueriesJson;` | `ToModel()`（`model.Copy(this)` 反射拷贝自动带新属性）、其余业务方法 |
| `NewLife.CubeNC/ViewModels/FieldCollection.cs`（Search 分支，L103-126） | `sf` 创建后调用新私有方法 `FillMapCandidates(sf, field)`：`field.Map != null` 且 `sf.LovCode` 空且 `sf.DataSourceMap` 空时——目标表行数 ≤ `CubeSetting.Current.MaxDropDownList` → 遍历 `field.Map.Provider.GetDataSource()` 填 `sf.DataSourceMap`（键=外键值字符串，值=目标实体显示串）；否则 `sf.LovCode = "Entity." + provider.EntityType.FullName`。行数判断经 MemoryCache 60s（key=`"LovMapCount:" + EntityType.FullName`） | Flags 多选标记、SearchBuilder 字段集、其余 ViewKinds 分支 |
| `NewLife.Cube/Services/LovAutoRegisterService.cs` | 新增 `ScanAndRegisterMapLovs()`（或并入 `ScanAndRegister`）：遍历 `EntityFactory` 已注册工厂，收集实体类型属性上 `MapAttribute` 的目标实体；对目标表行数 > MaxDropDownList 者注册 `LovDefinition { LovCode="Entity."+FullName, Type="LIST", Source="AUTO" }` + `LovListConfig { RequestUrl="entity:"+EntityType.Name, Pageable=true, PageNumField="pageNum", PageSizeField="pageSize" }` + `def.ValueField=Unique 字段名`、`def.LabelField=Unique 字段名`（行值翻译回退 `ToString()`，见 §4.3）；`Source != "AUTO"` 的已存在定义跳过 | 枚举注册逻辑、`Enum.` LovCode 规则 |
| `NewLife.Cube/Areas/Admin/Controllers/LovController.cs` | `FetchRemoteList` 开头识别 `config.RequestUrl` 以 `entity:` 开头 → 转内部查询分支：按名称解析 `EntityFactory`（未注册抛 `InvalidOperationException`），`FindAll(SearchWhereByKeys(Q), pager)` 分页（`pageNum` 从 1 起）+ `Q` 参数模糊；行输出 `{ValueField: 主键值, LabelField: entity.ToString()}`。`BatchLabel` LIST 分支经同一 `FetchRemoteList` 自动受益 | Meta/外部 HTTP 代理逻辑、授权特性（`PermissionFlags.Detail`） |
| `NewLife.Cube/Common/ReadOnlyEntityController.cs` `GetPage`（L96-142） | setting 输出对象增补 `masterTimeName`/`masterTimeDisplayName`（`Factory.MasterTime?.Name` / `?.DisplayName`；MasterTime 为 null 不输出两键） | setting 其余开关、`search/list/addForm/editForm/detail` 分区 |
| `NewLife.CubeNC/Views/Shared/_Common_List_Search.cshtml` | 若模板消费 `DataSourceMap` 渲染下拉：键数 > `CubeSetting.Current.MaxDropDownList` 时退回原文本框（NC 守卫，防大表候选灌入 MVC 下拉） | 枚举/布尔既有渲染分支 |

> NC 影响声明：`FieldCollection`/`DataField` 为 NC/非NC 共享源码；Search 分支仅**增填**元数据（DataSourceMap/LovCode），不改字段集合与名称。NC MVC 列表页需冒烟回归（AC-12）。

### 2.2 前端

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `packages/api-core/src/types.ts`（`ViewProfileModel`，L434 附近） | `filtersJson` 后新增 `queriesJson?: string \| null;` | 其余字段 |
| `web/src/core/utils/fieldControl.ts` `resolveSearchControl` | 返回值变更：DateTime→`'date'`（单值）、DateTime+需时分秒场景→`'datetime'`（单值 show-time）、数值→`'number'`、Time→`'time'`（单值）；删除 `dateRange/datetimeRange/numberRange/timeRange` 判定 | dataSource 优先、lovCode→lov/lovMulti、cascader 判定 |
| `web/src/components/SearchFieldInput.vue` | 四个 range 模板替换为单值控件：`date`→`a-date-picker`（value-format `YYYY-MM-DD`）、`datetime`→`a-date-picker show-time`（`YYYY-MM-DDTHH:mm:ss`）、`number`→`a-input-number`（hide-button，width 100%）、`time`→`a-time-picker`（`HH:mm:ss`）；均以字段名单键 `emitScalar` 提交；回车/变更语义与 text 一致 | select/switch/lov/cascader 分支、props/emits 签名 |
| `web/src/core/utils/searchFilters.ts` | 删除 `RANGE_CONTROLS` 与 `collectSearchKeys` 的 `_min/_max` 展开；`collectSearchKeys(fields)` 返回值并入保留键 `Q`、`dtStart`、`dtEnd`（导出常量 `RESERVED_SEARCH_KEYS`） | `cleanSearchParams`/`parseUrlSearch`/`matchesViewFilter`（OSC-0015 不动） |
| `web/src/core/utils/viewProfile.ts` | 新增 QueriesJson wire 类型与 `parseQueriesWire`/`serializeQueriesWire`/`normalizeSavedQuery`（schema 与归一见 §3.2） | FiltersJson/ViewsJson 既有函数 |
| `web/src/stores/viewProfile.ts` | entry 增 `queries: SavedQuery[]`、`activeQueryId: string \| null`（会话态不持久化）；加载时从 personal `queriesJson` 解析（**不做模板回退**）；新增动作 `saveQueryAs(name, params)` / `renameQuery(id, name)` / `deleteQuery(id)` / `applyQuery(id)` / `clearActiveQuery()`；`scheduleSave` payload 增 `queriesJson`（与 filtersJson 同 debounce/回滚链路） | views/filters/form/pageSize 各域保存 |
| `web/src/features/search/QueryInsightPanel.vue` | 字段网格末尾追加三个**固定项**（不进 `measureFieldGrid` 溢出测量，始终可见）：① 主时间范围 `a-range-picker show-time`（props 增 `masterTimeName?: string`、`masterTimeDisplayName?: string`；写 `model.dtStart/dtEnd`）② 关键字 `a-input`（props 增 `enableKey?: boolean`，默认 true；label「关键字」placeholder「全字段模糊搜索」，回车 `$emit('search')`，写 `model.Q`）③ `<QueryComboButton>`；`showSearchPanel` 判定放宽为 `searchFields.length \|\| masterTimeName \|\| enableKey` | 一行折叠「展开更多 N」、操作区四按钮、stat/chart 洞察区 |
| `web/src/features/search/QueryComboButton.vue`（新增） | 查询组合按钮（UI 规格见 §5） | — |
| `web/src/views/crud/DefaultList.vue` | `searchForm` 增 `Q/dtStart/dtEnd` 键；`loadFields` 读取 `meta.setting.masterTimeName/masterTimeDisplayName/enableKey` 透传面板；`loadData` 的 effectiveSearch 已含三键（经 collectSearchKeys 保留）；接线 QueryComboButton 事件：apply→回填 searchForm+`searchTouched=true`+`loadData()`+设 `activeQueryId`；save/rename/delete→store 动作；clear→清 searchForm 全键+`activeQueryId=null`+`loadData()` | URL 只读策略、savedSearch 优先级、OSC-0015 筛选逻辑 |
| `wwwroot/`（构建产物） | 重新 `npm run build` 生成，消除孤儿 `CascaderSearchPanel` | 其余 chunk 由构建自然更新 |

## 3. JSON schema 与兼容

### 3.1 QueriesJson 线缆格式（ViewProfile.QueriesJson）

```ts
/** 预定义查询条目 */
interface SavedQuery {
  /** 唯一 id；生成规则 `q_` + Date.now().toString(36) + 4 位随机 base36 */
  id: string
  /** 查询名；trim 后 1~50 字符 */
  name: string
  /** 查询参数：经 cleanSearchParams 清理的平坦键值（含 Q/dtStart/dtEnd 保留键） */
  params: Record<string, unknown>
}

/** QueriesJson 线缆 */
interface SavedQueriesWire {
  version: 1
  queries: SavedQuery[]
}
```

**归一化顺序（`parseQueriesWire`，宽容解析）**：
1. 输入 null/空串/解析失败/非对象 → `{ version: 1, queries: [] }`。
2. `queries` 非数组 → `[]`。
3. 逐条：非对象丢弃；`name` 非字符串或 trim 空丢弃；`name` 截断 50 字符；`id` 非字符串/空/与已有重复 → 重新生成；`params` 经 `cleanSearchParams(params, collectSearchKeys(searchFields))` 清理后为空对象 → 该条丢弃。
4. 序列化：`JSON.stringify({ version: 1, queries })`；空列表序列化为 `{"version":1,"queries":[]}`（与 FiltersJson 空壳语义一致）；PUT 空串 `''` 表示清除该域（沿用 OSC-0014 模板清除约定）。

**旧数据兼容**：无 `queriesJson` 列时代保存的 ViewProfile 行读取为 null → 空列表；`UpsertForUser` 中 `model.QueriesJson == null` 不覆盖（与既有各 Json 列语义一致）。

### 3.2 保留搜索键

`RESERVED_SEARCH_KEYS = ['Q', 'dtStart', 'dtEnd']`。`collectSearchKeys` 返回 `字段键 ∪ 保留键`；`cleanSearchParams`/`parseUrlSearch` 因此自动接纳三键（URL 带 `?Q=xx&dtStart=..&dtEnd=..` 可读入）。「保存到此视图」（FiltersJson）与预定义查询 params 共用同一清理函数，天然包含保留键。

**废弃行为声明**：`字段_min/字段_max` 键不再产生；已保存 FiltersJson 中的旧 `_min/_max` 键经 `cleanSearchParams` 自然丢弃（静默失效，无数据损坏，见 retro 关注点）。

## 4. 条件矩阵（穷尽）

### 4.1 第一行末尾固定控件渲染矩阵

| 条件 | 主时间范围 | 关键字 Q | 查询组合按钮 |
| --- | --- | --- | --- |
| `setting.masterTimeName` 存在 | ✅ 渲染（标签=masterTimeDisplayName，缺省「时间范围」） | — | — |
| `setting.masterTimeName` 缺失 | ❌ 不渲染 | — | — |
| `setting.enableKey === false` | — | ❌ 不渲染 | — |
| `setting.enableKey` 为 true/undefined | — | ✅ 渲染 | — |
| 任意情况（面板可见时） | — | — | ✅ 始终渲染 |
| `searchFields` 空 且 两保留控件均不可用 | 面板整体隐藏（`showSearchPanel=false`，既有逻辑） | | |
| `searchFields` 空 但 任一保留控件可用 | 面板显示，仅固定控件 | | |

### 4.2 查询组合按钮菜单项可用性矩阵

| 菜单项 | 可用条件 | 禁用态行为 |
| --- | --- | --- |
| 执行查询 | 恒可用 | — |
| 预定义查询条目（点击应用） | 列表非空时逐条可用；空列表显示灰字「暂无预定义查询」 | — |
| 保存当前查询为预定义… | 当前参数（cleanSearchParams 后）非空 | 置灰 + tooltip「请先输入查询条件」 |
| 重命名当前查询 | `activeQueryId` 非空且存在于列表 | 置灰 |
| 删除当前查询 | 同上 | 置灰 |
| 清空查询参数 | searchForm 任一键非空（含 Q/dt） | 置灰 |

**应用语义**：点击条目 → 用 `params` **整体替换** searchForm（先清空再回填，避免残留键）→ `searchTouched=true` → `loadData()` → `activeQueryId=该条目 id`。
**保存语义**：弹命名 Modal（a-modal，输入框，非空校验、重名允许但提示）→ 新增条目并 `activeQueryId` 指向新条目 → scheduleSave → **自动执行一次查询**。
**重命名/删除语义**：仅作用于 `activeQueryId` 对应条目；删除前 a-popconfirm 二次确认；删除后 `activeQueryId=null`，**不清空当前表单参数**。
**清空语义**：清 searchForm 全部键（含 Q/dt）+ `activeQueryId=null` + `loadData()`。
**脏态标记**：`activeQueryId` 存在但当前参数 ≠ 该条目 params 时，菜单中该条目不显示 ✓（仅参数完全一致显示 ✓）；不清空 `activeQueryId`。

### 4.3 Map 字段候选决策矩阵

| 目标表行数 | 手工 LovCode | 手工 DataSourceMap | 结果 |
| --- | --- | --- | --- |
| ≤ MaxDropDownList | 无 | 无 | 内联 `DataSourceMap`（前端自动渲染本地下拉） |
| > MaxDropDownList | 无 | 无 | `LovCode="Entity.{FullName}"`（前端 LovSelect 远程搜索） |
| 任意 | 已设 | — | 保留手工 LovCode，不覆盖 |
| 任意 | — | 已设 | 保留手工 DataSourceMap，不覆盖 |
| `field.Map == null` | — | — | 不处理 |

`entity:` 内部 ListData：`pageNum<1` 归一为 1；`pageSize` 缺省 20、上限 500；`Q` 参数走目标实体 `SearchWhereByKeys`；目标实体未注册 EntityFactory → 抛 `InvalidOperationException`（HTTP 500 语义由既有异常处理承接，不泄露堆栈）。BatchLabel 反查复用同一内部查询路径。

## 5. QueryComboButton UI 规格

```
[查询 ▾]   ← a-button type="primary" size 与面板搜索按钮一致；a-dropdown trigger="click"
└─ 下拉菜单（a-doption 组）
   ├─ 执行查询                     ← 图标 search；恒可用
   ├─ ──分隔线── 「预定义查询」分组标题（灰字）
   ├─ ✓ 昨日新增客户                ← 当前应用且参数一致时前缀 ✓；hover 行尾出现删除图标（popconfirm）
   ├─   本月大额订单                ← 点击应用
   ├─   （空列表时：灰字「暂无预定义查询」，不可点击）
   ├─ ──分隔线──
   ├─ 保存当前查询为预定义…          ← 图标 save；参数空则禁用
   ├─ 重命名当前查询                ← 图标 edit；无 activeQueryId 禁用
   ├─ 删除当前查询                  ← 图标 delete；无 activeQueryId 禁用；popconfirm 确认
   └─ 清空查询参数                  ← 图标 eraser；全空禁用
```

- 命名弹窗：`a-modal` 标题「保存为预定义查询」/「重命名查询」，单输入框（maxlength 50），确认按钮禁用条件=trim 空。
- 下拉内容超出 8 条时菜单区滚动（max-height 320px）。
- props：`queries: SavedQuery[]`、`activeQueryId: string | null`、`paramsDirty: boolean`、`canSave: boolean`、`canClear: boolean`；emits：`search`、`apply(id)`、`save(name)`、`rename(id, name)`、`delete(id)`、`clear`。**组件无状态**，全部状态在 DefaultList/store。

## 6. 状态与唯一来源

| 状态 | 唯一来源 | 说明 |
| --- | --- | --- |
| 搜索表单值 | `DefaultList.searchForm`（reactive） | 含 Q/dtStart/dtEnd；面板控件直接读写 |
| 生效搜索参数 | `effectiveSearch`（OSC-0012 既有推导） | URL > 已保存 > 表单 |
| 预定义查询列表 | `viewProfile store entry.queries` | 服务端 QueriesJson 为持久态 |
| 当前应用查询 | `entry.activeQueryId`（会话内存） | 不持久化；刷新后无"当前应用"态 |
| 视图默认筛选 | FiltersJson（OSC-0012） | 与本号互不覆盖 |

## 7. CascaderSearchPanel 清理

- 事实：`wwwroot/assets/index-DHwXUadr.js` 含 `CascaderSearchPanel` 组件定义（props：options/loading/activeKey/multiple/checkStrictly），但 `web/src`、`packages/**` 源码均无此组件（构建来源状态与当前源码不一致）。
- 动作：确认源码零引用（grep `CascaderSearchPanel` 于 `web/src`、`packages/**` 为 0 命中）→ `npm run build` 重新生成 wwwroot → 验收 grep `wwwroot/assets/**` 无命中。
- **保留区**：`web/src/components/CascaderField.vue`（地区级联输入控件，OSC-0009 基线）与 `fieldControl.ts` 的 `cascader` 控件类型**不得删除**。

## 8. 测试设计

### 8.1 后端 XUnit（XUnitTest 项目）
- Map 候选：构造带 Map 特性实体（复用测试基线实体）——小表目标 → SearchFields 对应字段 `DataSourceMap` 非空且键值正确；大表目标 → `LovCode` 以 `Entity.` 开头；手工预设 LovCode 不被覆盖。
- `entity:` ListData：注册 `LovDefinition/LovListConfig(RequestUrl="entity:Xxx")` → 分页正确、`Q` 过滤生效、未注册实体名抛 `InvalidOperationException`。
- GetPage setting：含 MasterTime 实体输出 `masterTimeName/masterTimeDisplayName`；无 MasterTime 不输出。
- QueriesJson：`UpsertForUser` 写入后 `FindByUserIdAndTypePath` 读回一致；`model.QueriesJson == null` 不覆盖已有值。

### 8.2 前端 Vitest
- `resolveSearchControl`：DateTime→date、数值→number、Time→time、dataSource/lovCode/cascader 既有分支不回归。
- `collectSearchKeys`：含保留键 `Q/dtStart/dtEnd`；不再生成 `_min/_max`。
- QueriesJson：`parseQueriesWire` 对 null/坏 JSON/重复 id/空 name/空 params 的归一；`serializeQueriesWire` round-trip。
- store：saveQueryAs/renameQuery/deleteQuery/applyQuery 后列表与 activeQueryId 正确；PUT payload 含 queriesJson。

### 8.3 构建与冒烟
- `npm.cmd --prefix packages/api-core run build|test`、`npm.cmd --prefix NewLife.Cube.ArcoVue\web run build|test`、后端 `dotnet build 魔方.sln`（或受影响项目）。
- 手工冒烟见 verify.md。

## 9. 核心文档影响

| 文档 | 影响 |
| --- | --- |
| `NewLife.Cube.ArcoVue/web/README.md` | 登记 OSC-0016 能力（查询组合/预定义查询/保留参数控件） |
| `Doc/功能清单.md` | SPA 搜索相关条目增补 OSC-0016 状态 |
| `Doc/Api/ArcoVue企业中后台迁移方案.md` | §8/§10 搜索面板相关章节最小增量更新（如提及搜索控件形态处） |
| `Doc/Api/核心接口架构.md` | `/Cube/ViewProfile` 载荷增 `queriesJson`、GetPage setting 增 masterTime 键（事实性登记） |

## 10. 风险

| 风险 | 缓解 |
| --- | --- |
| NC MVC 搜索模板消费新元数据出现大表下拉 | NC 守卫（§2.1）+ 冒烟 AC-12 |
| 旧 FiltersJson 中 `_min/_max` 键静默失效 | 文档标注；无数据损坏 |
| `Entity.` 自动注册扫描成本 | 启动期一次性 + 仅 Map 目标实体；行数判断缓存 |
| 内部实体值集越权读取 | 仅已注册 EntityFactory + LovController 既有 Detail 授权 |
