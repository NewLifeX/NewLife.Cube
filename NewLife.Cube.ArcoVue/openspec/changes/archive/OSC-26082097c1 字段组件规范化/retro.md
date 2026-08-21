# OSC-26082097c1 Retro

> 复盘在验收通过后由 openspec-retro 填写；归档后状态置 `Done`。

## 结果摘要

| 维度 | 结果 |
|------|------|
| AC 通过率 | 18/18 通过（AC-01~18） |
| 三步编排 | 实现审计 ✅ → 代码审查（零代码，🔴 无）→ 文档同步 ✅ |
| 自动化门禁 | N/A（纯文档，proposal §7 声明） |
| 交付物 | 五件套 + `ui/` + `web/docs/字段组件规范.md`（15 节）+ README 入口 |
| 交叉核对 | ControlType 22 / Search 11 / List 15 / FilterFieldKind 5 / ITEM_TYPE 18 键 / 消费方 12 路径 / labelIcon 27 名全部一致 |
| 缺口 | 无（P0/P1/P2 均无） |

## 实际完成范围

- **目录与契约**：FieldKind 注册表设计——目录 A 22 种已可用（四表面行表）+ 目录 B 17 种缺口目标行为（icon/duration/password/bytes/percent/code/lovTable/lovTableMulti/tree*/person*/role*/ip/formula/lookup）。
- **解析顺序**：itemType 缺口 → formula/lookup 仅 itemType → **duration 先于数值**（Int32+ItemType=TimeSpan）→ Guid/Boolean → 树/人员/角色/IP 名称启发式 → lookup 展示列 → dataSource/lov/其余。
- **启发式**：TreeCascader（area/department/menu + ParentID 自树）、PersonSelect（XCode 惯用名 + Id 后缀）、Id vs Ids 白名单词干、终端 IP（CreateIP/UpdateIP 与「创建地址/更新地址」，**不是地图**）。
- **只读边界**：formula 仅服务端已有 JSON 值（禁脚本/禁浏览器求值）、lookup 仅 Map+BatchLabel 关联名（禁 projections/禁双向写回）；二者不进提交体。
- **时长与图标**：`formatDuration` 友好中文省略零档（禁时钟 HH:mm:ss）；详情 labelIcon 全表 27 名（duration=`timer`，禁与 DateTime 共用 `time`），全部已在 `iconComponents.ts` 登记。
- **消费方迁移图**：10 表面（表单/对象页/详情/列表卡片看板/搜索/筛选/填色/批量/单元格/自动化条件）无「等」。

## 做得好的

1. **创建期即固化审计基线**：四套枚举映射表与目录 A/B 在 Draft 期写入 design.md，执行期只需复核（T1 子项「执行期复核」），避免执行期再猜映射——纯文档 OSC 的理想节奏。
2. **自审驱动的五轮 Draft 修订**：修订 2–5 逐轮收窄（撤销「人员仅 overlay」、补终端 IP、补只读公式/查找、补时长与 labelIcon），每轮都有明确的用户确认或自审依据；修订 5 一次抓出 7 处潜在错误（Int32+TimeSpan、duration 先于数值、formula 模糊命中收窄、DateOnly/TimeOnly、cascader 并入 tree、筛选禁裸秒、aria-hidden）。
3. **零代码边界守得干净**：全程未改 `.ts/.vue/.cs`；验收期 `git status` 核查确认本号交付仅 docs + openspec + README 入口，工作区并行 OSC 的代码改动未混入。
4. **交叉核对防虚构有效**：T5 与验收两轮 grep（枚举成员数、ITEM_TYPE 键、消费方路径、labelIcon 登记）全部实测；一次误报（`model`/`fields` 函数参数被误当映射键）通过读源码澄清，未造成文档返工。
5. **纠错即时补录**：`font-size-two` 未登记名在 T7 会话补录并同步 design + 持久文档，未拖到验收。

## 待改进

1. **验收脚本的 grep 模式需区分「定义」与「出现」**：首次 ITEM_TYPE 核对误把函数参数 `model:`/`fields:` 当作映射键、labelIcon 核对误把无引号键当缺失。教训：交叉核对脚本应锚定定义上下文（如 `const X = {` 块内），或核对后读源码二次确认，避免「False 即缺口」误判。
2. **消费方路径应在 design 期写全限定**：`useAutomationActionCard` 在 design/tasks 未写目录前缀，验收定位时先 False 再人工补全路径。纯文档 OSC 的路径引用也应带相对路径。
3. **README 入口位置**：本号入口插在「壳与 UserProfile」节之前，成为独立二级标题；后续文档入口宜集中在「文档索引」类小节，避免 README 章节序被文档号打乱。

## 偏差

- 无范围性偏差。T7（labelIcon `font-size-two`→`font-size`）为执行期纠错，已补录 tasks 与 status note。

## 遗留与后续

- 实现切片 §12 三步（注册表+投影 → FieldWidget 替换副本 → 目录 B 专用件）待另立 OSC；切片内单测最低覆盖清单已写入文档（duration 命中/formatDuration/timer 图标/formula 仅 itemType/可写外键非 lookup/CreateUser 非 person/Vip 非 ip）。
- `CascaderField` 目前仅绑 `/Cube/Area`，泛化为 TreeCascader 是实现号工作（本号只规定契约）。
- 工作区并行 OSC（AI 助手、OSC-0018、OSC-26081903c0）未提交改动与本号无关，复盘提交已排除。

## 过程备注

- 用户确认：OpenSpec 纯文档；全表面 FieldKind；目录=现有 22 ControlType + 已声明未接线 ItemType；飞书独有类型只路线图。
- 创建 ID：`OSC-26082097c1`（2026-08-20 Asia/Shanghai 随机 hex，已查重 changes/ 与 archive/）。
- 与 OSC-0018 正交：0018 页面自定义分层；本号 L0 字段控件契约。
- 创建期已把审计表与目录 A/B 写入 design.md，避免执行期再猜映射。
- **Draft 修订 2（2026-08-20）**：部门/地区/菜单专用级联、人员专用选择器、Id vs Ids 单多选。撤销「人员仅 overlay」。
- **Draft 修订 3（2026-08-20）**：用户要求「创建地址 / 更新地址」为终端 IP 专用控件（`CreateIP`/`UpdateIP`）。不进增改表单（审计），列表/详情/搜索走 `ip`；禁止当文本或地图。
- **Draft 修订 4（2026-08-20）**：只读公式/计算（仅展示服务端 JSON 已有值）；只读查找引用（Map + LOV BatchLabel 显示关联名）。可写外键仍改 ID。禁止脚本公式、projections、双向写回。
- **Draft 修订 5（2026-08-20）**：TimeSpan → 用户友好中文时长（省略零档）；详情按 FieldKind `labelIcon` 显示在标签前（时长 `timer`）。自审修正：Int32+ItemType=TimeSpan、duration 先于数值、收窄 formula 模糊命中、DateTimeOffset/DateOnly/TimeOnly、cascader 并入 tree、筛选不用裸秒、装饰图标 aria-hidden。
- **执行（2026-08-20）**：批准并落地 T4 持久文档 + T5 核对 + T6 README；纠错 labelIcon `font-size`。纯文档，无业务代码。
