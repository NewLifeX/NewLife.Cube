# OSC-260926c2b8 Design — 值集行权、日历看板与评论提及

适用前端：Arco Design Vue（https://arco.design/vue/docs/start）。日历弹层用 `a-popover`，提及用 `a-popover` + `a-tag`。图标只用已注册的 `people`（`iconRegistry.ts`）。不改 VTable、不改 FlowGram。`.vue` 只保留薄 script，业务进同目录 `use*.ts`。

## 0. 状态唯一来源

| 状态 | 来源 | 禁止 |
| --- | --- | --- |
| 值集行是否可见 | `LovEntityGuard` 的菜单 Detail + 租户表达式 + `DataScopeHelper` | 前端过滤下拉当安全边界；写 `DataScopeContext.Current` |
| 日历当日事件 | `useCalendarMonth` 已算好的 `cell.events` | 再向服务端按日查一次 |
| 看板列是否折叠 | `useKanbanBoard` 内存 `collapsedKeys` | 写入 `KanbanMapping` / ViewProfile |
| 待提及用户 | 评论框本地 `mentionUserIds` + 正文中的 `@显示名` | 新评论列、富文本 |

## 1. 值集行权

### 1.1 文件

| 文件 | 改动 | 不动 |
| --- | --- | --- |
| `NewLife.Cube/Common/LovEntityGuard.cs` | 新建。菜单 Detail、租户表达式、行权表达式、按主键判可见 | 不修改 `DataScopeHelper` |
| `NewLife.Cube/Areas/Admin/Controllers/LovController.cs` | `FetchEntityList`、实体分支 `BatchLabel` 调用 Guard | `Enum.*` 分支；外部 HTTP `FetchRemoteList` |
| `XUnitTest/LovEntityGuardTests.cs` | 新建 | — |

租户表达式从 `ReadOnlyEntityController2.CreateWhere` 的多租户分支抽出 `TenantScopeHelper.GetFilter(IEntityFactory)`，`CreateWhere` 改为调用它，避免两套租户规则。`DataPermissionAttribute` 仍只留在控制器 `CreateWhere`，值集不套用该特性。

### 1.2 条件矩阵

当前用户取 `ManageProvider.User`。`DataScopeContext.Create(user)` 只作参数，不写入 `Current`。

| 输入 | ListData `entity:` | BatchLabel 实体值 |
| --- | --- | --- |
| 未登录 | 401（现有 `EntityAuthorize`） | 同左 |
| 目标实体无菜单，或当前用户无该菜单 Detail | 403，空数据 | 该 value 不出现在字典 |
| 租户 Enforce 且无租户上下文 | 表达式 `1=0`，空列表 | 该 value 省略 |
| 租户 Shadow 且无上下文 | 与列表相同：不加租户过滤 | 同左 |
| 实体实现归属接口且行权范围外 | `FindAll` 带 `GetFilter`，行不返回 | `CanAccess` 为假则省略 |
| 实体无归属接口 | 只做菜单 + 租户 | 同左 |
| 系统角色 / DataScope 全部 | `GetFilter` 为 null，仍受租户约束 | 同左 |
| `Enum.*` | 不走 Guard | 现有反射翻译 |
| 外部 HTTP 值集 | 不走 Guard | 现有代理 |

`FetchEntityList` 在关键字表达式之后 AND 上 Guard 表达式，再 `FindAll`。分页上限仍是 500。

`BatchLabel` 不再为了翻译去扫全表。对每个待翻译主键调用与 `FindData` 相同的「先按主键取出，再 `CanAccess` + 租户 `Eval`」。不可见或没有的键省略。最多处理请求里的全部 value，但单次查询按主键集合，不用 20 页穷举。

### 1.3 菜单判定

用实体工厂类型名找到已启用菜单（Url 对应的 Area/Controller 与现有实体路由一致）。找不到菜单 → 失败关闭。系统角色仍要能通过菜单检查（与今天打开该实体列表一致）。

## 2. 日历与看板

### 2.1 文件

| 文件 | 改动 | 不动 |
| --- | --- | --- |
| `web/src/features/views/useCalendarMonth.ts` | 导出「某日事件」；不改月网格算法 | 周/议程 |
| `web/src/features/views/CalendarMonth.vue` | `+N` 改为按钮；空白日点击；新增 props/emits | `placement` 与月切换 |
| `web/src/features/views/calendarDay.spec.ts` | 门禁纯函数 | 不挂载 SFC |
| `web/src/features/views/useKanbanBoard.ts` | `collapsedKeys`、`toggleColumn` | 分列算法 |
| `web/src/features/views/KanbanBoard.vue` | 列头点击折叠 | `compact===true` 时不渲染折叠钮 |
| `web/src/features/views/kanbanCollapse.spec.ts` | 折叠集合纯函数 | — |
| `web/src/views/crud/DefaultList.vue` | 日历传入 `canAdd`，接 `@create` | 看板拖拽 |
| `web/src/views/crud/useRecordNav.ts` | `openAdd` 之后写入开始日期字段 | 抽屉方向仍为右侧 |

