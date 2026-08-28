# OSC-2608280e9e Verify

> 状态：**通过**（Validating，待复盘）  
> 时间：2026-08-28T19:55+08:00  
> 触发：请严格按照本项目 OpenSpec 规范，验收和复盘 0e9e 变更。  
> 编排：implementation-audit → code-review → doc-sync（含 V1 补齐复验）

## 验收阶段记录

### 会话小任务补录

- P3.11 Widget ops / 未联动角标；P3.12 模板回落；P6 分享 embed；P6.5 sfcThin；V1.1–V1.7 验收缺口补齐 — 均已入 `tasks.md` 并勾选。

### implementation-audit（二次）

- 愿景 1 ✅ DashboardJson 个人>模板  
- 愿景 2 ✅ Query 鉴权聚合 + **租户 Where（V1.1）** + 时间分桶（V1.2）  
- 愿景 3 ✅ Host / 筛选构建器联动 / 空槽添加入口（V1.4）；洞察槽 **暂缓 miniKanban**（文档已对齐 V1.6）  
- 愿景 4 ✅ registerWidget / legacy 只读 / **named Data 接线（V1.5）**  
- 冻结项 ✅ 守住  
- 无新 P0/P1；P2（卡片复制、CubeNC PUT 无 TryNormalize）仅记录不补齐（用户先前决策）

### code-review（二次抽查）

- 🔴 无  
- 🟡 CubeNC WebAPI 与 Razor 双轨 PUT 校验差异（P2）  
- 🟢 `rows: null` 序列化可接受  

### doc-sync（二次）

- DASH-1 / §8.5.3 / web README / proposal 决策 3 / design 锁定表：已写「洞察槽暂缓 miniKanban」  
- 分享 embed 已在 DASH-1 / README / §8.5.3 简述  

### 愿景对照结论

第一轮缺口已按用户决策补齐；无新阻断缺口。冒烟未全量浏览器复点，以代码+单测+会话联调为证。

## 冻结（全程）

- [x] `GetPage` / `Search` / `SearchData` / `CreateWhere` / `GetChartData` / `OnGetChartData` 未改签名  
- [x] 未引用 CubeNC `IWidget`；CubeNC 无 SPA `/Cube/Widget` Query API  
- [x] 未改 Cube.Vue `ListChartDialog`  
- [x] 无整页画布、无用户脚本、无迷你表格、无首页 `workspace.widgets`  
- [x] `Automation/Entities` 默认 permission 仍为 update  

## P1 后端 / P2–P6 / V1（摘要）

- [x] DashboardJson + Sources/Catalog/Query/Data + 校验  
- [x] 租户 Where + timeField 分桶 + list 投影收窄  
- [x] api-core widget + ArcoVue Host/三渲染器（洞察槽无看板）+ legacy  
- [x] 文档本号 + 看板暂缓说明  
- [x] 分享 embed + LoadToken 短令牌 + sfcThin  

## 冒烟

- [x] S.1 指标卡持久化 + 筛选构建器联动（代码：`useWidgetQuery` watch hostFilter；store PUT dashboardJson）  
- [x] S.2 Sources=Detail；非法 JSON PUT 400（XUnit + ConfigDrawer）  
- [x] S.3 miniKanban 洞察槽禁用（文档暂缓；compact 代码保留给工作台）  
- [x] S.4 legacy 合成仅 dashboard 未配置时（`useInsightPanel` + Vitest）  
- [x] S.5 空槽可编辑显示「添加部件」；满 12 禁用添加（V1.4）  
- [x] S.6 Cube.Vue GetChartData 未改（回归：无本号 diff）  
- [x] S.7 分享页列表+仪表盘（会话：LoadToken 修复后 200；embed 无自动化菜单；视口可滚）  

## 测试与构建门禁

| 命令 | 结果 |
|------|------|
| `dotnet test … ~Osc260828` | **10 passed** |
| `dotnet build NewLife.Cube` | 0 error |
| `dotnet build NewLife.CubeNC` | 0 error |
| `pnpm --filter @cube/api-core test/build` | 51 pass / build OK |
| `pnpm --filter @cube/arco-vue test` | **713 passed** |
| `pnpm --filter @cube/arco-vue build` | 0 error（验收轮已跑） |

## 风险（仅记录）

- 冒烟未在本轮二次验收中全量浏览器复点 S.1–S.6  
- P2：卡片「复制」、CubeNC PUT 无 `TryNormalize`  
- Oracle 等方言 timeField 仍 400（符合 design）  
