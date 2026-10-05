# OSC-261004d7f4 Verify — 实体地图散点视图

验收执行：2026-10-05（openspec-verify）。三层固定编排（实现审计 → 代码审查 → 文档同步）后，经用户决策「补齐并复验 + 文档修订」完成 J 组补齐（见下），全部 AC 通过。

## AC-1 创建门禁与可用性

- [x] AC1.1 地区创建菜单出现「地图视图」且可用（`viewMapping.spec` 双门禁用例 + `viewProfile.spec` 准入/拦截；实机创建并长期使用「临时验证地图」）。
- [x] AC1.2 无可识别坐标字段、或系统未配置地图服务商 → 创建项不可用并提示原因（`useViewTabsToolbar` reason 机制；`addView` 二次门禁修复已在执行期回归）。

## AC-2 全出血布局

- [x] AC2.1 无底部卡片/分页器/常规工具栏（`listContext` 对 map 排除 pager/表面背景；DOM 实机确认）；视图 Tabs 保留（切换/菜单/新建正常）。

## AC-3 工具栏

- [x] AC3.1 右上横条玻璃工具栏：缩小/放大/底图切换/查询（含关键字）/填色 **五键**（验收修订后文本，I7/I10 增补）；缩放边界（3/18）与**未就绪禁用**（J4）生效。
- [x] AC3.2 缩放生效（含边界禁用）；底图切换往返（高德版权号 5996↔1807 + RoadNet 叠加）；查询/填色弹层可开且复用（填色开关联动实测 4→3→4 键）；深色主题联动（暗色底图 + 工具栏/标尺/定位按钮暗色玻璃）。

## AC-4 悬停卡片与详情

- [x] AC4.1 悬停散点出现玻璃卡片：字段/顺序 = 「字段配置」可见列（computed style 实测 `rgba(255,255,255,0.76)` + `blur(10px)`、标题栏 0.4）；标题取 `mapping.titleField`（抽屉标签「悬停卡片标题」）。
- [x] AC4.2 点击散点打开对象详情（`emit('detail')` → `openDetail`；实机悬停内容含「北京」全字段验证）。

## AC-5 分类与填色

- [x] AC5.1 自定义配置分类字段 + 值→图标/颜色生效；**选字段自动列出分类值并推荐图标/颜色**（I12：实机「类型」自动生成 13 行、「启用」1 行；`buildMapCategoryRules` 循环分配 + 截断提示）；`normalizeMapMapping` 保留校验修复（Kind 可保持）。
- [x] AC5.2 填色规则命中时覆盖分类色（`mapPoints.spec` 优先级用例 + 实机）。

## AC-6 未配置服务商

- [x] AC6.1 创建菜单「地图视图」不可用（门禁单测）。
- [x] AC6.2 已存在的地图视图显示普通空态、无引导（`MapView` `!ready` 分支 `a-empty`）。

## AC-7 门禁

- [x] AC7.1 本 OSC 新增/相关聚焦 spec **156/156**（`features/views/map` + `mapCoord`/`mapTransform`/`glassStyle`/`mapPoints`/`mapViewport`/`viewMapping`/`viewProfile`/`iconRegistry`/`viewFormat` + app store 地图缓存）。
- [x] AC7.2 `vue-tsc -b` **0 错误**；`pnpm build`（vue-tsc + vite build）**成功**（25.51s）；`dotnet build NewLife.Cube -f net10.0` **0 错误**（2 条既有警告属工作流模块，非本号）。
- [x] AC7.3 全量 vitest **131/132 文件、1159/1163 通过**；4 项 `sfcThin` 失败为 OSC-261004e6ee 在途 `.vue`（FilterBuilderPanel/RecordDrawer/WfInstanceGraph/WorkflowTaskActions），非本号文件。

## AC-8 数据渐进

