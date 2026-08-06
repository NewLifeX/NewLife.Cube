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
- [x] AC-10：本 OSC 新增 Vitest 全过（266/266），api-core/web 构建无错误，事实性文档最小同步完成。

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

- 验收（2026-08-06）：web Vitest 266/266 通过；vue-tsc + vite 构建通过。
- 浏览器冒烟（部门页，admin）：
  - 筛选构建器「类型 = 公司」应用后 6→2 条（总公司/上海分公司），徽标「1」+ 清除筛选标签，刷新后自动应用保留，清除恢复 6 条；
  - 分组按 Type：组标题「📁 公司 (2)」+ checkbox 级联，组标题点击折叠/展开（rowCount 4→2→4）；
  - 搜索面板折叠「展开更多 N」；
  - 与「保存到此视图」并存：保存默认搜索不影响筛选方案。
- 冒烟产生的筛选/默认搜索已清除恢复干净。
