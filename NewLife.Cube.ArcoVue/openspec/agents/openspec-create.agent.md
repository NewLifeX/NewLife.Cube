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
2. **解析或生成 OSC 编号**（见下节「编号规则」）。**禁止** `changes/` 最大号 +1。
3. 加载 NewLife.Skills：`development.instructions` + skill **`development-process`**；（可选）**`project-architecture`**。
4. 回答开头：`> 已加载: openspec-create; skills=[development-process,…]`

## 编号规则（团队并行、禁止抢号）

**新变更 ID 格式：** `OSC-YYMMDDxxxx`

| 段 | 规则 |
| --- | --- |
| `OSC-` | 固定前缀 |
| `YYMMDD` | **创建当日**（时区 `Asia/Shanghai` / UTC+8） |
| `xxxx` | 4 位 **小写十六进制**（`0-9a-f`），紧接日期、**中间无连字符**；用密码学随机或等价 `Get-Random`/`crypto.randomBytes`，**禁止**用主题拼音、作者名、序号 `0001` |

**目录名：** `{ID}{一个空格}{简洁中文描述}`  
例：`OSC-260813c3e9 页面TS抽离与协作编号`

**生成步骤（未给合法新 ID 时必须执行）：**

1. `YYMMDD` = 今天上海日期。
2. `xxxx` = 4 位随机小写 hex。
3. 在 `openspec/changes/` **与** `openspec/changes/archive/` 搜索是否已有目录名前缀 `{ID}`（含历史 `OSC-00xx` 目录）。冲突则重新抽 `xxxx`，最多 8 次。
4. 用户只给中文主题、或给了旧式 `OSC-0020` / `OSC-00xx`：**拒绝沿用顺序号**，按上式生成新 ID，并在回复中写明新旧对照。
5. 用户已给出合法 `OSC-YYMMDDxxxx`：仍须做第 3 步唯一性检查；冲突则换 `xxxx`。

**历史豁免：** 已存在的 `OSC-0001` … `OSC-0019`（及 `OSC-0018` 进行中）**永不改名**。触发语 `批准 OSC-0018` 仍然有效。

**禁止：**

- 按落地顺序 / `max+1` / 预留空洞号（如「给 FlowGram 留 0010」）。
- 仅用 `OSC-00xx`、仅英文 slug、或目录名不含中文简述。
- 两人并行时「先看最大号再 +1」（这是旧冲突源）。

**定位变更：** 用户说 `批准/执行/验收/复盘 OSC-260813c3e9` 时，在 `changes/` 与 `archive/` 下找 **目录名以该 ID 为前缀** 的唯一文件夹；找到 0 或 ≥2 个则停止并询问。

## 动作

1. 创建目录：`openspec/changes/{ID} {简洁中文描述}/`。
2. 写 `status.md`：

```markdown
# Status
- id: OSC-YYMMDDxxxx
- state: Draft
- updated: <ISO时间>
- note: created by openspec-create
```

3. 写必选：`proposal.md`、`design.md`、`tasks.md`、`verify.md`（骨架）、`retro.md`（骨架）。其中 `proposal.md` **第 1 点固定为「目标愿景」**（见下节「目标愿景（强制）」）；「为何做 / 做什么 / 不做什么」等自第 2 点起。
4. 有 UI/UX 则建 `ui/`；否则不建空目录。
5. `design.md` 含「核心文档影响」与「测试设计」；`proposal` 含测试范围；`tasks` 含补测/跑测勾选项。
6. 若预计改前端/后端代码：proposal 不得写「无单元测试」；须规划补测与执行期跑测、验收期「新增单测全过 + 构建无错误」。仅纯文档/纯 openspec 文案可声明测试 N/A。
7. 对照迁移方案声明依赖 OSC；范围过大则建议拆号（拆号时 **各自生成新 ID**，不要用顺序号表达依赖）。
8. 涉及前端时，`design.md` 必须标明适用框架及需查阅的官方资料：设计系统 / 壳 / 表单使用 Arco Design Vue（https://arco.design/vue/docs/start）；多维数据视图使用 VisActor VTable（教程：https://arco.design/vue/docs/start；配置：https://visactor.com/vtable/option/ListTable；接口：https://visactor.com/vtable/api/Methods）；工作流使用 FlowGram.AI（指引：https://flowgram.ai/guide/getting-started/introduction.html；例子：https://flowgram.ai/examples/index.html；API：https://flowgram.ai/api/index.html）。
9. 涉及前端 `.vue` 时，遵守 README「SFC 职责分离」：业务 TS 进同目录 `useXxx.ts` / 纯函数，`.vue` 只留构薄 script（见 README）。

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

- **位置**：`proposal.md` 第 1 点，标题固定为 **`1. 目标愿景`**。
- **内容**：一句话愿景（本变更完成后达到的状态）+ 2~4 条**可验证目标**（避免空泛口号；每条目标应有对应 `tasks.md` 任务项或 `verify.md` AC 可追溯）。
- **用途**：总览该变更；验收阶段（`openspec-verify`）以此逐条对照代码实现与相关变更规范文档，未达成项即缺口。
- 示例：

```markdown
# OSC-YYMMDDxxxx — 主题

## 1. 目标愿景

让业务管理员无需代码即可配置实体增删改的自动化流程，一次配置永久生效。

- 目标 1：任意 XCode 持久化写入（控制器/导入/直接 Insert）均能触发启用规则；
- 目标 2：线性图执行器跑通 notify/update/create/find/http/delay 等动作，未知节点失败不静默；
- 目标 3：配置 UI 对标飞书「自动化」双栏，无 Update 权限不可见入口。

## 2. 为何做
…
```

## 禁止

- 写入 `Accepted` / `Implementing` / `Done`（批准/执行/复盘分属其他 Agent）。
- 实现业务功能。
- 为新变更分配 `OSC-00xx` 顺序号。