- [x] AC8.1 首批 1000 → 视口优先 → 异步续拉 → 拖动增量 → 回收；**地区实体按缩放分层加载 L1–L4**（I2）：实机「已加载 46518 / 无效点 47」与 Membership.db 统计逐位一致（总 46565）。
- [x] AC8.2 桩断言（G2 手工冒烟）：`window.__mapStubMarkers` 初始 35 → `setViewport` 驱动后 **2941**（视口增量 + 分层串行拉取生效）；`__mapStubApi`（getViewport/getZoom/setViewport/pan/getMarkerIds）就绪；桩下 `zoomIn/zoomOut` 不触发视口事件（已在 design §13 注明用 setViewport 驱动）。

## AC-9 会话补录（tasks I 组，12 项）

- [x] I1–I12 全部核对实现落位（高度自适应/分层加载/分页 1 基修复/标题栏与玻璃/定位按钮/深色联动/底图切换与缩放图标/实时比例尺/抽屉修订/工具栏开关对齐/图标 42 项/分类修复与自动生成）。

## AC-10 验收补齐（tasks J 组，2026-10-05）

- [x] J1–J7 全部完成（见「缺口处置记录」）。

## 固定编排摘要（2026-10-05）

| 步 | 结论 |
| --- | --- |
| 实现审计 | 6 目标 5 达成 + 1 部分（工具栏五键、聚合缺口）；AC1–AC8 实现层 7.5/8；P0 无、P1×2（`loadingMore` 死锁、点聚合三端未实现）、P2×7；tasks 勾选出入 4 处（B2/B3/B5/C3/C1）已回写注记。 |
| 代码审查 | 有条件放行：P1-1 死锁（一行级）、P1-2 聚合拍板、建议 P2-3/4/5；C# 规范/薄壳/防御性注释/纯函数检查通过；顺带发现 `QueryComboButton` 的 `message-search` 图标未注册（属 OSC-260830a1b2 范围，另行处理）。 |
| 文档同步 | 核心事实一致；正文滞后清单（AC3/AC8/§5 卫星/聚合/BMapGL/进度小条/缩略/chrome seed）→ 用户决策「修订对齐」，已随 J7 完成（proposal/design/tasks/status + 迁移方案 §3.1/§7.4 + web/README + 功能清单 SPA-7）。 |

## 命令

```powershell
cd NewLife.Cube.ArcoVue/web
pnpm exec vitest run src/features/views/map src/core/utils/viewMapping.spec.ts src/core/utils/viewProfile.spec.ts src/core/utils/iconRegistry.spec.ts
pnpm exec vitest run            # 全量
pnpm exec vue-tsc -b
pnpm build
cd ../../NewLife.Cube
dotnet build NewLife.Cube.csproj -f net10.0
# 桩冒烟（浏览器）：http://localhost:5183/Cube/Area?__mapStub=1
#   window.__mapStubMarkers 初始 35 → __mapStubApi.setViewport({...}) 后 2941
```

## 测试与构建记录

| 项 | 结果 |
| --- | --- |
| 聚焦 spec（map + 映射/配置/图标） | 156/156 通过 |
| 全量 vitest | 131/132 文件、1159 通过（4 项 sfcThin 为 e6ee 在途 .vue） |
| `vue-tsc -b` | 0 错误 |
| `pnpm build` | 成功（built in 25.51s） |
| `dotnet build NewLife.Cube -f net10.0` | 0 错误（2 条既有警告，非本号） |
| 桩手工冒烟 | `__mapStubMarkers` 35→2941；五键工具栏；比例尺 5 公里；未就绪禁用逻辑就位 |
| 实机（真实高德，执行期累计） | L1–L4 分层 46518/47 与 DB 逐位一致；深色/卫星/定位/标尺/悬停卡/分类 13 行自动生成；分页 2/3 页连续 |

## 缺口处置记录（用户决策 2026-10-05）

### 已补齐（tasks J 组）

