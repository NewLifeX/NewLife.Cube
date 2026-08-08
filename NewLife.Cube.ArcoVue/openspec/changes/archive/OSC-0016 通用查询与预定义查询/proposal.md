# OSC-0016 — 通用查询与预定义查询

## 1. 为何做

ArcoVue 多维视图（表格/树/卡片/看板/日历/甘特六视图）顶部 `QueryInsightPanel` 的搜索能力存在四处缺口：

1. **魔方保留参数缺位**：后端默认 `Search(Pager)` 内置 `Q`（全字符串字段模糊）与 `dtStart/dtEnd`（主时间区间）两个通用查询参数，但搜索面板没有对应控件，用户无法使用。
2. **"假范围"缺陷**：前端日期/数值/时间搜索控件以 `字段_min/字段_max` 提交，后端默认 `Search` 不消费这些键（静默忽略），搜索不生效。
3. **Map 外键字段搜索无候选**：GetPage search 分区中的 Map 字段（如 `RoleID`）在搜索面板只是文本框，无法下拉选择关联值。
4. **查询条件无组合管理**：用户反复使用的查询组合无法保存为预定义查询复用。

此外，`wwwroot` 构建产物中存在源码没有的孤儿组件 `CascaderSearchPanel`（源码已删、bundle 残留），需一并清理。

本号在**不改动 `Search(Pager)` 查询逻辑**的前提下，扩展后端元数据层与 LOV 链路，并在 `QueryInsightPanel` 内补齐通用查询与预定义查询管理。

## 2. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | 面板形态：在现有 `QueryInsightPanel` 内增强，不新建面板级组件；仅新增子组件 `QueryComboButton`。 |
| 2 | 能力边界：仅 GetPage search 分区字段的**简单等值查询**（后端 `Search` 已内置字段名=值 Equal），加上 `Q` 与 `dtStart/dtEnd` 两个保留参数；不引入操作符/表达式构建（操作符查询由 OSC-0015 筛选构建器承担，纯前端过滤，保持并存）。 |
| 3 | 第一行末尾固定控件顺序：`主时间范围（dtStart/dtEnd）` → `关键字（Q）` → `查询组合按钮`；三者不参与「展开更多 N」溢出折叠测量，始终可见。 |
| 4 | `Q` 标签「关键字」，placeholder「全字段模糊搜索」，回车执行查询；`PageSetting.EnableKey == false` 时不渲染。 |
| 5 | 主时间范围标签 = `Factory.MasterTime` 的 DisplayName（后端 GetPage setting 输出），控件为 show-time 日期范围选择器；实体无 MasterTime 时不渲染。 |
| 6 | **查询组合按钮**位于 Q 输入框之后，下拉菜单提供：执行查询、预定义查询列表（点击应用）、保存当前查询为预定义查询、重命名当前查询、删除当前查询、清空查询参数。 |
| 7 | 预定义查询为**实体级（typePath 粒度）个人配置**，存 ViewProfile 新列 `QueriesJson`；不走模板域（OSC-0014 模板不携带预定义查询）。 |
| 8 | 后端改造仅限**元数据层与 LOV 链路**：Map 字段搜索候选自动填充（小表内联 dataSource / 大表自动注册 `Entity.` 前缀 LOV LIST）、`LovController` 支持内部实体数据源、GetPage setting 增补 MasterTime 信息、ViewProfile 增列。**不改 `Search(Pager)` 查询逻辑**。 |
| 9 | 现存无效 range 控件改为**单值等值控件**：日期→date-picker、日期时间→date-picker(show-time)、数值→input-number、时间→time-picker；`searchFilters.ts` 删除 `_min/_max` 机制。 |
| 10 | `CascaderSearchPanel` 清理纳入本号：确认源码无引用后重新构建 wwwroot，使 bundle 不再携带该孤儿组件。 |
| 11 | 仅 ArcoVue 前端；Cube.Vue 等其他 SPA 不在本号范围。 |

## 3. 做什么

