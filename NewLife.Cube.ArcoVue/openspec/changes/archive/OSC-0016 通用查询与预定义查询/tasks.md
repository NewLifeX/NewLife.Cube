# OSC-0016 Tasks — 通用查询与预定义查询

> 依赖顺序：T1~T5 后端可并行；T6 api-core；T7~T10 前端（T9 依赖 T6/T8，T10 依赖 T7/T9）；T11 清理依赖前端构建；T12 收尾。
> 每项含补测/跑测勾选；执行阶段必须跑单元测试。

## 后端

- [x] **T1 ViewProfile 增列 QueriesJson**
  - `Cube.xml` ViewProfile 表 `FiltersJson` 后加 `QueriesJson` 列（String/-1）
  - xcode 重新生成 `视图配置.cs`；`ViewProfileModel` 加属性；`UpsertForUser` 加赋值行
  - 补 XUnit：upsert 读写一致、null 不覆盖
  - [x] 编译通过 [x] 测试通过
- [x] **T2 Map 字段搜索候选自动填充**
  - `FieldCollection.cs` Search 分支新增 `FillMapCandidates`（小表内联 DataSourceMap / 大表 `Entity.` LovCode / 手工优先 / 行数 MemoryCache 60s）
  - 补 XUnit：小表/大表/手工优先三分支
  - [x] 编译通过 [x] 测试通过
- [x] **T3 内部实体 LOV LIST（`entity:` 协议）**
  - `LovAutoRegisterService` 扫描 Map 目标实体注册 `Entity.` 值集（LovDefinition + LovListConfig）
  - `LovController.FetchRemoteList` 增 `entity:` 内部分支（分页 + Q 模糊 + 未注册实体抛异常）；BatchLabel 复用
  - 补 XUnit：分页/Q/未注册异常
  - [x] 编译通过 [x] 测试通过（注册逻辑 XUnit 覆盖；`entity:` 内部查询分支由 API 版 `LovController` 承载，XUnitTest 引用 NC 合并版不含该控制器，查询行为留待 verify 冒烟 AC-13）
- [x] **T4 GetPage setting 增补 MasterTime**
  - `ReadOnlyEntityController.GetPage` setting 输出 `masterTimeName`/`masterTimeDisplayName`（无 MasterTime 不输出）
  - 补 XUnit：有/无 MasterTime 两分支（NC 版 GetPage 同步增补）
  - [x] 编译通过 [x] 测试通过
- [x] **T5 NC 搜索模板守卫**
  - `_Common_List_Search.cshtml`：DataSourceMap 键数 > MaxDropDownList 退回文本框
  - [x] 编译通过 [ ] NC 冒烟（并入 T12）

## api-core

- [x] **T6 ViewProfileModel 类型增 queriesJson**
  - `packages/api-core/src/types.ts` 加 `queriesJson?: string | null`（并增 PageSetting masterTimeName/masterTimeDisplayName）
  - 跑 api-core 既有测试 + 构建
  - [x] 测试通过 [x] 构建通过

## 前端

- [x] **T7 单值等值控件与保留键**
  - `fieldControl.ts` resolveSearchControl：date/datetime/number/time 单值映射，删 range 判定
  - `SearchFieldInput.vue` 四个 range 模板替换为单值控件
  - `searchFilters.ts`：删 `RANGE_CONTROLS`/`_min/_max`；`RESERVED_SEARCH_KEYS=['Q','dtStart','dtEnd']` 并入 collectSearchKeys
  - 更新/补充 Vitest：控件映射、保留键、无 `_min/_max`
  - [x] 测试通过 [x] 构建通过
- [x] **T8 QueriesJson wire 工具**
  - `viewProfile.ts`：`SavedQuery`/`SavedQueriesWire` 类型、`parseQueriesWire`/`serializeQueriesWire`/`normalizeSavedQuery`（归一顺序见 design §3.1）
  - 补 Vitest：坏 JSON/重复 id/空 name/空 params/round-trip
  - [x] 测试通过
- [x] **T9 store 预定义查询动作**
  - `stores/viewProfile.ts`：entry 增 queries/activeQueryId；加载解析 personal queriesJson（不做模板回退）；saveQueryAs/renameQuery/deleteQuery/applyQuery/clearActiveQuery；scheduleSave payload 含 queriesJson
  - 补 Vitest：四动作状态变化、PUT payload
  - [x] 测试通过
