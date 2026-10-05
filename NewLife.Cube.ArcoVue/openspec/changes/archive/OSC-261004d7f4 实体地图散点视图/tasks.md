# OSC-261004d7f4 Tasks — 实体地图散点视图

> 前置：以合入 OSC-261004e6ee（壳层元数据与列表刷新优化）后的代码基线上执行；接入前复核 `listContext`/`useListQuery`/`stores/viewProfile` 现状。

## A 类型与纯函数

- [x] A1 `viewMapping.ts`：`ViewKind`+'map'；`MapMapping`/`MapCategoryRule` 类型（含 `DefaultLocation`/`DefaultIcon` 按评审命名；映射不存服务商）；`VIEW_KIND_LABEL`/`DEFAULT_VIEW_KIND_NAME`/`parseViewKind`；`canCreateViewKind('map')`（`mapCoord` 检测 + 系统服务商配置齐备）；`seedMapping('map')`；`normalizeMapping` map 分支 + `normalizeMapMapping` 宽容归一（`maxPoints` 总上限默认 1000000）；`resolveViewPageSize('map')`=1000；spec。
- [x] A2 新增 `core/utils/mapCoord.ts`（`detectCoordinateFields` + 合并坐标值解析）+ `mapCoord.spec.ts`。
- [x] A3 新增 `core/utils/mapTransform.ts`（WGS-84↔GCJ-02↔BD-09）+ spec（≥6 用例）。
- [x] A4 新增 `core/utils/glassStyle.ts`（chrome → 玻璃样式，含降级标记）+ spec。
- [x] A5 新增 `features/views/map/mapPoints.ts`（records→点位/分类规则解析/填色覆盖）+ spec。
- [x] A6 `iconRegistry.ts`/`iconComponents.ts`：`VIEW_KIND_ICONS.map`；`MAP_MARKER_ICONS`（≥20 个点位图标，唯一且经 IconType 校验）+ 注册登记；`iconRegistry.spec.ts` 更新（6→7 + 新注册表）。
- [x] A7 `viewFormat.ts`：`formatApplyOptions('map')`→`['cell']`。
- [x] A8 新增 `features/views/map/mapViewport.ts`（视口筛选与分帧纯函数：`pickViewportPoints`/`chunk`）+ spec（≥6）。

## B 地图适配层

- [x] B1 `mapScript.ts`：按 provider（高德/百度/腾讯）动态加载（单例、15s 超时、URL 覆盖、失败可读错误）。
- [x] B2 `mapAdapter.ts` 接口（mount/缩放/`getBounds`/视口事件/增量 `addPoints`·`removePoints`/默认样式/destroy）+ `amapAdapter.ts`（marker content、hover/click、暗色样式；**聚合暂缓**——2026-10-05 验收降级见 J5）。
- [x] B3 `baiduAdapter.ts`（**BMap 3.0 自定义覆盖物**——实现期修订，BMapGL 不支持 content DOM；视口事件与增量增删；BD-09 由上层换算；聚合暂缓）。
- [x] B4 `stubAdapter.ts`：`window.__mapStubMarkers` + 同构 DOM 点 + `window.__mapStubApi.setViewport/pan()`；注入开关（test 模式或 `__mapStub=1`）。
- [x] B5 `tencentAdapter.ts`（TMap GL + DOMOverlay；视口事件与增量增删；GCJ-02 由上层换算；聚合暂缓）。

## C MapView

- [x] C1 `MapView.vue` + `useMapView.ts`：装配适配器、fit/中心/缩放、ResizeObserver、销毁、加载/空态/失败重试（未配置服务商=普通空态不引导）；视口渐进数据泵（首批 1000 → 视口优先 → 异步续拉 → 拖动增量 → 超阈值回收 → 查询变化重置）。
- [x] C2 点位渲染：分类图标/颜色、填色覆盖优先级、分帧批量绘制、`maxPoints` 总上限、绘制统计（右下角定位按钮悬浮卡，I5；聚合暂缓）。
- [x] C3 `MapHoverCard.vue`：玻璃卡片（字段行/标题/「+M 项」；**图片缩略暂缓**，仅记录于 verify）、定位翻转、延迟与节流、单实例复用。
- [x] C4 点击 `emit('detail', row)` → `openDetail`；hover 高亮样式。