### 后端（NewLife.Cube / NewLife.CubeNC 共享源码）
- `Cube.xml` ViewProfile 表新增 `QueriesJson` 列，重新生成实体，`ViewProfileModel` 与 `UpsertForUser` 同步（`/Cube/ViewProfile` GET/PUT 自动透传，依赖既有大小写不敏感绑定）。
- `FieldCollection` Search 分支：Map 字段自动填充搜索候选——目标表行数 ≤ `CubeSetting.MaxDropDownList` 时内联 `DataSourceMap`（MapProvider.GetDataSource），否则置 `LovCode = "Entity.{目标实体全名}"`；手工已设 LovCode/DataSourceMap 优先不覆盖；行数判断走 MemoryCache 60s。
- `LovAutoRegisterService` 扩展：为 `Entity.` 值集自动注册 `LovDefinition(Type=LIST, Source=AUTO)` + `LovListConfig(RequestUrl="entity:{EntityTypeName}")`。
- `LovController`：`ListData`/`BatchLabel` 识别 `entity:` 协议走内部 EntityFactory 分页 + Q 模糊查询（不经 HTTP）；仅已注册 EntityFactory 的实体可查，权限沿用现有 Detail 授权。
- `ReadOnlyEntityController.GetPage`：setting 输出增补 `masterTimeName`/`masterTimeDisplayName`（MasterTime 为 null 不输出）。
- NC 守卫：`_Common_List_Search.cshtml` 消费 DataSourceMap 时键数超 `MaxDropDownList` 退回文本框。

### 前端（ArcoVue）
- `fieldControl.ts`/`SearchFieldInput.vue`：range 控件改单值等值；`searchFilters.ts` 删 `RANGE_CONTROLS`/`_min/_max`，`collectSearchKeys` 增加 `Q/dtStart/dtEnd` 保留键。
- `QueryInsightPanel.vue`：第一行末尾追加主时间范围、关键字、`QueryComboButton` 三个固定控件；`showSearchPanel` 放宽（三者任一可用即显示）。
- `QueryComboButton.vue`（新增）：执行/应用/保存/重命名/删除/清空的下拉菜单与命名弹窗。
- `viewProfile.ts`/`stores/viewProfile.ts`：QueriesJson wire 解析/归一/序列化与 store 动作（saveQueryAs/renameQuery/deleteQuery/applyQuery），随既有 debounce 保存链路 PUT。
- `DefaultList.vue`：searchForm 并入 `Q/dtStart/dtEnd` 与 effectiveSearch；setting 透传；`appliedQueryId` 会话状态管理。
- 清理 `CascaderSearchPanel`：重新构建 wwwroot 消除孤儿组件。

## 4. 不做什么

- 不改 `Search(Pager)` 默认查询逻辑（等值/Q/dtStart/dtEnd 均已内置）。
- 不引入字段级操作符/范围/IN 多值的服务端通用解析（后端多值/区间能力另立 OSC）。
- 不改 OSC-0015 筛选构建器（保持纯前端过滤，与本号并存互不覆盖）。
- 预定义查询不进模板域、不做共享/公开。
- 不改 URL 写回策略（URL 只读，Q/dt 参数可经 URL 读入但不写入 URL）。
- 不动 `CascaderField.vue`（地区级联组件，与孤儿 `CascaderSearchPanel` 无关）。
- 不改 Cube.Vue / NaiveUI 等其他前端。

## 5. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-0009 | Done：搜索字段元数据、控件解析基线 |
| OSC-0012 | Done：effectiveSearch 唯一搜索状态、FiltersJson、QueryInsightPanel |
| OSC-0014 | Done：NamedView/线协议 round-trip 保留未知字段 |
| OSC-0015 | Done：筛选构建器（并存）、搜索面板一行折叠、LOV LIST 远程搜索 |
| LOV 体系 | LovController Meta/ListData/BatchLabel、LovAutoRegisterService 枚举自动注册基线 |

## 6. 测试范围

| 类型 | 是否做 | 说明 |
| --- | --- | --- |
| XUnit | 是 | Map 候选（小表内联/大表 LovCode/手工优先）、`entity:` ListData 分页与 Q、GetPage setting masterTime、ViewProfile QueriesJson upsert |
| ArcoVue Vitest | 是 | resolveSearchControl 单值映射、collectSearchKeys 保留键、QueriesJson 解析/归一/round-trip、store queries 动作 |
| 构建 | 是 | api-core 与 ArcoVue web 无错误构建（vue-tsc + vite） |
| 手工冒烟 | 是 | 查询组合全流程、预定义查询刷新恢复、Map 下拉、Q/dt 生效、NC 搜索栏回归、bundle 清理验证 |

## 7. 成功标准

- [ ] 面板第一行末尾依次为主时间范围、关键字、查询组合按钮，控件渲染条件符合决策矩阵。
- [ ] 查询组合按钮可执行查询；可保存/应用/重命名/删除预定义查询、清空参数；预定义查询随 ViewProfile 持久化，刷新恢复。
- [ ] 日期/数值/时间搜索字段为单值等值控件，提交后后端 Equal 命中（不再有静默无效的 `_min/_max`）。
- [ ] Map 字段在搜索面板自动出候选：小表内联下拉、大表 LOV 远程搜索。
- [ ] `wwwroot` 与源码均无 `CascaderSearchPanel`。
- [ ] 本 OSC 新增单测全部通过，相关构建无错误，事实性文档完成最小同步。
