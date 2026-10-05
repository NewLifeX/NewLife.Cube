# OSC-261004d7f4 Design — 实体地图散点视图

## 0. 适用框架与资料

| 场景 | 框架 | 资料 |
| --- | --- | --- |
| 底图（高德） | AMap JSAPI 2.0 | https://lbs.amap.com/api/javascript-api-v2/summary |
| 底图（百度） | BMap JS API 3.0（实现期修订：BMapGL 不支持自定义覆盖物 content DOM，改用 3.0 自定义覆盖物） | https://lbsyun.baidu.com/index.php?title=jspopular |
| 底图（腾讯） | 腾讯位置服务 GL JS | https://lbs.qq.com/webApi/javascriptGL/glGuide/glBasic |
| 弹层/下拉/颜色选择 | Arco Design Vue | Dropdown / Popover / ColorPicker（既有） |
| 点位图标 | IconPark 按需注册 | `iconRegistry.ts` / `iconComponents.ts`（OSC-0017 体系） |

## 1. 参考实现研究（结论）

| 参考 | 文件 | 技术要点 | 局限 |
| --- | --- | --- | --- |
| CubeNC（MVC） | `NewLife.CubeNC/Areas/Cube/Views/Area/Map.cshtml` + `AreaController.Map()` | ECharts + `bmap.min.js` 扩展；`api.map.baidu.com/api?v=2.0&ak=1oZMeCCk9B0EzP5CrzaT4S7i6VFH0o2b`（AK 硬编码）；省级（`Area.Root.Childs`）+ 有经度城市；`Kind==='直辖市'` 拆两个系列；`value=[lng,lat,Rand(10,300)]` 演示值；`Layout=null`、`EnableNavbar=false` 整页 | 仅地区/仅百度/演示随机值/AK 明文散落；无交互与配置 |
| React 皮肤（历史） | `ca567059`：`AreaPage.tsx`（列表\|地图 Segmented 切换）+ `AreaMap.tsx` | ECharts geo + **本地 `china.json` 离线 GeoJSON**（无外网依赖）；三类系列：省份 scatter(11px,#3b7cf6)、城市 scatter(4px,#9ab7f8)、直辖市 effectScatter(15px,#f56c6c)；纯函数 `toScatter/splitDirect/buildOption` + 5 单测 + E2E；`AreaMap.test.ts` 断言 geo.map='china' 与三系列形状 | 皮肤已独立出库（`04df338a` 删除前端），未沉淀为通用能力；无边图/无卡片/无分类配置 |
| WebAPI 版端点（在役） | `NewLife.Cube/Areas/Cube/Controllers/AreaController.cs` `Map()`：`GET /api/Cube/Area/Map` | `EntityAuthorize(Detail)`；`FindAllWithCache` 一次加载；省级 `ParentID==0` 且有坐标；城市 `Longitude>0 && Latitude>0`；返回 `{provinces[],cities[]}` | 地区专用；**本方案不复用**（通用视图直接吃 `GetPage` 行数据），端点保留 |

**差距清单 → 本方案**：通用实体（任意带坐标）→ §3 §4；三家底图服务商（系统单配）→ §5 §12；玻璃卡片/悬浮横条工具栏 → §8 §9；配置抽屉落地 → §10；视口渐进加载与离线可测 → §5 §11 stub。

## 2. 文件级改动地图

| 文件 | 改动 | 保留 |
| --- | --- | --- |
| `web/src/core/utils/viewMapping.ts` | `ViewKind`+`'map'`；`MapMapping` 类型；`VIEW_KIND_LABEL`/`DEFAULT_VIEW_KIND_NAME`/`parseViewKind`；`canCreateViewKind('map')`；`seedMapping('map')`；`normalizeMapping` map 分支；`resolveViewPageSize('map')`=1000 | 既有六视图分支 |
| `web/src/core/utils/viewFormat.ts` | `formatApplyOptions('map')` → `['cell']` | 其余分支 |
| `web/src/core/utils/iconRegistry.ts` / `iconComponents.ts` | `VIEW_KIND_ICONS.map`（候选 `map-draw`，以 IconType 校验为准）；新增 `MAP_MARKER_ICONS`（≥20 个点位图标）与组件登记 | 既有图标体系 |
| `web/src/core/utils/mapCoord.ts`（新） | `detectCoordinateFields`、合并坐标值解析、`coordParseOrder` | — |
| `web/src/core/utils/mapTransform.ts`（新） | WGS-84 ↔ GCJ-02 ↔ BD-09 换算（纯函数） | — |
| `web/src/core/utils/glassStyle.ts`（新） | 玻璃面板样式纯函数（chrome.bgColor/bgOpacity/bgBlur → rgba+blur） | — |
| `web/src/features/views/map/mapPoints.ts`（新） | records→点位、分类规则解析、填色覆盖优先级 | — |
| `web/src/features/views/map/mapViewport.ts`（新） | 视口筛选与分帧纯函数（已加载点 → 待绘制集合） | — |
| `web/src/features/views/map/mapScript.ts`（新） | 高德/百度/腾讯脚本动态加载（单例、超时、URL 可配） | — |
| `web/src/features/views/map/mapAdapter.ts` + `amapAdapter.ts` / `baiduAdapter.ts` / `tencentAdapter.ts` / `stubAdapter.ts`（新） | 统一适配接口（视口事件 + 增量增删）与三实现 + 离线 stub | — |
| `web/src/features/views/map/MapView.vue` / `MapHoverCard.vue` / `useMapView.ts`（新） | 全出血地图、玻璃卡片、视口渐进数据泵、空态（未配置服务商不引导） | — |
| `web/src/views/crud/DefaultList.vue` / `listContext.ts` / `useListQuery.ts` | 地图分支布局（隐藏 topbar/分页/提示条）、悬浮工具栏、`formatButtonVisible` 加 map；**以合入 OSC-261004e6ee 后的基线为准** | 其它视图行为 |
| `web/src/views/crud/useListViews.ts` / `useViewTabsToolbar.ts` | 创建 `map` 视图接入；`createKinds`+'map'；禁用理由展示 | 既有创建流程 |
| `web/src/views/crud/ViewConfigDrawer.vue` / `useViewConfigDrawer.ts` | 基础配置「地图」节；字段配置语义提示；自定义配置「地图区」 | 既有节结构 |
| `web/src/stores/viewProfile.ts` | 确认 `serializeNamedView` 对 `map` mapping 原样 round-trip | e6ee 重构结果 |
| `packages/api-core/src/api.ts` / `service-path.ts` / `types.ts` | `getMapConfig` + `MapConfigModel`；服务名白名单加 `GetMapConfig` | — |
| `NewLife.Cube/NewLife.Cube/Setting.cs` | `MapProvider`（amap/baidu/tencent，空=未配置）/`MapKey`/`MapScriptUrl`（单服务商三字段） | — |
| `NewLife.Cube/NewLife.Cube/Controllers/CubeController.cs` | `GetMapConfig()`（对齐 `GetAiConfig`） | — |

## 3. 坐标字段检测（`mapCoord.ts`）

**分列经纬度**（两者齐全才算命中）：

| 角色 | 字段名（大小写不敏感） | 显示名关键词 | 类型要求 |
| --- | --- | --- | --- |
| 经度 | `Longitude` `Lng` `Lon` `GpsLng` | 含「经度」 | 数值型（Int16/32/64、Double、Decimal、Single） |
| 纬度 | `Latitude` `Lat` `GpsLat` | 含「纬度」 | 数值型 |

**合并坐标字段**（单字段）：

- 字段名 ∈ `Location` `Coord` `Coords` `Coordinate` `Coordinates` `Point` `Geo` `GeoPoint` `LngLat` `LatLng` `Position`；或显示名含「坐标」「经纬度」「位置」。
- 类型 String/文本；值解析支持：`lng,lat`、`lng lat`、`lng;lat`、全角逗号、`(lng,lat)`、`[lng,lat]`、JSON 数组 `[116.4,39.9]`；顺序默认 lng,lat，可由 `coordOrder` 配置为 lat,lng。

**规则**：

- 分列优先；两者同时存在时取分列，配置抽屉可切换合并。
- 排除主键；允许合成列（`column===false`）。
- 门禁只看字段存在性（不做取值抽样，避免首屏数据未加载时误判）；运行时按点过滤无效/缺失坐标，并在角落玻璃小条提示「N 个对象无坐标，未显示」。
- `detectCoordinateFields(fields)` 返回 `{ mode:'latlng', lngField, latField } | { mode:'merged', coordField } | null`。

## 4. MapMapping（存 `viewsJson.mapping`）

```ts
export interface MapCategoryRule { value: string; icon?: string; color?: string }
export interface MapMapping {
  kind: 'map';
  coordMode: 'latlng' | 'merged';       // 底图服务商不在映射中：统一取系统配置
  lngField?: string; latField?: string; // coordMode='latlng'
  coordField?: string;                  // coordMode='merged'
  coordOrder?: 'lnglat' | 'latlng';     // 合并解析顺序，默认 lnglat
  coordSystem?: 'gcj02' | 'wgs84' | 'bd09'; // 数据坐标系，默认 gcj02
  titleField: string;                   // 悬停卡片标题
  categoryField?: string;               // 分类字段（枚举/布尔/选项/LIST）
  categoryRules?: MapCategoryRule[];    // ≤50 条
  DefaultIcon?: string;                 // 默认图标（评审命名），缺省 'local'
  defaultColor?: string;                // #RRGGBB，默认主题主色 #165DFF
  cluster?: boolean;                    // 点聚合，默认 true
  maxPoints?: number;                   // 总拉取上限，默认 1000000，钳制 [1000,1000000]（后端支撑百万级）
  DefaultLocation?: [number, number];   // 默认地图位置（评审命名，原 center；缺省 fit 首批点）
  zoom?: number;                        // 初始缩放，钳制 [3,18]
}
```

**宽容归一化**（对照既有 `normalizeMapping` 风格）：字段名 canonical 校验（不在 fields 中丢弃/回落检测结果）；`categoryRules` 逐条丢弃非法项、去重 value、上限 50；`icon` 不在 `MAP_MARKER_ICONS` 中丢弃；`color` 校验 `#RRGGBB`；数字钳制；`coordMode` 非法回落检测结果；无法得到有效坐标或 `titleField` 时回落 `seedMapping('map', fields)`。

**种子**（`seedMapping('map')`）：坐标取检测结果；`titleField` 同卡片（`titleFieldCandidates` 首项）；`cluster:true`；`DefaultIcon:'local'`；`defaultColor:'#165DFF'`。

**示例（地区实体）**：

```json
{ "kind": "map", "coordMode": "latlng", "lngField": "Longitude", "latField": "Latitude",
  "coordSystem": "gcj02", "titleField": "Name", "categoryField": "Kind",
  "categoryRules": [ { "value": "直辖市", "icon": "local-two", "color": "#F53F3F" },
                     { "value": "省", "icon": "flag", "color": "#165DFF" },
                     { "value": "市", "icon": "pin", "color": "#3491FA" } ],
  "cluster": true, "maxPoints": 1000000 }
```

## 5. 地图适配层

```ts
export interface MapAdapter {
  mount(el: HTMLElement, opts: { provider: 'amap' | 'baidu' | 'tencent'; center?: [number, number]; zoom?: number; dark?: boolean }): Promise<void>;
  zoomIn(): void; zoomOut(): void; getZoom(): number;
  fit(points: Array<{ lng: number; lat: number }>): void;
  getBounds(): { minLng: number; minLat: number; maxLng: number; maxLat: number };
  onViewportChange(cb: () => void): void;          // moveend / zoomend（节流后）
  addPoints(t: MapRenderInput): void;              // 增量绘制（已换算+已定样式，含 cluster）
  removePoints(ids: string[]): void;               // 视口外回收
  // setDefaultStyle 已于实现期移除（样式随 addPoints 传入；2026-10-05 验收注记）
  destroy(): void;
}
```

- **加载器** `mapScript.ts`：按 provider 单例加载（高德 `webapi.amap.com/maps?v=2.0&key=`；百度 `api.map.baidu.com/api?type=webgl&v=1.0&ak=`；腾讯 `map.qq.com/api/gljs?v=1.exp&key=`），15s 超时、失败抛可读错误；URL 可由 `GetMapConfig.scriptUrl` 覆盖（内网自托管）。
- **实现差异**（2026-10-05 验收修订）：高德 `AMap.Map`/`AMap.Marker`（`content` 自绘 DOM）；百度 **BMap 3.0** 自定义 `BMap.Overlay`；腾讯 `TMap.MultiMarker` + `DOMOverlay`。**点聚合三端暂缓**（验收降级）：`cluster` 仅归一化存储、抽屉开关禁用，后续变更按 `AMap.MarkerCluster`/`MarkerClusterer`/官方插件方式补做。三者统一：DOM content marker（IconPark SVG + 颜色，命中热区 ≥28px）、hover/click 事件回调、视口事件与增量增删、`destroy()` 移除全部监听与实例。
- **坐标系**：各适配器收「已换算到目标底图坐标系」的点（高德/腾讯=GCJ-02、百度=BD-09），换算在 `mapPoints`→`mapTransform` 完成。
- **stub 适配器**（`stubAdapter.ts`）：`mount` 渲染占位容器；`addPoints/removePoints` 把点位写入 `window.__mapStubMarkers` 并以同构 DOM 渲染圆点（含 title/class，支持 hover/click）；暴露 `window.__mapStubApi.setViewport/pan()` 供 E2E 模拟拖动，触发视口事件与增量绘制；供内网/CI/E2E 离线验收与样式断言。注入方式：`useMapView` 读 `import.meta.env.MODE==='test'` 或 URL 参数 `__mapStub=1` 时选 stub。

## 6. 坐标换算（`mapTransform.ts`）

- 纯函数：`outOfChina(lng,lat)`、`wgs84ToGcj02`、`gcj02ToWgs84`（粗反解）、`gcj02ToBd09`、`bd09ToGcj02`、`convert(lng, lat, from, to)`。
- 中国大陆范围内按公开算法偏移；境外点原样返回；精度 1e-6；NaN/越界原样返回。
- 单测 ≥6：天安门 WGS84↔GCJ02、GCJ02↔BD09 往返容差、境外原样、边界（lng=180/90 极值）。

## 7. MapView 布局与生命周期

- **接入**：`DefaultList` 中 `activeViewKind==='map'` 走全出血分支——`.list-topbar`、`.list-pager`、`.list-pager--hint` 均不渲染；`.list-view-tabs` 保留；内容区 `<MapView>` 高度沿用 `measureTableHeight()` 测量体系（与 ListTable 同源）；全屏模式兼容。
- **层级**：地图容器 z=0；浮动工具栏 z=30；悬停玻璃卡片 z=40；Arco 弹层由组件库托管。
- **状态**：加载中（居中 spin）；无记录（空态）；有记录但全部无坐标（空态 + 统计文案）；未配置地图服务商（普通空态，不做引导）；SDK 加载失败（错误 + 重试）。
- **resize**：`ResizeObserver` 防抖 150ms + 全屏切换后重算；销毁时清理 marker/cluster/listener/observer。
- **主题**：跟随暗色切换底图 style（高德 `mapStyle` normal/dark；百度 `setMapStyleV2`）；`dark` 同步传给玻璃卡片 token。

## 8. 悬浮玻璃工具栏（横条，地图右上角）

```
┌ map(full bleed) ──────────────────────────────┐
│                  ┌─────────────────────────┐  │
│                  │ － ＋ │ 查询 ⌄ │ 填色 ⌄ │  │   ← 横条玻璃工具条
│                  └─────────────────────────┘  │      top:12 right:12
│                                               │
└───────────────────────────────────────────────┘
```

- **放大 / 缩小**：调 `adapter.zoomIn/Out`；到达 [3,18] 边界时禁用（按钮灰显）；`title` 提示。
- **查询**（组合框）：下拉展开玻璃面板，内容复用列表既有组件——关键字输入 + 查询图标（`handleSearch`）+ `QueryComboButton`（预定义/最近/保存/重置）+ 「自定义查询」入口（`FilterBuilderPopover` 锚点）；行为与列表完全一致（`searchForm.Q`、`viewFilter`、`handleApplyQuery`、`onFilterApply` 等由 `DefaultList` 经 slots 注入 MapView 工具栏）。
- **填色**（组合框):复用 `FormatPopover`（`viewKind='map'`，`formatApplyOptions` 返回 `['cell']`）；规则命中对象时点色 = 规则色（优先级：填色 > 分类色 > 默认色；`bold` 忽略）。
- 弹层互斥沿用 `activePopover`（filter/format 单开）；空态下 放大/缩小禁用，查询/填色可用。

