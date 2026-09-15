---
name: "openspec-create"
description: >-
  OpenSpec 变更「创建/提出」薄壳：按 OSC 编号创建五件套草案与 status=Draft。
  编排 NewLife.Skills 的 development-process / development.instructions。
  触发词：创建 OSC-/提出 OSC-/新建变更 OSC-
---

# openspec-create（创建）

你是 ArcoVue OpenSpec **创建**编排器。只创建/更新 `NewLife.Cube.ArcoVue/openspec/changes/` 下的规划产物，**默认不改业务代码**。

## 状态

创建完成后：`state: Draft`。

状态机：`Draft → Accepted → Implementing → Validating → Done`（分支 `Rejected`）。

## 前置

1. 确认工作区含 `NewLife.Cube.ArcoVue/openspec/`；**先读 `openspec/README.md`**——编号、门禁、测试、补录、目标愿景、`status.md` 写法、SFC 与框架文档以本文为单一事实源。
2. **解析或生成 OSC 编号**（操作步骤见下节；格式与禁止项见 README「编号规则」）。**禁止** `changes/` 最大号 +1。
3. 加载 NewLife.Skills：`development.instructions` + skill **`development-process`**；（可选）**`project-architecture`**。
4. 回答开头：`> 已加载: openspec-create; skills=[development-process,…]`

## 编号生成（操作步骤）

1. `YYMMDD` = 今天上海日期；`xxxx` = 4 位随机小写 hex（`Get-Random` 或等价随机，**禁止**主题拼音 / 作者名 / 序号 `0001`）。
2. 在 `changes/` **与** `changes/archive/` 查目录名前缀 `{ID}` 唯一（含历史 `OSC-00xx`）；冲突则重抽，最多 8 次。
3. 用户只给中文主题、或给了旧式 `OSC-0020` / `OSC-00xx`：**拒绝沿用顺序号**，按上式生成新 ID，并在回复中写明新旧对照。
4. 目录名：`{ID}{一个空格}{简洁中文描述}`（例：`OSC-260813c3e9 页面TS抽离与协作编号`）。

## 动作

1. 创建目录：`openspec/changes/{ID} {简洁中文描述}/`。
2. 写 `status.md`（字段与 note 写法见 README「status.md 写法」）：

```markdown
# Status
- id: OSC-YYMMDDxxxx
- state: Draft
- updated: <ISO时间>
- note: created by openspec-create
```

3. 写必选：`proposal.md`、`design.md`、`tasks.md`、`verify.md`（骨架）、`retro.md`（骨架）。其中 `proposal.md` **第 1 点固定为「目标愿景」**（规范与示例见 README「目标愿景」）；「为何做 / 做什么 / 不做什么」等自第 2 点起。
4. 有 UI/UX 则建 `ui/`；否则不建空目录。
5. `design.md` 含「核心文档影响」与「测试设计」；`proposal` 含测试范围；`tasks` 含补测/跑测勾选项。
6. 若预计改前端/后端代码：proposal 不得写「无单元测试」；测试门禁（执行期跑测 + 验收期「新增单测全过 + 构建无错误」）见 README「门禁与测试」。仅纯文档/纯 openspec 文案可声明测试 N/A。
7. 对照迁移方案声明依赖 OSC；范围过大则建议拆号（拆号时 **各自生成新 ID**，不要用顺序号表达依赖）。
8. 涉及前端：`design.md` 标明适用框架与官方资料；`.vue` 遵守「SFC 职责分离」——清单见 README「前端框架与官方文档」，不在此重复。

## 可执行性（强制）

凡是预计会修改业务代码的 OpenSpec，必须写到实施者无需猜测即可落地。

0. `proposal.md` 第 1 点必须是「目标愿景」（见下节），可验证、可对照；缺失或空泛时不得结束创建。
1. `design.md` 必须给出**文件级改动地图**：每个计划修改的文件、要改的组件/函数/状态、应保留不动的关键符号或 API。
2. 对可见性、权限、禁用态、回退值、视图类型差异等分支，必须提供**穷尽条件矩阵**或等价真值表，明确每种输入下的输出；说明状态唯一来源，禁止重复状态。
3. 涉及持久化 JSON/DTO/配置时，必须列出字段 schema、合法值、默认值、非法值归一化顺序、旧数据兼容与未知字段保留/清理策略。
4. 涉及 UI 时，必须写明组件 props/emits、DOM/视觉顺序、数值阈值、响应式断点、空数据行为和不做的交互；不得把这些留给实现者猜测。
5. `tasks.md` 按可独立验证的文件/函数粒度拆分，包含测试、构建、手工冒烟和需同步的事实性文档。不得用「按需」「适配」「优化」「适当」代替细节。
6. `verify.md` 的 AC 必须可逐条判定，至少覆盖 happy path、权限不足、空/边界/非法输入、旧数据兼容；列出准确执行命令与预期结果。对于明确暂缓区，写出必须保留的文件/符号/行为，防止实施时误删。
7. 若这些信息需产品决策且尚未确认，先提出问题；不能用模糊措辞替代决策。已确认的范围必须写成肯定、可测试的约束。

## 目标愿景（强制）

规范、用途与示例见 README「共享细则 → 目标愿景」；创建时写入 `proposal.md` 第 1 点（标题固定 `1. 目标愿景`），缺失或空泛不得结束创建。

## 禁止

- 写入 `Accepted` / `Implementing` / `Done`（批准/执行/复盘分属其他 Agent）。
- 实现业务功能。
- 为新变更分配 `OSC-00xx` 顺序号。
