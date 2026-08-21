# OSC-26082097c1 Design — 字段组件规范化

## 0. 适用框架与官方资料

| 场景 | 框架/资料 | 说明 |
| --- | --- | --- |
| 设计系统 / 壳 / 表单 | Arco Design Vue https://arco.design/vue/docs/start | `FieldWidget` 编辑/搜索/筛选值控件用 Arco 输入类 |
| 多维数据视图 | VisActor VTable 配置 https://visactor.com/vtable/option/ListTable ；接口 https://visactor.com/vtable/api/Methods | 本号**不改**列引擎；`mode=display` 只规定单元格文案/徽章/缩略图语义 |
| 工作流 | FlowGram.AI https://flowgram.ai/guide/getting-started/introduction.html | **不涉及** |
| 元数据 | `NewLife.CubeNC/ViewModels/DataField.cs`、`GetPage` 五分区 | TypeName + ItemType 权威 |
| 前端现状 | `web/src/core/types/field.ts`、`fieldControl.ts`、`filterBuilder.ts` | 四套并行枚举 |
| 规范依据 | 迁移方案 §8.2.6；竞品分析 §3.1 / §6.3；OSC-260819e483 P5 | 本号允许**只读**服务端计算列与 Map 关联名；禁止脚本公式与 projections |
| SFC | `web/README.md` 职责分离 | 实现号：`.vue` 薄壳，业务进 `useXxx.ts` |

**本号零代码改动**。下列映射与架构是执行期文档的权威事实源；实现另立 OSC。

## 1. 现状审计（创建期已核对）

### 1.1 后端输入

`DataField` 序列化到 SPA 的控件相关字段：`name` / `displayName` / `category` / `typeName` / `itemType` / `length` / `precision` / `scale` / `nullable` / `primaryKey` / `readOnly` / `required` / `lovCode` / `dataSource` / `multiple` / `url` / **`mapField`**（`packages/api-core/src/types.ts` 已有；ArcoVue `FieldMeta` 实现号须透传，现状可能未拷贝）。

**CLR `typeName`（前端已知表）**：`Int32` `Int64` `Int16` `UInt32` `UInt64` `Byte` `SByte` `Decimal` `Double` `Single` `Short` `UShort` `String` `Boolean` `DateTime` `DateTimeOffset` `DateOnly` `Date` `TimeOnly` `Time` `TimeSpan` `Guid` `Enum`；不在表内的 typeName 视为枚举类（`isEnumLikeTypeName`）。

**易混点（复审锁定）**：XCode 常用 `ItemType=TimeSpan` 标在 **`Int32` 秒数字段**上（如 `User.OnlineTime`），**不是** CLR `System.TimeSpan`。`typeName===TimeSpan` 与 `itemType` 以 `TimeSpan` 开头都必须进 `duration`，且必须**先于**数值分支，否则 OnlineTime 会被当成普通 `number`。

**已出现的 `itemType`（大小写不敏感）**：

| 来源 | ItemType | 前端现状 |
| --- | --- | --- |
| `fieldControl.ITEM_TYPE_TO_CONTROL` | file / image / json / html / markdown / color / icon / mail / mobile / url / singleSelect / multipleSelect / area / area4 / cascader / date / datetime / time | 已映射；**icon 表单未实现专用件** |
| XCode 手册 / 实体 | TimeSpan、TimeSpan:format、GMK、code、file-zip | **未映射或映射错误** |
| Cube AI 黑名单 / 测试 | password、secret | **未映射** |
| ViewHelper | percent、Percentage | **未映射** |
| CubeDemo 测试字段 | lovTable、lovTableMulti | **未显式映射**（List.* lovCode 已走 LovSelect 表格） |
| 本号目录 B | formula / compute；Map 查找 | 列表已能 BatchLabel；无公式 kind；`ListField.GetValue` 委托不能下发 SPA |
| 搜索单测 | dateRange | 搜索已改为单值等值（OSC-0016），**不恢复范围假字段** |

### 1.2 前端四套枚举（互不派生）

| 枚举 | 成员 | 解析函数 | 消费 |
| --- | --- | --- | --- |
| `ControlType` 22 | input textarea inputNumber switch datePicker timePicker select selectMulti lov lovMulti cascader upload image json richHtml richMarkdown color icon email tel url readonly | `resolveControl` | FieldInput、批量 controlType |
| `SearchControlType` 11 | text number date datetime time lov lovMulti switch fileExists select cascader | `resolveSearchControl` | SearchFieldInput |
| `ListControlType` 15 | text number boolean date time color icon image json html lov file select readonly url | `resolveListControl` | formatFieldValue、fieldBadge、viewMapping |
| `FilterFieldKind` 5 | enum string person number datetime | `resolveFieldFilterKind` | FilterBuilderPopover、FormatPopover、自动化条件 |

`resolveControl` **优先级（冻结，实现号不得擅自重排）**：Guid → Boolean/布尔字典 → 非空 dataSource（select/selectMulti）→ itemType 表 → lovCode → DateTime → TimeSpan（现状误为 timePicker）→ 数值 → Enum → 枚举类 typeName → String(length≥300=textarea) → input。

### 1.3 控件副本（必须收口的消费方）

| 表面 | 文件 | 现状 |
| --- | --- | --- |
| 新增/编辑表单 | `views/crud/FormContent.vue` + `FieldInput.vue` | 完整 ControlType switch |
| 对象页 | `views/object/DefaultObject.vue` | FieldInput |
| 详情 | `views/crud/RecordDrawer.vue` | formatDetail + `fieldIcon`（图标已在标签前，但映射粗：TimeSpan 与 DateTime 共用时钟 `time`） |
| 列表/卡片/看板 | `core/utils/fieldFormat.ts` `fieldBadge.ts` `listContext.ts` | ListControlType |
| 搜索抽屉 | `features/search/SearchDrawer.vue` + `SearchFieldInput.vue` | SearchControlType |
| 筛选弹层 | `views/crud/FilterBuilderPopover.vue` | 自写 person/enum/number/datetime |
| 填色弹层 | `views/crud/FormatPopover.vue` | 同上副本 |
| 批量修改 | `views/crud/BatchEditValueInput.vue` | 仅 select/数字/日期/时间/多行，其余文本 |
| 单元格编辑 | `views/crud/DefaultList.vue` cellEdit 弹层 | **纯 `a-input`** |
| 自动化条件 | `views/crud/automation/useAutomationActionCard.ts` | FILTER_OPS_BY_KIND |