## 9. 悬停玻璃卡片

- **触发**：marker hover 后 150ms 延迟显示（防扫过抖动）；移出 80ms 隐藏；聚合 balloon 不触发（SDK 行为）；点击散点 `emit('detail', row)` → 复用 `openDetail`（含 909b 的 `?id=` 自动打开链路）。
- **内容**：标题行 = `titleField` 值；分隔线；字段行 = 「字段配置」可见列（按顺序），label 取自定义标题/displayName，value 复用既有字段渲染纯逻辑（图片渲染 48px 缩略）；最多 10 行，超出显示「+M 项」。
- **定位**：跟随光标 offset(14,14)；右/下溢出翻转；卡片 `max-width:320px`。
- **样式（玻璃 token）**：

| 项 | 浅色 | 深色 |
| --- | --- | --- |
| 背景 | rgba(255,255,255,.62) | rgba(32,34,40,.62) |
| 模糊 | backdrop-filter: blur(14px) saturate(160%) | 同左 |
| 边框 | 1px solid rgba(255,255,255,.5) | 1px solid rgba(255,255,255,.14) |
| 阴影 | 0 8px 28px rgba(0,0,0,.16) | 0 8px 28px rgba(0,0,0,.45) |
| 圆角/内距 | 10px / 12px 14px | 同左 |

