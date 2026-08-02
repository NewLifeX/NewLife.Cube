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

1. 确认工作区含 `NewLife.Cube.ArcoVue/openspec/`。
2. 解析 OSC 编号与主题；未给编号则取 `changes/`（不含 archive）最大号 +1。
3. 加载 NewLife.Skills：`development.instructions` + skill **`development-process`**；（可选）**`project-architecture`**。
4. 回答开头：`> 已加载: openspec-create; skills=[development-process,…]`

## 动作

1. 创建目录：`openspec/changes/OSC-00xx <简洁中文描述>/`（编号与简述之间**一个空格**；简述宜短，如 `OSC-0002 后端三实体`）。**禁止**仅用 `OSC-00xx` 或英文 slug。
2. 写 `status.md`：

```markdown
# Status
- id: OSC-00xx
- state: Draft
- updated: <ISO时间>
- note: created by openspec-create
```

3. 写必选：`proposal.md`、`design.md`、`tasks.md`、`verify.md`（骨架）、`retro.md`（骨架）。
4. 有 UI/UX 则建 `ui/`；否则不建空目录。
5. `design.md` 含「核心文档影响」与「测试设计」；`proposal` 含测试范围；`tasks` 含补测/跑测勾选项。
6. 若预计改前端/后端代码：proposal 不得写「无单元测试」；须规划补测与执行期跑测、验收期「新增单测全过 + 构建无错误」。仅纯文档/纯 openspec 文案可声明测试 N/A。
7. 对照迁移方案声明依赖 OSC；范围过大则建议拆号。

## 小参数模型可执行性（强制）

凡是预计会修改业务代码的 OpenSpec，必须让上下文有限、不能自行补全隐含决策的小参数模型也能据此准确实现。不得只写「优化」「适配」「按权限控制」等抽象表述。

1. `design.md` 必须给出**文件级改动地图**：每个计划修改的文件、要改的组件/函数/状态、应保留不动的关键符号或 API。
2. 对可见性、权限、禁用态、回退值、视图类型差异等分支，必须提供**穷尽条件矩阵**或等价真值表，明确每种输入下的输出；说明状态唯一来源，禁止重复状态。
3. 涉及持久化 JSON/DTO/配置时，必须列出字段 schema、合法值、默认值、非法值归一化顺序、旧数据兼容与未知字段保留/清理策略。
4. 涉及 UI 时，必须写明组件 props/emits、DOM/视觉顺序、数值阈值、响应式断点、空数据行为和不做的交互；不得把这些留给实现者猜测。
5. `tasks.md` 按可独立验证的文件/函数粒度拆分，包含测试、构建、手工冒烟和需同步的事实性文档；每项避免跨越多个不相干职责。
6. `verify.md` 的 AC 必须可逐条判定，至少覆盖 happy path、权限不足、空/边界/非法输入、旧数据兼容；列出准确执行命令与预期结果。对于明确暂缓区，写出必须保留的文件/符号/行为，防止实施时误删。
7. 若这些信息需产品决策且尚未确认，先提出问题；不能用模糊措辞替代决策。已确认的范围必须写成肯定、可测试的约束。

## 禁止

- 写入 `Accepted` / `Implementing` / `Done`（批准/执行/复盘分属其他 Agent）。
- 实现业务功能。
