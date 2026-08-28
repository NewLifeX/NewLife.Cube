# OSC-2608280e9e Retro

> 复盘 2026-08-28 | 状态 Done  
> 验收决策：V1 补齐后二次验收通过；P2（卡片复制、CubeNC PUT 无 TryNormalize、冒烟未全量浏览器复点）仅记录

## 结果摘要

| 维度 | 结果 |
|------|------|
| AC 通过率 | 冻结 5/5；愿景 4/4（洞察槽看板按决策暂缓并文档对齐）；冒烟 S.1–S.7 以代码/单测/会话证据勾选 |
| 三步编排 | 实现审计 ✅ → 代码审查（🔴 无）→ 文档同步 ✅ |
| 自动化门禁 | Osc260828 **10**；api-core **51**；arco-vue **713**；Cube/CubeNC/arco build 0 error |
| 缺口 | 无 P0/P1；P2 记 verify |

## 范围回顾

| 维度 | 计划 | 实际 |
|------|------|------|
| 协议 | WidgetInstance + DashboardJson + Query/Sources/Catalog/Data | 落地；租户 Where / 时间分桶验收期补齐 |
| 渲染器 | metricCard / miniChart / miniKanban + registerWidget | 前两者洞察槽交付；**miniKanban 洞察槽暂缓**（工作台另号） |
| 旧 insight | 只读迁移，不再主写 NamedView.insight | ✅ 合成 + 禁 PUT legacyChart |
| 工作台 | 本期只做底座 | ✅；另衍生 **视图分享 embed**（P6） |
| 分享 | 计划外会话衍生 | Share 短令牌 + LoadToken 修 JWT 过滤 bug + EmbedLayout 视口滚动 |

## 实际完成范围

- 后端：`DashboardJson`、`WidgetController`、`WidgetQueryService`、`ICubeWidget` 扫描；Share API；`LoadToken` 不透明串。
- api-core：`dashboardJson` / `createWidgetApi` / parse·serialize。
- ArcoVue：`features/widget/*`、InsightPanel Host、配置器、legacy 合成、分享弹层、embed 壳。
- 文档：§8.5.3、DASH-1、核心接口、对接指南、web README、竞品行。
- 验收补齐 V1：租户 AND、方言时间分桶、list 投影、空槽添加、named Data、文档看板暂缓。

## 做得好的

1. **协议与列表解耦**：Query 不改 GetPage/GetChartData，避免假聚合与签名漂移。
2. **验收强制缺口决策**：第一轮查出 P0 租户洞后回 Implementing，避免带病 Done。
3. **分享短令牌踩坑被门禁兜住**：sfcThin 拦下 `.vue` 内 `watch`；LoadToken 只认 JWT 的根因可复现。
4. **看板暂缓写进契约**：避免「代码禁 / 文档写三种 kind」长期漂移。

## 待改进

1. **多租户 Where 必须进首轮单测**：CreateWhere 同等逻辑若只写 design 不写测，易漏 AND TenantId。
2. **平台 kind 砍范围要同步改 proposal/DASH**：洞察槽禁看板若早定，应在 Implementing 中改文档而非验收才发现。
3. **分享若为本号衍生应早进 tasks**：P6 会话补录偏晚，LoadToken 缺陷拖到联调才暴露。
4. **浏览器冒烟整包留到验收**：S.1–S.6 仍主要靠代码路径（沿用历史 lesson）。

## 关键决策记录

| 决策 | 理由 |
|------|------|
| 洞察槽暂缓 miniKanban | 用户验收决策；协议与 compact 保留给工作台 |
| 时间分桶做常见方言 | 对齐 design；Oracle 等仍 400 |
| 分享并入本号 tasks | 用户确认属 0e9e 衍生，不另拆 OSC |
| P2 不补齐 | 卡片复制等非阻断 |

## 偏差

- 提案决策 3 原含迷你看板洞察槽交付 → 文档改为暂缓（V1.6）。
- 空槽原「高度 0」→ 可编辑时「添加部件」（V1.4，更贴 IA）。

## 后续

- 首页工作台 OSC：复用 Host + 恢复 miniKanban / named 样例。
- 可选：卡片复制、CubeNC PUT `TryNormalize`、Oracle 时间分桶。
