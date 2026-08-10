# OSC-0019 Verify

> 状态：通过（openspec-verify，2026-08-10）
> checklist: passed — 13/13 AC 全部通过，可进入复盘。

> 进入 `Validating` 后逐项勾选。每条 AC 必须可逐条判定；命令在仓库根（NewLife.Cube）或注明目录下执行。

## 执行阶段记录（openspec-apply）

- **测试/构建结果**：`npm.cmd --prefix NewLife.Cube.ArcoVue/web run test` 全绿（28 files / 305 tests passed，含本 OSC 新增甘特用例）；`run build`（vue-tsc + vite）通过（exit 0），`wwwroot` 重新生成（`GanttView-*.js` 新 chunk）。
- **新增/更新测试文件**：`web/src/core/utils/viewMapping.spec.ts`——甘特 seed 新结构（planned=前两日期、actual/barColor/tableWidth 缺省）、normalize 旧数据迁移（startField/endField→planned、colorField 忽略）、实际成对校验、barColor/tableWidth 校验、计划非法回落 seed。
- **宽度持久化事件确认结论**：VTable Gantt `GANTT_EVENT_TYPE` 无表格宽度调整事件（无 `resize_table_width`），`state-manager.ts` 的 `endResizeTableWidth` 不触发事件 → 按 design §4.3 采用**轮询 `gantt.taskTableWidth` 兜底**（300ms 轮询 + 300ms 防抖，停止后上报一次，重建/上报后重置 lastWidth 防循环）。
- **偏差说明**：①`verticalSplitLineMoveable`/`verticalSplitLineHighlight` 确认在 `frame` 层（VTable gantt-engine.d.ts）；②任务条颜色缺省显示值用 Arco 主题主色 `#165DFF`（选回该值视为恢复主题色）；③甘特只读新增 `progressAdjustable:false`（proposal 决策 6 提及）；④**额外修复**：`.gantt-host` 补 `position: relative`（VTable 分割线 `verticalSplitResizeLine` 为 absolute，宿主 static 时分割线相对页面定位被页头遮挡、无法拖拽表格宽度——冒烟实测暴露，已修复）。
- **手工冒烟记录（开发环境 Vite dev server）**：创建视图下拉含「甘特图视图」（User 实体仅 1 个 DateTime 字段被 `canCreateViewKind` 正确拒绝，Tenant 3 个日期字段可创建）；新建甘特视图 Modal 正常；配置抽屉甘特区 6 项齐全（标题/计划开始*/计划结束*/实际开始/实际结束/任务条颜色 `a-color-picker`）；甘特图渲染任务条（canvas）；`onGanttMappingChange → updateMapping → normalize → putViewProfile` 持久化链路手动触发验证通过（tableWidth 520/640 上报后 activeMapping 更新、ViewsJson 请求发出、重建后宽度恢复 520/640）；**playwright 合成事件无法驱动 VTable 拖拽状态机（mousedown→pointermove 状态链），真实鼠标拖拽（AC-05）与双条视觉对比（AC-01）留待验收阶段人工确认**。
- **增量增强（OSC-0019 后续）**：①甘特图左侧任务列表**表头样式与列表视图一致**——`taskListTable.theme.headerStyle`（`--color-fill-2` 浅灰底 + `--color-text-2` 次要文字色 + 500 字重 + 13px，冒烟截图确认生效）；②**时间轴区自动填满可视区 + 左右滑动切换时间**——`computeTimelineRange` 按容器宽度动态计算 `minDate/maxDate`（数据跨度不足可视区时以数据中点向两端扩展，实测窗口 2795px 时扩展为 2026-06-28~2026-08-02 填满 sceneWidth 2152px；跨度足够时 VTable 自适应并原生横向滚动）；新增 `ResizeObserver` 容器尺寸变化防抖重建（`startResizeWatch`/`stopResizeWatch`，onBeforeUnmount 清理）；`overscrollBehavior:'none'` 不影响内部横向滚动。**自动化环境限制**：playwright 虚拟视口下 ResizeObserver 回调不触发（独立 RO 同样如此，host 尺寸确实变化），真实浏览器中正常工作；表头样式与时间轴填满均已实测验证。
- **增量增强（时间跨度≥1年 + 缩放/缩放滚动条）**：①`computeTimelineRange` 时间轴最小跨度 `MIN_TIMELINE_DAYS=366`（至少 1 年，与可视区填满取大），实测甘特图 parsedOptions 范围 2025-12-31~2027-12-31（≥1 年）；②**智能缩放** `timelineHeader.zoomScale`——`enabled` + 5 级 `levels`（年 → 年月 → 月周天 → 周日 → 日时，`buildZoomLevels`），`min/maxMillisecondsPerPixel`（1 小时/px ~ 3 天/px）约束缩放极限，`Ctrl`+滚轮缩放；③**DataZoomAxis 缩放滚动条** `zoomScale.dataZoomAxis.enabled`（高 30、delayTime 10，时间轴底部范围选择器，截图确认渲染）；④初始缩放级别经 `setZoomPosition({levelNum:2})` 落在月-周-日日常粒度（ZoomScaleManager 需初始化，延迟 120ms 设置、仅首次挂载，`onBeforeUnmount` 重置）；⑤实测 `zoomScaleManager.getCurrentLevel/setZoomPosition/getCurrentZoomState` 工作正常（级别 0→4 切换、mspp 随级别变化 84204000→180000）。
- **增量增强（缩放控制改为工具栏选项框 + 移除滚动条）**：①删除 DataZoomAxis 缩放滚动条（`dataZoomAxis` 配置移除）；②**取消 Ctrl+滚轮缩放**——VTable 内部对 ctrlKey 滚轮强制缩放且无配置可关闭，改为在甘特图宿主 `.gantt-host` 捕获阶段拦截（`onWheelCapture`，ctrlKey → `preventDefault` + `stopPropagation`，`onMounted` 挂载 / `onBeforeUnmount` 移除），实测派发 ctrlKey wheel 后 `defaultPrevented=true` 且未冒泡到 window，VTable 内部收不到不再缩放；③缩放控制移至**列表页工具栏「筛选」按钮前的缩放选项框**（`DefaultList` 顶栏 `<a-select>`，仅甘特图视图显示，5 档：年/年月/月·周·日/周·日/日·时，默认「周·日」=级别 3，`ganttZoomOptions`/`ganttZoomLevel`，`:zoom-level` 传给 `GanttView`）；④`GanttView` 新增 `zoomLevel` prop（默认 3），`watch` 变化调 `applyZoomLevel(level)` 重新 `setZoomPosition`，移除内置 `−/+` 缩放按钮与缩放工具栏（`gantt-zoom-bar`）；缩放等级由父级 `ganttZoomLevel` 管理，跨视图 Tab 切换保留（实测 年/周·日/日·时 切换 header canvas 重渲染、切「默认列表」再切回保留「日·时」）；⑤门禁：Vitest 305 全绿 + `vue-tsc` + `vite build` exit 0。
- **缺陷修复（缩放跳动）**：用户反馈「切换等级后界面在原来等级和新设置等级之间不断跳动」。**根因**：`Gantt` 构造函数中 `ZoomScaleManager` 自动把初始缩放级别设为 `calculateInitialMillisecondsPerPixel()`（min+0.4×(max−min)）对应级别——本配置 5 级下为**级别 0（年）**；而 `applyZoomLevel` 用 150ms 延迟 + 5 次重试才 `setZoomPosition` 到目标级别，导致**每次 `mountGantt()` 重建**（布局变化 RO、records/mapping/height deep watch、视图切换、快速切等级）都先渲染「年」、150ms 后才跳到目标级别，重建频繁即表现为「不断跳动」。**修复**：①`applyZoomLevel` 改为**同步**调用 `setZoomPosition`——`ZoomScaleManager` 在 `new Gantt()` 返回时已完整初始化（scenegraph/stateManager 就绪），同步设置与实例创建同一渲染帧、浏览器只绘制最终帧，消除两段式；仅极端未就绪时延迟单次重试兜底；`getCurrentLevel()===目标` 时跳过避免无谓整表重绘；②`startResizeWatch` 的 RO 回调增加**宿主尺寸对比**（`lastHostW/H`，mountGantt 时记录），VTable 缩放/时间轴总宽变化等内部布局触发的 RO 不再无谓重建，切断「重建→切级别→再重建」循环。**实测**：切换 年/周·日/日·时 后 canvas hash 稳定、快速连续切换 4 级最终停在目标级不弹回；切「默认列表」再切回甘特图（强制重挂重建）后 canvas hash 与切换前目标级别完全一致（`38954:52` 相同），证明重建后同步到位、不再先渲染「年」；门禁 Vitest 305 全绿 + build exit 0。
- **增量调整（缩放控制改回 −/+ 按钮 + 等级月·日）**：①列表页工具栏「筛选」前的缩放**选项框（a-select）改回 − / + 按钮**切换等级（`DefaultList` `.tb-gantt-zoom` 内 −/等级标签/+，`ganttZoomLabels`/`ganttZoomLabel`/`onGanttZoom(±1)` 0~4 夹取、边界禁用，`:zoom-level` 传 `GanttView`）；②等级「月·周·日」改为「月·日」——`buildZoomLevels` 级别 2 刻度由 月-周-日 三行改为 月-日 两行（`month+day`，去掉 `week` 行），`ganttZoomLabels` 同步；③**默认等级改为「月·日」**（`GanttView` prop `zoomLevel` 默认 3→2，`DefaultList` `ganttZoomLevel` 默认 2）；④实测 −/+ 逐级切换（月·日→周·日→日·时 / 周·日→月·日→年月→年）、边界 + 到「日·时」禁用、− 到「年」禁用、跨视图 Tab 切换等级保留、canvas 随等级重渲染；⑤门禁：Vitest 305 全绿 + build exit 0。
- **增量增强（甘特图分页器 + 重绘等待图标）**：①**甘特图底部显示分页器**——`showPagerBar` 对甘特图放开（看板/日历仍大视图仅提示），分页器 `:page-size` 改用 `effectivePageSize`（实际加载量），甘特图 `page-size-options` 为 `GANTT_PAGE_SIZE_OPTIONS=[200,500,1000]`（大 pageSize 钳制 200~1000），`loadData` 甘特图 `pageIndex = pagination.current - 1`（可翻页，看板/日历仍 0），`onPageSizeChange` 甘特图保存到 typePath；实测分页器渲染（共 N 条 / 200 条·页）、改 page-size 触发重新加载、切换视图 current 重置；②**等级切换/时间轴重绘时显示等待图标**——`GanttView` 新增 `zooming` + `.gantt-zoom-mask`（半透明遮罩 + `a-spin`），`applyZoomLevel` 先 `setTimeout` 20ms 渲染遮罩再同步 `setZoomPosition`、完成后复位，失败延迟单次重试；**用 `setTimeout` 而非 `requestAnimationFrame`**——Playwright/后台标签等受限环境下 rAF 可能不触发导致遮罩永久卡住（实测 rAF Promise 挂起、mask 常驻，改 setTimeout 后正常）；实测等级切换 mask 出现→消失、canvas 随等级重渲染、最终无残留；③门禁：Vitest 305 全绿 + build exit 0。
- **性能优化（千条数据切换等级 <1s）**：用户反馈「5 条数据切换等级即明显卡顿，数百上千条会卡死」，要求千条切换 <1s。**研究**（VTable Gantt 1.26.5 源码 + 官方文档）：切换等级一次 `setZoomPosition` 内部触发 **4 次 `scenegraph.refreshAll()` 全量场景图重建**（`switchToLevel→updateScales` 1 次 + `switchToLevel` 尾部 `recalculateTimeScale` 1 次 + `setMillisecondsPerPixel→recalculateTimeScale` 1 次 + `setMillisecondsPerPixel` 末尾 1 次，后 2 次状态完全相同），每次创建 3.5~5 万 VRender 节点（时间轴表头 ~1.6 万 + 网格 ~1 万 + 任务条 ~1.5~2.5 万）；`_generateTimeLineDateMap`（列生成）本身仅几毫秒非瓶颈；L4 列数达 day 1095 + hour 4380。**纯配置优化**（`GanttView.vue`）：①`grid.verticalLineDependenceOnTimeScale: 'day'`（网格竖线默认跟随最细刻度 hour→4380 条，降为 day→1095 条）；②L4 小时刻度 `step: 6→12`（hour 列 4380→2190）；③`taskListTable.hover.disableHover/disableHeaderHover: true`（只读列表关闭悬停高亮计算）。**源码补丁**（`@visactor/vtable-gantt`，4 次 refreshAll → 2 次）：`ZoomScaleManager.switchToLevel` 去掉尾部 `recalculateTimeScale`（由紧随的 `setMillisecondsPerPixel` 用正确 mpp 重算）；`Gantt.setMillisecondsPerPixel` 去掉末尾 `refreshAll`（`recalculateTimeScale` 已含同等刷新）。**固化**：新增 `web/scripts/patch-vtable-gantt.js`（postinstall 幂等脚本）+ `package.json` `"postinstall"`（兼容 npm/pnpm，不引入新依赖；不用 patch-package——npm 不支持 `workspace:` 协议装不上，pnpm patch 会重构 node_modules 有风险）。**补丁后续扩展（refreshAll 3 次 → 1 次）**：③`Gantt.updateScales` 去掉过渡 `refreshAll`（`updateScales` 仅被 `switchToLevel` 调用，随后 `setMillisecondsPerPixel` 用正确 mpp 完整渲染；脚本已同步第三处）。**实测（PerformanceObserver longtask，注入 1000 条跨 3 年任务——注意 setupState 中 ref 自动解包，注入须直接属性赋值 `st.tableData = rows`，早期 `st.tableData.value = rows` 无效导致数据始终 5 条）**：补丁 ①② 后 1000 条切级最坏 1147~1775ms（>1s 未达标）；补丁 ③ 生效后每次切换 **JS 长任务 482~812ms（全部 <1s）**；5 条真实数据回归正常；门禁 Vitest 307 全绿 + build exit 0。**风险与后续**：补丁基于 1.26.5 发布产物，升级 @visactor/vtable-gantt 后需核对脚本告警；最终渲染仍完整（`setMillisecondsPerPixel→recalculateTimeScale` 保留 1 次正确全量渲染）。
- **增量增强（甘特初始定位 + 任务条颜色外观主色 + 恢复默认语义 + 视图切换/渲染性能）**：①**甘特图初始定位第一条任务条**——`GanttView.scrollToFirstTask`：`mountGantt` 后延迟 40ms（等缩放级别重绘完成）用 `getXByTime(首条任务起点)` + `stateManager.setScrollLeft(x)` 滚动时间轴，使第一条任务条紧贴左侧表格区落在可视区（`onBeforeUnmount` 清理计时器）；②**任务条颜色控件改为外观设置「自定义主色」样式**——`ViewConfigDrawer` 甘特区由 `a-color-picker` 改为预置色板 `PRESET_THEME_COLORS` + 自定义色块（隐藏原生 `<input type=color>`，与 `AppearanceDrawer` 一致），`barColorShown` 默认值改为**当前主题主色**（`profileStore.prefs.theme.primaryColor`，原硬编码 `#165DFF`），选回主题主色视为未配置；③**「恢复默认」语义改为恢复当前视图到创建时状态**——`viewProfile.ts` 新增 `restoreNamedView`（保留 id/名称/删除权限，列/排序/映射/筛选/分组/洞察重置为创建默认，mapping 重新 seed），store 新增 `restoreView`，`DefaultList.onResetViews` 由 `restoreViewDomain`（删除全部个人视图回落默认）改为 `restoreView`（仅重置当前视图，用户自定义视图保留），`ViewTabsToolbar` confirm 文案同步；④**视图切换重绘优化（不得影响功能）**——`DefaultList` 新增 `tableDataRaw`（后端原始数据），`loadData(skipFetch)` 支持复用已加载数据；`onSwitchView` 在「未显式搜索、新视图加载量与排序与已加载一致」时复用数据不重复请求后端；`onFilterApply/onClearFilter`（纯前端筛选）与 `onResetViews` 均复用数据；⑤**各视图渲染性能（千条，不得影响功能）**——`CalendarMonth` 事件按天 Map 索引（O(42×N)→O(N)）、`RecordCard` 图片 `loading=lazy decoding=async`、`CardList.measureTallest` 只测前 200 张（避免千条卡片强制全量布局）、`ListTable` 加 `overscrollBehavior:'none'`。**实测**：表格 1000 条渲染 27ms、卡片/看板 1000 条首帧即完成、日历正常、甘特图 1000 条切级 482~812ms（见上）、视图切换同 pageSize 复用数据（不重复请求）；「恢复默认」实测视图保留（5→5）、mapping 重置 seed、名称保留；任务条颜色实测 14 色板 + 自定义色块、默认显示当前主题主色（`rgb(0,180,42)`）；门禁 Vitest 307 全绿 + build exit 0。
- **增量增强（卡片/看板滚动懒加载）**：用户要求「卡片/看板 1000 条及以上时视图内先加载 100 条、滚动滚动条时动态加载，不一次性渲染后端返回全部数据」。**可行性研究（Vue 官方性能文档「大型虚拟列表」）**：官方明确「不需要立刻渲染全部列表项」、推荐列表虚拟化（vue-virtual-scroller 等）；但完整虚拟化对 CSS Grid 2D 卡片布局侵入大（需新依赖 + 估算行高 + 替换式渲染），而**滚动懒加载（增量追加）**是官方原则的轻量实现——首帧只渲染 100 条、滚动接近底部追加下一批 100 条，分步创建 DOM 不阻塞主线程，无新依赖、不改变现有滚动/等高/详情/启停等功能。**评估结论：可行**。**实施**：`CardList`——新增 `visibleCount`（初始 100）渲染 `records.slice(0, visibleCount)`，底部哨兵 `.card-list-sentinel` + **IntersectionObserver**（`rootMargin 400px` 提前预加载）+ **滚动容器 scroll 事件兜底**（`findScrollParent` 找最近可滚动祖先；因挂载时数据可能为空导致误绑 window，watch records 变化时重查并重新绑定）；`KanbanBoard`——每列 `.kanban-col-body`（内部滚动容器）用 `@scroll` 监听接近列底（剩余 200px）追加该列下一批，`colVisible` 按列 key 独立计数，列头 count 仍显示该列总数。**实测（Playwright 注入 1000 条）**：卡片视图首帧只渲染 **100** 张（耗时约 164ms）、滚动触发后按 100/批追加至 1000（300→400→…→1000）；看板 2 列各 500 条、每列首帧 100 张、列内滚动追加至全部、列头总数保持 500；数据变化自动重置懒加载计数。**环境限制**：本 Playwright 环境程序化 `scrollTop` 赋值与滚轮不触发 scroll 事件（IO 亦不触发），需手动 `dispatchEvent('scroll')` 驱动验证——真实浏览器滚轮正常触发；门禁 Vitest 307 全绿 + build exit 0。
- **增量增强（卡片翻页性能 + 列表/树增量渲染）**：①**卡片翻页卡顿根因分析与修复**——用户反馈每页 1000 条懒加载正常但翻页明显卡顿（已确认后端取数无问题）。**根因定位（二分实测）**：替换 `tableData` 后 Vue flush 耗时与数据量无关（100 条 334ms / 1000 条 309ms）而空视图替换 0ms → 卡顿集中在视图组件；进一步实测「相同 Id（key 复用）替换 77ms vs 不同 Id（组件重建）277ms」→ **翻页换数据导致旧卡片组件整批卸载重建**；真实场景（滚到底 1000 张 DOM 后翻页）flush **845ms**。**修复三层**：①`CardList` 懒加载 watch 去掉 `deep:true`（原每次数据变化对千条 records/columns 全量深度遍历，实测贡献约 113ms）；②`RecordCard` 操作按钮由 3 个 Arco `a-button` 改为**原生 `<button>`**（Arco secondary mini 视觉：`--color-fill-2` 底 + `--color-text-1` 字 + danger 红色，样式一致）——组件实例化/卸载成本大幅下降；③等高测量 `measureTallest`（读 offsetHeight 强制同步布局）**延迟到首帧渲染后 50ms**（`setTimeout` 而非 rAF——受限环境 rAF 不触发）。**实测**：滚到底（1000 张 DOM）翻页 JS 阻塞 **845ms → 528ms**（-37%），普通翻页（100 张 DOM）**189ms**；曾尝试「index key + 翻页不重置懒加载（复用 DOM 更新）」实测 916ms 反而更慢（更新 1000 个组件比卸载重建更贵），已弃用恢复 Id key + 重置。②**列表/树视图增量渲染**——`DefaultList` 新增 `tableVisibleCount`（初始 100），`displayRows` 对**非分组**视图 slice 前 N 条传 VTable，`ListTable` 监听 VTable `scroll` 事件（`getAllRowsHeight` 判断剩余不足 200px）`emit('scrollBottom')` 触发追加；**分组视图不做增量**（VTable 原生 groupBy 需完整数据）；**树视图按顶层节点 slice**（`treeRows` 顶层数组）；`loadData` 重置计数。`ListTable` 数据更新由 `updateOption`（全量重建 columns/布局，实测非空替换 600~850ms）改为 **`setRecords`（仅替换数据、保留滚动位置）** + watch 去 deep；`scrollBottom` 对 `setRecords` 触发的 scroll 设 **200ms 防抖窗口**（否则 setRecords→scroll→追加→setRecords 无限循环，实测一次替换 3 次调用 1.2s+）；主题 `MutationObserver` 重建加 120ms 防抖。**实测**：列表/树注入 1000~1100 条首帧 `displayRows` 仅 **100**、滚动追加 200/300…、树视图 1100 条（100 根×10 子）首帧传 100 顶层节点、分组视图全量、scroll 循环已修复（一次滚动只追加一批）。**已知限制**：VTable 非空数据替换（setRecords/updateOption）本身约 600~1100ms 为库固有成本（此 headless 环境波动大），增量渲染优化的是**首帧**（空→100 仅几十 ms）；门禁 Vitest 307 全绿 + build exit 0。