- [x] **T10 面板控件与 QueryComboButton**
  - 新增 `QueryComboButton.vue`（UI 规格见 design §5，组件无状态）
  - `QueryInsightPanel.vue`：第一行末尾固定三项（主时间范围/关键字/查询组合按钮），不进溢出测量；showSearchPanel 放宽；props 增 masterTimeName/masterTimeDisplayName/enableKey 及 combo 相关
  - `DefaultList.vue`：searchForm 增 Q/dt 键；setting 透传；combo 事件接线（apply/save/rename/delete/clear）
  - Arco 组件实现前查阅官方文档（Dropdown/Modal/RangePicker 等）
  - **执行期面板重构**：两行布局（主行=前 N 字段 + 主时间/Q/查询按钮，第二行=其余字段默认收起，高度随展开自动调整）；原「展开更多 N/收起」折叠按钮与「搜索/重置/保存到此视图/清除默认筛选/未保存筛选」操作区并入「查询」组合按钮菜单（重置/展开收起/保存到此视图/清除默认筛选）；`DefaultList` 移除 source/sourceLabel
  - [x] 测试通过（逻辑层）[x] 构建通过

## 清理与收尾

- [x] **T11 CascaderSearchPanel 核查**（design §7「孤儿组件清理」经核实为误判，见下）
  - [x] grep 确认 `web/src`、`packages/**` 源码零引用
  - [x] `npm run build` 重新生成 wwwroot
  - [~] grep 确认 `wwwroot/assets/**` 无 `CascaderSearchPanel` 命中 —— **不成立**：该组件为 Arco Design Vue 库 `cascader-search-panel` 内部组件（`@arco-design/web-vue/es/cascader/`），被 `CascaderField.vue` 使用的 `a-cascader` 正常引用，非项目孤儿组件，无法也不应清理；AC-15 已按事实修正（见 verify.md）
  - [x] **保留区**：`CascaderField.vue` 与 `cascader` 控件类型未删除
  - [x] 构建通过 [x] 清理验证通过（源码零引用 + Arco 库归属确认）
- [x] **T12 验收与文档同步**
  - 全量门禁：api-core test/build、web test/build、后端编译 + 相关 XUnit
  - 手工冒烟（verify.md 清单）
  - 文档最小同步：`web/README.md`、`Doc/功能清单.md`、`ArcoVue企业中后台迁移方案.md`（实际位于 ArcoVue 目录）、`Doc/Api/核心接口架构.md`
  - [x] 门禁全绿 [ ] 冒烟通过（留待 openspec-verify） [x] 文档同步

## 会话小任务补录（openspec-verify，2026-08-08）

- [x] **T13 UserController 搜索兼容标准字段名**（执行期会话直接完成，不在原 design 计划内）
  - `NewLife.Cube/Areas/Admin/Controllers/UserController.cs`：重写 `Search(Pager)`——兼容搜索抽屉提交的 GetPage search 标准字段名（`roleIds↔RoleID`、`departmentId↔DepartmentID`、`enable↔Enable`、`q↔Q`），并新增通用字段等值过滤循环（跳过已处理字段）；删除废弃注释块
  - 保留原 XCode.User.Search 语义（roleIds 匹配 RoleID 或 RoleIds 包含、LastLogin 时间范围、Name/DisplayName/Mobile/Mail 关键字模糊）
  - [x] 编译通过 [x] 测试通过（回归由全量 XUnitTest 兜底）
- [x] **T14 面板两行重构**（执行期会话直接完成，已并入 T10 执行记录）
  - `QueryInsightPanel.vue` 删除，拆为 `InsightPanel.vue`（两行布局：主行=前 N 字段+主时间/Q/查询按钮，第二行=其余字段默认收起）+ `SearchDrawer.vue`（搜索抽屉宿主）；`QueryComboButton.vue` 菜单增「重置/展开更多条件（N）/收起/保存到此视图/清除默认筛选」
  - 原「展开更多 N/收起」折叠按钮与「搜索/重置/保存到此视图/清除默认筛选/未保存筛选」操作区并入「查询」组合按钮
  - 主行字段数 N 由不可见测量容器按宽度累加计算（ResizeObserver 响应容器尺寸）；面板高度随收起/展开自动调整
  - `DefaultList.vue` 移除 source/sourceLabel 传参与 searchSource/searchSourceLabel computed
  - [x] 测试通过（逻辑层）[x] 构建通过
- [x] **T15 GetPage 元数据扩展**（执行期会话直接完成，并入 T4/T6）
  - api-core `PageSetting` 类型增 `masterTimeName`/`masterTimeDisplayName`；NC 版 GetPage setting 同步增补（NC ReadOnlyEntityController.cs L999-1015）
  - [x] 构建通过
