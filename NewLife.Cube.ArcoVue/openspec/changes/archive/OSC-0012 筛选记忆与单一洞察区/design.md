# OSC-0012 Design — 筛选记忆与单一洞察区

## 1. 目标与契约边界

本号扩展个人 ViewProfile：`FiltersJson` 保存每个命名视图的默认搜索条件，`ViewsJson` 的每个 NamedView 保存受限 `insight`，新增顶层 `PageSize` 保存当前 `typePath` 页面偏好。后端 `ReadOnlyEntityController.GetChartData()` 已以 `WebHelper.Params` 创建 Pager 并调用 `SearchData`；前端仅需将当前有效搜索参数带入请求，不能复制后端搜索逻辑。

`GetPage` 继续是字段、权限和可搜索性的唯一事实源。保存前需将条件按 search 字段元数据正规化；不在 search 分区、空值、无权限或无法序列化的字段不得持久化或发送。

## 2. 文件级改动地图

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `NewLife.Cube/Entity/Cube.xml` | ViewProfile 增加 `PageSize` Int32 列及说明 | 既有 `(UserId, TypePath)` 唯一索引与 JSON 列 |
| `NewLife.Cube/Entity/视图配置.cs`、`Models/ViewProfileModel.cs` | 由 xcode 自动生成 PageSize | 禁止手改生成业务逻辑 |
| `NewLife.Cube/Entity/视图配置.Biz.cs`、`Controllers/CubeController.cs` | PageSize 的个人 upsert/GET/PUT 透传和合法范围归一 | `userId <= 0` 个人路径拒绝、既有 endpoint |
| `web/src/core/utils/viewProfile.ts` | 增加 FiltersJson/Insight schema、解析、迁移、未知字段保护与序列化 | 既有列、排序、chrome、mapping 结构及老 ColumnsJson 兼容 |
| `web/src/stores/viewProfile.ts` | 增加筛选域、页面 PageSize 读写、失败回滚和脏状态 | 400ms 合并保存、个人 profile API 和现有命名视图操作 |
| `web/src/views/crud/DefaultList.vue` | 解析 URL/保存条件、构造唯一 effectiveSearch、同参数加载 list/chart、渲染查询洞察面板、使用页面 PageSize | GetPage/字段分区、CRUD、分页、右侧 RecordDrawer |
| `web/src/views/crud/ViewTabsToolbar.vue` | 提供当前筛选保存/清除入口和来源提示 | 视图增删改切换 API |
| `web/src/views/crud/ViewConfigDrawer.vue` | 增加统计/图表两个独立开关 | 列、排序、chrome、mapping 已有配置 |
| `web/src/features/**/QueryInsightPanel.vue`（按实际目录） | 整合既有 SearchForm 与固定 stat/chart 结果区、空态、加载态和失败态 | 不承担任意查询编辑或图表渲染 |
| `packages/api-core/src/api.ts`、`types.ts` | getChartData 可选 params 与 ViewProfile.PageSize 线协议补充 | 既有无参数调用与 personal profile API |
| `NewLife.Cube/**Tests.cs`、`web/src/**/*.spec.ts`、`packages/api-core/**/*.spec.ts` | 补模型、JSON、store、URL、图表参数、PageSize 和组件用例 | OSC-0009 字段搜索序列化行为 |

## 3. JSON schema 与兼容

### 3.1 FiltersJson

```ts
interface SavedFiltersWire {
  version: 1
  views: Record<string, Record<string, unknown>>
}
```

- key 是 `NamedView.id`；值为经过 `serializeSearchValue` 后的平坦搜索参数。
- 缺失、空串、非对象、未知 version：归一为 `{ version: 1, views: {} }`。
- 读取时删除不存在于当前 searchFields 的 key、`undefined`、空字符串、空数组；`false`、`0` 合法且保留。
- 保存时只替换当前 view id 的完整筛选对象；清除时删除该 key。不得把 URL 参数写入 FiltersJson，除非用户以当前有效条件主动保存。