- **与视图配置联动**：`glassStyle.ts` 纯函数把 `chrome.bgColor/bgOpacity/bgBlur` 转为 rgba 背景与 blur 像素（沿用列表公式 `blur = bgBlur/5 px`）；地图视图新建时种入 `bgPreset:'custom', bgColor:'#FFFFFF', bgOpacity:62, bgBlur:70` 作为玻璃默认，用户可在自定义配置「背景色」中调整。
- **性能**：单实例常驻 DOM（更新内容而非重建）；`pointer-events:none` 不阻断地图漫游；滚动/移动经 `requestAnimationFrame` 节流。

## 10. 配置抽屉落点

**基础配置 tab（现有）**

- 新增「地图」小节（仅 `viewKind==='map'`；位于「数据范围」之后）：坐标来源（分列/合并 分段 + 经度/纬度或合并字段下拉，自动预选检测结果）、坐标系（GCJ-02/WGS-84/BD-09）、**点聚合开关（2026-10-05 验收降级：禁用并标注「暂不支持，后续变更实现」）**、总拉取上限（1000–1000000，默认 1000000）、**地图中心（`DefaultCenter`，按标题字段搜索；原 `DefaultLocation` 仅透传不展示）**、卡片标题字段（必选）；服务商为系统级配置，此处不提供选择；「背景色/宽度/高度」对地图隐藏。
- 「字段配置」小节（现有列表）：地图视图语义 = **悬停卡片字段**——沿用列清单（拖拽排序/自定义标题/显隐），顶部追加提示行「地图视图下用于定义鼠标悬停卡片的内容」；冻结按钮对 `map` 不显示（`isTableLikeViewKind` 分支复核）。

