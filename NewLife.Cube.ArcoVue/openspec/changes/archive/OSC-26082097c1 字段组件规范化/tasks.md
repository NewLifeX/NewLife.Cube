# OSC-26082097c1 Tasks — 字段组件规范化

> 本号纯文档。依赖：T1 审计（创建期基线已写入 design.md §1）→ T2 固化目录与契约（design §3~4）→ T4 写持久文档 → T5 交叉核对。无代码测试勾选项。

## 研究与固化

- [x] **T1 审计后端 TypeName / ItemType 与前端四套枚举**
  - 对照：`NewLife.CubeNC/ViewModels/DataField.cs`、`field.ts` ControlType 22 / Search 11 / List 15、`fieldControl.ts` ITEM_TYPE_TO_CONTROL 与 resolve 优先级、`filterBuilder.ts` FilterFieldKind 5
  - 产出：design.md §1 映射表（创建期已写入；执行期复核有无漏项）
  - [x] 创建期基线已固化
  - [x] 执行期复核：ControlType 22 与 `field.ts` 一致；ITEM_TYPE 含 area4；消费方路径均存在
- [x] **T2 固化 FieldKind 目录 A/B 与解析顺序**
  - 目录 A 22 种已可用；目录 B 含 icon/duration/password/bytes/percent/code/lovTable、tree/person/role/ip、**formula/lookup**
  - 解析：itemType 缺口与 formula/lookup → **TimeSpan/duration 先于数值** → Guid/Boolean → 树/人员/角色/IP 启发式 → mapField 展示列 lookup → 其余 resolveControl
  - Id vs Ids 仅白名单词干；`isAuditField` 不取消；formula/lookup 不提交
  - 产出：design.md §3（含 §3.5 / §3.6 / **§3.7 时长** / **§3.8 labelIcon** / §3.9 自审；修订 5 已写入）
  - [x] 创建期 + 修订 2–5 基线已固化
- [x] **T3 穷尽消费方与表面契约**
  - 消费方：FormContent、DefaultObject、RecordDrawer、fieldFormat/fieldBadge、SearchDrawer、FilterBuilderPopover、FormatPopover、BatchEditValueInput、DefaultList 单元格弹层、automation FILTER_OPS
  - mode / density / props 见 design §4 与 `ui/information-architecture.md`
  - [x] 创建期基线已固化
  - [x] 执行期路径复核：所列 `.vue`/`.ts` 均 True

## 编写

- [x] **T4 编写持久文档（核心交付）**
  - 新建 `NewLife.Cube.ArcoVue/web/docs/字段组件规范.md`
  - 按 design.md §2 成文；目录表与 §3.1/§3.2/§3.5/§3.6/§3.7/§3.8 **逐行一致**
  - 须含 TreeCascader / PersonSelect / RoleId·Ids / IpInput / **只读 formula 与 lookup（Map/BatchLabel）** / **formatDuration 友好时长** / **详情 labelIcon 全表**
- [x] **T5 交叉核对与自审**
  - 按 verify.md AC 逐条；grep 四套枚举成员数、ITEM_TYPE 键、消费方文件
  - 确认脚本公式 / projections / 双向写回未写入目录 A/B 表体；formula/lookup **应在**目录 B
  - 确认 duration 命中 Int32+ItemType=TimeSpan；详情图标在标签前且 duration=`timer`；formula 无模糊启发式
- [x] **T6 登记（可选最小增量）**
  - `web/README.md` 增加本号文档入口
  - 不改功能清单业务项（无代码）

## 会话补录

- [x] **T7 执行期纠错：labelIcon `font-size-two` → `font-size`**
  - design §3.8 误写未登记名；改为 `iconComponents.ts` 已有 `font-size`；持久文档同步

## 明确不勾选

- 不新建 `fieldKinds.ts` / `FieldWidget.vue`
- 不改 `resolveControl` 行为
- 不补 icon 选择器、DurationInput 等实现

## 测试 / 构建记录（本号）

- 代码测试：**N/A**（纯文档，proposal 声明）
- 未修改任何 `.ts` / `.vue` / `.cs`（本 OSC 交付仅 docs + openspec + README）
