# OSC-26092694a1 Design — 甘特分组、画册布局与多级排序

适用前端：Arco Design Vue（https://arco.design/vue/docs/start）；甘特继续用 VisActor VTable Gantt（https://visactor.com/vtable/guide/Getting_Started/Getting_Started）。分组用记录 `children` 表达，不开启任务条拖拽。`.vue` 薄 script。

## 0. 状态唯一来源

| 状态 | 来源 | 禁止 |
| --- | --- | --- |
| 甘特分组字段 | `GanttMapping.groupField` | 表格 `NamedView.group` 去驱动甘特 |
| 卡片布局 | `CardMapping.layout` | 新的 ViewKind |
| 排序 | `NamedView.sorts`，兼容旧 `sort` | 请求里的自由 `orderby` |

## 1. 甘特分组

### 1.1 Schema

`GanttMapping` 增加可选 `groupField?: string`。

| 值 | 含义 |
| --- | --- |
| 缺省 / `""` / 未知字段 | 不分组，记录扁平，与今天相同 |
| `groupFieldCandidates` 中的字段名 | 按该字段值分组 |

旧 mapping 无此键 → 不分组。保存时未知键仍放在 `_raw` 往返，不删除。

### 1.2 合成规则

纯函数 `groupGanttRecords(records, field, fields)`：

- 无 field：原样返回，且每条没有合成父节点。
- 有 field：每个不同值一个父节点。父节点 id 为 `__group:` + 值，标题为数据源 label 或原始值，空值标题为「未分组」。
- 父节点不是业务记录：点击不发 `detail`。
- 子节点是原记录，点击仍发 `detail`。
- 父节点不单独占一条可写任务。日期范围取子节点计划起止的最小/最大，只用于把组在时间轴上撑开；`taskBar.moveable` 保持 false。
- 分组不写回，不发 PATCH。

### 1.3 文件

| 文件 | 改动 | 不动 |
| --- | --- | --- |
| `web/src/core/utils/viewMapping.ts` | `groupField` 与 normalize：空或非候选则 `''` | 其他 mapping 字段 |
| `web/src/features/views/ganttGroup.ts` | 合成父节点 | — |
| `web/src/features/views/ganttGroup.spec.ts` | 空、两值、空值桶 | — |
| `web/src/features/views/useGanttView.ts` | 把合成结果交给 Gantt；父节点点击忽略 | `tableWidth`、缩放、双条 |
| `web/src/views/crud/useViewConfigDrawer.ts` | 甘特区块增加分组下拉，候选用现有 `groupFieldCandidates` | 看板分组逻辑 |
| `web/src/views/crud/ViewConfigDrawer.vue` | 甘特区一个「分组字段」下拉，含「不分组」 | — |

下拉顺序：第一项「不分组」值为 `""`，其余为候选项显示名。

## 2. 画册布局（已撤回）

产品要求删除画册。`CardLayout` 不含 `gallery`；旧值归一为 `standard`。配置抽屉不再出现「画册」。标准与偏大有封面时图片贴边；整行封面圆角为 2px。

## 3. 多级排序

### 3.1 协议

查询参数 `sorts`：逗号分隔，最多 3 段。段格式为字段名或 `-字段名`。字段名只允许字母、数字、下划线。

| 请求 | 行为 |
| --- | --- |
| 无 `sorts`，有 `sort`/`desc` | 保持今天的单列 |
| 有 `sorts` | 忽略 `sort`/`desc` |
| 段数 0 或 >3 | 400 |
| 字段不在 `Factory.Fields` 或不在 search∪list | 400 |
| 含空格、点、括号、引号 | 400 |
| 合法 | `pager.Sort = null`，`pager.OrderBy` = `Field asc, Field2 desc` 这种由服务端拼接的串 |

不读取客户端的 `orderby` / `OrderBy` 作为白名单入口。`PageSetting.OrderByKey` 仅当 Sort 与 OrderBy 都空时照旧。

实现位置：`ReadOnlyEntityController2.SearchData` 在默认主键排序之前。从 `Request.Query["sorts"]` 读取。不给 XCode `PageParameter` 加属性。

### 3.2 前端存储

```ts
sorts?: ViewSort[] | null  // 长度 0..3
sort?: ViewSort | null     // 始终等于 sorts[0]，无排序则为 null
```

读取：有 `sorts` 用 `sorts`（截断 3，丢掉无 field 项）；否则若有 `sort` 则 `sorts=[sort]`；否则空。

表头单击：该列成为 `sorts` 的唯一元素（与今天切换升降序相同），清掉第 2、3 列。

工具栏仅 `table` / `tree` 显示「排序」按钮，打开 `a-popover`：最多 3 行，每行字段下拉 + 升序/降序 + 删除。不足 3 行时有「添加排序」。确定后写入视图并重新查询。

请求：`sorts.length <= 1` 时只发 `sort`/`desc`（与 `buildSortPayload` 相同）。`length >= 2` 时只发 `sorts=Name,-Id` 这种串，不发 `sort`。

### 3.3 文件

| 文件 | 改动 | 不动 |
| --- | --- | --- |
| `NewLife.Cube/Common/SortWhitelist.cs` | 解析与 400 | — |
| `NewLife.Cube/Common/ReadOnlyEntityController2.cs` | 调用解析，写 OrderBy | 筛选 AST |
| `XUnitTest/SortWhitelistTests.cs` | 矩阵上的例子 | — |
| `web/src/core/utils/viewProfile.ts` | `sorts` 读写、`buildSortPayload` 分叉 | 列冻结 |
| `web/src/views/crud/useListQuery.ts` | 把 payload 放进 getList | — |
| `web/src/views/crud/SortPopover.vue` + `useSortPopover.ts` | 弹层 | — |
| `web/src/views/crud/DefaultList.vue` | 表头单击改为只保留第一列；挂排序按钮 | 分组弹层 |
| `web/src/core/utils/viewProfile.spec.ts` | 旧 sort 兼容、两列 payload | — |

表头单击的现有函数若只写 `sort`，改为写 `sorts` 长度 1，并同步 `sort`。

## 4. 核心文档影响

| 文档 | 影响 |
| --- | --- |
| 迁移方案 §7.4、§10.4 #19 #20 #21 | 完成后标本号 |
| 竞品报告 §3.2 排序、§6.2 #22 #23、§6.4 #13 | 同期 |

## 5. 测试设计

- 甘特：两枚举值 → 两个父节点；空值 →「未分组」；父 id 以 `__group:` 开头。
- 画册：`normalizeCardLayout('gallery')`；列数被压成 1；`standard` 仍可 2 列。
- 排序：`Name,-CreateTime` → `Name asc, CreateTime desc` 且 Sort 为空；`Salary` 不在白名单 → 400；4 段 → 400；仅 `sort=Name&desc=true` 仍单列。

## 6. 明确保留

甘特 `moveable:false`。卡片视图种类仍是 `card`。单列排序 URL 不强制迁移成 `sorts`。
