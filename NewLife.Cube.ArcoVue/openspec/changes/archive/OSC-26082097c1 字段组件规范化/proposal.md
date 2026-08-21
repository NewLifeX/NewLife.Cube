# OSC-26082097c1 — 字段组件规范化

## 1. 目标愿景

让 ArcoVue 用**一套 FieldKind 注册表**驱动所有表单与弹层的字段控件（飞书多维表格/应用表单范式：一类型、多表面），后续新增抽屉/弹层不再各自 switch。

- 目标 1：产出可追溯的字段目录——后端 `TypeName`/`ItemType` ↔ 前端四套并行枚举 ↔ 统一 `FieldKind`，覆盖编辑/展示/搜索/筛选。
- 目标 2：冻结表面契约（`mode` + `density` + 值协议），消费方清单穷尽（表单抽屉、对象页、详情、列表、搜索、筛选、填色、批量修改、单元格编辑、自动化条件）。
- 目标 3：目录含启发式专用件、只读公式/查找、**TimeSpan 友好时长**、**详情态按 kind 的标签前图标**。
- 目标 4：给出后续实现 OSC 切片（注册表 → FieldWidget 替换副本 → 缺口控件），本号不写业务代码。

## 2. 为何做

ArcoVue 已有 22 种 `ControlType` 与 `FieldInput`，但**不是**飞书式字段类型体系：

1. **四套分类互不派生**：`ControlType`（表单）/ `SearchControlType`（搜索）/ `ListControlType`（列表）/ `FilterFieldKind`（筛选/填色/自动化）各自推断，同一字段在不同表面可落到不同语义（典型：`TimeSpan` 表单当时钟、`icon` 声明了却退回文本框）。
2. **控件副本发散**：`FieldInput`、`SearchFieldInput`、`BatchEditValueInput`、筛选/填色弹层、单元格编辑弹层各画一套；批量与单元格覆盖面明显小于表单。
3. **后端 `ItemType` 已开放、前端未收口**：XCode/Cube 已有 `password`/`GMK`/`percent`/`code`/`TimeSpan`/`lovTable` 等，GetPage 会下发，前端无统一目录。
4. 竞品分析 P2 #16「字段控件注册表」与迁移方案「固定容器 + 元数据驱动」一致：扩展点应是注册表，不是再复制 switch。

OSC-0018 解决「页面怎么自定义」；本号解决「字段控件怎么标准化」，二者正交。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | **纯文档 OSC**。交付物：本目录五件套 + `ui/` + 执行期 `web/docs/字段组件规范.md`。不改 `.ts/.vue/.cs`。 |
| 2 | **全表面**：同一 FieldKind 覆盖表单编辑 / 列表展示 / 搜索 / 筛选与填色弹层 / 批量修改（含单元格编辑）。 |
| 3 | **目录 = 现有能力 + 已声明缺口 + 名称启发式 + 只读公式/查找**。公式/查找引用**只读**进目录 B。双向关联写回 / 条码 / 位置 / 评分 / 签名仍只进路线图。 |
| 4 | **GetPage 仍是字段权威**。注册表只消费已下发 `FieldMeta`；禁止前端发明列、改 `ReadOnly`。名称启发式**不要求**后端新增 `ItemType=person`。公式值必须已在实体 JSON 中（C# 扩展属性）；**不**新增 GetPage `projections`（对齐 OSC-260819e483）。 |
| 5 | **树状实体**（地区 Area / 部门 Department / 菜单 Menu）外键与自身 `ParentID` 使用**专用级联**，禁止退回普通 LOV 下拉。现有 `CascaderField` 仅绑 `/Cube/Area`，实现号泛化为按树种懒加载。 |
| 6 | **人员**使用专用选择器。命中 XCode 惯用名：创建者/创建人员/更新者/更新人员（及 `CreateUser`/`CreateUserID`/`UpdateUser`/`UpdateUserID`/`Creator`/`Updater` 等，见 design §3.5）。审计字段仍不进新增/编辑表单（`isAuditField`），但搜索/筛选/填色/非审计 `UserId` 必须走人员控件。 |
| 7 | **单选/多选**：人员 / 角色 / 部门（及地区 / 菜单）按字段名是否 `Ids` 后缀推断。`RoleId` 单选，`RoleIds` 多选；`UserId`/`DepartmentId` 同理。显式 `multiple` / `itemType=multipleSelect` 仍优先。 |
| 8 | **终端 IP**：XCode「创建地址 / 更新地址」（字段 `CreateIP` / `UpdateIP`）使用专用 IP 控件（IPv4/IPv6 展示与校验），不是普通文本，也不是地图定位。审计规则同人员：不进新增/编辑表单，列表/详情/搜索/筛选走 IP 控件。 |
| 9 | **只读公式/计算**：展示服务端已算好的值（C# 扩展属性 / `ItemType=formula|compute`）。禁止浏览器求值、禁止用户脚本。表单只读，不参与提交。 |
| 10 | **只读查找引用**：用现有 `MapField` + LOV/`BatchLabel` 显示关联**名称**；写回仍是外键本身。不新增跨表投影协议；额外关联列若已作为实体属性下发则当公式或只读文本。 |
| 11 | 实现按 Arco 栈设计，**不迁移** Cube.Vue `FieldInput`。 |
| 12 | 旧 `resolve*` 在后续实现号中改为 FieldKind **投影函数**（可暂留薄封装）。 |
| 13 | **时长**：`typeName=TimeSpan` **或** `itemType` 以 `TimeSpan` 开头（含 `Int32` 秒数字段如 OnlineTime）→ `duration`。展示用户友好中文（`1小时 2分钟`），禁止当时钟 `HH:mm:ss`。`TimeSpan:format` 有自定义格式时用该格式。 |
| 14 | **详情标签图标**：每个 FieldKind 指定 `labelIcon`（IconPark）。详情态图标在字段**标签文字之前**（现 RecordDrawer DOM 已如此）。`fieldIcon()` 改为 kind 投影，禁止 DateTime 与 TimeSpan 共用时钟图标。编辑表单不强制加图标。 |