## D 列表集成

- [x] D1 `isFullbleedViewKind('map')` + `DefaultList` 分支：隐藏 `.list-topbar`/分页器/底部提示条；高度接入 `measureTableHeight`；全屏兼容。
- [x] D2 悬浮横条玻璃工具栏（地图右上角）：放大/缩小（边界禁用）、查询组合框（复用 QueryCombo + FilterBuilder + 关键字）、填色（FormatPopover，apply 限 cell）；`activePopover` 互斥；`formatButtonVisible`/`showPagerBar`/`isLargePageView` 等 map 分支核对。
- [x] D3 `useViewTabsToolbar.ts`：`createKinds`+'map'；禁用条件（无坐标字段 / 未配置服务商）沿用既有 reason 机制展示。
- [x] D4 `useListViews.ts`：创建 map 视图（`addView` 种子 columns/mapping；chrome 玻璃默认未种，CSS 兜底——status ⑥）与切换路径验证；签名复用不回归。

## E 配置抽屉

- [x] E1 基础配置「地图」节：坐标来源（分列|合并）+ 字段/坐标系/聚合/总上限（默认 1000000）/`DefaultLocation` 默认位置/标题字段；不提供逐视图服务商选择。
- [x] E2 字段配置：地图视图提示行「悬停卡片字段」；冻结按钮隐藏复核。
- [x] E3 自定义配置「地图区」：分类字段 + 规则编辑（值/图标/颜色）+ `DefaultIcon`/`defaultColor` 默认样式 + 恢复默认；图标栅格选择器组件。
- [x] E4 `useViewConfigDrawer.ts` 接线（`localMapping` map 分支、emit 校验）+ spec 增补。

## F 后端与配置接口

- [x] F1 `Setting.cs`：`MapProvider`（amap/baidu/tencent，空=未配置）/`MapKey`/`MapScriptUrl`（单服务商三字段）。
- [x] F2 `CubeController.GetMapConfig()`（登录可读，对齐 `GetAiConfig`）。
- [x] F3 api-core：`getMapConfig` + `MapConfigModel`；`service-path.ts` 白名单加 `GetMapConfig`；spec；前端会话缓存接入。
- [ ] F4 演示配置：为 CubeDemo 写入演示 Key（供真实 SDK 手工冒烟；无外网则跳过并记录）。——跳过：无真实地图 Key（内网受限）；已由桩模式覆盖本地链路

## G 测试与文档

- [x] G1 新增全部 spec 通过；既有 spec 更新（iconRegistry/viewProfile/useViewTabsToolbar/formatApplyOptions）。
- [ ] G2 E2E（stub）：布局无底部卡片/横条工具条 4 键/悬停卡片/点击详情/分类与填色/未配置服务商（创建项禁用 + 普通空态）/拖动视口增量绘制。——待验收执行（桩能力与 `__mapStub` 断言口已就绪；需后端 + dev server）
- [ ] G3 真实 SDK 手工冒烟（高德/百度/腾讯，浅色+深色截图；内网不可用时记录为环境限制）。——环境限制：无 Key/外网，跳过后记
- [x] G4 文档同步：迁移方案（多视图矩阵 + 地图条目）、`Doc/功能清单.md` SPA-7 注解、`web/README.md` 地图视图一节。

## H 验收准备

- [x] H1 地区演示数据坐标完备性检查——2026-10-04 完成：总 46565 / 有坐标 46518 / 无坐标 47，与 Membership.db 统计逐位一致。
- [ ] H2 验收脚本与截图素材整理（含 stub 与真实 SDK 两组）。

## I 会话补录·使用反馈与验收反馈（2026-10-04 ~ 2026-10-05）