| 项 | 处理 |
| --- | --- |
| P1-A 续页 `loadingMore` 世代死锁 | `resetPipeline` 复位 + 世代守卫（`levelBusy` 同批）；防「查询变更后续页永久停摆」 |
| P2 失败不可恢复 | `mapScript` 失败 Promise 缓存清理 + MapView 错误态「重试」入口 |
| P2 并发泄漏 | `buildAdapter` 构建序号守卫（过期构建自我销毁，防双实例挂载/SDK 监听泄漏） |
| P2 未就绪工具栏 | MapView 暴露 `ready`；DefaultList 对缩放/底图键 `!mapReady` 禁用（design §8） |
| P1-B 点聚合 | **验收降级**：抽屉开关禁用并标注「暂不支持，后续变更实现」；proposal 锁定 #5、design §5/§11/§13 回写「聚合暂缓」 |
| 备注同步 | MapView/DefaultList/ViewConfigDrawer/useListQuery 注释；`chunk` 死代码清理（mapViewport + spec） |
| 文档修订 | proposal AC3/AC8/§5 + §9 验收修订节；design 实现修订注记；tasks 勾选注记 + J 组；迁移方案 §3.1/§7.4 + 版本行；web/README；功能清单 SPA-7 |

### 仅记录（风险表）

| 缺口 | 级别 | 处置 |
| --- | --- | --- |
| 悬停卡片图片缩略（design §9） | P2 | 仅记录：模板仅文本；后续按需补（`fieldFormat` image 分支） |
| 无记录 / 全部无坐标细分空态（design §7） | P2 | 仅记录：当前仅未配置空态；统计经定位按钮悬浮卡可见 |
| 百万级渲染优化（`pickViewportPoints` 全量扫描、`appendRows` 组拷贝） | P2 | 仅记录：当前 4.6 万演示规模无感；兑现 100 万上限前优化 |
| 桩 `zoomIn/zoomOut` 不触发视口事件 | P2 | 仅记录：design §13 已注明 E2E 用 `setViewport` 驱动 |
| 百度/腾讯真实 SDK 冒烟（G3） | P1 | 环境豁免：无 Key/外网；高德已全流程实测（浅色/深色/卫星/悬停/分层）；百度/腾讯适配器经类型/直发回归覆盖 |
| G2 Playwright 地图 E2E spec | P1 | 桩能力与断言口就绪，验收已手工冒烟覆盖 ①–⑥ 场景；spec 待 CI 化时补写 |
| F4 演示 Key（CubeDemo） | — | 环境豁免：无真实 Key，桩覆盖本地链路 |
| 顺带：`QueryComboButton` `message-search` 图标未注册 | P2 | 非本号范围（OSC-260830a1b2）：运行时回落默认图标；另行注册 |

## 验收结论

**checklist: passed**。目标愿景 6 条全部达成（工具栏五键含底图切换、全出血、悬停卡/详情、分类与填色、单服务商门禁、数据渐进/分层）；P1 缺口经用户决策补齐或正式降级；文档已修订对齐。全量单测仅余 4 项 e6ee 在途文件失败（非本号）。状态 **Validating** → 可复盘归档（openspec-retro）。

---

# 二轮迭代验证（2026-10-05）

验收执行：2026-10-05（openspec-verify，二轮）。**对象**：本变更恢复 working 后至 10-05 晚的全部迭代（视口/底图记忆、查询聚焦与结果卡片、工具栏「+」添加记录与地图拾取、弹层细节、查询清空持久化、底图图标、比例尺深色）。执行**固定编排三步**（实现审计 → 代码审查 → 文档同步）+ **门禁复跑**；一轮 AC（archive 版 verify）结论继续有效，本文档仅覆盖二轮增量。

## 二轮范围对照（需求 → 落位 → 证据）