## 4. 做什么

1. 审计后端 `DataField.TypeName`/`ItemType` 与前端四套枚举、全部控件消费方，写入 `design.md` 映射表（本号创建期已固化基线）。
2. 定义 `FieldKindSpec`（kind / 中文名 / **labelIcon** / edit / display / search / filterOps / density / serialize / fullWidth）。勿把图标槽位命名为 `icon`。
3. 编写持久文档：目录 A/B、§3.5 启发式、§3.6 只读公式与查找、**§3.7 时长友好格式**、**§3.8 详情标签图标**、表面契约、消费方迁移图。
4. `ui/information-architecture.md`：mode / density / 调度壳 / 弹层值控件顺序。
5. 交叉核对路径与枚举，登记与 OSC-0018 / OSC-260819e483 的边界。

## 5. 不做什么

- 不改业务代码、不改 GetPage / `DataField.Fill` 签名、不改 XCode。
- 不强制后端下发 `ItemType=person`；**不做**用户脚本/客户端公式引擎、**不做**查找引用双向写回、不做对外表单视图。
- 不新增 GetPage `projections` 协议（查找关联名走现有 Map/BatchLabel）。
- 不把任意含 `IP` 的字段收成终端 IP（仅 CreateIP/UpdateIP 与显示名「创建地址/更新地址」）；不做地图选点。
- 不把 FieldInput 拆成 21 个独立 `.vue`（实现号：注册表先行，专用件按复杂度拆）。
- 不引入 FlowGram；不改 VTable 列引擎。
- 不评审/改写 OSC-0012~0017 已交付的运行期自定义语义（本号只规定控件从哪来）。
- 不取消 `isAuditField`：创建/更新用户、IP、时间仍不出现在新增/编辑表单。

## 6. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-0008 / OSC-0009 | 提交归一化、ItemType 治理、Cascader、日期壁钟时间 |
| OSC-0013 / OSC-0015 / OSC-0016 | 表单布局、筛选构建器、搜索抽屉（消费方） |
| OSC-0017 | `fieldIcon` / IconPark；缺口 `icon` 选择器复用注册表 |
| OSC-0018 | 正交：0018=页面自定义分层；本号=L0 字段控件契约 |
| OSC-260819e483 | 无签名冲突；P5 不新增 projections，公式/查找用扩展属性与 Map；批量修改是消费方 |
| OSC-26081903c0 | 填色弹层值控件是本号消费方 |
| 迁移方案 §8.2.6 | 本号收窄：允许只读服务端计算列与 Map 关联名；仍禁止脚本公式、跨实体查询引擎、双向写回 |
| 竞品分析 §6.2 #16 / §6.3 | 注册表；公式/查找由本号以只读形式进目录 |

## 7. 测试范围

| 类型 | 是否做 | 说明 |
| --- | --- | --- |
| 代码测试 | 否 | 本号零代码，声明 N/A |
| 文档自审 | 是 | verify.md 逐条：目录完整、formula/lookup 只读边界、TimeSpan 友好时长、详情 labelIcon、脚本公式不进目录 |
| 交叉核对 | 是 | 四套枚举、ItemType 映射、消费方路径与工作区一致 |

## 8. 成功标准

- [ ] `web/docs/字段组件规范.md` 交付（执行期），结构见 design.md §2。
- [ ] FieldKind 目录覆盖现有 22 种 `ControlType` + 目录 B（含树/人员/IP/只读公式/查找引用/**友好时长**/详情 **labelIcon**）；每条含 resolve 条件与四表面行为。
- [ ] 消费方迁移表覆盖 §3 锁定的全部表面，无遗漏副本。
- [ ] 后续实现 OSC 切片按依赖排序，与 0018 / e483 无冲突。
- [ ] 本号未修改任何 `.ts/.vue/.cs`。
