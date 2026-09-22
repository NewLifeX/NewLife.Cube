# Verify

> 验收在 `Validating` 阶段（checklist passed）由 openspec-verify 填写；实现期手工冒烟清单见下，供 openspec-verify 复用。

## 验收记录（2026-09-22，openspec-verify）

### AC 对照（正常 / 边界 / 旧数据）

| # | AC | 结果 | 证据 |
| --- | --- | --- | --- |
| 1 | 洞察槽/工作台实体部件可配置查询条件，保存进 `query.extraFilter` 并参与取数 | ✅ | `WidgetConfigDrawer.vue` 查询条件区（提交快照）；`useWidgetConfigDrawer.save()` 合并保留；XUnit `StaticExtraFilter` count/list；api-core/widget.spec round-trip |
| 2 | 条件编辑器整体复用列表页同一组件 | ✅ | `FilterBuilderPopover` + `useFilterBuilderPopover` 复用（提交 diff）；DefaultList 调用零改动；widget 特性无第二套条件行 UI |
| 3 | 跨实体 `$host`：宿主有值 → 过滤且无未联动角标 | ✅ | XUnit `HostRefResolved`；`unlinkedAfterQuery` 以 `hostFilterApplied` 定角标（widget.spec 3 例） |
| 4 | 跨实体 `$host`：宿主缺值 → 条件跳过 + `hostFilterApplied=false` + 未联动角标 | ✅ | XUnit `HostRefMissing`；`ResolveHostRefs` 丢弃条件；前端 `unlinkedAfterQuery` |
| 5 | 工作台三域保存 `$host` → 400；前端无宿主入口 | ✅ | XUnit `WorkbenchRejectsHostRef`；`WorkbenchController` 各 PUT 传 null；workbench `hostEditorFields` 为空 |
| 6 | 越权/未知字段保存即 400 | ✅ | XUnit `UnknownFieldRejected`/`IllegalOpRejected`；候选与后端白名单一致（G1 已修，下拉不再出现超集字段；手改 JSON 仍被 400 拒绝） |
| 7 | 既有无 `extraFilter` 配置保存/读取行为不变 | ✅ | XUnit `OldOverloadCompat`；归一不强写键 |
| 8 | 编辑存量部件不再丢 `extraFilter` | ✅ | `save()` 合并保留；`buildQueryBody` extraFilter 不变式 + `normalizeFilter` $host 保留用例（G3 已补） |
| 9 | 协议零扩展（`query.extraFilter` 唯一持久化载体） | ✅ | 提交未新增请求/持久化字段；`parseDashboardJson/serializeDashboardJson` 原样透传 |
| 10 | 非 `$host` 对象值保存/查询两端 400 | ✅ | XUnit `HostRefIllegalObjectRejected`；畸形形状（非对象/条件元素非对象）保存即 400（G2 已修） |

### 固定编排三步摘要

1. **实现审计**：P1–P7 主体与 design 一致，无 P0；缺口见 G1–G5（P1 级 2 项、P2 级若干）。
2. **代码审查**：无严重问题；安全基座扎实（白名单同源、参数化下推、fail-closed）；2 项「重要」发现 → G1（候选超集）、G2（结构校验放行路径）；轻微项 G4–G7。
3. **文档同步**：4 处登记全部落地且未被后续提交改写；DASH-4 三维状态与 13 用例数字吻合；表述偏差 → G1/G5。

### 会话小任务补录核对

自 `Accepted`（2026-09-03）至今，会话内无计划外独立事项；`status.md` 实现期 note 与 tasks.md 记录一致，无需补录。

### 测试与构建门禁（复验 2026-09-22，G1–G5 补齐后重跑）

| 门禁 | 命令 | 结果 |
| --- | --- | --- |
| 后端构建 | `dotnet build NewLife.Cube/NewLife.Cube.csproj` | **0 error**（多 TFM，0 警告） |
| XUnit 新增+回归 | `dotnet test --filter "FullyQualifiedName~Osc260903WidgetQueryTests\|FullyQualifiedName~Osc260828"` | **44 通过 / 0 失败**（e2a4 16 + 回归 28） |
| api-core Vitest | `npm.cmd --prefix packages/api-core test` | **54 通过 / 0 失败**（含 npm test 附带 node 用例） |
| arco-vue Vitest（全量） | `vitest run --config vitest.config.ts` | **889 通过 / 0 失败**（93 文件） |
| 前端构建 | `npm run build`（vue-tsc -b && vite build） | **退出码 0**，`✓ built`（wwwroot 产物已重建） |

