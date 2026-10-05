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

---

# 二轮迭代复盘（2026-10-05）

## 摘要

| 项 | 结论 |
| --- | --- |
| 迭代范围 | 视口/底图记忆、查询聚焦与结果卡片、工具栏「+」添加记录（含地图拾取）、位置单框/必填/整行/标签合并、去标题栏与间隙压缩、查询清空持久化、底图图标 layers、比例尺深色 |
| 门禁 | 聚焦 **160/160**；全量 **1168/1172**（4 项 e6ee 在途基线）；`vue-tsc -b` **0 错误** |
| 判据同步 | proposal §5 修订；design 增《二轮迭代增强》；tasks K 组；status 二轮 note |
| 提交 | K1 已随 698f498a 合入；其余改动未提交（待用户裁决时机） |

## 实际完成范围

- K1 视口/底图记忆：`lastViewport`（平移缩放防抖 600ms）/`lastBasemap`（切换即写）；`mappingConfigSig` 排除 last* 字段防重载回环；桩 `mount` 消费初始 center。
- K2/K2b 查询聚焦与结果卡片：三入口（关键字/预定义/自定义）置标记 → 数据就绪 `focusFirstLocated`（首个有效坐标、保持缩放）；>2 条结果卡片（分类图标/颜色 + 标题字段、5 行滚动、点击定位高亮、可关闭）；`onMapFilterApply` 三处绑定修复。
- K3–K6 添加记录链路：工具栏「+」（canAdd）→ `MapAddDialog` 轻量弹层（必填项 + 位置信息）→ 单框「经度,纬度」+ 地图点击拾取（四端 `onMapClick` + `pickDataCoord` + `__pick__` 标记）→ 三组校验 → `createRowQuick` 提交 → 列表刷新；弹层细节（标签合并/提示下移/整行/去标题栏/间隙 52→16px）；**根因修复**：无遮罩 modal 的 container/wrapper 全屏 `pointer-events` 拦截 → `:has()` 放行。
- K7–K9 收尾：Q 清空同步清持久化（保留 filter）；底图图标 `layers`；比例尺深色可读。
- 架构与门禁：新增 `useMapTools` composable（SFC 构薄门禁驱动业务收口）；`mapPoints.pickDataCoord` + 3 用例；文档同步（README/迁移方案 05b–05e/功能清单 SPA-7）。

## 测试与构建

- 聚焦 spec **160/160**（5 文件：`features/views/map` + `viewMapping` + `viewProfile` + `iconRegistry`）；全量 vitest **132/133 文件、1168/1172 通过**（4 项 `sfcThin` = e6ee 在途 `.vue`：FilterBuilderPanel/RecordDrawer/WfInstanceGraph/WorkflowTaskActions）；`vue-tsc -b` **0 错误**。
- 实机：stub（拾取/校验/卡片/聚焦/清空/穿透）+ 真实高德（拾取 114.06192/23.244223 → 保存 120.5/30.5 后端核对；深色/比例尺/layers 图标）。

## 做得好

1. **根因级修复**：弹层「地图点击无反应」定位到 Arco 无遮罩 modal 的 container/wrapper 全屏 `pointer-events:auto`（`elementFromPoint` 实证命中 `.arco-modal-container`），以 `:has()` 精准放行而非猜着调 z-index；修复后真实鼠标拾取一次通过（stub + 真实高德）。
2. **验证方法论升级**：发现 Playwright `dispatchEvent` 绕过 `elementFromPoint` 命中测试（曾误判「已修好」），穿透/遮挡类验证此后一律 `page.mouse.click` 真实命中链。
3. **SFC 构薄门禁驱动架构收敛**：工具栏 + 弹层 + 查询聚焦全部业务逻辑收口 `useMapTools`，`DefaultList` 保持纯接线，门禁 spec 全程绿。
4. **交互闭环完整性**：拾取→回填→校验→提交→刷新→标记清理全链路，含异常分支（空/非法/半截）；后端落库核对。
5. **持久化语义对称**：清空 Q 与执行查询写持久化对称（watch 非空→空），保留 filter 不误伤。
6. **上轮教训兑现**：「增强即回写判据」——「+」添加记录推翻 proposal §5 原「不做地图上的新增/编辑」条目时，当场修订判据文本（proposal/design/tasks 同步），不再等验收期集中补。

## 偏离与教训

1. **无遮罩模态仍全屏拦截**：`a-modal :mask="false"` 的 `.arco-modal-container`(fixed)/`.arco-modal-wrapper`(absolute) 依旧 `pointer-events:auto`，底层地图/画布点击被吃；需要「弹层可见但底层可点」时，`:has(.map-add-dialog)` 放行容器层、弹体 `auto`；且 CSS 须放全局样式块（本工程 scoped `:global` 不生效）。
2. **穿透验证必须真实命中链**：`dispatchEvent` 直接派发到目标元素、绕过 `elementFromPoint`，点击穿透/遮挡类断言会用出假阳性（本轮曾据此误判修复未生效/已生效各一次）。
3. **Arco 无 `field` 表单项内容区是 flex 行**：`arco-form-item-content-flex`（row/nowrap）会把 hint 与输入框并排压缩；要整行堆叠需覆盖 `display:block`（本次 hint 146→400px）。
4. **小输入框也值得宽容解析**：单框坐标 `parseCoordText` 支持全角逗号/空白分隔（恰两数约束），把「按字段形态拆分」（分列 vs 合并、`coordOrder`）留到提交层——比在输入层强制格式更顺手且不丢信息。
5. **模板批量替换要核实例数**：查询聚焦接线期两处模板替换失败（旧文本实例不止一处），靠替换报错暴露并补齐——机械替换前先确认目标唯一性。

## 风险与后续

- P2 记录：弹层去标题栏后不可拖；`useMapTools`/`useMapView` 坐标检测逻辑重复（可提取 util）；结果卡片窄屏与工具栏换行潜在重叠。
- 一轮遗留风险表继续有效（聚合降级/缩略/空态细分/百万优化/百度腾讯冒烟/G2 E2E spec）。
- 提交：本轮改动未提交（16 文件 +474/−38 + 2 新增文件 + changes 目录）；K1 已随 698f498a 合入。