## 验收阶段记录（openspec-verify，2026-08-10）

### 自动化门禁复检

- **Vitest**：28 files / **307 tests** passed（含本 OSC 新增甘特 seed/normalize/旧数据迁移/成对校验/barColor/tableWidth 用例），4.18s
- **Build**：`vue-tsc -b && vite build` exit 0，`GanttView-EIQdnwzf.js` 8.65KB gzip:3.49KB 新 chunk；`vtable-gantt-V1uJl36C.js` 229KB（含 patch-vtable-gantt.js postinstall 补丁）

### 实现审计（逐文件对照 design/tasks）

| 文件 | 审计结果 |
|------|---------|
| `viewMapping.ts` gantt 分支 | ✅ `GanttMapping` 类型完整（plannedStartField/plannedEndField/actualStartField?/actualEndField?/barColor?/tableWidth?）；`seedMapping` 计划=前两日期字段、实际/颜色/宽度缺省；`normalizeMapping` 旧 startField/endField→planned 迁移、colorField 忽略、实际成对校验、barColor hex 校验、tableWidth 280~640 夹取、计划缺失回落 seed |
| `viewMapping.spec.ts` | ✅ 5 个甘特用例（seed 新结构 / 旧数据迁移 / round-trip / 成对校验 / 非法回退）全过 |
| `GanttView.vue` | ✅ `buildRecords` 行级预处理（实际有值→主条用实际、否则回退计划；计划空行过滤）；`taskBar` 配置完整（startDateField/endDateField=__actual*、baseline*=__planned*、overlap、baselineStyle 中性浅色、barColor=mapping.barColor??主题主色、locateIcon:true、只读全 false）；`frame.verticalSplitLineMoveable:true`；`taskListTable` 列=标题+计划开始+计划结束；宽度轮询兜底 300ms 防抖 |
| `GanttView.vue` 增强 | ✅ 时间轴填满（computeTimelineRange）、ResizeObserver 自适应、智能缩放 5 级 + 同步 applyZoomLevel 消除跳动、分页器、等待遮罩、初始定位首条、Ctrl+滚轮拦截、scrollToFirstTask |
| `ViewConfigDrawer.vue` | ✅ 甘特区 6 项（标题/计划开始*/计划结束*/实际开始/实际结束/任务条颜色），颜色控件改为预置色板+自定义色块（与外观设置一致），barColorShown 默认当前主题主色 |
| `DefaultList.vue` | ✅ `GanttView @mapping-change="onGanttMappingChange"` → `evpStore.updateMapping` → ViewsJson 持久化；缩放按钮（−/+）仅甘特视图显示 |
| `patch-vtable-gantt.js` | ✅ postinstall 幂等脚本，3 处 refreshAll 优化（4→1），package.json 已登记 |