### 2.2 日历交互

| 操作 | 条件 | 结果 |
| --- | --- | --- |
| 点事件条 | 始终 | 已有 `detail` |
| 点 `+N` | `events.length > 3` | `a-popover` 列出当日全部事件；点一条发 `detail`。每天最多展示 3 条的规则保留 |
| 点本月空白日（含日期数字） | `canAdd && cell.inMonth && mapping.startField` | `create`，载荷 `{ date: 'YYYY-MM-DD' }` |
| 点本月空白日 | 无 Insert | 无请求、无抽屉、无提示 |
| 点非本月格子 | 任意 | 不发 `create` |
| 未配置 startField | 任意 | 保持现有警告空态 |

`DefaultList` 收到 `create`：调用现有 `openAdd()`，再把 `formModel[startField]` 设为该日期的本地 0 点 ISO 字符串。不填 `endField`。

新增 props：`canAdd: boolean`。新增 emit：`create`。

### 2.3 看板折叠

- 列头整行可点。`aria-expanded` 反映状态。
- 展开：现有列宽与卡片列表。
- 折叠：列宽 48px，只显示列名（竖排或单行省略）和条数，卡片区 `display:none`。
- 默认全部展开。切换视图或 `groupField` 变化时清空折叠集合。
- `compact`（工作台迷你看板）不显示折叠，避免部件高度跳动。

## 3. 评论提及

### 3.1 文件

| 文件 | 改动 | 不动 |
| --- | --- | --- |
| `packages/api-core/src/api.ts` | `comment.post` 增加可选 `mentionUserIds?: number[]` | URL 与 GET/DELETE |
| `web/src/views/crud/useRecordDrawer.ts` | 选人、截断 20、提交时附带 ID | `AddComment` 调用形态以外的历史/表单 |
| `web/src/views/crud/commentMention.ts` | 纯函数：去重、去掉自己、最多 20、从正文删除对应 `@显示名` | — |
| `web/src/views/crud/commentMention.spec.ts` | 上列纯函数 | — |
| `web/src/views/crud/RecordDrawer.vue` | 发送按钮旁 `people` 按钮、已选 `a-tag` | 评论仍纯文本，最多 500 字 |

### 3.2 交互

- 顶层与回复各有自己的提及列表。
- 点 `people` 打开弹出层。输入关键字后调用 `cubeApi.page.getList('/Admin/User', { pageIndex: 0, pageSize: 20, q })`。失败时列表为空，仍可发送不带提及的评论。
- 点一名用户：若未选且未满 20，把其 ID 加入列表，并在光标处插入 `@显示名 `（显示名用行的 Name，没有则用 ToString 字段）。已选的不再插入。
- 关掉标签：从列表删除 ID，并从正文去掉第一次出现的 `@显示名`。
- 发送体：`{ category, linkId, content, parentId, mentionUserIds }`。ID 为空则不传该字段。
- 自己的用户 ID 不进入列表。后端已会跳过自己、禁用用户和重复项；前端仍先去掉自己并 `slice(0, 20)`。
- 刷新评论后不还原提及标签。提及只负责发站内信，不落在评论表。

## 4. 核心文档影响

| 文档 | 影响 |
| --- | --- |
| 迁移方案 §8.6.5 BE-D2 / BE-D3、§7.4 日历/看板、§10.4 #17 #18 | 实施完成后把对应行标为本号 |
| 竞品报告 §6.2 #9 #20 #21 | 同期改状态 |
| `Doc/功能清单.md` | 若已有值集/评论编码，补测试列；没有则不新编码体系 |

## 5. 测试设计

- Guard：无菜单 403；`1=0` 租户；`GetFilter` AND 进查询；BatchLabel 对范围外主键返回的字典不含该键；枚举翻译仍在。
- `canCreateOnCalendarCell(canAdd, inMonth, hasStart)` 真值表与上表一致。
- `toggleCollapsed` 对同一 key 两次回到展开。
- `buildMentionIds` 去重、去自己、截断 20。

## 6. 明确保留

`CalendarMonth` 仍只有月网格。`KanbanBoard` 不发分组字段 PATCH。`CubeController.SendMentionNotification` 方法体不改。