- [x] I1 高度自适应：`useMapView` 依容器顶部到视口底部动态测量（ResizeObserver + window resize，clamp 420–1600）；后并入父级列表 `measuredTableHeight`（列表视图同款面板测高），消除竖直滚动条（`scrollOverflow=0` 实测）。
- [x] I2 地区逐层加载：`detectLevelField` 自动检测数值层级字段；按缩放分层拉取/绘制（zoom<7=省、≥7=市、≥9=区县、≥11=街道，1000/页串行）；缩小回收超层点、放大 `syncLevels` 补拉；L1=34/L2=362/L3=3275/L4=42894，终态「已加载 46518 / 无效点 47」与 DB 逐位一致。
- [x] I3 分页 off-by-one 修复：后端 GetPage 为 1 基；`useListQuery.loadData` 直发 `pagination.current`、分层 `loadLevel` 从 1 起、地图续页从 2 起；修复「第 2 页重复第 1 页」及分层每层重复首屏（表格/树/甘特同受益）。
- [x] I4 悬停卡片标题栏化（对齐 RecordCard 头部：浅底 + 分隔线 + 8px 12px 内边距）；工具栏/输入框/卡片玻璃半透明化（工具栏 45% + blur16、输入 50%、卡片 82%→76% + blur10；暗色覆盖齐备）。
- [x] I5 定位按钮（替换绘制统计条）：`DefaultCenter` 默认中心（抽屉「地图中心（按标题字段搜索）」；存量 `DefaultLocation` 仅透传不展示）；点击聚焦已加载点内精确匹配、未命中 Q 搜索一次、命中 `setCenter(…,10)`；加载自动定位；悬浮显示统计卡片；与右上工具栏右缘对齐（实测均 891px），后下移至 `bottom:8px`。
- [x] I6 深色主题联动：`MutationObserver`（观察 `body[arco-theme]`）→ `MapAdapter.setDark`（高德 `amap://styles/dark|normal`；百度/腾讯无内置暗色 no-op 注释；桩 class 标记供 E2E）。
- [x] I7 底图切换 + 缩放图标：缩放键改 IconPark `zoom-out`/`zoom-in`，「缩小」后新增底图切换键（图标 `copy`，2026-10-05 用户要求终稿改 `layers`）→ `setSatellite`（高德 Satellite+RoadNet；百度 setMapType；腾讯 setBaseMap try/catch；桩标记）。
- [x] I8 实时比例尺：`156543.03392·cos(lat)/2^zoom` 米/像素、目标 80px、宽 clamp 40–120；显示于定位按钮左侧；终稿去容器化（透明背景仅刻度线+文字，11px/400，`bottom:19px` 与按钮垂直居中；实测 5/10/20 公里随缩放）。
- [x] I9 配置抽屉修订：移除「地图服务商/背景色宽度高度/默认视野」三处展示；「标题字段（悬停卡片）」改名「悬停卡片标题」；分类字段候选改用 `mapCategoryCandidates`（地区 `Kind` 可选）。
- [x] I10 工具栏配置对齐实际键位：新增「填色」开关（`showColor`，table/tree/card/map 门禁）；「筛选」改走 `QueryComboButton.customEnabled` 实际生效；地图隐藏「分享」；开关实测联动（关→工具栏键 4→3、开→恢复）。
- [x] I11 图标扩容与抽象化：新增 18 个抽象点位图标（点/方/圆/三角/菱形/八角/星号/@/十字/减号/叉/勾/靶/瞄准/聚焦/火箭/太阳/四点连接），候选 24→42（多两行）。
- [x] I12 分类字段修复与自动生成（2026-10-05）：修复 `normalizeMapMapping` 保留 `categoryField` 误用 `groupFieldCandidates` 致选择被归一化剔除（现象＝选择后无效）；新增选择字段后自动列出分类值（`loadMapCategoryValues` 抽样 1000 行去重、dataSource 优先）并预填推荐图标/颜色（`buildMapCategoryRules` 循环分配、上限 50 截断提示）；实机「类型」自动生成 13 行且选择保持。