专用件（实现号继续复用，不重写）：`CascaderField.vue`、`LovSelect.vue`、`LovSelectTable.vue`、`JsonEditor`、`RichEditor`。

## 2. 持久文档结构（T4 交付物大纲）

`web/docs/字段组件规范.md` 必须含：

1. 背景与目标（飞书范式：一类型多表面；GetPage 权威）
2. 解析模型：`FieldMeta` → `resolveFieldKind` → `FieldKindSpec`
3. **目录 A** 已可用 FieldKind 全表（§3.1）
4. **目录 B** 已声明缺口目标行为（§3.2）
5. **名称启发式**（§3.5）与 **只读公式/查找引用**（§3.6）
6. **时长友好格式**（§3.7）与 **详情标签图标**（§3.8）
7. 表面契约：mode / density / 空值 / 只读 / 禁用（§4）
8. 值协议：提交归一化、多选逗号/位掩码；formula/lookup 不提交；时长按存储单位
9. 消费方迁移图（§1.3 表「目标」列）
10. 后续实现切片（§7）
11. 路线图与非目标（§8）
12. 与 OSC-0018 / e483 边界

## 3. 统一 FieldKind

`FieldKindSpec` 槽位（实现号冻结字段名）：

| 字段 | 含义 |
| --- | --- |
| `kind` | 稳定英文 id |
| `title` | 中文名 |
| `labelIcon` | 详情标签前 IconPark kebab 名（§3.8）。**禁止**命名为 `icon`（与 kind=`icon` 冲突） |
| `edit` / `display` / `search` / `filterOps` | 四表面 |
| `fullWidth` / `density` 降级 | 见 §3.1 / §4.2 |
| `serialize` | 提交归一（formula/lookup 剔除；duration 按存储单位） |

### 3.1 目录 A — 已可用（规范命名，不改语义）

`kind` 为稳定英文 id。resolve 命中后四表面必须一致。

| kind | 中文 | 命中条件 | edit | display | search | filterOps |
| --- | --- | --- | --- | --- | --- | --- |
| `text` | 文本 | String 默认 | `a-input` | 文本 | text | string 六操作符 |
| `textarea` | 多行文本 | String length≥300 | `a-textarea` | 截断文本 | text | 同 string |
| `number` | 数字 | NUMERIC_TYPES 且非目录 B | `a-input-number` | 右对齐数字 | number | number 八操作符 |
| `boolean` | 开关 | Boolean 或布尔 dataSource | `a-switch` | 徽章 | 是否下拉 | enum 四操作符 |
| `date` | 日期 | itemType=date；`typeName`∈{DateOnly,Date} | datePicker 无时 | yyyy-MM-dd | date | datetime 五操作符 |
| `datetime` | 日期时间 | DateTime / DateTimeOffset 默认 | datePicker+时 | 壁钟完整 | datetime | 同 datetime |
| `time` | 时刻 | itemType=`time` 或 `Time:*`（**不是** TimeSpan）；`typeName`∈{TimeOnly,Time} | timePicker | HH:mm:ss | time | 同 datetime |
| `select` | 单选 | 非空 dataSource 且非多选 | `a-select` | 徽章 | select | enum |
| `selectMulti` | 多选 | dataSource + multiple/multipleSelect | `a-select` multiple | 标签 | select | enum |
| `lov` | 值集单选 | lovCode 单选 | LovSelect | BatchLabel | lov | enum |
| `lovMulti` | 值集多选 | lovCode 多选 | LovSelect multiple | 标签 | lovMulti | enum |
| `cascader` | 级联（地区） | itemType=area/area4/cascader | CascaderField（**仅** `/Cube/Area`） | 路径/叶子 label | cascader | eq/neq/isNull/notNull |
| `upload` | 附件 | file / file-* | 路径+上传 | 文件链 | fileExists | isNull/notNull |
| `image` | 图片 | image | 上传+预览 | 缩略图 | fileExists | isNull/notNull |
| `json` | JSON | json | JsonEditor | 折叠摘要 | text | string |
| `html` | 富文本 | html | RichEditor | html 摘要 | text | string |
| `markdown` | Markdown | markdown | RichEditor | 摘要 | text | string |
| `color` | 颜色 | color | 色板 | 色块 | text | string |
| `email` | 邮箱 | mail | input type=email | 文本 | text | string |
| `tel` | 手机 | mobile | input type=tel | 文本 | text | string |
| `url` | 网址 | url 或字段 url | input type=url | 链接 | text | string |
| `readonly` | 只读 | Guid / 主键只读展示 | disabled input | 只读文本 | text | string |

string 操作符：`eq neq contains notContains isNull notNull`。  
number：`eq neq gt gte lt lte isNull notNull`。  
datetime：`eq after before isNull notNull`。  
enum：`eq neq isNull notNull`。  
（与现 `FILTER_OPS_BY_KIND` 对齐，按 kind 细化，**取消 5 类粗桶作为唯一真相**。）

`fullWidth`：textarea / json / html / markdown / lovMulti（与现 `isFullWidthControl` 一致；实现号可把 upload/image 保持半宽）。

### 3.2 目录 B — 已声明未接线（写入目标，实现号落地）