**自定义配置 tab（现有）**

- 新增「地图区」折叠段（`map` 时替代「列表区」段）：分类字段（下拉，候选沿用 `groupFieldCandidates`：枚举/布尔/选项/LIST）；分类规则列表（行 = 值 picker（候选来自 `dataSource`/`options`，允许手输） + 图标 picker（`MAP_MARKER_ICONS` 栅格弹层） + 颜色（复用色板 + 颜色选择器）；行增删、上限 50、清空全部）；默认样式（`DefaultIcon` 默认图标 / `defaultColor` 默认颜色）；「恢复默认」。
- 「背景色」保留（即玻璃卡片/工具条底色来源）；「工具栏」段对 map 仅显示「查询」开关（分组/排序/分享隐藏）；宽度/高度保留。

## 11. 数据与性能（视口渐进加载）

- **首批同步**：进入地图视图先 `GetPage(pageSize=1000, pageIndex=1)` 取回前 1000 条，**视口优先绘制**——地图初始视野取 `DefaultLocation`/`zoom`（未配置时 fit 首批点范围），仅先绘制当前视口内的点。
- **异步续拉**：首批完成后后台继续 `pageIndex=2..N` 顺序拉取（默认 1000/批，特大总量可按后端上限调大批次并串行拉取，避免并发打爆），直至 `pagination.total` 或 `maxPoints`（总上限，默认 1000000——后端支撑百万级总量）；每批到达即并入「已加载点集」，不立即全量绘制。
- **拖动增量绘制**：监听 `moveend`/`zoomend`（节流 ≥150ms）→ `getBounds()`（外扩 20% padding）→ 从「已加载未绘制」点中筛出视口内点（纯函数 `mapViewport.ts`）→ 分帧批量 `addPoints`（≤300 点/帧，`requestAnimationFrame`）。
- **回收**：已绘制点超过 3000 时，把落在外扩 2 倍视口之外的点 `removePoints`（保持内存可控；聚合插件同步增删）；渲染量与总量解耦（百万级总量也只在视口附近保留数千点）。
- **状态与提示**（2026-10-05 验收修订）：绘制统计改由右下角**定位按钮悬浮卡片**显示「已绘制 X / 已加载 Y / 共 Z / 层级 / 无效点」，其左侧为**实时比例尺**（I5/I8）；触达 `maxPoints` 提示「已达上限，可用查询缩小范围」；无效坐标计入统计。
- **重置与取消**：查询/排序变化 → 中止在途分页（AbortController/标志位）并清空已加载集重走首批；视图切换/销毁 → 中止并清理。
- **聚合暂缓**（2026-10-05 验收降级，随点聚合一并补做）；点击单点进详情；hover 单点才弹卡片。
- 切换视图（map ↔ 列表）依赖 OSC-261004e6ee 的请求签名复用：查询/排序不变时复用行数据不重复请求；地图自身分页续拉不进入行缓存语义（独立请求序列）。

