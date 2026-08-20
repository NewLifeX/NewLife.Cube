# OSC-260819e483 Design — 元数据契约与只读投影

适用前端：Arco Design Vue（https://arco.design/vue/docs/start）；列表/单元格用 VisActor VTable（配置 https://visactor.com/vtable/option/ListTable；接口 https://visactor.com/vtable/api/Methods）。不涉及 FlowGram。`.vue` 薄 script，业务进 `useXxx.ts`。

## 基类接口冻结（全阶段最高优先级）

| 符号 | 冻结 |
|------|------|
| `DataField.Fill(FieldItem)` / `Fill(PropertyInfo)` | 不改签名；**不在 Fill 内写 Required**（`DataFieldTests` 仍断言 Fill 后 Required=false） |
| `EnableFieldValidation` | 不改签名、默认仍 **false** |
| `GetPage` / `GetFields` / `OnGetFields` / `PrepareFieldsForApi` | 不改签名；GetPage 匿名对象可**加键** |
| `Search(Pager)` / `SearchData` / `CreateWhere` | 不改签名；禁止新增 `protected virtual ApplyRequestFilter` |
| `Insert` / `Update` / `OnUpdate` / `CopyFrom` / `EnableSelect` | 不改签名 |
| `WriteLog(...)` / `XCode.Membership.Log` 表 | 不改签名与表结构；**不装饰** `LogProvider`（`WriteLog(String, IEntity)` 非虚方法，且已写出字段对比） |
| `EntityComment.AddComment(userId, userName, category, linkId, content, parentId)` | 不改六参数签名 |
| `DataField` 属性列表 | 不加 formula / LookupDisplays / projections 字段 |
| GetPage `[AllowAnonymous]` | 不取消 |

加法：方法体内接线、`partial` 新 Action（P3，仅 WebAPI）、评论 POST 可选提及 Id、可选查询参数。禁止为已有能力再造平行类型。

## 对照现码的裁剪（审查）

对照 `NewLife.XCode`（含 `LogProvider` / `SqlBuilder.BuildOrder` / `FindAll(..., page.State)`）与 `NewLife.Cube`（含 `AutomationFilter` / `ValidateEntityFields` / `EnableOrDisableSelect` / `EntityComment` / `NotificationRecord`）。