| kind | ItemType | 现状 | 目标 edit | 目标 display | search/filter |
| --- | --- | --- | --- | --- | --- |
| `icon` | icon | ControlType 有，FieldInput 落到 `a-input` | IconPark 选择器，值=kebab 名，选项来自 `iconRegistry`/`iconComponents` | `<icon-park :type>` | 文本；不强制图标搜索 |
| `duration` | `typeName=TimeSpan` **或** `itemType` 以 `TimeSpan` 开头（含 Int32 秒，如 OnlineTime） | 表单 `timePicker`、展示 `formatTime`、图标 `time`（三重缺陷） | **DurationInput** 按存储单位写入（默认秒）；禁止 `a-time-picker` | **用户友好中文时长**（§3.7），禁止时钟 `HH:mm:ss` | 比较用归一化秒；值控件仍是 DurationInput，禁止手输裸秒 |
| `password` | password、secret | 普通文本 | `a-input-password`；不回显明文 | `••••` 或 `-` | **无值控件**（不进搜索/筛选值） |
| `bytes` | GMK | 当数字/文本 | 数字 | GB/MB/KB（对齐 ViewHelper） | number |
| `percent` | percent、Percentage | 当数字/文本 | 数字 + `%` 后缀 | `n%`（列表可进度条，**不是**新产品「进度字段」） | number |
| `code` | code | 当 textarea/input | 等宽编辑（可复用 JsonEditor 高亮，**不**引入重型 IDE） | `<pre>` 截断 | text |
| `lovTable` | lovTable | 依赖 lovCode=List.* 隐式 | 显式 LovSelect 表格模式 | lov | lov / enum |
| `lovTableMulti` | lovTableMulti | 同上 | 表格多选 | 标签 | lovMulti / enum |
| `tree` | —（名称/树种） | 部门/菜单外键走数字或 LOV；仅 Area 有级联 | **TreeCascader** 按树种懒加载 | 路径/名称 | 级联；filter ops 同 cascader |
| `treeMulti` | — | 无 | TreeCascader multiple；值=逗号分隔 ID | 多标签 | 同 tree |
| `person` | —（名称启发式） | 筛选 overlay 用户下拉；表单无专用件 | **PersonSelect** `/Admin/User`，存 User.ID | 用户名（BatchLabel） | person ops `eq/neq` |
| `personMulti` | — | 无 | PersonSelect multiple；值=逗号分隔 ID | 多标签 | 同 person |
| `role` | — | RoleId 常靠 lovCode/dataSource | 角色下拉 `/Admin/Role` | 角色名 | enum |
| `roleMulti` | — | User.RoleIds 为 String，未必标 multiple | 角色多选；存逗号分隔（对齐 XCode `RoleIds.SplitAsInt`） | 多标签 | enum |
| `ip` | —（名称启发式） | CreateIP/UpdateIP 当普通 String | **IpInput**：IPv4/IPv6 等宽展示与格式校验；可复制 | 等宽 IP | text/eq（string 六操作符） |
| `formula` | formula / compute / computed | 无 kind；计算列当文本或不可见 | **只读**展示实体 JSON 已有值；禁止前端求值 | 按 typeName 格式化 | 可搜则按底层 typeName；默认 string |
| `lookup` | lookup（可选）或 `mapField` | FK 显示名靠 BatchLabel，无独立 kind | **只读**关联名称（Map + LOV BatchLabel） | 关联名/链接样式 | eq/neq/isNull（按标签或外键值，实现号锁定一种） |

`file-zip` 归入 `upload`（accept 可收窄为 zip，非新 kind）。  
目录 A 的 `cascader` 实现号并入 `tree`（treeKind=`area`），保留 `cascader` 别名以免旧投影断裂。

### 3.3 解析伪代码（实现号必须单测锁定）

```
resolveFieldKind(field, ctx?):   # ctx.typePath 用于 ParentID 自树
  it = lower(itemType)
  if it in {password, secret} → password
  if it == icon → icon
  if it in {percent, percentage} → percent
  if it == gmk → bytes
  if it == code → code
  if it in {lovtable} → lovTable
  if it in {lovtablemulti} → lovTableMulti
  if it in {formula, compute, computed} → formula
  if it == lookup → lookup
  # 时长必须先于数值：Int32 + ItemType=TimeSpan（OnlineTime）不是普通数字
  if typeName == TimeSpan or it starts with timespan → duration
  # itemType=time / Time:* 是时刻，不得在已命中 duration 后再抢
  if typeName == Guid → readonly
  if Boolean / 布尔 dataSource → boolean
  # 名称启发式（先于 dataSource/lov 下拉，避免部门变成普通 select）
  if matchTree(field, ctx) → inferIds(field) ? treeMulti : tree
  if matchPersonId(field) → inferIds(field) ? personMulti : person
  if matchRole(field) → inferIds(field) ? roleMulti : role
  if matchClientIp(field) → ip
  # 查找：有 mapField 且本字段不是可写外键编辑槽（只读 / 非 *Id 外键本身）
  if matchLookupDisplay(field) → lookup
  # 可写外键（DepartmentId 等）保持 tree/person/lov；其 display 槽仍走 BatchLabel
  # 以下保持现 resolveControl 余下顺序
  if dataSource nonempty → select / selectMulti
  if itemType 表 → …
  if lovCode → lov / lovMulti
  DateTime / 数值 / Enum / 枚举类 / String
```

`inferIds(name)`：字段名匹配 `/ids$/i` 为多选，否则单选。仅对白名单词干生效（§3.5），避免 `Grids` 误伤。  
显式 `field.multiple===true` 或 `itemType=multipleSelect` **覆盖** `inferIds`（强制多选）。

### 3.5 名称启发式（Draft 修订 2–3）

#### 3.5.1 树状级联 — 部门 / 地区 / 菜单