## 12. 后端配置与接口

- `Setting.cs` 新增（**单服务商三字段**）：`MapProvider`（`"amap"`/`"baidu"`/`"tencent"`，空=未配置）、`MapKey`、`MapScriptUrl`（空=官方默认地址）。同一时间仅一家服务商生效；切换服务商只需改这三项。
- `GET /Cube/GetMapConfig`（`CubeController`，对齐 `GetAiConfig`；登录可读）：

```json
{ "provider": "amap", "key": "…", "scriptUrl": "" }
```

- 安全说明：浏览器端 JS SDK 密钥属公开参数（由服务商域名白名单保护），非敏感信息；文档中显式标注，避免与「输出敏感凭据」规则混淆。
- api-core：`configApi.getMapConfig()` + `MapConfigModel`；`service-path.ts` 服务名白名单加 `GetMapConfig`（否则会被拼成 `/api/Cube/GetMapConfig` 404）；前端会话级缓存一次（并入 OSC-261004e6ee 的会话缓存资源集合）。

## 13. 测试设计

- **Vitest 纯函数**：`mapCoord`（分列/合并/显示名/主键排除/数值校验，≥12）；`mapTransform`（≥6）；`mapPoints`（解析/顺序/分隔符/无效统计/分类规则/填色覆盖，≥10）；`mapViewport`（视口筛选/分帧，≥6）；`normalizeMapMapping` round-trip（≥8）；`glassStyle`（2）；`canCreateViewKind('map')`（4：有分列 / 有合并 / 无坐标 / 未配置服务商）。
- **既有 spec 更新**：`iconRegistry.spec`（VIEW_KIND_ICONS 6→7；`MAP_MARKER_ICONS` 有效性与去重）；`viewProfile.spec`（map mapping round-trip）；`useViewTabsToolbar` 相关创建项。
- **E2E（Playwright，stub 模式离线）**（2026-10-05 验收修订）：① 地区实体新建地图视图 → 无分页器/无 `.list-topbar`/横条工具条 5 键（缩/放/底图/查询/填色）；② stub 悬停 → 玻璃卡片含配置字段；③ 点击 → `.record-drawer` 打开；④ 分类规则与填色改点色（断言 `window.__mapStubMarkers`）；⑤ 未配置服务商：创建项不可用 + 已存在地图视图为普通空态（无引导）；⑥ 拖动增量：`stub.setViewport()/pan()` 后 `window.__mapStubMarkers` 数量增长（验收期已手工冒烟：35→2941）；桩的 `zoomIn/zoomOut` 不触发视口事件，E2E 请用 `setViewport` 驱动。
- **真实 SDK 冒烟**（有外网/自托管时手工）：高德/百度/腾讯各一次截图（浅色+深色）。
- **视觉**：玻璃卡片与工具条在深浅两色下的对比度截图检查。

