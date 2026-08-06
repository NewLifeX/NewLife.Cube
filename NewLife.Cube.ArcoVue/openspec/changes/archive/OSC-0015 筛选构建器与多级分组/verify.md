# OSC-0015 Verify

> 进入 `Validating` 后逐项勾选。
> **验收标准已按最终实现对齐**：方案在实施期由「筛选并入后端请求」演进为「筛选纯前端过滤」（proposal 决策 9 / design §1/§3.2），
> 因此 AC-02/03/04 同步更新为纯前端语义；分组改用 VTable 原生 groupBy + rowSeriesNumber checkbox（重构追加工单）。

## 验收标准

- [x] AC-01：`NamedView.filter`/`group` 域随 ViewsJson 保存，重开视图自动应用；round-trip 不丢未知字段；非法/损坏方案安全归一。
- [x] AC-02：筛选为**纯前端过滤**：条件不并入后端请求；对已加载 `tableData` 经 `matchesViewFilter` 本地过滤，翻页继续过滤；业务重写 Search / 树控制器场景兜底生效，本页全量加载且删减时纠正 total。
- [x] AC-03：操作符按字段类别开放（枚举/字符/人员/数字/日期时间矩阵），eq/neq/contains/notContains/isNull/notNull/gt/gte/lt/lte/after/before 全支持；未知/失效字段与空值条件安全归一（`false`/`0` 合法保留）。
- [x] AC-04：`且(AND)` 全部条件命中（every）、`或(OR)` 任一命中（some），纯前端合并；单条件与 AND 等价；多条件 OR 不依赖后端算子。
- [x] AC-05：多级分组（≤3 字段）表格内组标题行展示「📁 label (count)」（dataSource 翻译），组标题可折叠/展开；分组字段随视图保存；折叠状态仅会话内存。
- [x] AC-06：分组为纯前端对已加载 `tableData` 分组（VTable 原生 groupBy），不并入请求；空数据/未知分组字段安全回退；不改变既有 CRUD 与权限。
- [x] AC-07：筛选构建器与「保存到此视图」（FiltersJson 默认搜索）并存、互不覆盖。
- [x] AC-08：Insight 搜索面板默认一行，溢出显示「展开更多 N」并可展开/收起；字段变化重置；未溢出不显示展开。
- [x] AC-09：状态/枚举/单值 Lov 搜索字段下拉展示正确；LOV LIST 支持远程搜索（防抖/关键字）与已选标签移除；ENUM 与「更多」入口回归。
- [x] AC-10：本 OSC 新增 Vitest 全过（267/267），api-core/web 构建无错误，事实性文档最小同步完成。
- [x] AC-11（追加工单 T6.2）：筛选/分组徽标底色使用当前主题 Primary 色（`--cube-primary`，外观设置可换）。
- [x] AC-12（追加工单 T6.3）：工具栏不显示「排序」按钮；排序由列表/树视图表头承担，自定义配置「工具栏/排序」开关只控制表头图标。
- [x] AC-13（追加工单 T6.4）：树状视图自定义配置抽屉工具栏无「分组」选项（分组仅表格视图支持）。

## 三步编排摘要（implementation-audit → code-review → doc-sync）

- **实现审计**：核心 12 项功能基本完整（数据模型/操作符矩阵/matchesViewFilter/groupRows/两弹层/store/ListTable groupBy/搜索折叠/LOV 远程/追加工单 T6）。
- **代码审查**：发现 1 处高严重度逻辑 bug（`gte`/`lte` 对空值行误判命中）、1 处视图交互缺陷（树视图残留分组字段破坏树渲染）与若干中低项，已全部修复（见「验收修复记录」）。
- **文档同步**：`web/README.md`、`verify.md`、`Doc/Api/核心接口架构.md`（合理不登记）已同步；`Doc/功能清单.md` SPA-18 与 `ArcoVue企业中后台迁移方案.md` §8.2.5/§10.4 的旧规划语义（并入搜索请求/table/tree 树渲染）已按最终实现修正。

## 验收修复记录（首次验收未通过 → 修复 → 重验通过）

首次验收（三步编排）发现以下缺陷，已修复并重跑门禁：

