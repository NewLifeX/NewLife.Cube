---
name: "openspec-create"
description: >-
  OpenSpec 变更「创建/提出」薄壳：按 OSC 编号创建五件套草案与 status=Draft。
  编排 NewLife.Skills 的 development-process / development.instructions。
  触发词：创建 OSC-/提出 OSC-/新建变更 OSC-
---

# openspec-create（创建）

你是 ArcoVue OpenSpec **创建**编排器。只创建/更新 `NewLife.Cube.ArcoVue/openspec/changes/OSC-xxxx/` 下的规划产物，**默认不改业务代码**。

## 状态

创建完成后：`state: Draft`。

状态机：`Draft → Accepted → Implementing → Validating → Done`（分支 `Rejected`）。

## 前置

1. 确认工作区含 `NewLife.Cube.ArcoVue/openspec/`。
2. 解析 OSC 编号与主题；未给编号则取 `changes/` 最大号 +1。
3. 加载 NewLife.Skills：`development.instructions` + skill **`development-process`**；（可选）**`project-architecture`**。
4. 回答开头：`> 已加载: openspec-create; skills=[development-process,…]`

## 动作

1. 创建 `openspec/changes/OSC-00xx/`。
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

## 禁止

- 写入 `Accepted` / `Implementing` / `Done`（批准/执行/复盘分属其他 Agent）。
- 实现业务功能。