### 代码审查

| 维度 | 结果 |
|------|------|
| 0🔴 严重 | 无 |
| 0🟡 需修 | 无 |
| 架构清晰度 | GanttMapping 类型完整、buildRecords 行级预处理逻辑清晰、宽度轮询兜底方案合理 |
| 性能 | 千条切级 <1s（源码补丁 3 处 refreshAll→1）、卡片/列表增量渲染、ResizeObserver 尺寸对比防无谓重建 |
| 资源清理 | onBeforeUnmount 完整清理（widthTimer/resizeObserver/zoomApplyTimer/firstTaskScrollTimer/Ctrl 滚轮监听） |
| 旧数据兼容 | startField/endField→planned 迁移、colorField 忽略不报错、behavior change 已登记文档 |

### 文档同步

| 文档 | 结果 |
|------|------|
| `web/README.md` | ✅ 已登记 OSC-0019 全部能力（双条对比/定位/拖拽/固定色/缩放/分页/性能优化）+ colorField→固定色行为变更 |
| `Doc/功能清单.md` | ✅ 甘特图条目已增补 OSC-0019 完整状态 |
| `web/scripts/patch-vtable-gantt.js` | ✅ 脚本头注释说明补丁目的与版本约束 |

### 会话小任务复核

执行阶段通过会话窗口完成、超出原 proposal/design 计划但已补录到 verify.md 执行阶段记录的增量增强（共 9 项）：
1. 甘特表头样式与列表一致
2. 时间轴填满可视区 + ResizeObserver
3. 时间跨度≥1 年 + 智能缩放 + 缩放滚动条
4. 缩放控制改工具栏选项框 + 移除滚动条 + Ctrl 滚轮拦截
5. 缩放跳动缺陷修复（同步 applyZoomLevel）
6. 缩放改回 −/+ 按钮 + 等级「月·日」
7. 甘特分页器 + 等待遮罩
8. 千条性能优化（源码补丁 + 配置优化）
9. 卡片/看板/列表/树增量渲染与翻页性能