**专用件**：将现 `CascaderField`（写死 `/Cube/Area`）泛化为 `TreeCascader`，参数 `treeKind`：

| treeKind | 命中 | 懒加载 | 现状 API |
| --- | --- | --- | --- |
| `area` | itemType∈{area,area4,cascader}；字段名 `AreaId`/`AreaIds`/`AreaID` | `parentid` | 已有 `GET /Cube/Area?parentid=` |
| `department` | 字段名 `DepartmentId`/`DepartmentID`/`DepartmentIds`；显示名含「部门」且为外键 ID；lovCode 指向部门实体 | `parentid` | `GET /Cube/DepartmentSearch?parentid=`（CubeController 已有）；亦可 `/Admin/Department` |
| `menu` | 字段名 `MenuId`/`MenuID`/`MenuIds`；显示名含「菜单」且为外键 ID | `parentid` | `GET /Admin/Menu`（`MenuController` : `EntityTreeController`） |

**自身父级**：当前页 `typePath` 为 `/Cube/Area`、`/Admin/Department`、`/Admin/Menu` 时，字段名 `ParentID`/`ParentId` → 同种 `tree`（选上级节点，不含自己；实现号过滤当前行 ID）。

提交值：单选=叶子或所选节点 ID（与现 Area 叶子 ID 一致）；多选=`Ids` 字段为逗号分隔字符串。回显：沿 ParentID 向上拼路径（现 Area 逻辑复用）。

**禁止**：这些字段再走普通 `a-select` / 无树结构的 Lov 下拉。

#### 3.5.2 人员选择器

**专用件** `PersonSelect`：选项来自 `/Admin/User`（启用用户）；display 用 BatchLabel / 用户名。不要求后端 `ItemType=person`。

**ID 字段才绑选择器**（存 `User.ID`）：

| 模式 | 示例 |
| --- | --- |
| XCode 审计 ID | `CreateUserID` / `CreateUserId` / `UpdateUserID` / `UpdateUserId` |
| 中文显示名（配 ID 字段） | 显示名为 创建者 / 创建人员 / 创建人 / 更新者 / 更新人员 / 更新人 **且** 字段名以 `Id`/`ID` 结尾 |
| 通用人员外键 | `UserId` / `UserID` / `UserIds`；`CreatorId` / `UpdaterId` |

沿用并扩展现 `filterBuilder.ts` 的 `PERSON_FIELD_RE`（`Creator|Updater|CreateUser|UpdateUser|CreateBy|UpdateBy|创建者|更新者|创建人员|更新人员|创建人|更新人`）。

**字符串孪生列** `CreateUser` / `UpdateUser`（存姓名文本）：**不**用人员选择器，保持文本；表单侧本就由 `isAuditField` 隐藏。筛选若只出现姓名列，可按文本 `contains`。

**审计**：`isAuditField` 继续从新增/编辑表单去掉创建/更新用户与时间。人员控件仍用于：搜索、筛选、填色、以及**非审计**的 `UserId`/`UserIds`。

filterOps：`eq` / `neq`（及多选时的包含，实现号可先 eq/neq 与现 overlay 一致）。

#### 3.5.3 Id vs Ids 单多选

白名单词干（大小写不敏感）：`User` `Person` `Role` `Department` `Area` `Menu`，以及 `CreateUser` / `UpdateUser` 的 ID 变体。

```
RoleId / RoleID     → role（单选）
RoleIds / RoleIDs   → roleMulti（多选，逗号分隔，对齐 XCode User.RoleIds）
UserId              → person
UserIds             → personMulti
DepartmentId        → tree (department)
DepartmentIds       → treeMulti
AreaId              → tree (area)   // 与现 cascader 合流
MenuId              → tree (menu)
```

判定：`/ids$/i` 为多选，`/id$/i` 为单选；主键字段名恰好为 `Id`/`ID` → `readonly`/数字，**不**当人员或角色。

`itemType=multipleSelect` 或 `multiple=true` 时即使名为 `RoleId` 也走多选。

#### 3.5.4 终端 IP — 创建地址 / 更新地址

XCode 惯例：`CreateIP` / `UpdateIP`，显示名 **「创建地址」/「更新地址」**，存用户终端 IP 字符串（IPv4 或 IPv6），由服务端写入，不是地理「位置」字段。

**命中（任一）**：

| 来源 | 值 |
| --- | --- |
| 字段名 | `CreateIP` / `CreateIp` / `UpdateIP` / `UpdateIp`（大小写不敏感整词） |
| 显示名 | 恰好或去空白后为 `创建地址` / `更新地址` |

**专用件** `IpInput`：等宽字体；占位 `IPv4 / IPv6`；失焦校验合法 IP（非法值不阻断展示，编辑提交可提示）。display：原样 IP，可一键复制；不解析经纬度、不嵌地图。

**审计**：`createip` / `updateip` 仍在 `isAuditField` 中，**新增/编辑表单不出现**。列表、详情、搜索、筛选、填色必须走 `ip` kind，禁止当普通 `text`。

**不做**：把任意含 `IP` 的字段（如 `Vip`、`Ship`）收成 IP；飞书「位置」选点；IP 归属地查询（实现号可选后续，本规范不要求）。

### 3.6 只读公式与查找引用（Draft 修订 4）

对齐飞书「公式 / 查找引用」的**只读展示**，不引入画布公式引擎，不破坏 OSC-260819e483「不新增 projections」。

#### 3.6.1 `formula` — 公式 / 计算

| 项 | 约定 |
| --- | --- |
| 值从哪来 | **仅**实体接口已返回的属性（XCode 扩展属性、控制器补的只读列）。前端**不得**解析表达式。 |
| 命中 | **仅** `itemType` ∈ {formula, compute, computed}（大小写不敏感）。**不做**「只读且不在表单」的模糊启发式——会把主键只读列、审计隐藏列、大量列表只读列误标为公式 |
| edit | 只读控件（同 `readonly` 外观，可加「计算」标记） |
| display | 按 `typeName` 走数字/日期/文本格式化 |
| 提交 | **剔除**：`serializeSubmitModel` 不包含 formula 字段 |
| 搜索/筛选 | 若 GetPage search 分区含该字段，按 typeName 当 number/text；否则无值控件 |

