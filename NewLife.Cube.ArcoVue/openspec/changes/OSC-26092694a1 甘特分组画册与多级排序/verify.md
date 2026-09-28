# Verify

验收命令：

- `dotnet test` 中筛选 `SortWhitelistTests`
- `web` 目录 `npx vitest run src/features/views/ganttGroup.spec.ts src/core/utils/viewMapping.spec.ts src/core/utils/viewProfile.spec.ts`
- `dotnet build`（NewLife.Cube）
- `npx vue-tsc --noEmit`（web）

若 `viewMapping.spec.ts` 不存在，画册用例放在 design 指定的 card spec 中，并把实际路径写回本文件。

## AC

- [ ] AC1 甘特不选分组时，记录扁平，与变更前一致。
- [ ] AC2 选择枚举分组后出现分组父节点；点击父节点不打开详情；点击子记录打开详情。
- [ ] AC3 空分组值显示「未分组」。
- [ ] AC4 任务条仍不可拖拽改期。
- [ ] AC5 卡片布局出现「画册」；选中后图区高 160，正文不超过 2 个字段。
- [ ] AC6 旧视图 `layout=standard` 打开后仍是标准布局，不是画册。
- [ ] AC7 表头单击只保留一列排序，请求里是 `sort`/`desc`，没有 `sorts`。
- [ ] AC8 排序弹层加入第二列后，请求带 `sorts`，不带 `sort`；翻页顺序与两列一致。
- [ ] AC9 `sorts` 含未知字段或第 4 列时接口 400，列表不更新为未授权排序。
- [ ] AC10 只含 `sort` 的旧 ViewProfile 仍能单列排序。

## 必须保留

- 甘特 `moveable:false` 与 `tableWidth` 持久化。
- 不新增 `gallery` 视图种类。
- 不接受客户端任意 `OrderBy` 字符串。

## 执行记录（2026-09-27）

- `SortWhitelistTests` 6 通过（含大小写不敏感编译为 `Name asc, CreateTime desc`）。
- Vitest：`ganttGroup.spec.ts`、`viewMapping.spec.ts`、`viewProfile.spec.ts`、`ganttLayout.spec.ts` 共 122 通过。
- `npx vue-tsc -b` 无错误。`SortWhitelist` 随 `SortWhitelistTests` 编进 NewLife.CubeNC。
- 浏览器未打开。AC1–AC10 留到验收勾选。

