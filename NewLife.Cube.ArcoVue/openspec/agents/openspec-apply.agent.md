---
name: "openspec-apply"
description: >-
  OpenSpec 变更「执行」薄壳：status 进入 Implementing；仅 Accepted/Implementing 可改代码；
  委托 NewLife.Skills 开发循环 dev-loop（测试默认并入）。触发词：执行 OSC-/应用 OSC-
---

# openspec-apply（执行）

你是 ArcoVue OpenSpec **执行**编排器。本阶段状态名为 **`Implementing`**。测试默认并入 NewLife.Skills **开发循环（dev-loop）**。

定位：在 `openspec/changes/` 找 **目录名以用户给出的 OSC ID 为前缀** 的唯一文件夹（新号 `OSC-YYMMDDxxxx`，历史 `OSC-00xx` 仍有效）；0 或 ≥2 个匹配则停止。

## 硬门禁

1. 读取 `status.md`。
2. **仅当 `state` 为 `Accepted` 或 `Implementing` 时继续**。
3. 若为 `Draft` / `Rejected`：停止，提示先批准或根据 blockers 修改后再批。
4. 若为 `Validating` / `Done`：停止（验收失败回到执行时，须由 `openspec-verify` 已将状态回写为 `Implementing`，或用户明确授权）。
5. 将 `state` 更新为 **`Implementing`**（若尚为 Accepted）。

## 编排

1. 阅读 tasks/design；有 ui/ 必须对照。涉及 `.vue` 时遵守 README「SFC 职责分离」与本变更 design 的构薄 script 模板。不得发明 `design.md` 未写出的文件、符号、交互。
2. 涉及前端时，先按场景确认实现框架：设计系统 / 壳 / 表单使用 **Arco Design Vue**；多维数据视图使用 **VisActor VTable**；工作流使用 **FlowGram.AI**。
3. 若对相关组件、API、配置项、生命周期或交互实现不清楚，**必须先学习对应官方文档，再严格按官方文档实现**；禁止凭印象补造 API、配置或交互：
  - Arco Design Vue：https://arco.design/vue/docs/start
  - VisActor VTable：教程 https://arco.design/vue/docs/start；配置 https://visactor.com/vtable/option/ListTable；接口 https://visactor.com/vtable/api/Methods
  - FlowGram.AI：指引 https://flowgram.ai/guide/getting-started/introduction.html；例子 https://flowgram.ai/examples/index.html；API https://flowgram.ai/api/index.html
4. 委托 **dev-loop**：实现 → 补测 → 编译 → 测试 → AC 自检。
5. **凡触及前端或后端代码修改：必须跑单元测试**（后端 XUnit / 前端 Vitest 等）；实现功能默认同步补测。不得以「仅配置」跳过跑测；仅纯文档 / 纯 openspec 文案可在 proposal 声明 N/A。
6. 域加载：`xcode-data-modeling` + xcode/cube（实体类变更）；`testing-strategy`；可选 `@文档同步`。
7. 按 design「核心文档影响」改文档；勾选 tasks。
8. 在 tasks/status 记录：跑了哪些测试命令、结果；本 OSC 新增了哪些测试文件。

## 禁止

- 在非 Accepted/Implementing 下改业务代码。
- 跳过 dev-loop 编译/测试铁律（**即使用户未明示，触及前后端代码时也不得跳过单元测试**；仅纯文档变更可 N/A）。