`ListField.GetValue` 委托 `JsonIgnore`，SPA **拿不到** MVC 计算列。本号不要求改该委托；计算值必须成为普通 JSON 属性。

#### 3.6.2 `lookup` — 查找引用（关联名称）

| 项 | 约定 |
| --- | --- |
| 值从哪来 | 外键 ID 仍在关联字段上；**显示**用 `MapField` + `lovCode` + `POST /Admin/Lov/BatchLabel`（现 `formatFieldValue` / `fetchBatchLabel`） |
| 命中 | `itemType=lookup`；或 `mapField` 非空且本字段是映射展示列（只读、名称常为关联实体名如 `RoleName`/`DepartmentName`），**不是** `RoleId` 等可写外键 |
| 可写外键 | `RoleId`/`DepartmentId`/`UserId` 仍为 role/tree/person/lov；单元格**显示**关联名，编辑仍改 ID |
| edit（lookup 列） | 只读展示关联名，禁止把名称写回当 ID |
| display | 关联名称；可有跳转（现有 `url`/`dataAction` 保留） |
| 拉更多关联列 | 订单要「客户等级」：仅当该列已作为实体属性出现在 GetPage；**不**新开 projections。未下发则不做 |

**禁止**：查找引用写回关联表、双向同步、前端 join。

### 3.7 时长友好格式（Draft 修订 5）

对齐 Cube MVC `ViewHelper.FormatValue` 的**单位解析**，但默认展示改为用户友好中文，而不是 CLR `TimeSpan.ToString()`（`1.02:03:04`）或前端现状的时钟 `HH:mm:ss`。

#### 3.7.1 命中与存储单位

| 项 | 约定 |
| --- | --- |
| 命中 | `typeName === 'TimeSpan'` **或** `itemType` 以 `TimeSpan` 开头（忽略大小写），含 `TimeSpan:format` |
| 典型实体 | `User.OnlineTime`：`Int32` + `ItemType=TimeSpan` + Description「累计在线总时间，单位秒」 |
| 默认单位 | **秒**。数值 `90` → 90 秒 |
| 单位覆盖 | 读 `FieldMeta.description`（及显示名兜底），与 ViewHelper 同一顺序：**毫秒/`ms` → 秒 → 分 → 小时**。先匹配「毫秒」再匹配「秒」，避免「毫秒」被「秒」抢走 |
| 自定义格式 | `itemType` 为 `TimeSpan:{format}` 时，display **优先**用该 .NET 自定义格式（与 MVC 一致） |
| 提交 | 写回**同一存储单位**的数字（Int32/Int64 字段写整数秒等）；CLR TimeSpan 字段可写 `"hh:mm:ss"` 或总秒，实现号单测锁定一种并与 GetPage typeName 一致 |
| 解析入站 | 数字按存储单位；字符串兼容 `d.hh:mm:ss` / `hh:mm:ss` / ISO `PnDTnHnMnS`（若出现） |
| 不命中 | 仅 description 含「秒/小时」而**没有** TimeSpan typeName/itemType 的数字字段（如「默认7200秒」）保持 `number`，禁止靠文案把普通整数收成长时 |

#### 3.7.2 默认友好文案（无 `TimeSpan:` 自定义格式）

纯函数 `formatDuration(totalSeconds, opts?)`（实现号可放 `datetime.ts` 旁或 `fieldFormat.ts`）：

- 把存储值先换成绝对秒（或毫秒），再拆 **天 / 小时 / 分钟 / 秒**（毫秒单位时末档为毫秒）。
- **省略零档**：`3661` 秒 → `1小时 1分钟 1秒`；`90000` 秒 → `1天 1小时`；`90` 秒 → `1分钟 30秒`。
- **全零**：`0秒`（毫秒单位则 `0毫秒`）；空/null 仍为 `-`。
- **负数**：前缀 `-` 再格式化绝对值。
- **禁止**：`formatTime` / `a-time-picker` 的时钟语义；禁止把时长当壁钟时刻。
- 列表、详情、卡片、看板、搜索回显、筛选值回显共用此函数。

这是相对 MVC 默认 `ToString()` 的 SPA 体验提升，changelog 写明「缺陷修复 + 友好展示」，不是协议破坏。

#### 3.7.3 DurationInput

| 表面 | 行为 |
| --- | --- |
| edit default | 分段输入：天 / 小时 / 分钟 / 秒（单位为毫秒时末档换成毫秒）。提交归一成存储单位数字 |
| compact（筛选/填色/批量/单元格） | 同一分段，`size=small`；填色约 132px 时允许纵向堆叠或「自动最大单位 + 一个数字」（§3.9 R13）；**禁止**逼用户手填「裸秒」整数框 |
| search | 等值：DurationInput，不是 `SearchControlType=time` |
| filterOps | 与 number 相同八操作符，比较在**归一化秒**上进行 |

`itemType=time`（时刻）与 `duration` 互斥：已命中 duration 则不得再走时刻。

### 3.8 详情标签图标（Draft 修订 5）

现状：`RecordDrawer` 已把 `<icon-park>` 放在标签文字**之前**（`.detail-field__icon`）。缺陷是 `fieldIcon()` 按 typeName 粗分，**DateTime 与 TimeSpan 共用 `time`（时钟）**，且缺 person / tree / ip / formula / json / color / password 等。

**目标**：`fieldIcon(field) === spec.labelIcon`（kind 投影）。详情态每个字段标签前显示该类最佳匹配图标。

