# OSC-0015 Retro

## 结果摘要

- 状态：已完成（验收 + 复盘，2026-08-06；终态 Done）。
- 计划：可视化筛选构建器（多条件 AND/OR、操作符按字段类别开放）+ 多字段多级分组（表格内组头行可折叠）+ 搜索面板一行折叠 + LOV LIST 远程搜索；方案随命名视图保存到 ViewProfile；纯前端零后端改动。
- 不做：拖拽排序、任意查询表达式、跨实体数据源、多级 AND/OR 嵌套组、分组跨页聚合、折叠状态持久化。

## 实际结果

- **代码范围（纯前端）**：
  - `viewProfile.ts`：`NamedView` 增 `filter`/`group` 域；`ViewFilter`/`ViewFilterCondition`/`ViewGroup` 类型；`normalizeFilter`/`normalizeGroup`（宽容解析、非法归一、值规则 `false`/`0` 合法、未知字段保留）；序列化透传。
  - `searchFilters.ts`：`matchesViewFilter` 全 12 操作符（eq/neq/contains/notContains/isNull/notNull/gt/gte/lt/lte/after/before）、all/any 合并、camelCase 容错、`resolveStatEntries`。
  - `filterBuilder.ts`：`resolveFieldFilterKind`（人员→枚举→数字→日期→字符）、`FILTER_OPS_BY_KIND` 操作符矩阵、draft↔filter 转换纯函数。
  - `viewMapping.ts`：`groupRows`/`GroupNode`/组头工具与分组草稿操作（`GROUP_FIELDS_LIMIT=3`、push/move/remove/nextGroupFieldNames）。
  - `FilterBuilderPopover.vue` / `GroupPopover.vue`（新增）：弹层构建器，字段/操作符/值控件按类别（人员用户下拉、枚举 dataSource、LovSelect、数字、日期、字符），条件行增删、AND/OR、有序分组、应用/保存/清除/取消。
  - `DefaultList.vue`：两弹层互斥（`activePopover` 单一 ref）、`filterFields`=可见列∪人员字段、loadData 后 `matchesViewFilter` 兜底过滤 + total 纠正、徽标/清除标签、分组禁用树状视图、交互矩阵（应用即持久化、保存不刷新、清除写空方案）。
  - `stores/viewProfile.ts`：`updateFilter`/`updateGroup`/`getFilter`/`getGroup`（`patchActiveFilter`/`patchActiveGroup`，400ms debounce + 失败回滚）。
  - `ListTable.vue`：**VTable 原生 `groupConfig.groupBy` + `rowSeriesNumber` checkbox**（重构追加工单）；`titleCheckbox`/`enableCheckboxCascade` 级联；组标题文本「📁 label (count)」+ dataSource 翻译；`groupTitleStyle` 浅灰底；勾选态宏任务延后读取。
  - `QueryInsightPanel.vue`：搜索字段一行折叠「展开更多 N」/「收起」，字段变化重置。
  - `LovSelect.vue`：LIST 远程搜索（防抖 300ms 携带关键字）、已选标签移除。
- **偏差（2 项，均已收口）**：
  1. **筛选实现从「并入后端请求」演进为「纯前端过滤」**（proposal 决策 9 / design §1/§3.2）：初版 AC-02/03/04 描述并入请求 + `_min/_max` 范围 + OR 前端降级，实施中改为条件不并发、对已加载数据本地 `matchesViewFilter` 过滤（业务重写 Search / 树控制器兜底）。**verify.md 验收标准已在验收时对齐更新**。
  2. **分组从「tree/hierarchy 组头行」重构为「VTable 原生 groupBy」**：tree 模式下 checkbox 列被 VTable 自动置为 tree 列导致勾选异常（追加工单，见下）。
- **测试证据**：web Vitest 267/267 通过（ViewFilter 归一/round-trip、操作符矩阵、matchesViewFilter 全操作符含 gte/lte 空值边界、groupRows、分组草稿、store filter/group、默认视图名映射等）；`vue-tsc` + vite 构建 0 错误；api-core 未改动。
- **浏览器冒烟**：筛选构建器「类型=公司」6→2 条 + 徽标 + 刷新持久化 + 清除恢复；分组组标题「📁 公司 (2)」+ checkbox 级联 + 折叠/展开（rowCount 4→2→4）；搜索面板「展开更多 N」；与「保存到此视图」并存。冒烟数据已清理。
- **文档**：`tasks.md` 全勾选（含追加工单 T6 补录）、`verify.md` 验收标准对齐并勾选（含 AC-11/12/13）、`status.md` 置 Done、`Doc/功能清单.md` SPA-18 与 `ArcoVue企业中后台迁移方案.md` §8.2.5/§10.4 旧规划语义按最终实现修正、本 retro、`harness/lessons.md` 追加。

## 实施期关键问题与修复

1. **分组后勾选框不可用（追加工单，`3874b117`）**：分组改用 tree/hierarchy 渲染时，VTable 把 checkbox 列自动置为 tree 列 → checkbox 图标不渲染、勾选态与选中集不同步。
   - 根因定位：官方 groupBy demo 用 `rowSeriesNumber` checkbox + `groupConfig`；tree 模式与 checkbox 列语义冲突。
   - 修复：改用 VTable 原生 `groupBy` + `rowSeriesNumber(cellType/headerType:'checkbox')` + `titleCheckbox` + `enableCheckboxCascade`；`checkbox_state_change` 延后到宏任务遍历展示行读状态（规避 VTable 内部级联监听注册时序导致的状态重置）。
