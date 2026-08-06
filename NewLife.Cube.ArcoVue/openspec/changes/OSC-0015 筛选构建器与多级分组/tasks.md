# OSC-0015 Tasks

> 仅在进入 `Implementing` 后逐项勾选；完成每项先执行对应测试。

## T1 数据模型与序列化（viewProfile.ts / searchFilters.ts）

- [x] 1.1 在 `viewProfile.ts` 定义 `ViewFilter`、`ViewFilterCondition`、`ViewGroup` 类型；`NamedView` 增加 `filter?`/`group?`。
- [x] 1.2 实现 `normalizeFilter(raw)`：宽容解析、非法归一（logic/conditions/op/value 规则）、按可搜索字段集清理未知 field、round-trip 保留未知键。
- [x] 1.3 `serializeNamedView`/`parseNamedViews` 透传 filter/group 域（含 `_raw` 兼容）。
- [x] 1.4 在 `searchFilters.ts` 实现 `matchesViewFilter`（eq/neq/contains/notContains/isNull/notNull/gt/gte/lt/lte/after/before 全操作符、all/any、camelCase 容错）；`filterBuilder.ts` 提供字段类别 → 操作符矩阵。
- [x] 1.5 在 `viewMapping.ts` 实现 `groupRows(records, groupFields, fields, dataSource)` 与 `GroupNode`：单字段/多级分组、计数、dataSource 翻译、未分组项、未知字段回退。
- [x] 1.6 补 Vitest：ViewFilter 解析/归一/round-trip、字段类别/操作符矩阵、matchesViewFilter 全操作符、groupRows 分组/计数/回退。

## T2 筛选构建器 UI

- [x] 2.1 新增 `FilterBuilderPopover.vue`（a-popover 锚定「筛选」按钮）：字段下拉（可见字段∪人员字段）、操作符下拉（按字段类别矩阵）、值控件按类别渲染（人员用户下拉/枚举/数字/日期/字符）、条件行竖排增删、AND/OR 切换、互斥展开。
- [x] 2.2 接入 `DefaultList.vue`：「筛选」按钮打开构建器；`viewFilter` 并入 `effectiveSearch`（filter 覆盖同名字段、统一 clean）；「已筛选 N 条」标签与清除。
- [x] 2.3 store 新增 `patchActiveFilter`/读取当前视图 filter；应用/保存/清除/无 active view 交互矩阵落地。
- [x] 2.4 补组件/逻辑测试：构建器增删条件、逻辑切换、值控件、保存/清除、覆盖同名参数、any 降级提示、弹层互斥/关闭不丢弃编辑。

## T3 多级分组

- [x] 3.1 新增 `GroupPopover.vue`（a-popover 锚定「分组」按钮）：分组候选字段（listFields∩可分组）、有序增删（最多 3）、上移/下移、应用/保存/清除、与筛选弹层互斥。
- [x] 3.2 `ListTable.vue` 分组改用 **VTable 原生 `groupConfig.groupBy`**（参考官方 list-table-group-checkbox）：checkbox 置于 `rowSeriesNumber`（每行最前面）、`titleCheckbox: true` 组标题行 checkbox、`enableCheckboxCascade` 级联；组标题文本「📁 label (count)」（`titleFieldFormat` + dataSource 翻译）、`groupTitleStyle` 浅灰底；**不再用 tree/hierarchy 渲染分组**（tree 模式下 checkbox 列被自动置为 tree 列导致勾选异常）。
- [x] 3.3 `DefaultList.vue` 分组直接传原始行（`displayRows = treeRows`），传 `groupFields`/`groupLabelOf` 给 ListTable 由 VTable 内部分组；工具条分组按钮徽标/底纹/点击撤销。
- [x] 3.4 store 新增 `patchActiveGroup`；加载视图自动应用。
- [x] 3.5 补测试：groupRows 输入到 ListTable 组头/折叠、GroupPopover 上限与排序、弹层互斥、store 保存/回滚/隔离。
- [x] 3.6 分组后勾选框修复：`checkbox_state_change` 延后到宏任务遍历展示行读取状态（规避 VTable 内部级联监听注册时序导致的状态重置）；组标题 checkbox 与子行级联勾选/取消、表头全选/取消、数据行单独勾选均验证通过。

## T4 搜索面板折叠与 LOV 远程搜索

- [x] 4.1 `QueryInsightPanel.vue`：搜索字段容器默认一行，`offsetHeight > clientHeight` 判定溢出，显示「展开更多 N」/「收起」；字段变化重置展开态。
- [x] 4.2 `LovSelect.vue` LIST 模式：下拉远程搜索（输入关键字 → `fetchLovListData`，防抖 300ms，空输入首页）、已选标签可移除；ENUM 与「更多」入口不变。
- [x] 4.3 补测试：溢出判定/展开收起/字段变化重置；LovSelect 防抖/关键字参数/标签移除/ENUM 回归。

## T5 验证与文档

- [x] 5.1 执行 api-core 与 web 相关/新增 Vitest，修复后全过。
- [x] 5.2 执行 api-core、ArcoVue web 构建，无错误。
- [ ] 5.3 手工冒烟：筛选构建器保存→刷新→自动应用；AND/OR 生效与翻页完整；any 多条件降级提示；多级分组折叠/展开；搜索面板「展开更多」；LOV LIST 远程搜索；与「保存到此视图」并存不覆盖。
- [x] 5.4 最小同步迁移方案、功能清单、web README；评估附录B/C 是否需要事实性更新。