| 约束 | 约定 |
| --- | --- |
| 表面 | **强制**详情 RecordDrawer。编辑表单**不**强制加标签图标（避免表单拥挤） |
| 已有调用点 | 自动化条件等已调用 `fieldIcon` 的地方随投影纠正，**不**把图标扩展到列表表头/卡片标题（本号不要求） |
| 无障碍 | 装饰性图标 `aria-hidden="true"`，不朗读 |
| 登记 | `labelIcon` 必须是 `iconComponents.ts` 已登记 kebab 名；缺则实现号在该文件补一条，禁止运行时未登记名 |
| 树种细分 | `tree`/`treeMulti` 按 `treeKind` 换图标；其余 kind 一张表 |

**`labelIcon` 全表**（优先用已登记名）：

| kind | labelIcon | 理由 |
| --- | --- | --- |
| text / textarea | `font-size` | 文本（须已登记于 iconComponents） |
| number | `list-numbers` | 数值 |
| boolean | `switch` | 开关 |
| date | `calendar` | 日历，不是时钟 |
| datetime | `time` | 壁钟日期时间 |
| time（时刻） | `time` | 时钟 |
| duration | **`timer`** | 计时/时长；**禁止**再用 `time` |
| select / selectMulti | `tag` | 选项 |
| lov / lovMulti / lovTable / lovTableMulti | `tag` | 值集 |
| cascader / tree（area） | `network-tree` | 行政区划树 |
| tree（department） | `building-one` | 组织 |
| tree（menu） | `tree-list` | 菜单树 |
| treeMulti | 同对应 treeKind | |
| person / personMulti | `people` | 人员 |
| role / roleMulti | `permissions` | 角色权限 |
| ip | `computer` | 终端地址 |
| upload | `file-text` | 附件 |
| image | `pic` | 图片 |
| json / code | `data` | 结构化/代码 |
| html / markdown | `edit` | 富文本 |
| color | `background-color` | 色板 |
| email | `mail` | |
| tel | `phone` | |
| url | `link` | |
| password | `preview-close` | 不可见 |
| icon（字段值=图标名） | `star` | 与 labelIcon 字段名区分 |
| formula | `lightning` | 计算 |
| lookup | `link` | 关联 |
| bytes | `inbox` | 容量 |
| percent | `chart-line` | 比例 |
| readonly / Guid / 主键 | `key` | 标识 |

主键/Guid 仍可覆盖 number 的 `list-numbers`（与现状 `fieldIcon` 主键→key 一致）。可写外键 `DepartmentId` 走 tree 图标，**不要**因 lovCode 再打成 `link`（现状 Map 外键一律 `link` 是粗映射，实现号按 kind 纠正）。

### 3.9 方案自审修正（Draft 修订 5）

| # | 发现 | 修正 |
| --- | --- | --- |
| R1 | 只写 `typeName=TimeSpan` 会漏掉 `Int32`+`ItemType=TimeSpan`（OnlineTime） | duration 命中 itemType 前缀；且必须先于数值 |
| R2 | `itemType=time` 与 TimeSpan 可能互相抢 | duration 先命中；时刻仅 `time`/`Time:*`/`TimeOnly` |
| R3 | 目录 A `cascader` 与目录 B `tree` 双轨 | 实现号并入 `tree`+`treeKind=area`，`cascader` 仅别名 |
| R4 | 缺 `DateTimeOffset` / `DateOnly` / `TimeOnly` | 分别映射 datetime / date / time |
| R5 | formula「只读且不在表单」过宽 | **删除**模糊启发式，只认 itemType |
| R6 | spec 若叫 `icon` 与 kind=`icon` 撞名 | 字段名固定 `labelIcon` |
| R7 | 时长筛选若用 number 输入会逼用户填秒 | filter 值控件=DurationInput；比较才用秒 |
| R8 | 详情图标未标装饰性 | `aria-hidden` |
| R9 | 把标签图标扩到列表/卡片会噪 | 本号只强制详情；不新增列表表头图标 |
| R10 | lookup 若先于 DepartmentId 会把可写外键吃掉 | 树/人员/角色启发式仍在 mapField lookup 之前（§3.3 已有，保持） |
| R11 | MVC 默认 `TimeSpan.ToString()` 不够友好 | SPA 默认中文省略零档；有 `TimeSpan:format` 仍跟后端 |
| R12 | `inferDateKind(TimeSpan)→time` 会污染 formatFieldValue | 实现号：kind=duration 走 `formatDuration`，禁止再调 `formatTime` |
| R13 | 填色值区约 132px，四分段 DurationInput 放不下 | compact 允许纵向堆叠或「自动最大单位 + 一个数字」；比较仍用归一化秒 |
| R14 | description「默认7200秒」若当单位会误伤普通 Int32 | 单位关键字**仅**在已命中 duration 后解析 |

## 4. 表面契约

### 4.1 mode

| mode | 用途 | 组件 |
| --- | --- | --- |
| `edit` | 写入 | FieldWidget → 专用件 |
| `display` | 只读展示 | formatFieldValue / fieldBadge / 缩略图；详情同 |
| `search` | 高级搜索等值（OSC-0016） | 无范围 `_min/_max` |
| `filter` | 筛选/填色/自动化条件 | 先选 op，再值；`isNull/notNull` 无值控件 |

### 4.2 density

| density | 使用处 | 约束 |
| --- | --- | --- |
| `default` | FormContent、DefaultObject、RecordDrawer 编辑 | 完整预览（图片缩略图、JSON 编辑器） |
| `compact` | 筛选/填色/批量/单元格弹层 | `size=small`；json/html/markdown → textarea；image 不预览只路径；宽度随弹层（填色值区约 132px） |

实现号：同一 spec，不写第二套 kind。

### 4.3 只读 / 禁用 / 空值