### 3.2 Insight

```ts
interface ViewInsight {
  showStat: boolean
  showChart: boolean
}
```

`insight` 缺失时归一为 `{ showStat: false, showChart: false }`；两个开关各自按 Boolean 归一，非法值为 false。两个均为 false 表示关闭；仅一个为 true 表示单项；均为 true 表示统计标签与一张图表同时显示。兼容早期草案 `mode`：`stat`→仅 showStat，`chart`→仅 showChart，`none`/其他→都关闭。`NamedView` 的未知顶层属性和未知 insight 扩展字段在 round-trip 时原样保留；本号只读写两个开关，避免为后续 OSC 丢弃 JSON。

### 3.3 PageSize

`ViewProfile.PageSize` 是 Int32 页面级持久字段，`0`/null 表示未配置。只接受 `PAGE_SIZE_OPTIONS` 已支持的正整数；非法、负数、过大值或非选项值归一为 0，不保存未定义自定义值。读取优先级为：合法 `ViewProfile.PageSize` → 合法旧 `UserProfile.workspace.pageSize` → 20。首次用户改变分页器条数时，保存至当前 typePath 的 ViewProfile，并停止调用 `patchWorkspace({ pageSize })`；旧全局值保留仅作未迁移页面的种子，不能被本号删除。

## 4. 状态与优先级

`DefaultList` 的唯一搜索状态是 `effectiveSearch`，每次来源或表单变化重新派生，不维护与 searchForm 并行的第二份请求状态。

| 条件 | effectiveSearch 来源 | 是否可保存 |
| --- | --- | --- |
| URL 中存在至少一个合法 search key | URL（包含合法值） | 是，点击保存后写入当前视图 |
| 无合法 URL key，当前视图有 saved filter | FiltersJson 当前 view | 是，可覆盖或清除 |
| 其余 | 空对象 | 是，保存空条件等价于清除 |

URL 只影响当前页面会话，不修改个人存储。切换命名视图后重新计算来源；分页、排序、查询洞察面板中的统计和图表、GetList 全部使用同一 effectiveSearch。用户在表单输入但未执行搜索时，是否提交由现有 `handleSearch` 规则决定；保存动作先以当前表单做同一正规化，再同步 searchForm/effectiveSearch 并 reload。

`pageSize` 是与 effectiveSearch 无关的页面状态：普通 table/tree/card 使用 `ViewProfile.PageSize`；kanban/calendar/gantt 继续调用既有 `resolveViewPageSize` 放大策略（200～既有上限），不得覆盖存储的普通页面偏好。用户在大视图修改分页器时，仅当当前 UI 实际允许选择标准 `PAGE_SIZE_OPTIONS` 值才更新 ViewProfile.PageSize；大视图自动放大不是用户偏好写入。

## 5. UI 及交互矩阵

固定页面顺序：`QueryInsightPanel → ViewTabsToolbar → active view → Pagination → RecordDrawer`。QueryInsightPanel 是一个视觉容器，不是第二套搜索状态；现有 SearchForm 的字段、submit/reset、校验和 emits 必须原样复用或等价迁入。

| 位置 | 行为 | 空/无权限/失败 |
| --- | --- | --- |
| 面板上部：搜索字段与操作 | 保留搜索、重置、“保存到此视图”与“清除默认筛选” | 无 active view 时保存/清除禁用并提示 |
| 面板上部：来源 | URL / 已保存 / 临时辅助文字 | 不显示内部 JSON 或字段值 |
| 面板下部：统计标签 | `showStat=true` 时读取当前列表响应 stat | 无 stat 时展示“暂无统计”而非编造 0 |
| 面板下部：固定图表 | `showChart=true` 时调 `getChartData(typePath, effectiveSearch)` | 404/403/错误显示非阻塞失败态，不影响列表或统计 |
| ViewConfigDrawer | 统计标签、固定图表两个开关，可同时开启 | 无图表端点权限时可保存；运行时只降级图表区域 |
| 分页条 | 用户选择标准 PageSize 时保存当前 typePath 的 ViewProfile.PageSize | 未配置回落全局种子；大视图自动放大不写入 |

