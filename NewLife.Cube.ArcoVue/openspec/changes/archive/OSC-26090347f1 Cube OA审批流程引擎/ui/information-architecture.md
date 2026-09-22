# OSC-26090347f1 IA — Cube OA审批流程引擎

适用：Arco Design Vue（https://arco.design/vue/docs/start）；列表操作用 VisActor VTable 操作列（https://visactor.com/vtable/option/ListTable）；设计器用 FlowGram.AI 固定布局（https://flowgram.ai/guide/getting-started/introduction.html）。`.vue` 薄脚本，逻辑进 `useXxx.ts`。

> **Amd-3（2026-09-19）**：流程设计器与审批详情按飞书审批收口交互，**不**做飞书式独立表单设计器。细稿见 [process-and-approval.md](./process-and-approval.md)。

## 1. 入口与信息架构

```
壳顶栏
  Inbox（已有 remind）
  待办槽「审批」——仅 Meta.enabled=true 显示；角标=当前用户待办数（与 Todo API 同口径）
侧栏菜单（WorkflowHost.EnsureMenus）
  流程审批 / 流程定义
  流程审批 / 流程设计
  流程审批 / 我的待办
  流程审批 / 我发起的
  流程审批 / 已办
实体 DefaultList（typePath 有已发布定义；仅表格视图出批量提交）
  工具栏「批量提交」（多选）
  行状态列 `__wfStatus`
  行操作「提交」「进度」
RecordDrawer
  Tab「审批」——节点流程 + 意见 + 发起附件（表单仍在「表单」Tab）
独立页
  /Cube/Workflow/Designer  —— FlowGram 固定布局（?id= 直达）
  /Cube/Workflow/Todo
  /Cube/Workflow/Started
  /Cube/Workflow/Done
```

DOM 顺序（实体列表工具栏）：筛选 | 搜索 | **批量提交** | 高级 | 自动化 | 更多。

## 2. 页面清单

| 页面 | 路由/挂载 | 谁可见 | 空态 |
| --- | --- | --- | --- |
| 提交确认抽屉 | DefaultList 触发 | `canStart`（GetPage.workflow.canStart） | 无选中：禁用按钮 |
| 进度抽屉 | 行「进度」或抽屉 Tab | 能 Detail 该记录或为候选人/发起人 | 「暂无审批」 |
| 待办中心 | `/cube/workflow/todo` | 登录用户 | 插画 +「暂无待办」 |
| 定义列表 | 实体 CRUD WorkflowDefinition | 流程定义菜单 Update | 引导「新建流程」 |
| 设计器 | `/cube/workflow/designer/:id` | 定义 Update | 画布默认 Start→Approve→End |
| 常用语管理 | 定义编辑侧栏 | 定义 Update | 内置 3 条 |

## 3. 组件契约

### 3.1 `SubmitApprovalDrawer`

- props：`typePath: string`，`ids: Array<string|number>`
- emits：`submitted`，`cancel`
- 视觉顺序：已选 N 条 → 定义下拉（Enable+Published+TypePath） → **流程标题**（无 placeholder） → **流程摘要**（编辑/预览水平 Tab） → 发起意见（常用语） → 附件 → 提交
- 附件：提交成功后 `POST /Cube/Workflow/Attachments?instanceId=`，禁止随审批任务上传
- 非法：混 TypePath（前端拦截，后端 400）；N>100 截断并 Message 警告
- 断点：`>=768` 抽屉 520px；更小全屏

### 3.2 `WorkflowProgressPanel`

- props：`instanceId: string`（雪花 Id，禁止 `Number()`）
- **主视图（Amd-3）**：按节点列出候选人状态（待处理/已同意/已驳回/已取消/已转办），当前待办节点高亮；发起块含标题、摘要 Markdown、发起意见、发起附件
- **次视图**：全部动态（Comment 时间轴）默认折叠
- 当前用户为该任务候选人：底栏 **同意 | 驳回 | 更多（加签 / 转办 / 知会 / 回退）**；发起人额外「撤回」（Running 且无人同意）
- 宽屏待办宿主可把本面板放右侧，左侧放记录只读摘要 +「打开记录」
- 不做：在面板里改业务字段（走表单 + 流程 PATCH）；审批弹层上传附件

### 3.3 `TodoList` / 已办

- 列：标题、实体、节点、到达时间、截止、状态、操作（待办：审批/驳回/进度）
- 只显示**当前用户**待办/已办；顶栏角标同一口径
- 多选批量同意/驳回：上限 50；部分失败返回每行错误
- 点击行：窄屏打开进度抽屉；`>=1100` 左右分栏（左记录摘要，右进度）
- 「打开记录」链：`#/{typePath}/{entityKey}`

### 3.4 `WorkflowDesignerPage`

- FlowGram **fixed-layout** only；`<1024` 只读链式预览
- 节点仅：`oa.start` `oa.approve` `oa.cc` `oa.xor` `oa.end`
- 插入：节点间 `[+]` 与顶栏「在选中节点后插入」等价
- 画布卡片必须展示模式徽章与接收人摘要；未配审批人 warning 描边
- 右侧检查器分组：定义级 LockPolicy / StartFilter；审批人 / 方式 / 超时 / 操作权限 / 可写字段；**XOR 可视化 cases + defaultTarget**（ViewFilter）
- 保存 `PUT /Cube/Workflow/Definitions/{id}`；发布另按钮
- 不做：自由布局、循环边、办理人节点、并加签、从自动化面板拖入 notify/http、发起人自选/N 级上级

## 4. 列表按钮矩阵（前端只消费 GetPage/GetList）

| GetPage.workflow.enabled | 行 `__wfStatus` | canStart | 工具栏提交 | 行提交 | 行进度 |
| --- | --- | --- | --- | --- | --- |
| false | — | — | 不渲染 | 不渲染 | 不渲染 |
| true | none / rejected / withdrawn / approved | true | 可用 | 可用 | 无实例则隐藏进度 |
| true | running | * | 可用（其它行） | 禁用 + tooltip「审批中」 | 显示 |
| true | * | false | 禁用 | 禁用 | 有实例则显示 |

状态唯一来源：服务端 `__wfStatus`，禁止前端再查一遍实例表拼状态。

## 5. 不做的交互

- 不在自动化抽屉里画审批节点
- 不把待办混进 Inbox 时间轴主列表（Inbox 只收通知；待办独立槽）
- 不在分享 embed（`?embed=1`）显示提交按钮
- 不在卡片/看板/甘特做拖拽改审批状态
- 不做飞书式独立「表单设计」页签（单据=实体表单）
- 审批节点不上传附件；不把待办展示他人任务