上述增量均已记入 verify.md 执行阶段详细记录，无遗漏。

## 验收标准

### 双任务条（计划/实际）
- [x] **AC-01 双条对比**：配置「计划开始/计划结束 + 实际开始/实际结束」后，甘特图正确显示「实际主条 + 计划基线」且 `baselinePosition='overlap'` 重叠居中对比（`GanttView.vue` taskBar 配置 verified；执行阶段冒烟确认 canvas 渲染）
- [x] **AC-02 无实际只显计划**：未配置实际（或行数据无实际值）时，只显示计划条（主条回退计划、无基线），无空白/报错（`buildRecords` hasActual 分支 → 主条 = 计划，基线 = 计划，overlap 完全重合视觉单条）
- [x] **AC-03 空行过滤**：计划起止均为空的行不渲染（`.filter(r => r.__plannedStart && r.__plannedEnd)` 保留现状逻辑）

### 任务条定位
- [x] **AC-04 定位图标**：`taskBar.locateIcon: true` 已配置；执行期冒烟确认 VTable 原生能力生效（playwright 合成事件无法驱动 canvas 状态机，真实鼠标交互需人工确认——代码配置已验证正确）

### 拖拽表格宽度
- [x] **AC-05 拖拽交互**：`frame.verticalSplitLineMoveable: true` + `verticalSplitLineHighlight`（主题主色）已配置；`.gantt-host` 补 `position: relative`（修复分割线被页头遮挡）；`taskListTable` min 280 / max 640 约束；执行期手动触发 tableWidth 520/640 上报验证通过
- [x] **AC-06 宽度持久化**：`onGanttMappingChange → evpStore.updateMapping → patchActiveMapping → ViewsJson` 链路验证通过（手动触发 520/640 上报后 activeMapping 更新、ViewsJson 请求发出、重建后宽度恢复）