| 草案曾设计 | 现码已有 | 本号改法 |
|------------|----------|----------|
| `EntityListFilter` 新 AST | `ViewFilterDto` + `AutomationFilter.TryBuildWhere` / `Match`（logic=`all`/`any`，与 OSC-0015 同构） | **复用**；`SearchData` 把可下推表达式并进 `p.State`（XCode `FindAll` 已 AND `WhereBuilder`/`Expression`） |
| 新参数 `sorts=` + 白名单 | `SqlBuilder.BuildOrder`：`Sort` 已支持 `Name,Code` / `Name desc`，非法字段抛异常；**`OrderBy` 原样返回、不校验** | **不新增 sorts**。Cube `Pager` 构造函数**已注释**从请求绑定 `OrderBy`。本号**不再**「清空客户端 OrderBy」。前端 `ViewSort` / `buildSortPayload` 维持**单列** `sort`+`desc` |
| `CubeFieldDiffLogProvider` + Remark JSON | `LogProvider.WriteLog(action, entity)` 已用 `Dirtys.GetDictionary()` 写 `Field=old -> new`（Update/修改）；`WriteLog(String, IEntity)` **不是 virtual** | **不包装、不改 Remark**。历史 Tab **解析**现有文法 |
| `EntityFieldChange` 表 | 无必要 | **不做** |
| 评论 `MentionsJson` 列 + xcode | `EntityComment.AddComment` 六参数；回复已有 `ReplyUserId`；通知已有 `NotificationRecord`（自动化 InApp） | **不改表**。POST 可选 `mentionUserIds`，保存后写通知 |
| `EntityFieldValidationGate` 复制校验 | `ValidateEntityFields` 私有方法已完整（WebAPI `EntityController`） | **不复制**。Insert/Update/**PATCH 写请求**的 `if` 增加请求头 |
| GetPage `projections` / `[EntityProjection]` | `DataField.MapField`/`DataSourceMap`；列表 `lovCode` + `fetchBatchLabel`；公式可用 C# 扩展属性 | **不新增 projections 协议**。图表 option 存 ViewProfile，不走后端生成 |
| Stat「分槽」新属性 | `RetrieveState` 时 `FindAll` 会把 `p.State` **换成**统计实体；无统计时 `SearchData` 把 `WhereBuilder` 留在 State，Index 强转才 500 | **只改** WebAPI `Index.Stat = p.State as TEntity` |
| 强迫改 Department/EntityTree 各写一套 AST | Department `FindAll(exp, p)` 会吃 `p.State`；EntityTree 走缓存、**不走** FindAll | 默认 Search / 走 FindAll 的 override **自动吃 State**。WebAPI 与 CubeNC **两份** `EntityTreeController.Search` 都调用 `AutomationFilter.Match` |

## 双栈文件地图（实施必对照）

CubeNC **Link** 了 `ReadOnlyEntityController2.cs` / `EntityController2.cs` / `Pager.cs` / `DataField.cs`；下列文件是**各写一份**的，改一边不等于另一边。

| 改动 | WebAPI（`NewLife.Cube`） | CubeNC MVC | 说明 |
|------|--------------------------|------------|------|
| `PrepareFieldsForApi` 写 Required | `Common/ReadOnlyEntityController.cs` | `CubeNC/Common/ReadOnlyEntityController.cs` | **两处都改**；`DataField.cs` 在 CubeNC，Fill 仍不写 Required |
| `Index.Stat as TEntity` | `ReadOnlyEntityController.Index` | **不改** | MVC `Index` 不返回 `Stat` JSON |
| 校验头 + `ValidateEntityFields` | `Common/EntityController.cs` Insert/Update/**PatchFields/BatchUpdateFields** | **不改** | CubeNC 无 `EnableFieldValidation` |
| `SearchData` 并 `viewFilter` | **共享** `ReadOnlyEntityController2.SearchData` | 自动生效 | 不改 `CreateWhere` 签名 |
| EntityTree 内存 `Match` | `Common/EntityTreeController.cs` | `CubeNC/Common/EntityTreeController.cs` | **两处都改** |
| `PatchFields` / `BatchUpdateFields` | WebAPI `EntityController` 的 `partial` 新文件 | **不加 Action、不加 MVC UI** | 勿放进被 Link 的 `EntityController2.cs`，以免 MVC 多出无 UI 的写接口 |
| 评论 `mentionUserIds` | `Controllers/CubeController.cs` | `CubeNC/Controllers/CubeController.cs` | **两处都改**；不改 Cube.xml / `EntityCommentModel` 生成属性 |
| `GetChartData` | **不改**（不新增 `autoChart`） | **不改** | 仅子类 `OnGetChartData` 非空时 Insight 仍消费；用户图走 ViewProfile |

## 跨框架影响

| 宿主 | P1 | P2–P5 |
|------|----|--------|
| ArcoVue | `required` 与 `isFieldRequired` 对齐；写请求（POST/PUT/**PATCH**）带头 | 传 `viewFilter`；单列 `sort`/`desc`；PATCH/批量；历史解析；@；Insight 配图写入 ViewProfile |
| NaiveUI | `DynamicPage` 已绑 `field.required`，PrepareFieldsForApi 后出现星号（**预期对齐，本号不改 NaiveUI**） | 不传新参则列表/写入与今日一致 |
| Cube.Vue | 文档已消费 required；本号不改 Vue 仓 | 同上 |
| CubeNC MVC | GetPage 出现 `required`（同源 PrepareFieldsForApi）；无 Stat JSON、无校验头 | SearchData 自动吃 viewFilter；无 PATCH Action；评论 POST 可带提及 |
| 外部 WebAPI | 无校验头则 Insert/Update 同今日 | 无 viewFilter、不调新 Action 则同今日 |

## P1 契约加固

### 文件地图

| 文件 | 计划 | 不动 |
|------|------|------|
| `NewLife.Cube/Common/ReadOnlyEntityController.cs` | `PrepareFieldsForApi` 循环内写 Required（矩阵见下）；`Index.Stat = p.State as TEntity` | GetPage 签名、AllowAnonymous、Fill |
| `NewLife.CubeNC/Common/ReadOnlyEntityController.cs` | **同一套** Required 赋值 | 不改 Index |
| `NewLife.Cube/Common/EntityController.cs` | Insert/Update 现有 `if (EnableFieldValidation)` 增加请求头判断，**仍调用私有** `ValidateEntityFields` | 方法签名；不抽第二份校验类 |
| `NewLife.CubeNC/ViewModels/DataField.cs` | **不改** | Fill 后 Required 仍 false |
| `web/src/core/utils/submitPayload.ts` + spec | `isFieldRequired`：`nullable===false` **或** `required===true` | OSC-0008 空值矩阵 |
| `Doc/Api/核心接口架构.md`、`Doc/功能清单.md` | 匿名边界、DATA-7 / SPA-7 | — |

不必新建 `EntityFieldRequired` / `EntityFieldValidationGate` 类型；若单测需要可抽 **10 行以内** static，禁止平行复制 `ValidateEntityFields`。

### Required 矩阵（PrepareFieldsForApi 末尾，Fill 之后）

| 条件 | Required |
|------|----------|
| 字段为 null | 跳过 |
| PrimaryKey / ReadOnly / Nullable==true | false |
| 其余（含 **布尔 NOT NULL**） | true |

布尔 `false`、数字 `0` 不是空（`ValidateEntityFields` 已如此）。因此启用列会出现星号，提交 `false` 仍合法——**已知 UX，本号不为此把布尔剔出 Required**。Apply 对 null 列表 no-op。

### Stat

`p.State` 仍可放 WhereBuilder（SearchData 赋值保留）。**仅 WebAPI** `Index.Stat`：`as TEntity`，非实体则为 null，**不得抛 InvalidCastException**。`RetrieveState` 换成统计实体（如 UserStat）时，若该对象是 `TEntity` 则 Stat 仍可用；否则为 null（统计实体类型不同时本来就不能当 TEntity 用）。

### 校验头

| 项 | 值 |
|----|-----|
| 头 | `X-Cube-Field-Validation` |
| 开 | `1` / `true` / `yes`（忽略大小写） |
| 与 EnableFieldValidation | 任一为真即校验 |

ArcoVue：实体 **POST / PUT / PATCH（含 BatchUpdateFields）** 附带。读请求、GetPage、GetList、GetChartData、评论 GET **不加**。

### isFieldRequired

`nullable===false` **或** `required===true` → 必填。两者皆缺/false → 非必填。

## P2 筛选 AST 与排序

### 筛选：复用自动化，不新造 AST

| 项 | 约定 |
|----|------|
| JSON | 与 OSC-0015 / `ViewFilterDto` 相同：`logic` 为 **`all`/`any`**（不是 and/or）；**无嵌套 groups** |
| 反序列化 | `AutomationExecutor.DeserializeFilter` |
| SQL | `AutomationFilter.TryBuildWhere`；**任一**条件无法下推则整段返回 null（现码行为，保持） |
| 本号允许补的下推 | `TryBuildWhere` 增加 `notcontains`（`Match` 已有）。**不**为数组 `eq`、嵌套 groups 新造 AST |
| 无法下推 | 不下推、不 500；当前页靠前端 `matchesViewFilter`。**翻页结果不完整是已知限制**（只保证本页复核） |
| 权限 | 不改 `CreateWhere`。`logic=any` 只 OR 筛选条件，必须与权限表达式 **AND** |
| 接线 | `SearchData`：现有 `p.State = builder` 之后处理 `p["viewFilter"]`。可下推时：`WhereBuilder.Factory ??= Factory`，再 `p.State = builder.GetExpression() & viewExp`（无 builder 则 `p.State = viewExp`）。XCode `FindAll` 对 `Expression`/`WhereBuilder` 均会 AND |
| 内存列表 | WebAPI + CubeNC 的 `EntityTreeController.Search`：对返回列表 `AutomationFilter.Match` |
| 其它 override Search | **不走** `FindAll(..., p)` 的（除已点名 EntityTree）**不会**自动吃 filter；本号不扫全库改 override |
| 空条件 | 不传 `viewFilter`；JSON 损坏 → **400**，不执行半截 |
| 传输 | GET 查询串；JSON 文本 **超过 4096 字符 → 400**（避免超长 URL） |
| 未知字段 | `TryBuildWhere` 对缺字段返回 null（整段不下推）。保持该行为 |

前端：`useListQuery` 有条件才把 `viewFilter` 放进 `getList` / `getChartData` 的查询参数（与 `effectiveSearch` 一起）。无条件不传该键。

### 排序：复用 Pager.Sort，不新增 sorts

| 项 | 约定 |
|----|------|
| 参数 | 现有 `sort` + `desc`。**不新增** `sorts` |
| 前端本号 | `ViewSort` / `buildSortPayload` **维持单列**。不承诺多列排序 UI（XCode `Sort` 多列能力留给以后，不在本号验收） |
| 校验 | 非法 `Sort` 字段维持 `SqlBuilder.BuildOrder` 抛 `XCodeException`；前端只发 GetPage 列表字段名 |
| `OrderBy` | Cube `Pager(IDictionary)` **已不从请求绑定** `OrderBy`。`SearchData` 仍可写 `uk.Desc()`（`PageSetting.OrderByKey`）。**禁止**再加一套「清空客户端 OrderBy」 |

## P3 PATCH 与批量改字段

对照 `EnableOrDisableSelect` 的循环：`ParseKeys`/`FindData` → `Valid` → `SetItem` → 持久化。**不要**学启停的 **GET + query keys**。

### HTTP 契约

| Action | 方法 | 权限 | Body | 成功响应 |
|--------|------|------|------|----------|
| `PatchFields` | **PATCH** | `PermissionFlags.Update` | `{ "id": "<主键>", "values": { "Field": x, ... } }` | `{ ok, fail, errors: [{ id, message }] }`；单行 fail=0 或 1 |
| `BatchUpdateFields` | **POST** | `PermissionFlags.Update` | `{ "keys": "<逗号分隔主键>", "field": "Name", "value": x }` | 同上，汇总全部 keys |

| 边界 | 行为 |
|------|------|
| 未登录 / 无 Update | 现有 `[EntityAuthorize]`（401/403） |
| `keys` 空或解析后长度为 0 | **400** |
| keys 条数 > 500 | **400** |
| `field` / `values` 键不在白名单 | 该键跳过或整单 400（**未知字段 → 400**，与错误表一致） |
| 单行 FindData 失败 / Valid 失败 / Update 异常 | 计入 `fail`，`errors` 带 id+message，**继续后续行**（部分成功） |
| 值类型 | `ChangeType` 到字段类型；失败计入该行 fail |
| 校验头 | 与 Insert/Update 相同：头或 `EnableFieldValidation` 为真则对该实体调用现有 `ValidateEntityFields(..., Update)` |

白名单 = `EditFormFields` ∩ `!ReadOnly` ∩ 非主键。循环内：**禁止**一条 SQL 批量 UPDATE。

持久化：`Valid(entity, Update, true)` + `SetItem` + **`OnUpdate`**（JSON 无 Form，`OnUpdate` 现码会落到 `entity.Update()`，子类 override 仍生效）。自动化已由 `AutomationPersistence` 包装 Persistence，不必再包一层。

**不**额外 `WriteLog("Update")`（与启停不同）：`LogOnChange` 默认 false，本号**不全局打开**；未开日志的实体历史 Tab 仍空。布尔列 UI 仍走 `EnableSelect`/`DisableSelect`。不改 PUT / `CopyFrom` 签名。

文件：`NewLife.Cube/Common/EntityController.Patch.cs`（`partial`，仅 WebAPI 工程编译）。不必 `EntityFieldWriteGuard` 类型。

单元格：失焦提交 `PatchFields`（避免 PUT 绑 TModel 默认值把未提交列打脏）。批量：高级菜单「批量修改」→ `BatchUpdateFields`。

## P4 变更 diff 与评论提及

### Diff：解析现有 Log.Remark，不改写入

`LogProvider.WriteLog(String action, IEntity entity)`（XCode，**非虚方法**）在 `action` 为 `Update`/`修改` 时已经：

- `Dirtys.GetDictionary()` → 旧值，`entity[fi]` → 新值
- 主键：`ID=12`
- 脏字段：`Name=张三 -> 李四`
- `pass`/`password` 两侧清空
- Insert/Delete：仅当前快照 `Name=值`
- `Remark` 最长约 2000，超长截断

Cube `Valid`（`LogOnChange`）已调用该重载。Admin/Log 与历史 Tab 读的就是这些行。

**本号后端不装饰 LogProvider、不改 Remark、不新建表、不改 XCode。**

前端（`web/src/core/utils/logRemarkDiff.ts`，历史 Tab 调用）：

1. Action 为 Update/修改/Edit 时，用抽屉已有 `fields`（GetPage 字段名）锚定：字段名 **长名优先**、忽略大小写，匹配 `Field=` / `,Field=`，段内再拆 ` -> `。
2. 解析出至少一条带箭头的变更 → 渲染字段表（显示名用字段元数据）。
3. 失败（逗号在值内、截断、Insert 快照、自动化 JSON）→ 现有 `historyRemark`。
4. **不要** `JSON.parse` 整段 Remark。
5. 口令列解析后仍不要展示明文（原写入已空）。

### 提及：不改评论表

`AddComment` 六参数不变，不增加 `MentionsJson`，不改 Cube.xml。

控制器内新增 **非 xcode** 请求类型（例如 `EntityCommentPostModel : EntityCommentModel`，仅多 `mentionUserIds`），WebAPI 与 CubeNC 的 `EntityComment` POST **同步**改参数类型。无该字段则与今日相同。

`AddComment` 成功后写 `NotificationRecord`：

| 项 | 值 |
|----|-----|
| Channel | `InApp` |
| Action | `Mention` |
| Title | `{当前用户名} 在评论中提到了你` |
| Content | 评论正文截断（建议 ≤200 字） |
| Target | `{category}#{linkId}`（即 typePath#id） |
| UserId | 被提及用户 |
| Success | true |
| 抄写 | 插入方式对齐 `AutomationActions` notify 的 `new NotificationRecord { ... }.Insert()` |

| 边界 | 行为 |
|------|------|
| 最多 | 20 个；`Distinct` |
| 非法 Id / 找不到 / `Enable==false` / 等于当前用户 | **跳过**，不 400 |
| 单条通知 Insert 失败 | 不回滚评论、不 500（评论已成功） |
| 未登录发表 | 现有 401 |

UI：`@` 选人后 Id 放 `mentionUserIds`，正文仍是纯文本。历史 Tab 用抽屉已有 `fields` 做 diff 锚点。

## P5 只读引用与用户图表（ViewProfile）

不新增 GetPage `projections`、不给 `DataField` 加属性、不加 `[EntityProjection]`。**不**新增 `GetChartData?autoChart=`，不在后端拼默认 ECharts option。

| 能力 | 现码 | 本号 |
|------|------|------|
| 查找展示 | `MapField` / `DataSourceMap` / `lovCode`；ArcoVue 列表已 `fetchBatchLabel` | **接线缺口用现有字段配置**，不新协议。「Map 仍显示数字」先查 lovCode/DataSourceMap 是否未配 |
| 只读公式 | C# 扩展属性（与 Map 扩展同类） | 修订迁移方案 §8.2.6：允许扩展属性只读列；禁止用户 JS/SQL 与双向写回 |
| 图表 | `ViewInsight` 仅 `showStat`/`showChart`；`normalizeInsight` 丢弃其它键；Insight 向 `GetChartData` 取 option；`OnGetChartData` 默认 `[]` | 用户在 **InsightPanel** 配置一张图的 ECharts option，经现有 `updateInsight` → ViewProfile **`ViewsJson` 当前 NamedView.insight** 持久化。不改 Cube.xml、不加列。修订 §8.2.2 / §8.2.3（取消「禁止用户 option」） |

### 图表存储（穷尽）

`ViewInsight` 扩展（仍在 ViewsJson，无新实体字段）：

| 字段 | 合法值 | 默认 |
|------|--------|------|
| `showStat` / `showChart` | boolean | false（与 OSC-0012 相同） |
| `chartOption` | JSON 对象（ECharts option **模板**）或缺省/null | 无图 |

| 规则 | 约定 |
|------|------|
| 数量 | **最多一张**（单对象，不是 option 数组；不做仪表盘拖拽） |
| 保存通道 | 现有 `PUT /Cube/ViewProfile`；`serializeInsight` **显式写入** `chartOption`；`normalizeInsight` **必须读回**（现码只返回双开关，本号要改，否则 round-trip 丢失） |
| 保存前清洗 | 深拷贝 JSON；删除 `dataset.source` 与每个 `series[i].data`（禁止把列表快照写进 Profile） |
| 体积 | 清洗后 `JSON.stringify` **> 32KB → 拒绝保存**，界面报错，不截断半写 |
| 非法值 | 非对象的 `chartOption` 归一化为缺省；禁止函数（JSON 本来就没有） |
| 旧数据 | 无 `chartOption` 的 insight 与今日相同（仅开关） |
| 模板 | `UserId=0` 全局模板同样可带 `chartOption`；个人 Profile 覆盖整份 insight |

### 渲染优先级

`showChart===false`：不画图、不请求 `GetChartData`。

`showChart===true`：

1. 若 `GetChartData`（现有 `OnGetChartData`）返回 **非空数组** → **开发者图优先**，直接 `setOption`（与今日 Insight 相同）。不把用户 `chartOption` 叠上去。
2. 否则若有 `chartOption` → 前端 `applyChartData(option, 当前列表行)`：写入 `dataset.source = rows`（行对象键=GetPage 列表字段名）；无 `dataset` 则补上。然后 `setOption`。数据随当前 GetList（含 search / viewFilter），**不另开 1000 行通道**（图表范围=已加载列表，已知限制）。
3. 否则空态：InsightPanel 提供 **配置图表** 入口（不要只显示「暂无图表数据」却无法配置）。

仅当第 1 步需要时才请求 `GetChartData`（有用户 `chartOption` 且不依赖开发者图时 **不请求**）。

### InsightPanel 配置 UI

- 入口：洞察区图表空态按钮，以及 ViewConfigDrawer「固定图表」打开后的「配置图表」（同一套 `chartOption`）。
- 编辑：JSON 可编辑的 ECharts option（Arco 文本框即可，不必新图表设计器）。预览用当前列表行 `applyChartData`。
- 保存：走已有 `onInsightChange` / `evpStore.updateInsight`（防抖与 ViewProfile 其它字段相同）。
- 清除：`chartOption=null`，开关可仍为 true（回到空态+配置入口）。

明确不做：多图看板、option 内 JS formatter、后端按 MasterTime 自动出图、改 `GetChartData` 签名。

## 错误与边界（全阶段）

| 情况 | 行为 |
|------|------|
| 未知筛选字段 | 整段不下推，不 500 |
| 未知 PATCH 字段 | 400 |
| 非法 mention | 跳过 |
| viewFilter JSON 损坏 / 超 4KB | 400，不执行半截 AST |
| 批量 keys 空或超 500 | 400 |
| 无 Update 权限调 PATCH | 401/403 |
| GetPage 匿名 | 不得下发权限表达式、连接串、密钥 |
| 无法下推的 viewFilter | 本页前端复核；跨页不保证完整，不假装服务端已滤 |
| chartOption 超 32KB 或非 JSON 对象 | 拒绝保存，不写半截 ViewsJson |

## 核心文档影响

| 文档 | 内容 |
|------|------|
| `Doc/Api/核心接口架构.md` | 匿名 GetPage、required、校验头、viewFilter、Sort、PATCH/Batch 契约、双栈差异；图表 option 在 ViewProfile 不在 GetPage |
| `Doc/功能清单.md` | DATA-7、DATA-10、SPA-7/18、AI-7 一句 |
| `ArcoVue企业中后台迁移方案.md` §8.2.2 / §8.2.3 / §8.2.6 | 洞察允许一张用户 option 存 ViewsJson；只读公式例外 |
| `web/README.md` | 新查询参数与头；单列 sort；viewFilter 4KB；insight.chartOption |
| `竞品分析报告.md` §8.6 | 回写本 OSC（P5 图表=ViewProfile，不是 autoChart） |

## 测试设计

| 层 | 要点 |
|----|------|
| P1 | Required 在 PrepareFieldsForApi 赋值（WebAPI+CubeNC 同源矩阵）；Fill 后仍 false；Stat as 不抛；无头不校验；布尔 false 可通过校验 |
| P2 | 复用 AutomationFilter；viewFilter AND 进 State；`any` 不能放大权限；`notcontains` 可下推；超长/坏 JSON 400；不测多列 UI |
| P3 | 白名单；部分失败汇总；超 500 / 空 keys 400；EnableSelect/PUT 回归；GET 不得改字段 |
| P4 | 解析 `Field=old -> new`（长名优先）；逗号值回退原文；提及只写 NotificationRecord；非法 Id 跳过 |
| P5 | `normalizeInsight`/`serializeInsight` round-trip `chartOption`；保存剔除 data；超 32KB 拒绝；无 `autoChart` 参数；§8.2.2–8.2.6 文档 |
| 前端 | 各阶段纯函数 spec；构建 arco-vue |

手工冒烟见 `verify.md`。
