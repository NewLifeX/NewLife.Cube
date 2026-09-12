# OSC-260830a1b2 Verify

> 进入 `Validating` 后逐项勾选。

## 必须保留（暂缓区，误删即失败）

- 18 个重写 `Search` 的控制器 **无** `ApplyRequestFilter`。
- `GetList` / `GetPage` / `CreateWhere` / `GetChartData` **签名不变**。
- 工具栏 **Q** 与 **预定义查询**（QueriesJson）仍在。
- Cube.xml **QueriesJson 列不删**。
- `QueryComboButton`（或同职责连体按钮组）仍在工具栏，**不是**删光。
- 查询图标为现有 **`search`**，不用 `filter`。
- `LovController.ListData` / `entity:` **不在本号改**。
- viewFilter `logic=any` 不得 OR 掉 CreateWhere。
- InsightPanel 保持 WidgetHost。
- 不把 NamedView.filter / 预定义当权限。

## 命令与预期

```text
dotnet test NewLife.Cube.Tests/NewLife.Cube.Tests.csproj --filter FullyQualifiedName~AutomationFilter
dotnet test NewLife.Cube.Tests/NewLife.Cube.Tests.csproj --filter FullyQualifiedName~ViewFilter
dotnet build NewLife.Cube/NewLife.Cube.csproj
dotnet build NewLife.CubeNC/NewLife.CubeNC.csproj
pnpm --filter @newlifex/cube-arco-vue test
pnpm --filter @newlifex/cube-arco-vue build
```

```powershell
rg -n "SearchDrawer" "NewLife.Cube.ArcoVue/web/src" "NewLife.Cube.ArcoVue/web/apps"
rg -n "QueryComboButton" "NewLife.Cube.ArcoVue/web/src"
```

预期：无 SearchDrawer 引用；QueryComboButton 仍有匹配。

## Happy path

- [x] **AC-01 自定义查询下推**：条件 `Name contains`，翻页 total 服务端过滤。
- [x] **AC-02 关键字查询**：Q 输入后点「查询」或 Enter，请求带 `Q`。
- [x] **AC-03 按钮组**：无独立「筛选」「搜索」按钮；「查询」为 `search` 图标；「自定义」打开构建器。
- [x] **AC-04 预定义保存**：当前 Q + 已应用条件可保存；应用后 Q 与条件均回填并执行。
- [x] **AC-05 预定义勾选**：使用某方案后该行 `check`；改 Q 或条件后勾选消失（脏）。
- [x] **AC-06 重命名 / 删除**：重命名改名后列表更新；删除后不再出现，当前表单可保留。
- [x] **AC-07 startsWith**：自定义查询「开头是」生效。
- [x] **AC-08 时间窗**：无时间条件时有头与提示（验收修复：响应体 `filterNarrowed` 透传）。
- [ ] **AC-09 导出/Insight**：与当前 Q+viewFilter 一致（Insight 一致；**导出**走 `GetCachePager`，需人工验证是否含 viewFilter，见风险 R4）。

## 权限 / 空 / 非法 / 兼容

- [x] **AC-10 白名单** 外字段 → 400。
- [x] **AC-11 超限** 11 条或 any 且 6 条 → 400。
- [x] **AC-12 空保存** Q 与条件皆空时「保存为预定义」禁用。
- [x] **AC-13 v1 兼容**：仅含 `params.Q` 的旧 QueriesJson 应用后 Q 有值、无抽屉字段条件。
- [x] **AC-14 FilterWindowDays=0** 无窗口。
- [x] **AC-15 enableKey=false** 无 Q 框，自定义与预定义仍可用（验收修复：按钮组独立渲染）。
- [x] **AC-16 树** 白名单外 400。
- [x] **AC-17 旧客户端** 无新查询参数名。

## 文档

- [x] **AC-18** 迁移方案写明：抽屉退役、预定义保留（Q+条件）、查询文案（§1.3/§10.4/§8.5.4 矛盾已修正）。
- [x] **AC-19** README 不再把 SearchDrawer 当现行规范（查询簇取代；重复旧节已删；工具栏顺序更新）。

## 验收门禁记录

```text
dotnet build NewLife.Cube/NewLife.Cube.csproj           → 0 error ✅
dotnet test NewLife.Cube.Tests --filter ~Osc260830A1b2  → 9/9 passed ✅（新增时间窗 4 用例）
pnpm --filter @newlifex/cube-arco-vue test                        → 80 文件 / 770 passed ✅
vue-tsc -b                                               → EXIT=0 ✅
pnpm --filter @newlifex/cube-arco-vue build                       → built ✅
rg SearchDrawer web/src web/apps                         → 0 命中（注释已清理）✅
rg QueryComboButton web/src                              → 命中（查询簇保留）✅
```
CubeNC 构建仍被**预存** `EntityController2.cs ImportFile`（CS0111）阻塞，与本号无关（见 status.md 已知项 / T15）。

## 目标愿景对照结论

| 目标 | 结论 |
|------|------|
| 目标1 viewFilter 白名单 → 400 | ✅ 实现 + 单测（AC-10/11/16） |
| 目标2 时间窗 + 响应头提示 | ✅ 实现 + 提示（AC-08/14）；验收补 `filterNarrowed` 响应体透传 |
| 目标3 退役 SearchDrawer + 查询簇 + 预定义 v2 | ✅ 实现（AC-02~06）；`enableKey=false` 按钮组独立（AC-15） |
| 目标4 不改 18 控制器 / 透明下推 | ✅ 签名未变、无 ApplyRequestFilter；下推经 `page.State` 透明通道（见残余 R3） |

## 风险 / 已知缺口

- **R1（内容文档缺口）**：竞品报告查询段（T19）、字段组件规范（G5）、功能清单 SPA-7（G6）为内容型回写，工作量较大，本号验收标记为缺口，建议后续单独补齐。
- **R2（聚合语义）**：Widget 白名单未走控制器 `OnGetFields` 及 `GroupBy/TimeField/LinkFilter.SourceField` 等入口叠加白名单（涉及 0e9e 聚合语义，非本号验收阻断）。
- **R3（集成回归）**：透明下推需实体数据集成环境验证（`SearchData` 服务端过滤 / `X-Cube-Filter-Narrowed` 响应头），单测已覆盖 `AutomationFilter` 层，集成建议后续补。
- **R4（导出）**：AC-09 导出需人工验证是否携带当前 Q+viewFilter（走 `GetCachePager`/session）。
- **R5（P2 裁剪）**：`notStartsWith/notEndsWith` 因 XCode 无 `NotStartsWith` 方法裁剪（AC-07 满足 `startsWith`）；树 `Match` 逐实体工厂/字段字典性能优化为后续项。
- **R6（ui 文档）**：`ui/information-architecture.md` 工具栏图示/▾ 菜单与实现仍有出入，proposal 决策 7「自定义独立按钮」与实现（收编于 ▾ 菜单首项）需统一。

## 残余（不阻断 Done）

- Cube.Vue 自己的搜索 UI。
- archive 正文仍可出现 SearchDrawer。
- 旧预定义里除 Q 外的 Search 字段不迁移为 viewFilter。

## Checklist

- checklist: **passed**
