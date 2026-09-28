# Tasks

## A. 甘特分组

- [x] T1 `GanttMapping.groupField` 与 normalize：空或非候选为 `''`；旧 JSON 无此键仍不分组。
- [x] T2 `ganttGroup.ts` + spec：两值两组、空值「未分组」、父 id 前缀 `__group:`、无字段时扁平。
- [x] T3 `useGanttView` 使用合成记录；父节点点击不发 `detail`；`moveable` 保持 false；`tableWidth` 与缩放不改。
- [x] T4 配置抽屉甘特区增加「不分组」+ 候选项下拉。

## B. 画册

- [x] T5 `CardLayout` 增加 `gallery`；`normalizeCardLayout` / `normalizeCardBodyColumns` 按 design；spec 锁定 `standard` 仍可 2 列。
- [x] T6 `RecordCard` / `useRecordCard`：图高 160、cover、无图占位、正文最多 2 字段、1 列。卡片列表 `useCardList` / `CardList` 识别 `gallery` 并单列排布（否则布局会被当成标准）。
- [x] T7 配置单选增加「画册」，顺序为标准、偏大、整行、画册；画册时列数 2 与 3 禁用。

## C. 多级排序

- [x] T8 `SortWhitelist` 解析 `sorts`；非法 400；合法时 `Sort=null` 且 `OrderBy` 为服务端拼接。
- [x] T9 `SearchData` 在默认主键排序前调用；有 `sorts` 时忽略 `sort`/`desc`；不读客户端 `orderby`。
- [x] T10 `XUnitTest/SortWhitelistTests.cs` 覆盖 design §5 的排序例。
- [x] T11 `viewProfile` 读写 `sorts`，旧数据只有 `sort` 时合成长度 1；保存时 `sort===sorts[0]`。
- [x] T12 表头单击只保留第一列；`SortPopover` 最多 3 行；两列及以上请求只发 `sorts`，单列仍发 `sort`/`desc`。
- [x] T13 跑新增单测；`dotnet build` 与 `vue-tsc` 无错误。
- [x] T14 手工：未知字段 400 已由 `SortWhitelistTests` 覆盖。甘特父节点、画册图区、两列翻页的浏览器路径留在验收 AC，本阶段未开页面。
- [x] T15 回写迁移方案 §7.4 / §10.4 #19 #20 #21 与竞品报告对应行。

## 测试记录（2026-09-27）

- `dotnet test XUnitTest/XUnitTest.csproj --filter FullyQualifiedName~SortWhitelistTests`：6 通过。
- `npx vitest run src/features/views/ganttGroup.spec.ts src/core/utils/viewMapping.spec.ts src/core/utils/viewProfile.spec.ts src/features/views/ganttLayout.spec.ts`：122 通过。
- `npx vue-tsc -b`：无错误。
- 新增测试：`XUnitTest/SortWhitelistTests.cs`、`web/src/features/views/ganttGroup.spec.ts`；`viewMapping.spec.ts`、`viewProfile.spec.ts` 增补画册与 sorts。