## 14. 风险与缓解

| 风险 | 缓解 |
| --- | --- |
| 与 OSC-261004e6ee（进行中）在 `listContext`/`useListQuery`/`stores/viewProfile` 冲突 | 执行前以其合入基线为准复核接入点；纯函数与适配层文件独立，不受影响 |
| 内网/CI 无外网，SDK 与 Key 不可用 | stub 适配器离线验收 + SDK 地址可自托管；真实冒烟另行记录 |
| 三家 Key 未申请或域名白名单未配 | 未配置时创建不可用（不引导）；文档给出申请与白名单步骤 |
| 坐标系错配导致点位偏移（同一坐标在三底图偏差数百米） | `coordSystem` 显式配置 + 自动换算 + 换算单测；地区数据默认 GCJ-02 注明 |
| 腾讯 GL JS 的聚合/多标记 API 与另两家差异 | 适配层隔离差异（直发回归已覆盖）；聚合暂缓（验收降级），补做时按官方插件方式加载 + stub/单测覆盖 |
| 拖动增量与聚合插件交互抖动 | 增删按帧批量提交、`moveend` 节流、回收用外扩 2 倍视口 |
| 大点量卡顿（总量上限 100 万） | 渲染量与总量解耦：视口渐进 + 分帧批量绘制（≤300/帧）+ 超阈值回收 + marker 样式缓存；聚合补做后同受益 |
| `backdrop-filter` 低端设备性能/不支持 | 提供降级：不支持时回退纯半透明背景（`@supports` 检测） |
| 地区演示数据坐标不完整 | 验收前检查省份+城市坐标完备性（H1），缺失时以省份层冒烟 |

## 实现修订注记（2026-10-05 验收）

- 适配器接口：去 `setDefaultStyle`（样式随 `addPoints` 传入）；增 `setCenter/setDark/setSatellite`（深色主题联动 I6、卫星切换 I7）。
- 百度改用 BMap 3.0 自定义覆盖物（非 BMapGL+MarkerClusterer）；腾讯用 `DOMOverlay`。
- 高度采用父级列表 `measuredTableHeight` 优先 + 自测兜底（I1），消除竖直滚动条。
- 玻璃样式：工具条与卡片由 CSS 自适应明暗；创建地图视图未种 chrome 玻璃默认（CSS 兜底）；「背景色」配置对地图隐藏后该联动不可达。
- 验证期修正：`normalizeMapMapping` 分类字段保留改用 `mapCategoryCandidates`（原误用 `groupFieldCandidates`）；续页 `loadingMore` 世代复位；`mapScript` 失败任务缓存清理；`buildAdapter` 并发守卫（均本次修复，见 tasks J 组）。
- 悬停卡片：实际固定 280px（design 320px）、`v-if` 非常驻单实例、无图片缩略（仅记录）；`ResizeObserver` 无 150ms 防抖（仅记录）。
