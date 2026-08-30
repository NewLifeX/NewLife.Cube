# OSC-260830a1b2 Design

## 技术方案

### 1. 前置认知（决定整个设计）

**XCode State 透明通道**（2026-08-29 源码实测）：

```csharp
// XCode Entity.cs L1137-1144（FindAll(Expression, PageParameter)）
if (page.State is Expression exp)
    where &= exp;
else if (page.State is WhereBuilder builder)
{
    builder.Factory ??= Meta.Factory;
    where &= builder.GetExpression();
}
```

`SearchData` 已把 `p.State = CreateWhere() & viewExp`。因此 **18 个重写 `Search` 的控制器无需任何改动**——viewFilter 与 DataPermission 走同一通道。这颠覆了竞品报告早期「需 `ApplyRequestFilter` 让重写方并入」的结论（已回写 §8.2(1) 划线降级）。

### 2. 字段白名单

```csharp
// AutomationFilter.cs 签名改造
public static Expression TryBuildWhere(IEntityFactory fact, ViewFilterDto filter,
    Func<String, Boolean>? allowedField = null)
```

- `allowedField` 为字段白名单谓词：`name => searchFields ∪ listFields` 包含该字段名。
- `TryBuildCondition` 校验 `allowedField == null || allowedField(c.Field)`；不通过返回 `null` → 整段放弃下推（沿用"任一条件无法下推则整段放弃"语义）。
- 调用方（`ReadOnlyEntityController2.SearchData` / `EntityTreeController.Search` / `WidgetQueryService`）从 `GetPage` 的 `search`/`list` 分区构造白名单集合传入。

### 3. 时序实体时间窗（性能）

```csharp
// 命中规则：实体含 DataScale 分表 或 继承 LogEntity 且表名匹配 *Log*
// 注入：条件无时间字段时，AND 注入 UpdateTime/CreateTime >= now-30d
// 响应头：X-Cube-Filter-Narrowed: 30d（前端展示「已自动限定近 30 天」）
```

- 时间字段名取 `updateTime` 优先，其次 `createTime`（字段存在才注入）。
- 窗口天数走 `CubeSetting`（默认 30，`0` 关闭）。
- 关闭时不注入、不设响应头（保底兼容，大表风险由用户承担）。

### 4. startsWith 操作符

- 后端 `TryBuildCondition` 增加 `startswith` → `fi.StartsWith(val)`（XCode `FieldItem` 支持，编译为 `LIKE 'v%'`）。
- 前端 `searchFilters.ts` / `filterBuilder.ts` 增加 `startsWith` 与 `notStartsWith`；`matchesViewFilter` 同构同步。
- `contains` 保留，但 `Logic=any` 且含 contains 时视为「代价高」，不强制禁（硬件大表性能依赖策略 3 + 时间窗）。

### 5. 条件复杂度上限

- 条件数 `Conditions.Count > 10` → 整段放弃下推（沿用现有"任一项不可下推则放弃"）。
- `logic=any` 的 OR 项数 `> 5` → 放弃下推（OR 使 index merge 不稳定）。
- 现有 4KB 长度限制保留（`ParseViewFilter`）。

### 6. 前端收口

- **删除**：`features/search/SearchDrawer.vue`、其 composable、`QueriesJson` 相关（`viewProfile.ts` parse/serialize、store 方法、`viewProfile.spec.ts` 对应块）。
- **保留**：工具栏 `Q` 输入框（下推 `Q` 参数走既有 `SearchWhereByKeys`，BaseController 默认行为）。
- **改造**：`DefaultList.vue` 工具栏移除「搜索」抽屉入口；保留筛选/分组/填色/自动化/高级；`SearchDrawer` 移除后 `InsightPanel` 不再有搜索区（已是 WidgetHost）。
- `listContext.ts` 的 `filterFields` 继续用可见列 ∪ 人员字段（白名单前端的候选）——与后端白名单交集，前端只展示后端会接受的字段。

### 7. 文档影响

| 文档 | 影响 |
|------|------|
| 迁移方案 §8.5.4 | 标记本号实施；§10.4 #14 标注已解决 |
| 竞品报告 §6.2 #5 / §8.2(1) / §9 | #5 改「筛选服务端化已收口」；§8.2(1) 补白名单落实；§9 路线第 1 条标注已完成 |
| web README | 「通用查询与预定义查询」段更新为筛选单轨描述 |

## 规格与界面

- ui/：无新增界面（筛选构建器/操作符已存在）。Q 搜索框为既有工具栏控件，仅移除 SearchDrawer 入口。

## 测试设计

| 项 | 方法 | 落点 |
|----|------|------|
| 白名单拒绝 | 构造含白名单外字段的 viewFilter，断言 `TryBuildWhere` 返回 null | `AutomationFilterTests` |
| 时间窗注入 | Log 实体无时间条件，断言 SQL 含 UpdateTime 谓词 | `AutomationFilterTests` |
| startsWith 编译 | 断言 `LIKE 'v%'` 生成 | `AutomationFilterTests` |
| 条件数上限 | >10 条件 / any-OR>5 断言放弃下推 | `AutomationFilterTests` |
| 透明下推回归 | 调 `UserController.Search` 伪造 viewFilter，断言结果被服务端过滤且 total 正确 | `XUnitTest`（User/Department） |
| 前端操作符 | filterBuilder 含 startsWith/notStartsWith；`matchesViewFilter` 同构 | `filterBuilder.spec.ts`、`searchFilters.spec.ts` |
| 无残留引用 | 全仓 grep `SearchDrawer`/`QueriesJson` 为 0 | `sfcThin.spec.ts` 或独立 grep 单测 |
| 构建 | `dotnet build NewLife.Cube` + `pnpm build` 无错误 | CI |
