# OSC-26090347f1 IA — Cube OA审批流程引擎

适用：Arco Design Vue（https://arco.design/vue/docs/start）；列表操作用 VisActor VTable 操作列（https://visactor.com/vtable/option/ListTable）；设计器用 FlowGram.AI 固定布局（https://flowgram.ai/guide/getting-started/introduction.html）。`.vue` 薄脚本，逻辑进 `useXxx.ts`。

## 1. 入口与信息架构

```
壳顶栏
  Inbox（已有 remind）
  待办槽「审批」——仅 Meta.enabled=true 显示；角标=当前用户 Pending 任务数
侧栏菜单（模块扫描）
  流程 / 流程定义
  流程 / 我的待办
  流程 / 我发起的
  流程 / 已办
实体 DefaultList（typePath 有已发布定义）
  工具栏「提交审批」（多选）
  行状态列 `__wfStatus`
  行操作「提交」「进度」
RecordDrawer
  新增 Tab「审批」——当前实例时间轴 + 意见
独立页
  /cube/workflow/designer/:id  —— FlowGram 固定布局
  /cube/workflow/todo
  /cube/workflow/started
  /cube/workflow/done
```

DOM 顺序（实体列表工具栏，已有自动化按钮之后）：筛选 | 搜索 | 高级 | 自动化 | **提交审批** | 更多。

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

- props：`typePath: string`，`ids: Array<string|number>`，`definitions: Array<{id,name,version}>`
- emits：`submitted`，`cancel`
- 视觉顺序：已选 N 条摘要 → 定义下拉（仅 Enable 且 Published 且 TypePath 匹配） → 发起意见（可选） → 附件 → 提交
- 非法：混 TypePath（前端拦截，后端 400）；N>100 截断并 Message 警告
- 断点：`>=768` 抽屉 480px；更小全屏

### 3.2 `WorkflowProgressPanel`

- props：`instanceId: number`
- 时间轴：节点名 / 处理人 / 动作 / 意见 / 附件图标
- 当前用户若为该任务候选人：底部操作条 同意 | 驳回 | 加签 | 转办 | 知会；发起人额外「撤回」（仅 Running 且未有人同意）
- 不做：在面板里改业务字段（走表单 + 流程 PATCH）

### 3.3 `TodoList`

- 列：标题、实体、节点、到达时间、超时倒计时
- 多选批量同意/驳回：上限 50；部分失败返回每行错误
- 点击行打开进度抽屉（不跳实体页，提供「打开记录」链）

### 3.4 `WorkflowDesignerPage`

- FlowGram **fixed-layout** only
- 节点面板仅：`oa.start` `oa.approve` `oa.cc` `oa.xor` `oa.end`
- 选中 `oa.approve` 右侧：模式 or/and/sequence、候选人 to、字段权、超时、可回退
- 保存走 `PUT /Cube/Workflow/Definitions/{id}` GraphJson；发布另按钮
- 不做：自由布局、循环边、从自动化节点面板拖入 notify/http

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