| 条件 | 行为 |
| --- | --- |
| `field.readOnly` 或 mode=display 或 kind∈{formula,lookup} | 不发射有效写入；edit 渲染 disabled |
| `disabled` prop | 全部控件 disabled |
| 值为 null / `''` | display 显示 `-`（现 formatFieldValue） |
| 审计字段 | 新增/编辑表单不展示（现 `isAuditField`，含 CreateIP/UpdateIP） |
| 租户字段 | 多租户关闭时隐藏（现 `isTenantField`） |
| password | display 永不展示明文；search/filter 无值控件 |
| ip | display 等宽 IP；search/filter 用 IpInput compact |
| duration | display 走 `formatDuration`；search/filter 用 DurationInput compact，禁止 timePicker |

状态唯一来源：`FieldMeta`（GetPage）+ 调用方传入的 `mode`/`density`/`disabled`。禁止弹层再维护平行 `controlType` 字符串（批量今日的 `batchRowControlType` 实现号删掉，改 resolveFieldKind）。

### 4.4 FieldWidget props / emits（实现号锁定）

```
props:
  field: FieldMeta
  modelValue?: unknown
  mode: 'edit' | 'search' | 'filter'   // display 走纯函数，不强制进 Widget
  density?: 'default' | 'compact'      // 默认 default
  disabled?: boolean
  typePath?: string                    // 上传
  op?: ViewFilterOp                    // 仅 filter；isNull/notNull 时不渲染值
emits:
  update:modelValue: [unknown]
  search?: []                          // 仅 search，回车触发（对齐 SearchFieldInput）
```

DOM 顺序：控件本体 100% 宽；compact 不额外 label（label 由弹层行提供）。空字段名：不渲染值控件。

不做的交互：单元格 Canvas 内嵌编辑（仍走弹层）；筛选 AND/OR 语法变更（仍用现 ViewFilter）。

## 5. 值协议（不新开协议）

继续 OSC-0008：

- 数值字符串 → number（Int64 超安全整数保留字符串）
- Boolean `'true'/'1'` → true
- 枚举类纯数字字符串 → number
- 多选：位掩码字段 `keysToBitmask`，否则逗号分隔字符串
- **formula / lookup 列不进入提交体**（实现号在 `serializeSubmitModel` 过滤）
- **duration**：按 §3.7 存储单位写回数字；禁止把友好中文或时钟字符串原样提交

实现号：`normalizeSubmitValue` / `serializeSubmitModel` **保留函数名**；内部可改用 FieldKind，但单测断言不得回退。

## 6. 本号文件地图 vs 实现号文件地图

### 6.1 本号实际改动（文档）

| 路径 | 动作 |
| --- | --- |
| `openspec/changes/OSC-26082097c1 字段组件规范化/**` | 本五件套 + ui |
| `web/docs/字段组件规范.md` | 执行期新建（T4） |
| `web/README.md` | 执行期最小登记（T6，可选） |

**禁止本号修改**：任何 `.ts` `.vue` `.cs`。

### 6.2 实现号建议改动（不在本号执行）

| 文件 | 动作 | 保留 |
| --- | --- | --- |
| 新建 `core/utils/fieldKinds.ts` + `fieldKinds.spec.ts` | FieldKind 联合类型、spec 表、`resolveFieldKind` | — |
| `core/utils/fieldControl.ts` | `resolve*` 改为投影；ITEM_TYPE 表并入 fieldKinds | `normalizeSubmitValue` `serializeSubmitModel` `isAuditField` `isTenantField` 签名 |
| `core/utils/filterBuilder.ts` | `FILTER_OPS_BY_KIND` 从 spec.filterOps 派生；`isPersonField` 保留 | ViewFilter 结构 |
| 新建 `components/FieldWidget.vue` + `useFieldWidget.ts` | mode+density 调度 | LovSelect / JsonEditor / RichEditor |
| `FieldInput.vue` | 薄封装 `FieldWidget mode=edit` 或删除并改引用 | 调用方 props `field/modelValue/disabled/typePath` |
| `SearchFieldInput.vue` | 薄封装 `mode=search` | press-enter search |
| `BatchEditValueInput.vue` | 改为 FieldWidget compact 或删除 | 批量提交 API |
| `FilterBuilderPopover.vue` / `FormatPopover.vue` | 值区换 Widget `mode=filter density=compact` | 条件 AST / 填色规则 schema |
| `DefaultList.vue` 单元格弹层 | 换 Widget，禁止纯 a-input | PATCH/单元格保存 |
| `CascaderField.vue` / `useCascaderField.ts` | 泛化为 TreeCascader：`treeKind=area\|department\|menu`，保留 Area 默认以免旧引用崩 | `/Cube/Area` 懒加载行为 |
| 新建 `PersonSelect.vue` + `usePersonSelect.ts` | `/Admin/User` 单/多选 | 筛选里现有用户下拉可删除 |
| 新建 `IpInput.vue` + `useIpInput.ts`（或并入 FieldWidget 分支） | IPv4/IPv6 校验与等宽展示 | 不引入地图 SDK |
| `core/utils/fieldNormalize.ts` / `FieldMeta` | 透传 `mapField` | 现有归一字段 |
| `core/utils/iconRegistry.ts` | `fieldIcon` 改为 `resolveFieldKind` → `labelIcon` 投影 | 函数名 `fieldIcon` 可保留 |
| `views/crud/RecordDrawer.vue` | 标签前图标继续；`aria-hidden`；值走 formatDuration | 现有分组/折叠 DOM |
| `core/utils/datetime.ts` / `fieldFormat.ts` / `detailFormat.ts` | `formatDuration`；TimeSpan 不再走 `formatTime` / `inferDateKind→time` | `formatDate`/`formatDateTime`/`formatTime` 仍服务时刻与日期 |
| 缺口专用件 | IconPicker、DurationInput、角色下拉、formula/lookup 只读展示 | iconRegistry 现有名表；新图标须登记 `iconComponents.ts` |