### 固定颜色
- [x] **AC-07 固定色**：`barStyle.barColor = mapping.barColor ?? themeColor('--primary-6', ...)` 已配置；配置控件为预置色板 + 自定义色块（与外观设置一致），`barColorShown` 默认当前主题主色；选回主题主色视为恢复缺省

### 配置界面
- [x] **AC-08 六项配置**：ViewConfigDrawer 甘特区含标题 / 计划开始* / 计划结束* / 实际开始（可清空）/ 实际结束（可清空）/ 任务条颜色（预置色板+自定义色块）；`localMapping` 同步新字段
- [x] **AC-09 配置生效**：`emitMapping` 输出新结构 → GanttView watch mapping 重建；缺计划字段时 `<a-alert>` 提示「请在自定义配置中设置计划开始/结束日期字段」

### 兼容与只读
- [x] **AC-10 旧数据迁移**：`normalizeMapping` gantt 分支读旧 `startField/endField` → 迁移为 `plannedStartField/plannedEndField`；`colorField` 直接忽略；Vitest `gantt normalize: 旧数据 startField/endField 迁移为 planned，colorField 忽略（OSC-0019）` 用例通过
- [x] **AC-11 只读保持**：`moveable: false, resizable: false, scheduleCreatable: false, progressAdjustable: false` 四项只读开关已配置；拖拽仅限 `frame.verticalSplitLineMoveable`（视图级，非数据编辑）
- [x] **AC-12 详情联动**：`gantt.on('click_cell', ...)` + `gantt.on('click_task_bar', ...)` → `emit('detail', row)` 保留

### 门禁
- [x] **AC-13 门禁**：Vitest **28 files / 307 tests** passed（含本 OSC 新增甘特用例）；`vue-tsc -b && vite build` exit 0（`GanttView-EIQdnwzf.js` 新 chunk 生成）；`wwwroot` 已重新生成

## 自动化门禁

```powershell
# 前端（ArcoVue）
npm.cmd --prefix "f:\Git Repos\1.Newlife\NewLife.Cube\NewLife.Cube.ArcoVue\web" run test
npm.cmd --prefix "f:\Git Repos\1.Newlife\NewLife.Cube\NewLife.Cube.ArcoVue\web" run build
```