2. **筛选应用后请求未过滤（`9550ce15`/`95d4e8d6`）**：业务重写 Search（如 Department 仅处理 id/parentId/enable/visible）不应用通用等值过滤 → 前端兜底 `matchesViewFilter` 过滤 + total 纠正，保证筛选在任何控制器下生效。
3. **徽标底色透明（追加工单，`efb3bec4`）**：`--color-primary-6` 为 Arco 未定义变量导致透明，改用主题 Primary 色 `--cube-primary`。
4. **验收三步编排发现并修复（首次验收未通过 → 修复 → 重验通过）**：
   - `gte`/`lte` 空值行误判命中（`compareValues` 空值返回 `'na'`，`'na' !== 'lt'` 恒真 → 空值行被纳入「>=/<=」，与 `isNull` 语义冲突）→ 改为 `cmp === 'gt' || 'eq'` 并补空值边界单测；
   - 树视图残留分组字段破坏树渲染（表格配过分组切树视图时 `group-fields` 未按 `isGrouped` 门控 → ListTable 进 groupedMode 跳过 hierarchy）→ `:group-fields="isGrouped ? viewGroup : []"`；
   - `toDataField` 仅首字母小写，全大写缩写字段（`ID`/`URL`）与后端 camelCase 数据 key 不匹配 → 完整 .NET camelCase；
   - LOV 远程搜索并发无序号保护 + 卸载未清防抖 timer + `loadMeta` 重复调用 → `loadSeq` 序号 + `onBeforeUnmount` 清理 + 去重；
   - 文档未同步（功能清单 SPA-18 / 迁移方案 §8.2.5/§10.4 残留「并入搜索请求」「table/tree 树渲染」旧语义）→ 最小增量修正。

## 经验沉淀候选

- **筛选与搜索职责分离**：搜索 = 向后端取数的查询条件（并入请求）；筛选 = 对已返回数据的前端过滤。二者叠加生效，筛选为空时请求与基线完全一致，回归安全。
- **纯前端过滤规避后端算子差异**：操作符矩阵按字段类别开放，不依赖后端 `Search(Pager)` 支持任意算子；对重写 Search 的控制器天然兜底。
- **受控组件值写回**：Arco `a-select` 用 `:model-value` + `@update:model-value` 受控写法；冒烟发现点击下拉选项需作用域到当前打开的 popup，避免多下拉（搜索面板/构建器）同名选项误选——测试/冒烟脚本需注意。
- **VTable 分组勾选**：分组场景 checkbox 必须走 `rowSeriesNumber` + `groupConfig`（原生 groupBy），勿用 tree/hierarchy 渲染分组；级联状态读取需延后宏任务。
- **验收文档需随实现演进同步**：verify.md 初版基于「并入后端」方案，实现改为纯前端后若不同步更新会产生「按旧标准验收失败」的误判。验收前应先对齐验收标准与最终实现。
- **比较运算符对空值必须显式语义**：`gte`/`lte` 用「非 lt/非 gt」反推会把空值（`'na'`）误判为命中；`>=`/`<=` 应显式 `==='gt'||'eq'` / `==='lt'||'eq'`，与 `gt`/`lt` 严格对称，并补空值边界单测。
- **视图门控要落到数据传递层**：UI 隐藏按钮不等于状态不会残留——`group-fields` 这类跨视图数据传递必须按当前视图能力（`isGrouped`）过滤，否则表格配置切树视图时残留分组字段破坏树渲染。
- **字段名转换对齐后端序列化**：前端将 PascalCase 字段名匹配后端 camelCase 数据 key 时，须按后端序列化策略（.NET JsonNamingPolicy.CamelCase）实现，仅首字母小写处理不了全大写缩写（`ID`/`URL`）。
- **异步请求序号与卸载清理**：防抖/远程搜索类组件需 `seq` 丢弃过期响应 + `onBeforeUnmount` 清理 timer，避免慢响应覆盖新结果与写已卸载组件。

## 后续建议

- **分页 + 前端复核筛选的完整性**：对业务重写 Search 且数据多页的控制器，前端仅过滤当前已加载页，跨页可能见未过滤数据。建议后端支持筛选参数，或对业务控制器场景大 pageSize 拉取后前端全量过滤（性能换正确性）。
- **组件级测试基础设施**：当前无 `@vue/test-utils`，筛选构建器/分组弹层交互依赖手工冒烟。建议引入 `@vue/test-utils` + happy-dom 补关键组件用例（与迁移方案 §10.4 差距 6 对齐）。
- 多级分组（2~3 字段）组合场景的浏览器冒烟可补充（当前冒烟覆盖单字段分组）。
- 筛选条件数较多时（>3）构建器性能与可用性可评估优化（当前 max-height 滚动）。
- 分组/筛选方案与模板（OSC-0014）、默认视图（保存为默认视图）的联动（管理员发布含筛选/分组的模板视图）可后续验证。