| # | 需求（使用反馈） | 实现落位 | 实测/门禁证据 |
| --- | --- | --- | --- |
| K1 | 视口/底图记忆（重开恢复中心/缩放/底图） | `useMapView` 防抖 600ms 写回 `lastViewport`、切底图即写 `lastBasemap`；`mappingConfigSig` 排除 last* 防重载回环；桩 `mount` 消费初始 center | 刷新/切视图/重开恢复；**已随用户提交 698f498a 合入** |
| K2 | 查询后聚焦（关键字/预定义/自定义三入口） | `useMapTools` 包装三入口置标记 → `watch(tableData)` 就绪后 `focusFirstLocated`（首个有有效坐标行、保持缩放，扫描上限 200） | 实机「市」34 条 → 定位「市中」116.997/36.651；空结果不跳转；2 处模板绑定修复后三入口一致 |
| K2b | 结果 >2 条显示结果卡片 | `MapView` 结果卡片（宽 240、top 64/right 12、玻璃）+ `useMapView.resultItems/selectResult/setResultsVisible`（渲染上限 200、分类图标/颜色+`titleField`） | 实机 34 条卡片；点击「市南」→ 120.413/36.075 动态定位+高亮；≤2 条自动隐藏 |
| K3 | 工具栏「+」添加记录（canAdd） | `DefaultList` 按钮（查询簇前、tooltip「添加记录」）+ `MapAddDialog` + `useMapTools` + `useListCrud.createRowQuick` | 实机权限 gating/按钮出现；弹层仅必填项 + 位置信息 |
| K4 | 位置信息单输入框 + 地图点击拾取 | 单框「经度,纬度」（`parseCoordText` 恰两数、宽容 `[,，\s]+`）；`MapAdapter.onMapClick` 四端实现 + `pickDataCoord`（target→数据坐标系 6 位圆整）+ `__pick__` 临时标记（独立于数据管线） | stub 拾取 114.6654/23.500072 回填；**真实高德**拾取 114.06192/23.244223；保存 120.5/30.5 后端核对 |
| K4b | 位置必填校验 | 空 →「请选择位置：在地图上点击选取，或输入「经度,纬度」」；非法 →「位置（经纬度）无效：应为「经度,纬度」，如 114.5,23.5」；空/无效不允许保存 | 三组拦截实测（空/非法/半截只输入一个数） |
| K5 | 拾取失效根因修复 | 无遮罩 `a-modal` 的 `.arco-modal-container`(fixed)/`.arco-modal-wrapper`(absolute) 全屏 `pointer-events:auto` 拦截 → `:has(.map-add-dialog)` 放行容器层、仅弹体可交互（全局样式块） | 修复前 `elementFromPoint` 命中 container；修复后 `page.mouse.click` 命中地图且标记落地；stub+真实高德双复验 |
| K6 | 弹层表单细节（标签/提示/整行/去标题栏/间隙） | `MapAddDialog`：`required` 标签「位置信息（经纬度）」、hint 下移、覆盖 `-flex` 整行、`:hide-title="true"`、body/footer/末项三处 padding 压缩 | 提示行 146→400px 整行；底部间隙 52→16px；去标题栏后无标题条 |
| K7 | 查询关键字清空持久化 | `useListQuery` watch：Q 非空→空时 `persistLastQuery(typePath, { q: '', filter })`（保留 filter） | 清空后 sessionStorage `cube:lastQuery:*` 的 q 移除；重启 dev、刷新均不回灌 |
| K8 | 底图切换图标 `copy`→`layers` | `iconComponents.ts` 注册 `Layers`；按钮 `type="layers"` | 截图确认（图层堆叠图标） |
| K9 | 比例尺深色可读性 | 全局样式块暗色覆盖（白字黑晕） | 实机深色主题确认 |
| K10 | 门禁 | 聚焦 spec / 全量 vitest / `vue-tsc -b` | 见下方「测试与构建记录」 |

## 固定编排摘要（二轮）

