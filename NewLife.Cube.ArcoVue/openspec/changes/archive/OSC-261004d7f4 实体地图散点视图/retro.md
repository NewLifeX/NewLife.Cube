# OSC-261004d7f4 Retro — 实体地图散点视图

## 摘要

| 项 | 结论 |
| --- | --- |
| 目标愿景 | 达成：通用地图视图（坐标识别门禁 / 全出血 / 五键玻璃工具条 / 悬停卡与详情 / 分类与填色 / 单服务商 GetMapConfig）+ 验收增强（分层加载 / 定位+比例尺 / 深色·卫星 / 分类自动生成） |
| 缺口处置 | P1×2 + P2×7（审计口径）：J1–J7 补齐（死锁/重试与缓存/并发守卫/未就绪禁用/聚合降级/注释/文档）；其余（缩略/空态细分/百万优化/桩事件）仅记录于 verify 风险表 |
| 归档 | 已归档（本次提交） |

## 实际完成范围

- A–H（design 内）：类型与纯函数（`mapCoord`/`mapTransform`/`glassStyle`/`mapPoints`/`mapViewport`）、适配层（amap / BMap 3.0 自定义覆盖物 / 腾讯 `DOMOverlay` / 桩）、MapView 视口渐进数据泵与渲染、DefaultList 接入（全出血 + 玻璃工具条）、配置抽屉（基础/地图区）、后端三字段 + `GetMapConfig` + api-core 会话缓存、文档首轮同步（G4）。
- I 组（会话补录 12 项）：高度自适应（`measuredTableHeight`）、地区分层加载 L1–L4、分页 off-by-one 修复（1 基）、卡片标题栏与玻璃半透明体系、定位按钮（`DefaultCenter` + 悬浮统计）、深色主题联动、底图切换与缩放图标、实时比例尺、抽屉三处修订（服务商/背景三件套/默认视野）、工具栏开关对齐（`showColor`/`customEnabled`/分享隐藏）、图标 42 项（含 18 抽象）、分类字段修复与自动生成（`mapCategoryCandidates` 修复 + `buildMapCategoryRules`/`loadMapCategoryValues`）。
- J 组（验收补齐 7 项）：续页死锁与世代守卫、失败缓存清理 + 重试入口、`buildAdapter` 并发守卫、未就绪禁用、点聚合降级（开关禁用 + 提案/设计回写）、注释与 `chunk` 死代码、文档修订对齐（proposal/design/tasks/status + 迁移方案/README/功能清单）。
- 环境豁免：F4 演示 Key、G3 百度/腾讯真实冒烟（无 Key/外网）；G2 E2E spec 待 CI 化（桩手工冒烟已覆盖数据泵/五键/比例尺）。

## 测试与构建

- 聚焦 spec **156/156**；全量 vitest **131/132 文件、1159 通过**（4 项 `sfcThin` 为 e6ee 在途 `.vue`）；`vue-tsc -b` **0 错误**；`pnpm build` 成功（25.51s）；`dotnet build` **0 错误**；桩冒烟 `__mapStubMarkers` **35→2941**；实机分层 **46518/47** 与 Membership.db 逐位一致。

## 做得好

1. 视口渐进 + 分层加载与真实数据核对（46565/46518/47）逐位一致；分页 off-by-one 实机发现即刻修复并固化为 1 基直发（列表/树/甘特同受益）。
2. 玻璃视觉体系（工具栏/输入框/卡片/标尺/按钮）与深色主题、卫星底图、比例尺、分类自动生成等多轮反馈逐项落地，均有实机截图/几何/计算样式证据。
3. 验收严格按规范执行：三编排（审计/审查/同步）+ 门禁 + 目标愿景对照 + 用户决策；缺口修复（J 组）全部带意图注释并复验。

## 偏离与教训

1. **归一化与 UI 候选不同源**（本次最典型案例）：`normalizeMapMapping` 保留 `categoryField` 误用 `groupFieldCandidates` → 选「类型」(Kind) 保存即被剔除、回载重置（表象＝"选择无效"）；改用 `mapCategoryCandidates`。同类校验（门禁/归一/UI）应共用同一候选函数。
2. **世代标志未复位 / 守卫不对称**：`loadingMore` 在 `generation++` 后旧任务被 gen 守卫丢弃、finally 不复位 → 新续页被拦截永久停摆；`levelBusy` 反向误清零。异步状态 = 环境值 + 世代属主，双管理。
3. **增强未即时回写判据文档**：多轮会话增强后 proposal/design 文本滞后（四键 vs 五键、进度小条 vs 定位按钮、"不做卫星图"vs 已交付），验收前才集中修订；此后增强应同步修订判据文本。
4. **计划内功能未实现却勾选**：B2/B3/B5 勾选含"聚合"而实现暂缓——勾选粒度应按实际交付标注；本轮回写注记并正式降级（提案锁定 #5 同步改写）。
5. **桩能力边界未先标注**：桩 `zoomIn/zoomOut` 不触发视口事件，首次冒烟 markers 不变；改 `setViewport()` 驱动后 35→2941；design §13 已补注。

## 风险与后续

- 点聚合按后续变更补做（高德 `AMap.MarkerCluster` / 百度 `MarkerClusterer` / 腾讯官方插件）；当前开关已禁用并标注。
- 悬停卡图片缩略、无记录/全无坐标细分空态、百万级渲染优化（`pickViewportPoints` 全量扫描、`appendRows` 组拷贝）按需补做（verify 风险表）。
- 百度/腾讯真实 SDK 有环境后补浅色+深色冒烟；G2 Playwright 地图 E2E spec 待 CI 化（design §13 已注明 setViewport 驱动）。
- `QueryComboButton` 的 `message-search` 图标未注册（属 OSC-260830a1b2），另行处理。
