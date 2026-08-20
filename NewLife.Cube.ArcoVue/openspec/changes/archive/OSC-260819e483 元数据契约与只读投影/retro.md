# OSC-260819e483 Retro

> 复盘 2026-08-21 | 状态 Done  
> 验收决策：仅记录不补齐（S.1–S.7 浏览器冒烟与 design 已知限制）

## 结果摘要

| 维度 | 结果 |
|------|------|
| AC 通过率 | P1–P5 + T1–T5 自动化 AC 全过；S.1–S.7 仅记录不补齐 |
| 三步编排 | 实现审计 ✅ → 代码审查 ✅ → 文档同步 ✅ |
| 自动化门禁 | XUnit 8+21 · Vitest 593 · api-core 36+51 · Cube/CubeNC/arco-vue build 0 error |
| 代码质量 | 冻结约束零破坏；复用 AutomationFilter / NotificationRecord / 现有 Remark 文法 |
| 工期 | P1–P5 主实现 + 验收补录 T1–T5 + Validating 放行归档 |
| 手工冒烟 | S.1–S.7 归人工残余，不阻断 Done |

## 实际完成范围

- **P1**：两栈 `PrepareFieldsForApi` Required；WebAPI `Index.Stat as TEntity`；写请求 `X-Cube-Field-Validation` 选择校验。
- **P2**：`viewFilter` → `AutomationFilter.TryBuildWhere` 下推（含 `notcontains`）；EntityTree 内存 Match；单列 sort。
- **P3**：WebAPI `PatchFields` / `BatchUpdateFields`（含 `Fields[]`≤50）；`onlyFields` 局部校验；CubeNC 无 Action。
- **P4**：`logRemarkDiff` 解析 `Field=old -> new`；评论 `mentionUserIds` → `NotificationRecord`。
- **P5**：`insight.chartOption` 存 ViewProfile；无 projections / autoChart。
- **T1–T5**：批量多字段 UI、typeName 控件、InsightPanel 瘦身/默认隐藏、批量确定修复。
- **文档**：功能清单 DATA-7 / DATA-10a / SPA-7 / SPA-18 / SPA-19；核心接口架构；竞品 §8.6；迁移方案 §8.2；web README。

## 验证证据

| 项 | 结果 | 日期 |
|---|---|---|
| XUnitTest `~Osc260819` | 8/8 | 2026-08-21 |
| NewLife.Cube.Tests `~Osc260819` | 21/21 | 2026-08-21 |
| Cube / CubeNC build | 0 error | 2026-08-21 |
| arco-vue Vitest | 593/593 | 2026-08-21 |
| arco-vue / api-core build+test | 通过 | 2026-08-21 |

## 做得好的

1. **冻结约束零破坏**：未改 Fill / EnableFieldValidation 默认 / Search / GetPage / Insert / Update / OnUpdate / AddComment / WriteLog。
2. **复用现码**：viewFilter 走 AutomationFilter；提及走 NotificationRecord；diff 只解析现有 Remark。
3. **双栈接线清晰**：共享 `SearchData` 一处；PrepareFieldsForApi / EntityTree / 评论 POST 各改两份；PATCH 仅 WebAPI partial。
4. **阶段门禁可重复跑**：验收重跑 filter 全过。
5. **执行期真实坑有防御**：WhereBuilder 放弃下推不 500；Request==null；`onlyFields` 局部校验。

## 待改进

1. **写路径应在阶段内冒烟**：批量「确定后不成功」、多字段、Insight 瘦身拖到验收补录——阶段末应用真实表单点一次。
2. **浏览器冒烟勿整包留到验收**：S.1–S.7 集中留后，只能「仅记录」放行。
3. **tasks 与 verify 冒烟双份勾选**：P3.6/P4.6/P5.5 与 S.1–S.7 易不一致；宜只保留 verify 一处。
4. **无关 WIP 干扰门禁计数**：曾有 FormatPopover 未跟踪导致 Vitest 外部失败（已由 03c0 修复）。

## 偏差

- Validating → Implementing（T1–T5 补齐）再回 Validating：属验收缺口补齐，非范围蠕变。
- 用户「仅记录不补齐」放行浏览器冒烟与 design 已知限制。

## 遗留与后续

- S.1–S.7 人工冒烟可在联调时顺手勾掉，不另开 OSC。
- 字段 ACL（竞品 §8.6-5）独立号。
- chunk 体积为项目级问题。
- 必填语义三处同源（PrepareFieldsForApi / 请求头 / `isFieldRequired`），后续改动须同步三套测试。

## 过程备注

- 用户要求 §8.6 第 1/2/3/4/6 项合并一号；不含第 5 项。
- 曾误拆 `8ccf`/`11c1`/`45c2`，合并前删除且无代码。
- Draft 修订取消 LogProvider 装饰、EntityListFilter、sorts、projections、MentionsJson、autoChart；图表进 ViewProfile。
