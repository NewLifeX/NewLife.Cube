# OSC-260830a1b2 Design — 查询收口与筛选服务端化

适用前端：Arco Design Vue（https://arco.design/vue/docs/start）。列表多维视图：VisActor VTable（https://visactor.com/vtable/option/ListTable）。不涉及 FlowGram。`.vue` 薄 script；查询/筛选逻辑进 `useXxx.ts` 或 `core/utils`。

## 0. 状态唯一来源

| 状态 | 来源 | 禁止 |
| --- | --- | --- |
| 自定义查询条件 | 会话 `viewFilter`（打开视图时由 NamedView.filter 灌入默认值）→ 请求 `viewFilter` | SearchDrawer 的 GetPage Search 字段当条件 |
| 关键字 | 工具栏 Q → 请求 `Q` → `SearchWhereByKeys` | 把 Q 做成 viewFilter 的一行 |
| 预定义方案 | `ViewProfile.QueriesJson` v2：`q` + `filter` | 用 NamedView 当预定义列表；v1 其它 params 当条件 |
| 行权 / 租户 | `CreateWhere` 在 `p.State` 左侧 | viewFilter `logic=any` OR 进权限 |
| 可查询字段 | GetPage `search[]` ∪ `list[]` 的 `name` | `Factory.AllFields` 当白名单 |
| 时间窗 | 响应头 `X-Cube-Filter-Narrowed` / Widget `filterNarrowed` | 前端猜 30 天 |

查询簇 UI 见 `ui/information-architecture.md`。查询图标固定 `search`。

## 1. 前置：XCode State 透明通道（禁止推翻）

```csharp
// XCode Entity.FindAll(Expression, PageParameter)
if (page.State is Expression exp)
    where &= exp;
else if (page.State is WhereBuilder builder)
{
    builder.Factory ??= Meta.Factory;
    where &= builder.GetExpression();
}
```

`ReadOnlyEntityController2.SearchData` 已 `p.State = CreateWhere() & viewExp`。**禁止**为 18 个控制器加 `ApplyRequestFilter`。

例外（本号处理边界）：

| 路径 | 行为 |
| --- | --- |
| `id>0` 单条 | 不走列表筛选，合理 |
| `EntityTreeController.Search` | 缓存内存 `AutomationFilter.Match`：Match 必须用同一白名单，非法字段 400 |
| `LovController.ListData` | **本号不改** |

## 2. 字段白名单

### 2.1 签名

```csharp
public static Expression TryBuildWhere(
    IEntityFactory fact,
    ViewFilterDto filter,
    Func<String, Boolean>? allowedField = null)
```

- `allowedField == null`：仅兼容自动化内部调用；**列表 / Widget / 树** 必须传入非 null。
- `TryBuildCondition`：`c.Field` 在 `fact.Fields` 找不到 → 沿用 null（无法下推）。
- `allowedField(c.Field)==false` → **抛 `ApiException(400, "筛选条件含未授权字段")`**，不要 return null。

`Match(IEntity, ViewFilterDto, Func<String,Boolean>? allowedField)` 同样：白名单失败抛 400。

### 2.2 白名单构造

调用方：

```csharp
static Func<String, Boolean> AllowSearchOrList(PageInfo page /* 或 FieldCollection */)
{
    var names = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
    foreach (var f in searchFields) names.Add(f.Name);
    foreach (var f in listFields) names.Add(f.Name);
    return n => names.Contains(n);
}
```

`SearchData` / `EntityTreeController` 用当前控制器 `OnGetFields(ViewKinds.Search|List)` 结果，**不要**用客户端传来的列名列表。

`WidgetQueryService`：用目标实体控制器 GetPage 同等 search∪list；拿不到控制器时用实体 `Factory` 的 List/Search 默认 ShowIn（与 GetPage 一致的 FieldCollection），禁止 AllFields。

### 2.3 条件矩阵

| 输入 | TryBuildWhere / 列表 | 调用方 |
| --- | --- | --- |
| `viewFilter` 空 | null，不改 State | 仅 CreateWhere |
| 字段 ∈ 白名单且 op 可编译 | Expression | AND 进 State |
| 字段 ∉ 白名单 | 400 | 不执行查询 |
| 字段不在 Factory.Fields | null（e483） | 内存 Match 当前页（仅此路径） |
| Conditions.Count > 10 | 400 `"筛选条件过多"` | |
| logic=any 且 Count > 5 | 400 `"OR 条件过多"` | |
| JSON 损坏 / 长度>4096 | 已有 400 | 保持 |
| 未知 op | null | e483 内存过滤 |

`logic` 仅 `all`（默认）与 `any`。其它值按 `all`。

## 3. 时序实体时间窗

### 3.1 命中

实体满足任一：

- 表启用 `DataScale` 分表；或
- 类型名/表名匹配 `*Log*`（含 `Log`、`OAuthLog` 等），且存在 `UpdateTime` 或 `CreateTime` 数据字段。

### 3.2 注入