首轮重跑（补齐前，2026-09-22 早）：XUnit 41 / api-core 53 / arco-vue 884 / 双构建 0 error；两轮差异为本轮新增用例。

### 缺口处置（用户 2026-09-22 决策「全部补齐」→ 已完成 P8）

| # | 级别 | 缺口 | 处置结果 |
| --- | --- | --- | --- |
| G1 | P1 | 前端条件候选含 `AutomationMeta` 全字段（超集） | ✅ `loadEntityFilterFields` 改为 AutomationMeta 仅富化、不扩充（按 search∪list 名字集过滤）；新增 `listFieldMeta.filter.spec.ts` 用例 |
| G2 | P1 | `extraFilter` 畸形形状静默放行 | ✅ 非对象 / conditions 元素非对象 → 保存即 400；纯静态条件不再强制解析宿主实体（hostTypePath 归一，仅存在 $host 时解析）；XUnit `DashboardJson_MalformedShapeRejected` |
| G3 | P2 | 测试构成缺口 | ✅ XUnit 补 group 模式、group+$host、$host 引用时间字段（等值解析生效 / after 不解析跳过）3 例（13→16）；前端补 `buildQueryBody` extraFilter 不变式、`normalizeFilter` $host 保留用例 |
| G4 | P2 | `$host` 空串识别边界不一致 | ✅ `isHostRefValue` 拒绝空串/空白串，`hostRefField` 去空白（与后端一致）；api-core spec 同步 |
| G5 | P2 | 文档限定缺失 | ✅ `web/README.md` 限定「工作台无宿主引用」；`功能清单.md` DASH-4 / `核心接口架构.md` / `前端对接指南.md` 补「条件跳过」语义、复杂度数字（≤10/any≤5）、候选来源与迷你图表「数据范围」标签 |

### 目标愿景对照（10 项 AC 全过）

- 愿景「同一源实体、不同部件各看各的子集」达成：配置抽屉 → `query.extraFilter` → 后端 AND（租户/行权之后）全链路可用，条件编辑器与列表页同源复用。
- 缺口补齐后无遗留 P0/P1；P2 全部关闭；剩余风险（CubeNC 保存路径校验缺失、手工冒烟待宿主）见「风险」。

### 结论

**通过（checklist: passed）**——AC 10 项全过，三步编排无 P0/P1 遗留，复验门禁全绿。可执行 `复盘 OSC-260903e2a4`。

### 风险

- CubeNC 兼容层 ViewProfile 保存路径不走 `DashboardJson.TryNormalize`（既有差异，非本号引入），该栈可写入非法 `$host` 配置；已记录，未纳入本号范围。
- 手工冒烟 10 项（下述清单）仍待 CubeDemo 宿主人工执行；验收以单测+构建+静态审查为准。
- 临时工作树 `%TEMP%\osce2a4-wt`（快照 3ca4d31e）在验收完成后移除。

## 手工冒烟清单（实现后执行）

- [ ] 实体列表洞察槽：给当前实体添加 metricCard（计数/求和）并配置静态条件 → 卡片数值随条件变化
- [ ] 实体列表洞察槽：miniChart 分组/时间序列配静态条件 → 分组项/序列被条件过滤
- [ ] 跨实体部件：配置 `$host` 宿主引用 → 宿主筛选该字段时数据收敛且无「未联动」角标
- [ ] 跨实体部件：宿主未筛该字段 → 「未联动」角标（与 linkFilter 缺值一致）
- [ ] 首页工作台 `/home`：实体部件配静态条件生效；条件编辑器无宿主引用入口
- [ ] 角色模板 / 命名工作台：保存含 `$host` 配置被 400 拒绝并有提示
- [ ] 编辑存量（含历史保存）实体部件：`extraFilter` 不再丢失
- [ ] 越权/未知字段：保存即 400（候选下拉本不应出现；手改 JSON 验证）
- [ ] 条件编辑器与列表页「查询」组件为同一套 UI 组件（无第二套漂移）
- [ ] 既有无 extraFilter 的部件保存/读取与今日一致