## 7. 后续实现 OSC 切片（建议，各自新 ID）

1. **注册表 + 投影**：`fieldKinds.ts` + 把 `resolve*` 改为投影；单测锁目录 A 与 TimeSpan/icon 命中。行为与今日目录 A 一致（除测试锁定的错误映射可在本切片修 duration/icon）。`fieldIcon` 改为 labelIcon 投影；`formatDuration` 替换 TimeSpan 的 `formatTime`。
2. **FieldWidget 替换副本**：FormContent / DefaultObject / SearchDrawer / 筛选 / 填色 / 批量 / 单元格 / 自动化 ops。
3. **目录 B 专用件**：TreeCascader、PersonSelect、RoleId/Ids、IpInput、**formula/lookup 只读展示**；password / bytes / percent / code / lovTable；IconPicker；DurationInput。详情标签 `aria-hidden`。

切片 1 的 `resolveFieldKind` 单测须覆盖 formula/lookup 命中与「可写外键不是 lookup kind」。须覆盖 `Int32+ItemType=TimeSpan`→duration、`formatDuration` 省略零档、duration 的 `labelIcon=timer`。

## 8. 路线图（不进本号目录）

条码、位置、评分、签名、对外表单视图、飞书式人员头像协作、**用户脚本公式**、**查找引用拉未下发的关联表其它列（projections）**、双向关联写回。

## 9. 关键设计决策

| # | 决策 | 理由 |
| --- | --- | --- |
| D1 | 一个 FieldKind，四表面是 spec 槽位不是四套枚举 | 对齐飞书；消灭副本 |
| D2 | display 用纯函数，不强制进 Vue | 列表/VTable 每 cell 高频；现 formatFieldValue 已缓存 |
| D3 | 人员/树/角色是目录 B FieldKind，靠字段名推断，不强制 ItemType | 用户 Draft 修订 2；XCode 惯用名已稳定 |
| D4 | 树启发式优先于 dataSource 下拉 | 避免 DepartmentId 被字典做成扁平 select |
| D5 | `Ids` 后缀仅白名单词干 | 防止无关字段误变多选 |
| D6 | 注册表先行，不拆 21 个 vue | 多数 kind 是 Arco 原生件 + 参数 |
| D7 | 本号纯文档 | 与 0018 同；实现需独立批准 |
| D8 | TimeSpan 目标=用户友好时长不是时钟 | 后端常存秒（含 Int32+ItemType）；当前 timePicker/`formatTime` 是缺陷 |
| D9 | CreateUser 文本列不用人员选择器 | 存的是姓名快照；选择器只绑 *UserID / UserId |
| D10 | CreateIP/UpdateIP 用终端 IP 控件 | XCode 显示名「创建地址/更新地址」；不是文本也不是地图 |
| D11 | 公式只读且只展示服务端已有 JSON 字段 | 禁止客户端引擎；对齐 e483 无 projections；**只认 itemType** |
| D12 | 查找引用 = Map/LOV 关联名，外键仍可写 | 写回 ID；名称列只读；不 join 未下发列 |
| D13 | 详情标签图标来自 `labelIcon`，时长用 `timer` | 现 DOM 已在标签前；纠正 TimeSpan 共用时钟；不强制编辑表单图标 |
| D14 | SPA 默认中文省略零档，自定义 `TimeSpan:format` 跟 MVC | 用户友好优先；实体仍可声明精确格式 |

## 10. 文档影响

| 文档 | 影响 |
| --- | --- |
| `web/docs/字段组件规范.md` | 本号权威交付（执行期） |
| `web/README.md` | 登记本号（可选） |
| 迁移方案 / 竞品分析 | 可选回写「字段注册表已立项 OSC-26082097c1」 |
| OSC-0018 文档 | 交叉引用：L0 控件以本号为准 |

## 11. 测试设计

本号 N/A。实现号最低：

- `fieldKinds.spec.ts`：目录 A/B 命中；Guid/Boolean 优先；password 无 search 值控件；`RoleId`→role、`RoleIds`→roleMulti；`DepartmentId`→tree 而非 lookup；`CreateUserID`→person；`CreateUser`→非 person；`CreateIP`→ip；`Vip` 非 ip；`itemType=formula`→formula 且不提交；只读无 itemType 的列**不是** formula；`mapField` 只读展示列→lookup；`Int32+itemType=TimeSpan`→duration 不是 number；`itemType=time` 且非 TimeSpan→time。
- `formatDuration` / `fieldIcon`：`90`→`1分钟 30秒`；`0`→`0秒`；TimeSpan 图标 `timer` 不是 `time`；DateTime 仍 `time`；date 用 `calendar`；DepartmentId 用 `building-one` 不是 `link`。
- 投影函数：与现 `fieldControl.spec.ts` / `filterBuilder.spec.ts` / `iconRegistry.spec.ts` / `datetime.spec.ts` 对齐后改期望（duration、icon、人员不再只 overlay）。
- 构建：`vue-tsc` + Cube 编译。无组件测试门槛（与现状一致），不强制本切片引入 Vue Test Utils。

## 12. 风险

| 风险 | 缓解 |
| --- | --- |
| 文档与代码枚举不一致 | verify 交叉核对 AC；以 field.ts 22 个 ControlType 为准 |
| 实现号一次替换爆掉批量/填色 | 切片 2 按消费方逐个替换，薄封装保留旧文件名 |
| 把脚本公式/projections 做成控件 | §3.6 与 e483 冻结；只读展示已有字段 |
| 部门级联无现成懒加载页 | 优先复用已有 `DepartmentSearch`；菜单走 EntityTree 列表 `parentid`；缺参则实现号补查询参数而非新协议 |
| TimeSpan 修解析导致已存「时钟」表单行为变化 | 切片 1 单测+手工：在线时长类字段；changelog 写明缺陷修复 |