`CubeSetting.FilterWindowDays`：`Int32`，默认 30，范围钳制 0–3650；负数当 0。

条件已含该时间字段（op 为 after/before/eq/gt/gte/lt/lte）→ **不**再注入。

否则 AND `timeField >= UtcNow.Date.AddDays(-days)`（实现用实体时间类型，与现有 dtStart 惯例一致，**本地日期 0 点或 UTC 在单测钉死一种**，推荐：`DateTime.UtcNow.AddDays(-days)`）。

不存在时间字段 → 不注入。

### 3.3 告知前端

| 通道 | 字段 |
| --- | --- |
| GetList / Index JSON 列表 | 响应头 `X-Cube-Filter-Narrowed: {days}d`（例 `30d`） |
| Widget Query | 响应对象增加可选 `filterNarrowed?: string`，无则省略 |
| `FilterWindowDays=0` | 不注入、无头、无 JSON 字段 |

CORS：若 SPA 读自定义头，需 `Access-Control-Expose-Headers: X-Cube-Filter-Narrowed`（仅 WebAPI 已有 CORS 配置处追加，禁止新开跨域策略）。

## 4. startsWith 操作符

| 层 | 符号 | SQL / 匹配 |
| --- | --- | --- |
| JSON / 前端 | `startsWith` / `notStartsWith` | 加入 `ViewFilterOp`、`FILTER_OPS`、`FILTER_OPS_BY_KIND.string`、`FILTER_OP_LABELS` |
| 后端 | `startswith` / `notstartswith`（大小写不敏感） | `fi.StartsWith(val)` / `!fi.StartsWith` → `LIKE 'v%'` |
| 自动化图 | `automationGraph` / `matchesViewFilter` 同构 | 未知 op 失败不静默（已有） |

`contains` 保留。不在本号禁止 contains。

非法值：空字符串 startsWith → 与 eq 空串同一归一（现有 Unwrap）。

## 5. ViewFilter JSON schema（增量）

沿用 e483 `ViewFilterDto`。本号增量：

```json
{
  "logic": "all | any",
  "conditions": [
    { "field": "Name", "op": "startsWith", "value": "张" }
  ]
}
```

- `conditions` 最多 10；`any` 时最多 5。
- 未知字段名：白名单 400；非实体字段 null。
- 旧客户端无 startsWith：忽略不出现即可。

## 5.1 QueriesJson schema（预定义查询）

**写出（本号后）：** `version: 2`

```json
{
  "version": 2,
  "queries": [
    {
      "id": "q_xxx",
      "name": "本部门张姓",
      "q": "张",
      "filter": {
        "logic": "all",
        "conditions": [{ "field": "Name", "op": "startsWith", "value": "张" }]
      }
    }
  ]
}
```

| 字段 | 规则 |
|------|------|
| `id` | 沿用 `generateQueryId()`：`q_` + 时间戳 36 进制 + 4 位随机 |
| `name` | trim，1–50 |
| `q` | 字符串 trim；空则省略或 `""` |
| `filter` | `normalizeFilter`；可无条件（仅关键字方案） |
| 合法条目 | `q` 非空 **或** `filter.conditions.length>0`，否则丢弃 |
| 条数上限 | 沿用今日 store 行为；若无上限则本号不新加 |

**读入兼容：**

| 输入 | 结果 |
|------|------|
| `version===2` 且有 `q`/`filter` | 按上表规范化 |
| `version===1` 或条目含 `params` | `q = String(params.Q ?? params.q ?? "").trim()`；`filter` 空方案；其它键丢弃 |
| 迁完后既无 q 又无条件 | 丢弃该条 |
| JSON 损坏 / version 其它 | 空列表（与今日 parseQueriesWire 一致） |

`canSave`：当前 Q trim 非空或已应用条件数>0。

应用：`searchForm.Q = saved.q`；`viewFilter = saved.filter`；`activeQueryId`；`loadData()`。不写 ViewsJson。

脏：`q` 或 `normalizeFilter(viewFilter)` 与快照不等。

## 6. 前端收口

### 6.1 删除

| 文件 | 动作 |
| --- | --- |
| `web/src/features/search/SearchDrawer.vue` | 删除 |
| `web/src/features/search/useSearchDrawer.ts` | 删除 |
| `SearchFieldInput.vue` | grep 后：仅抽屉用则删；构建器仍用则留 |

**保留并改造** `QueryComboButton.vue` + `useQueryComboButton.ts`：从抽屉标题挪到工具栏 Q 后，扩成三键组（查询 / 自定义 / ▾），菜单见 ui/。

`InsightPanel` 禁止把搜索表单加回去。

### 6.2 改造