| 步 | 结论 |
| --- | --- |
| 实现审计 | K1–K10 逐项核对实现与测试落位；改动 **16 文件 +474/−38** + 新增 **2 文件**（`MapAddDialog.vue`/`useMapTools.ts`）；无未落位项；tasks 增 K 组 |
| 代码审查 | 无 P0/P1；P2 记录见下（不可拖/检测逻辑重复/窄屏重叠）；SFC 构薄门禁通过：新增 `watch`/`cubeApi` 调用全部收口 `useMapTools`，`DefaultList`/`MapAddDialog` 无内嵌 watch |
| 文档同步 | proposal §5 判据修订（「不做地图上的新增/编辑」→ 轻量添加记录已交付）；design 增《二轮迭代增强（2026-10-05）》节；tasks K 组；status 二轮 note；web/README（地图节）+ 迁移方案（05b–05e）+ 功能清单 SPA-7 已随迭代同步 |

## 命令

```powershell
cd NewLife.Cube.ArcoVue/web
# 聚焦（地图 + 映射/配置/图标）
pnpm exec vitest run src/features/views/map src/core/utils/viewMapping.spec.ts src/core/utils/viewProfile.spec.ts src/core/utils/iconRegistry.spec.ts
# 全量
pnpm exec vitest run
# 类型
pnpm exec vue-tsc -b
# 实机（stub / 真实高德）
#   http://localhost:5183/Cube/Area?__mapStub=1        （桩）
#   http://localhost:5183/Cube/Area                    （真实高德，CubeDemo:5000 + dev:5183）
cd ..\openspec
powershell -NoProfile -ExecutionPolicy Bypass -File harness\verify-lessons.ps1
```

## 测试与构建记录（2026-10-05 二轮复跑）

| 项 | 结果 |
| --- | --- |
| 聚焦 spec（map + viewMapping + viewProfile + iconRegistry） | **160/160 通过**（5 文件，1.38s） |
| 全量 vitest | **132/133 文件、1168/1172 通过**；4 项失败均为 `sfcThin.spec` 扫到的 **OSC-261004e6ee 在途 .vue**（`FilterBuilderPanel`/`RecordDrawer`/`WfInstanceGraph`/`WorkflowTaskActions`），非本号文件 |
| `vue-tsc -b` | **0 错误**（exit 0） |
| 实机（stub，`__mapStub=1`） | 拾取回填/三组校验拦截/结果卡片（34 条 + 点击定位）/聚焦首行/Q 清空持久化/弹层 pointer-events 穿透（`mouse.click` 真实命中） |
| 实机（真实高德 + 后端核对） | 拾取 114.06192/23.244223 → 保存 120.5/30.5（列表核对）；深色主题；比例尺深色；`layers` 图标 |
| 回归确认 | 拖动/缩放/底图切换/悬停卡片/详情抽屉/分层加载（46518/47）等一轮能力无回归（迭代期实机抽查） |

## 缺口处置记录（二轮）

### 仅记录（风险表）

| 缺口 | 级别 | 处置 |
| --- | --- | --- |
| 弹层去标题栏后不可拖（原 `draggable` 依赖标题栏） | P2 | 仅记录：轻量弹层位置固定右上，拖动价值低 |
| `useMapTools` 与 `useMapView` 各自实现坐标字段检测（`detectionOf` 逻辑重复） | P2 | 仅记录：两模块职责独立（视图配置 vs 添加表单）；后续如需可提取公共 util |
| 结果卡片与工具栏窄屏换行时可能重叠（卡片 `top:64px` 固定） | P2 | 仅记录：当前工具栏单行无风险；窄屏布局后续按需处理 |

### 一轮遗留（继续有效）

点聚合降级（开关禁用「暂不支持」）、悬停卡图片缩略、空态细分、百万级渲染优化、百度/腾讯真实冒烟（环境豁免）、G2 E2E spec 待 CI 化、`QueryComboButton` `message-search` 图标（属 OSC-260830a1b2）——均见 archive 版 verify 风险表。

## 验收结论

**checklist: passed**。二轮十项（K1–K10）全部落位，均有门禁/实机证据；固定编排三步（实现审计/代码审查/文档同步）完成；判据文档同步修订（proposal/design/tasks/status + README/迁移方案/功能清单）；全量门禁与基线一致（4 项失败为 e6ee 在途，非本号）；一轮能力无回归。状态 **Done**（二轮验收 + 复盘完成）。

