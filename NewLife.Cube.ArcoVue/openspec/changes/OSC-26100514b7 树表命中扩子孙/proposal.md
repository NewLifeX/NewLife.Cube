# OSC-26100514b7 — 树表命中扩子孙

## 1. 目标愿景

树表视图下查询树实体时，命中节点的完整子树一次返回，管理员展开即可看到全部下级，不再因分页只带回命中扁平行而树残缺。

- 目标 1：后端按实体设计自动探测树实体（`IEntityTree` 或 `ParentID`/`ParentId` 与主键同型）；仅当请求 `viewKind=tree` 时对命中行扩全部子孙。
- 目标 2：关键字 `Q` / 自定义 `viewFilter` 等条件先筛命中，再并入子孙（子孙无需匹配条件；不强制补祖先）。
- 目标 3：响应仍为扁平列表；`TotalCount` 表示命中行总数；`Data` 可大于 `pageSize`；扩后总行硬顶 **100000**，超出截断并记日志、不 500。
- 目标 4：前端树表 GetList 传 `viewKind=tree`，且客户端复核不得裁掉扩入子孙。

## 2. 为何做

多维「树状」视图依赖父子关系展开。菜单走 `EntityTreeController` 已一次返回缓存全树；部门、地区等普通 `EntityController` + `ParentID` 在关键字/筛选后只返回命中行，前端 `buildTree` 拼不出子树，用户看不到命中节点下的子对象。

后端已有 `GetTreeParentFieldName()` 约定，缺少「树表 + 命中扩子孙」统一挂钩。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | 语义：命中行扩全部子孙；不补祖先；普通 table 视图不扩。 |
| 2 | 门控：请求参数 `viewKind=tree`（大小写不敏感）且实体为树；缺省/其它值行为与今日一致。 |
| 3 | 树探测：`Factory.EntityType` 实现 `IEntityTree`，或 `GetTreeParentFieldName()!=null`。 |
| 4 | 扩点：`Index`/`GetList` 在 `SearchData` 之后、`OnFillListValues` 之前；先按命中分页再扩。 |
| 5 | 返回扁平行；不强制嵌套 `children`（前端 `buildTree`）。 |
| 6 | 扩入子孙须再过与列表同源的行权/`CanAccess`；不可见丢弃。 |
| 7 | 单次响应扩后总行数硬顶 **100000**（ `CubeSetting` 通用配置项，放在 `筛选时间窗天数` 前，默认 100000）；超出截断 + 日志。 |
| 8 | 小表用 `FindAllWithCache` 建 parent→children 索引后 BFS/DFS；大表按 `ParentID IN (...)` 分层 BFS。 |
| 9 | `EntityTreeController` 已返回全树时短路，不重复扩。 |
| 10 | `GetPage.setting` 增加 `isTreeEntity`，供前端创建树视图门禁对齐。 |

## 4. 做什么

1. 新增 `TreeDescendantExpand`：去重、环检测、行权过滤、100000 封顶。
2. `ReadOnlyEntityController.Index` 挂钩扩子孙；复用 `GetTreeParentFieldName`。
3. `GetPage.setting.isTreeEntity` 下发。
4. ArcoVue：树表 `loadData` 传 `viewKind=tree`；请求签名纳入；避免 `matchesViewFilter` 裁掉扩入行。
5. XUnit + 必要前端 spec；迁移方案/功能清单短句。

## 5. 不做什么

- 不补祖先链。
- 不改为「树表永远忽略分页一次拉全表」（`EntityTreeController` 维持现状）。
- 不改变普通 table 的分页与搜索语义。
- 不做懒加载「点开节点再取 children」（可另号）。
- 不把封顶做成按实体可配多档（本号固定默认 100000，若进 Setting 则单项即可）。

## 6. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-260830a1b2 / OSC-260819e483 | `Q` / `viewFilter` 列表查询链已存在，本号在其后扩子孙。 |
| OSC-261004e6ee | `listRequestSignature` 需纳入 `viewKind`。 |

## 7. 测试范围

- 后端：扩子孙纯函数图用例（命中 A 含 A1、不含旁支；环；行权剔除；超 100000 截断）；`viewKind` 缺省不扩。
- 前端：树表带 `viewKind`；非树表不带；签名变化。
- 验收：部门等 ParentID 实体树表 + 关键字，展开可见命中节点全部下级。