| 文件 | 改 | 不动 |
| --- | --- | --- |
| `DefaultList.vue` | 查询簇；去掉 SearchDrawer、「搜索」「筛选」按钮；FilterBuilder 挂到「自定义」 | 分组/填色/高级；薄 script |
| `useDefaultList.ts` / `useListQuery.ts` | 去掉 drawer；保存/应用预定义改为 q+filter；Q 在工具栏 | |
| `useListViews.ts` | 筛选文案→查询处仅用户可见字；`onFilterApply` 仍写会话+可选视图 | |
| `FilterBuilderPopover.vue` | 标题「查询」 | 条件行交互 |
| `stores/viewProfile.ts` | `saveQueryAs` payload 改为 q+filter | dashboardJson 等 |
| `core/utils/viewProfile.ts` | v2 parse/serialize + v1 兼容；`SavedQuery` 类型 | NamedView.filter |
| `listContext.ts` | `filterFields` 对齐 search∪list | |
| `filterBuilder.ts` | startsWith + 标签 | |
| `matchesViewFilter` | 同构 startsWith | |
| 请求 | 读时间窗头 | 不绑抽屉 |

Q：不 debounce 自动搜；Enter 与「查询」相同。空 Q 不传或传空（与今日 SearchDrawer 一致）。

### 6.3 apps / Section

`ListSearchBar` = 可覆写查询簇。grep `SearchDrawer` 于 `web/src` 与 `apps` 为 0。`QueryComboButton` **必须仍存在**。

### 6.4 规格与界面

见 `ui/information-architecture.md`。

## 7. 文件级改动地图（后端）

| 文件 | 改 | 不动 |
| --- | --- | --- |
| `NewLife.Cube/Automation/AutomationFilter.cs` | 白名单、400 上限、startsWith、Match 重载 | ParseViewFilter 4KB；未知 op → null |
| `NewLife.Cube/Common/ReadOnlyEntityController2.cs` | 传入白名单；时间窗；写响应头 | CreateWhere 签名；viewFilter AND 顺序 |
| `NewLife.Cube/Common/EntityTreeController.cs` | Match 白名单 + 时间窗（若走内存列表） | 缓存策略 |
| `NewLife.CubeNC/Common/EntityTreeController.cs` | **同步**（非 Link 副本） | |
| `NewLife.Cube/Widgets/WidgetQueryService.cs` | TryBuildWhere 传白名单；时间窗；`filterNarrowed` | Query 鉴权/聚合语义（0e9e） |
| `NewLife.Cube/Setting.cs` | `FilterWindowDays` | 其它配置 |
| `NewLife.Cube.Tests/*AutomationFilter*`（新建或扩现有 Osc260819） | 本号用例 | |
| 18 个 `*Controller.Search` | **不改** | |

CubeNC 已 `Link Automation\*.cs` 与 `ReadOnlyEntityController2.cs`：改一处即可。EntityTree **两份**都要改。

## 8. 核心文档影响

| 文档路径 | 影响类型 | 说明 |
|----------|----------|------|
| `NewLife.Cube.ArcoVue/web/README.md` | 修改 | 查询簇 + 预定义保存 Q 与条件；无抽屉 |
| `NewLife.Cube.ArcoVue/web/docs/字段组件规范.md` | 修改 | 搜索抽屉行改为自定义查询构建器 |
| `Doc/Api/内置前端皮肤.md` | 无或一句 | 仅当仍写 SearchDrawer |
| `Doc/功能清单.md` | 修改 | SPA-7 查询收口；QueriesJson 仍用 |
| `Doc/Api/核心接口架构.md` | 修改 | viewFilter 白名单 400；响应头；QueriesJson v2 不新增 HTTP 路径 |
| `ArcoVue企业中后台迁移方案.md` | 修改 | §8.5.4：退役抽屉，**保留**预定义；§1.3 对齐 |
| `竞品分析报告.md` | 修改 | 查询单轨 + 预定义方案 |

## 9. 测试设计

| 项 | 期望 | 落点 |
| --- | --- | --- |
| 白名单外 Salary | 400，无列表体 | AutomationFilterTests |
| 白名单 Name contains | 有 Expression | 同上 |
| 11 条条件 | 400 | 同上 |
| any 6 条 | 400 | 同上 |
| startsWith | SQL 含 `LIKE 'v%'` 或表达式等价 | 同上 |
| Log 无时间条件 | where 含 UpdateTime/CreateTime >= 窗口 | 同上 + SearchData 测头 |
| FilterWindowDays=0 | 无窗口谓词 | |
| UserController.Search + viewFilter Name | total/行被服务端过滤 | Cube.Tests 集成 |
| Department 同上 | 同上 | |
| filterBuilder startsWith | 字符串 kind 含该 op；matchesViewFilter 同构 | Vitest |
| QueriesJson v2 | 保存 q+filter；v1 params 只迁 Q；空方案丢弃 | viewProfile.spec |
| 应用预定义 | 回填 Q 与 filter 后请求同时带两者 | useListQuery / store spec |
| 生产 src grep | 无 `SearchDrawer`；**有** `QueryComboButton` | 手工/单测 |
| 构建 | 0 error | CI |

## 10. 与 OSC-2608273d95

本号可先落地。SearchData 合并顺序保持 e483：CreateWhere（含未来 GetFilter）**先**，viewFilter **后**。本号不得把 viewFilter OR 进权限。