当 `showStat=true && showChart=true` 时，统计标签先于图表，二者同属面板下部且只发起一次 GetList 与一次 GetChartData；切换筛选、视图或刷新时须以最新请求结果覆盖旧结果，不能把旧筛选 chart 覆盖到新筛选上。响应式：宽度 `< 768px` 时搜索字段、统计标签均换行，图表占满宽度；不引入侧栏、拖拽、浮动容器或第二个卡片边界。chart 的渲染仅使用既有项目中已支持的固定图表组件/数据格式；实施前须阅读 Arco 与 VTable 官方文档，若图表组件 API 不明确则限制为现有 chart 数据安全文本/固定视图，不虚构配置。

## 6. API 约定

`getChartData(type, params?)`：

- `params` 缺失时保持原 URL 与行为；
- params 通过既有 api-core query serializer 编码，数组遵循 GetList 当前约定；
- 只传 `effectiveSearch`，不传分页、排序、视图 UI 配置；
- 服务端 API 路径、权限和返回结构不变。

ViewProfile 个人 GET/PUT/POST 既有路径增加可选 `pageSize`（camelCase）线协议。缺失时返回/保持 null 或 0 的未配置语义；不得读取或写入全局 UserProfile 的 workspaceJson。

## 7. 核心文档影响

| 文档路径 | 影响 | 说明 |
| --- | --- | --- |
| `Doc/Api/ArcoVue企业中后台迁移方案.md` | 修改 | 事实性登记 OSC-0012、查询洞察面板和页面级 PageSize 边界 |
| `Doc/附录C_实体参考.md` | 修改 | ViewProfile.PageSize 的页面级语义 |
| `Doc/Api/核心接口架构.md` | 修改 | ViewProfile profile API 的 pageSize 契约 |
| `Doc/功能清单.md` | 修改 | 更新 SPA/DATA 对应实现与测试状态 |
| `NewLife.Cube.ArcoVue/web/README.md` | 修改 | 补命名视图默认筛选和洞察使用说明 |
| `Doc/附录B_API参考.md` | 评估 | 无新 API；仅既有 GetChartData 参数行为确有变化才最小补充 |

## 8. 测试设计

| 目标 | 自动化证据 |
| --- | --- |
| FiltersJson | 缺失/损坏/旧值、false/0、多视图隔离、非法字段清除 |
| 优先级 | URL 覆盖、saved fallback、空条件、保存后刷新 |
| Insight | 双开关/旧 mode 迁移、统计+图表组合、切换视图隔离、GetList 与 GetChartData 参数相等、竞态结果不回写 |
| PageSize | Cube.xml/xcode 映射、0/非法值、页面隔离、全局种子回退、大视图不污染个人偏好 |
| api-core | 无 params 兼容、含数组/特殊字符的 query 编码 |
| UI | 单一查询洞察面板、保存/清除、空 active view、空 stat、chart 失败、统计+图表、窄屏布局 |

## 9. 风险与回滚

| 风险 | 缓解 |
| --- | --- |
| FiltersJson 历史数据形态不一致 | 宽容读取、严格写 V1，损坏数据仅降级空条件 |
| URL 与保存筛选冲突 | 固定优先级并显示来源；URL 不自动写回 |
| 图表接口不可用 | 列表独立加载；洞察区非阻塞错误态 |
| 统计/图表异步竞态 | 按 effectiveSearch 请求标识丢弃过期响应；两个区域独立错误态 |
| 全局 PageSize 回归 | 只在页面 PageSize 缺失时读取 workspace 种子；分页器改变不再 patchWorkspace |
| XCode 生成遗漏 | 先改 Cube.xml 并生成实体/Model；XUnit 与构建验证 DTO/API 映射 |
| JSON round-trip 丢未来字段 | 解析时保留未知对象属性，只局部更新已知域 |
