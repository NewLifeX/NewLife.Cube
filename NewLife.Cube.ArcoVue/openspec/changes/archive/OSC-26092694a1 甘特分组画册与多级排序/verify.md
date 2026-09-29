# Verify

> 状态：passed（Validating，checklist passed，可复盘）
> 验收：2026-09-29
> 触发：按照本项目 OpenSpec 规范，对 94a1 变更进行验收和复盘。

## 三步编排

| 步 | 结果 |
| --- | --- |
| 实现审计 | 目标 1、目标 3 有代码与单测，浏览器抽查通过。目标 2 已在 proposal / design 撤回，代码与之一致。无 P0 / P1。 |
| 代码审查 | 无 🔴。`SortWhitelist` 使用 `String` / `Boolean`、file-scoped namespace，公共方法有 XML 注释。测试项目无 NewLife 别名，用例用 `string` / `bool`。 |
| 文档同步 | 迁移方案 §7.4 / §10.4 #19–#21 与竞品报告已在执行期标到本号，画册标为撤回。本轮把 proposal §4 / §6 / §8、design §5、tasks T16 与撤回后的实现对齐。 |

## 目标愿景对照

| 目标 | 结论 |
| --- | --- |
| 1 甘特 `groupField`，父节点不打开详情，`moveable` 仍为 false | 达成。见 AC1–AC4。 |
| 2 画册布局 | 执行中按产品要求撤回。配置无「画册」，`gallery` 归一 `standard`。见 AC5–AC6。 |
| 3 `sorts` 最多 3 列，工厂字段且属于 search∪list，非法 400；单列旧参数仍可用 | 达成。见 AC7–AC10。 |

缺口：无需要用户决策的 P0 / P1。布尔分组在字段没有 dataSource 时标题为原始值 `true` / `false`（定时作业「启用」），与 design「数据源 label 或原始值」一致。

## AC

- [x] AC1 甘特不选分组时，记录扁平，与变更前一致。定时作业新建甘特、分组为「不分组」时左表 8 条平铺（BackupDb … WorkflowTimeoutTick），没有 `__ganttGroup` 父节点。
- [x] AC2 选择分组后出现分组父节点；父节点不是业务记录。分组选「启用」后记录变为 `__group:false`（6 条子记录）与 `__group:true`（2 条）。`useGanttView` 的 `click_cell` / `click_task_bar` 在 `__ganttGroup` 上直接返回，不发 `detail`。现场点父行时配置抽屉遮罩正在关闭，点击被遮罩接住，未再打开记录详情。
- [x] AC3 空分组值显示「未分组」。`ganttGroup.spec.ts` 锁定空值桶标题与 id `__group:`。本次定时作业「启用」没有空值，页面上不出现该桶。
- [x] AC4 任务条仍不可拖拽改期。`useGanttView` 的 `taskBar.moveable` / `resizable` / `scheduleCreatable` / `progressAdjustable` 均为 false。
- [x] AC5 画册已撤回。`cardLayouts` 只有标准、偏大、整行；`normalizeCardLayout('gallery')` 为 `standard`（`viewMapping.spec.ts`）。
- [x] AC6 旧视图 `layout=standard` 打开后仍是标准布局。归一化只接受 `large` / `row`，其余回落 `standard`。
- [x] AC7 表头单击只保留一列排序。用户列表先有「名称 + 性别」两列，单击「名称」表头后请求为 `/api/Admin/User?pageIndex=0&pageSize=20&sort=Name&desc=false`，没有 `sorts`。弹层只剩「名称 / 升序」。
- [x] AC8 排序弹层加入第二列后请求带 `sorts`、不带 `sort`。应用「名称 + 性别」后请求 `sorts=Name,Sex`（`Name%2CSex`）。返回顺序 ABCDSW、admin、smokeuser1、smokeuser2、smokeuser3!!!，与名称升序一致。当前列表 5 条、每页 20，没有第二页可翻。
- [x] AC9 未知字段或第 4 列时接口业务码 400，列表不按未授权字段排序。`sorts=NotAField` → `code:400`「排序字段不存在」；`sorts=Name,Sex,Enable,ID` → `code:400`「排序条件无效」。
- [x] AC10 只含 `sort` 的请求仍能单列排序。`sort=Name&desc=true` 返回 `code:0`，名称降序为 smokeuser3!!!、smokeuser2、smokeuser1、admin、ABCDSW。

## 必须保留

- 甘特 `moveable:false` 与 `tableWidth` 持久化：`tableWidth` 仍在 mapping 归一化里夹取 280–640，宽度轮询未改。
- 不新增 `gallery` 视图种类。`CardLayout` 仍是 `'standard' | 'large' | 'row'`。
- 不接受客户端任意 `OrderBy`。`SearchData` 只读查询参数 `sorts`，编译后写 `pager.OrderBy` 并清空 `pager.Sort`。

## 测试与构建（2026-09-29）

- `dotnet test XUnitTest/XUnitTest.csproj --filter FullyQualifiedName~SortWhitelistTests`：6 通过，0 失败。
- `npx vitest run src/features/views/ganttGroup.spec.ts src/core/utils/viewMapping.spec.ts src/core/utils/viewProfile.spec.ts src/features/views/ganttLayout.spec.ts src/views/crud/useSortPopover.spec.ts`：5 文件、126 通过。
- `dotnet build NewLife.Cube/NewLife.Cube.csproj`：0 错误（既有 XML 注释警告，非本号）。
- `npx vue-tsc -b`：无错误。

## 验收现场清理

验收时在班级、定时作业各建过一个名为「甘特图视图」的临时视图，验完已从视图配置删除。定时作业上原有的「任务进度」甘特视图保留。用户列表表头单击后当前排序为单列「名称」升序。