## J 验收缺口补齐（2026-10-05，openspec-verify）

- [x] J1 修复续页 `loadingMore` 世代死锁（`resetPipeline` 复位 + 世代守卫）；`levelBusy` 同批世代守卫。
- [x] J2 `mapScript` 失败 Promise 缓存清理（onLoad 未得全局对象时 `pending.delete`）+ 错误态「重试」入口（MapView）。
- [x] J3 `buildAdapter` 并发守卫（构建序号 + 完成后自检销毁，防双实例挂载/SDK 泄漏）。
- [x] J4 未就绪态缩放/底图键禁用（MapView 暴露 `ready`，DefaultList 门禁；design §8）。
- [x] J5 点聚合降级：抽屉开关禁用并标注「暂不支持」；proposal 锁定 #5、design §5/§11/§13 回写「聚合暂缓」。
- [x] J6 注释同步（MapView pageIndex 1 基、DefaultList 工具条说明、ViewConfigDrawer 残句、loadMapLevelPage 失败语义）与 `chunk` 死代码清理（mapViewport + spec）。
- [x] J7 文档修订对齐：proposal AC3/AC8/§5/§9、design 修订注记、迁移方案 §3.1/§7.4、web/README、功能清单 SPA-7。

## K 二轮迭代（2026-10-05，使用反馈与瑕疵修复）

- [x] K1 视口/底图记忆（`lastViewport`/`lastBasemap`）：平移缩放防抖 600ms、切底图即写回映射，刷新/切视图/重开恢复；桩 `mount` 消费初始 center。已随用户提交 698f498a 合入。
- [x] K2 查询聚焦与结果卡片：关键字/预定义/自定义查询后定位**首个有坐标**结果行（保持缩放）；结果 >2 条显示透明卡片（分类图标/颜色 + 标题字段、5 行高滚动、点击动态定位、关闭按钮）；`MapView` 卡片 UI + `useMapView`（`resultItems`/`focusFirstLocated`/`selectResult`/`setResultsVisible`）；自定义查询应用纳入链路（`onMapFilterApply`）。
- [x] K3 工具栏「+」添加记录（canAdd）：轻量弹层仅必填项（`isFieldRequired`）+ 位置信息；`createRowQuick` 复用提交归一；`useMapTools` composable（SFC 构薄门禁）。
- [x] K4 位置信息单输入框与地图拾取：单框「经度,纬度」（宽容分隔解析、按字段形态拆分写入、必填校验——未选择/无效不允许保存）；`MapAdapter.onMapClick` 四端实现 + `pickDataCoord` 换算 + 临时标记（`__pick__` 独立于数据管线）。
- [x] K5 拾取失效根因修复：无遮罩 `a-modal` 的 `.arco-modal-container`/`.arco-modal-wrapper` 全屏 `pointer-events:auto` 拦截地图点击 → `:has(.map-add-dialog)` 放行容器层、仅弹体可交互（Playwright 真实鼠标点击复验，stub + 真实高德）。
- [x] K6 弹层表单细节：标签合并「位置信息（经纬度）」+ 提示行下移 + 表单末项/整行布局（覆盖 Arco 无 field 项 `-flex`）；去标题栏（`hideTitle`）并压缩底部间隙（body/footer/末项三处，52px→16px）。
- [x] K7 查询关键字清空持久化：手工清空 Q 时同步清除 `cube:lastQuery:*` 的 q（保留 filter），刷新/切视图不回灌。
- [x] K8 底图切换图标 `copy`→`layers`（iconComponents 注册 Layers）。
- [x] K9 比例尺深色可读性：暗色主题白字黑晕（全局样式块；scoped `:global` 在本工程不生效）。
- [x] K10 验证与门禁：聚焦 spec 通过（新增 `pickDataCoord` 3 用例）；全量 vitest 1168 通过（4 项 sfcThin 为 e6ee 在途）；`vue-tsc -b` 0 错误；实机 stub + 真实高德全链路（拾取/卡片/弹层/记忆/清空）。
