# OSC-26082097c1 Verify

> 状态：**通过**（openspec-verify 2026-08-21）
> checklist: **passed**（18/18 AC，零缺口）
> 进入 `Validating` 后逐项勾选。纯文档 OSC，代码门禁 N/A。

## 执行阶段记录（openspec-apply）

- T4：`NewLife.Cube.ArcoVue/web/docs/字段组件规范.md` 已新建（15 节，含时长/labelIcon/消费方迁移/实现切片）
- T5 grep：
  - `ControlType`：`field.ts` 22 成员与文档 §2.3 一致
  - `ITEM_TYPE_TO_CONTROL`：file/image/json/html/markdown/color/icon/mail/mobile/url/singleselect/multipleselect/area/area4/cascader/date/datetime/time 均写入文档
  - 消费方路径：FieldInput、SearchFieldInput、BatchEditValueInput、FilterBuilderPopover、FormatPopover、FormContent、DefaultObject、DefaultList、RecordDrawer、filterBuilder、SearchDrawer、useAutomationActionCard 均存在
- T6：`web/README.md` 增加「字段组件规范（OSC-26082097c1）」入口
- T7：design `font-size-two` → `font-size`（已登记名）
- 测试：N/A（纯文档）
- 零代码：本号未改 `.ts/.vue/.cs`

## 验收阶段记录（openspec-verify，2026-08-21）

### 三步编排摘要

| 步骤 | 结论 |
| --- | --- |
| 实现审计 | T1–T7 全勾选；交付物齐全（五件套 + `ui/` + `web/docs/字段组件规范.md` 15 节 + README 入口）。proposal §1 四目标均有落点，无实现缺口（本号不实施代码） |
| 代码审查 | 本 OSC 未触及任何 `.ts/.vue/.cs`（工作区其他代码改动均属并行 OSC：AI 助手、OSC-0018、OSC-26081903c0、FormatPopover 等），🔴 无 |
| 文档同步 | 规范文档与 `field.ts`/`fieldControl.ts`/`filterBuilder.ts`/`iconComponents.ts` 逐项核对一致；README diff 仅含本号入口 |

### 交叉核对实测（2026-08-21）

| 项 | 实测 | 结论 |
| --- | --- | --- |
| `ControlType` 成员 | `field.ts` L2–24 恰 **22** 个（input…readonly），与文档 §2.3 列举一致 | ✅ |
| `SearchControlType` / `ListControlType` | 11 / 15，与文档 §2.3 一致 | ✅ |
| `FilterFieldKind` | `filterBuilder.ts` L41 恰 5 个（enum/string/person/number/datetime） | ✅ |
| `ITEM_TYPE_TO_CONTROL` 键 | L79–98 恰 **18** 键（file…time），全部出现在文档 §2.3（`model`/`fields` 为函数参数非映射键，前次误报已澄清） | ✅ |
| 消费方路径 | FieldInput / SearchFieldInput / BatchEditValueInput / FilterBuilderPopover / FormatPopover / FormContent / DefaultObject / DefaultList / RecordDrawer / filterBuilder / SearchDrawer / automation `useAutomationActionCard.ts`（实际位于 `views/crud/automation/`）全部存在 | ✅ |
| §8 labelIcon 27 名 | 全部已在 `iconComponents.ts` 登记（含 `timer`/`font-size`；`font-size-two` 未出现） | ✅ |
| 零代码核查 | `git status` 中本号交付仅 openspec 目录 + `web/docs/字段组件规范.md` + README 入口，无业务代码 diff | ✅ |

### 目标愿景对照（proposal §1）

| 目标 | 结论 |
| --- | --- |
| 1 可追溯字段目录（TypeName/ItemType ↔ 四套枚举 ↔ FieldKind，四表面） | ✅ 文档 §2.3 + §3 目录 A 四列表 |
| 2 冻结表面契约 + 消费方穷尽 | ✅ §9（mode/density/值协议）+ §11 迁移图 10 表面无「等」 |
| 3 启发式专用件 / 只读公式与查找 / 友好时长 / labelIcon | ✅ §5 / §6 / §7 / §8 |
| 4 实现 OSC 切片（本号不写代码） | ✅ §12 三步切片 + 单测最低覆盖；零代码 ✅ |

### 缺口清单

**无缺口**（P0/P1/P2 均无）。无需用户决策放行事项。

### 验收结论

18/18 AC 通过（AC-01~18），checklist **passed**，保持 Validating，可复盘。

## 验收标准

### 交付物

- [x] **AC-01 持久文档**：`web/docs/字段组件规范.md` 存在，含 design.md §2 列出的各节（含时长 §3.7 与详情图标 §3.8）。（执行期自检；正式勾选留给 verify）
- [x] **AC-02 五件套完整**：本目录含 proposal / design / tasks / verify / retro / status / `ui/information-architecture.md`。
- [x] **AC-03 零代码**：本 OSC 未修改任何 `.ts` `.vue` `.cs`（允许新增 `web/docs/` 与可选 README 登记）。

### 目录与契约