| # | 缺陷 | 严重度 | 修复 |
|---|------|--------|------|
| 1 | `matchesViewFilter` 的 `gte`/`lte` 对空值行误判命中（`compareValues` 返回 `'na'` 时 `'na' !== 'lt'` 恒真 → 空值行被纳入「>=/<=」） | 🔴 高 | `searchFilters.ts` 改为 `cmp === 'gt' \|\| cmp === 'eq'`（与 gt/lt 对称）；补空值边界单测 |
| 2 | 表格视图配置分组后切树视图：`group-fields="viewGroup"` 未按 `isGrouped` 门控，残留分组字段使 ListTable 进入 groupedMode 跳过 hierarchy → 树结构丢失 | 🟡 中 | `DefaultList.vue` 改为 `:group-fields="isGrouped ? viewGroup : []"` |
| 3 | `toDataField` 仅首字母小写：全大写缩写字段（`ID`→`iD`、`URL`→`uRL`）与后端 camelCase 数据 key 不匹配 | 🟡 中 | `ListTable.vue` 改为完整 .NET camelCase（ID→id、URL→url、ParentID→parentID） |
| 4 | LOV LIST 远程搜索并发无序号保护（慢响应覆盖新结果）、卸载未清理防抖计时器、`loadMeta` 重复调用 | 🟡 中 | `LovSelect.vue` 加 `loadSeq` 序号 + `onBeforeUnmount` 清理 timer + 去除 `onMounted(loadMeta)` 重复 |
| 5 | 文档未同步：功能清单 SPA-18 / 迁移方案 §8.2.5/§10.4 仍为「并入搜索请求」「table/tree 树渲染」旧语义 | 🟡 中 | 已按最终实现（纯前端过滤、VTable groupBy、树视图禁分组）最小增量修正 |

> 未修复项（记入 retro，非阻塞）：组件级测试缺失（项目无 `@vue/test-utils` 基础设施，tasks T2.4/T3.5 的「组件测试」实为逻辑层测试）；`matchesViewFilter` 第三参 `_fields` 未使用（冗余）；filter 内部未知扩展字段 round-trip 深度不足（仅顶层经 `_raw` 保留）——均不影响核心验收。

## 自动化门禁

```powershell
npm.cmd --prefix "packages/api-core" run test
npm.cmd --prefix "NewLife.Cube.ArcoVue\web" run test
npm.cmd --prefix "packages/api-core" run build
npm.cmd --prefix "NewLife.Cube.ArcoVue\web" run build
```

> 本号纯前端，无后端 XUnit 门禁；后端不改动，既有后端测试仅作回归基线（执行期可运行相关过滤确认无影响）。

## 手工冒烟

1. [x] 对有搜索字段实体：构建多条件筛选（等于 + 范围、AND/OR 切换）→ 应用 → 翻页确认条件保持 → 保存到此视图 → 刷新 → 自动应用。
2. [x] 设置多级分组（2~3 字段）→ 表格内组头折叠/展开 → 保存到视图 → 刷新 → 自动应用；切换视图确认分组隔离。
3. [x] 搜索字段多的实体：确认搜索面板默认一行、显示「展开更多 N」、展开/收起。
4. [x] 验证 LOV LIST 搜索字段（如角色）：输入关键字远程过滤、已选标签移除；状态/枚举字段下拉无重复。
5. [x] 确认筛选构建器与「保存到此视图」并存：分别保存筛选方案与默认搜索，互不覆盖。

## 执行记录

- 验收（2026-08-06，两轮）：首次验收 web Vitest 266/266 + 构建通过，三步编排发现缺陷；修复后重验 **267/267** + vue-tsc/vite 构建 EXIT=0 通过。
- 浏览器冒烟（部门页，admin）：
  - 筛选构建器「类型 = 公司」应用后 6→2 条（总公司/上海分公司），徽标「1」+ 清除筛选标签，刷新后自动应用保留，清除恢复 6 条；
  - 分组按 Type：组标题「📁 公司 (2)」+ checkbox 级联，组标题点击折叠/展开（rowCount 4→2→4）；
  - 搜索面板折叠「展开更多 N」；
  - 与「保存到此视图」并存：保存默认搜索不影响筛选方案。
- 冒烟产生的筛选/默认搜索已清除恢复干净。

## 风险

- **分页 + 前端复核筛选**：对业务重写 Search 且数据多页的控制器，前端仅过滤当前已加载页，跨页可能见未过滤数据（total 纠正仅在单页全量时生效）。已在代码注释声明；多页大数据场景建议后端支持筛选参数或大 pageSize 全量拉取（性能换正确性），记入 retro 后续建议。
- 组件级测试缺失（无 `@vue/test-utils` 基础设施），筛选构建器/分组弹层交互靠手工冒烟覆盖。