- [x] **AC-04 目录 A**：规范文档列出的已可用 kind 覆盖 `field.ts` 的 22 个 `ControlType` 语义（icon 在目录 B 写目标，不假装 FieldInput 已有选择器）。
- [x] **AC-05 目录 B**：至少含 icon、duration、password、bytes、percent、code、lovTable、lovTableMulti、tree/person/role/ip、**formula、lookup**；每条有现状 vs 目标。
- [x] **AC-06 解析顺序**：写明 itemType 缺口与 TimeSpan；**duration 先于数值**（Int32+ItemType=TimeSpan）；Guid / Boolean 之后、dataSource 之前命中树/人员/角色启发式。
- [x] **AC-07 四表面**：每个目录 A kind 有 edit / display / search / filterOps；password 明确无搜索/筛选值控件。
- [x] **AC-08 density**：default vs compact 的使用处与 compact 降级规则（json/html/markdown→textarea、image 不预览）写死。

### 消费方

- [x] **AC-09 迁移表**：覆盖 FormContent、DefaultObject、RecordDrawer、列表 format、SearchDrawer、FilterBuilderPopover、FormatPopover、BatchEditValueInput、DefaultList 单元格弹层、自动化条件，无「等」。
- [x] **AC-10 名称启发式**：文档写死 (1) 部门/地区/菜单 → TreeCascader 及 API；(2) 人员 ID 字段命中 XCode 创建者/更新者惯用名 + UserId；(3) `RoleId` 单选 / `RoleIds` 多选；(4) **`CreateIP`/`UpdateIP` 与显示名「创建地址/更新地址」→ `ip`，非地图**；`CreateUser` 文本列不是人员选择器；主键 `Id` 不误判；`Vip` 不是 ip。
- [x] **AC-10b 审计**：声明 `isAuditField` 仍隐藏创建/更新用户**与 IP、时间**于新增/编辑表单；人员/IP 控件用于列表/详情/搜索/筛选/非审计 UserId。

### 边界

- [x] **AC-11 与 0018**：文档声明 0018=页面自定义分层，本号=L0 字段控件。
- [x] **AC-12 非目标**：用户脚本公式、GetPage projections、双向写回、条码/位置/评分/签名不在目录 A/B。**只读** formula/lookup **必须**在目录 B。
- [x] **AC-12b 查找边界**：写明可写外键（如 DepartmentId）不是 lookup kind；lookup 只显示 Map/LOV 关联名；未下发的关联列不做。
- [x] **AC-12c 时长**：写明友好中文省略零档、禁止时钟 `HH:mm:ss`、单位解析对齐 ViewHelper、`TimeSpan:format` 优先、筛选值控件不是裸秒、搜索不是 timePicker。
- [x] **AC-12d 详情图标**：每个 kind 有 `labelIcon`；duration=`timer` 不是 `time`；图标在标签文字之前；装饰性 `aria-hidden`；不强制编辑表单图标；不扩到列表表头。
- [x] **AC-12e formula 命中**：只认 itemType，无「只读且不在表单」模糊规则。
- [x] **AC-13 实现切片**：≥3 步（注册表、Widget 替换副本、目录 B 专用件），声明另立 OSC、本号不实施。

### 交叉核对（防虚构）

- [x] **AC-14 枚举**：`ControlType` 成员与 `web/src/core/types/field.ts` 一致。
- [x] **AC-15 ItemType 表**：`fieldControl.ts` 的 `ITEM_TYPE_TO_CONTROL` 键均在文档出现。
- [x] **AC-16 消费方路径**：AC-09 所列文件在工作区存在。

### 门禁

- [x] **AC-17 测试 N/A**：未声称跑过 Vitest/XUnit 作为本号通过条件。

### 收尾补录 AC

- [x] **AC-18**：执行期纠错 design `labelIcon` 文本图标为已登记 `font-size`（非 `font-size-two`）。

## 自动化门禁（T5 执行）

```powershell
$web = "f:\Git Repos\1.Newlife\NewLife.Cube\NewLife.Cube.ArcoVue\web"
$osc = "f:\Git Repos\1.Newlife\NewLife.Cube\NewLife.Cube.ArcoVue\openspec\changes\OSC-26082097c1 字段组件规范化"

# 持久文档
Test-Path "$web\docs\字段组件规范.md"

# ControlType 行
Select-String -Path "$web\src\core\types\field.ts" -Pattern "^\s+\| '"

# ITEM_TYPE 键
Select-String -Path "$web\src\core\utils\fieldControl.ts" -Pattern "^\s+(file|image|json|html|markdown|color|icon|mail|mobile|url|singleselect|multipleselect|area)"

# 消费方
@(
  "src\components\FieldInput.vue",
  "src\components\SearchFieldInput.vue",
  "src\views\crud\BatchEditValueInput.vue",
  "src\views\crud\FilterBuilderPopover.vue",
  "src\views\crud\FormatPopover.vue",
  "src\views\crud\FormContent.vue",
  "src\views\object\DefaultObject.vue",
  "src\views\crud\DefaultList.vue",
  "src\views\crud\RecordDrawer.vue",
  "src\core\utils\filterBuilder.ts"
) | ForEach-Object { "$_ $(Test-Path (Join-Path $web $_))" }

# 本号不应出现业务代码 diff（执行后检查）
# git status --short NewLife.Cube.ArcoVue
```

## 必须保留（防实施误删）

实现号亦不得擅自删除：`normalizeSubmitValue`、`serializeSubmitModel`、`isAuditField`、`isTenantField`、`isPersonField`（可改为 resolveFieldKind 的薄封装）、`CascaderField`（可泛化不可无替代删除）、`LovSelect`、`JsonEditor`、`RichEditor`、OSC-0016 搜索单值等值（不恢复 `_min/_max`）、`GET /Cube/Area?parentid=`、`DepartmentSearch`。
