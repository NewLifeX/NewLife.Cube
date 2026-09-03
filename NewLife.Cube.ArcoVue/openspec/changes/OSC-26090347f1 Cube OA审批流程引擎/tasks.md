# OSC-26090347f1 Tasks

> 顺序：模型 → 引擎 → Cube 挂钩 → API → ArcoVue → 文档 → 测试构建。批准前不写业务代码。  
> **2026-09-03 Amd-2（最终定稿）**：后端不再建独立项目、也不进 ArcoVue 皮肤仓，改为并入 **WebAPI 核心库 `NewLife.Cube/Workflow`**（命名空间不变，与 Automation 同模式）；NewLife.CubeNC/CubeDemoNC 不 Link 该目录。T1 勾选保留、语义以修订后为准；T2–T11 无实质拆分变化，代码落点均为 `NewLife.Cube/Workflow`。

## T1 数据模型

- [x] 新建 `NewLife.Cube/Workflow/Entity/Workflow.xml`（先后置于独立项目/ArcoVue 皮肤仓，Amd-2 定稿迁入 WebAPI 核心库，命名空间不变）：Definition / Instance / Subject / Task / Comment，列与索引对齐 design §3
- [x] 该目录执行 `xcode`；禁止改 `NewLife.Cube/Entity/Cube.xml`
- [x] Biz：Definition 发布校验入口；Subject 在途查询；Task 按用户待办
- [x] **Amd-2 架构修订（定稿）**：后端并入 `NewLife.Cube/Workflow`；Tests 直接引用 NewLife.Cube；CubeNC/CubeDemoNC 不 Link 该目录——工作流只随 WebAPI 版（引用 NewLife.Cube 的宿主如 CubeDemo）生效

## T2 接收人公共化（不升级执行器）

- [x] 将 `AutomationActions.ResolveRecipientUserIds` 抽到 `NewLife.Cube/Membership/RecipientResolver.cs`
- [x] 自动化改为调用同一方法；`Osc260815` 接收人相关测试仍过（回归 14/14）
- [x] Workflow 只引用 Resolver，不引用 `AutomationExecutor`

## T3 状态机

- [x] `WorkflowEngine.Start`：同 TypePath、keys≤100、EntityKey 归一、复合主键 400、排他 409、StartFilter 全主体 Match、钉扎 GraphSnapshot、首节点任务（已实现+单测）
- [x] Approve/Reject：or/and/sequence 矩阵；乐观并发 409（or/and/sequence 已实现+单测；乐观并发在事务内重读）
- [x] Rollback：下游 Cancelled + 目标重生（已实现，T10 矩阵单测补）
- [x] AddSign 前/后临时节点；不写回 Definition（已实现，T10 矩阵单测补）
- [x] Transfer / Cc / Withdraw / Cancel / Jump（已实现；Withdraw 单测绿）
- [x] XOR：Filter 第一条主体；无命中走 default；运行期无 default 视为数据损坏失败停止（已实现，T10 矩阵单测补）
- [x] `WorkflowWriteScope` AsyncLocal（已实现）
- [x] 通知：任务到达/驳回/办结/知会 → NotificationRecord（InApp，T7 语义随引擎已落地）

## T4 拦截器与超时

- [x] `WorkflowWriteInterceptor.Init/Valid` 矩阵 design §6.2（full 锁/Scope 放行/nodeFields 候选人字段权；WorkflowLockTests 绿）
- [x] 模块未注册不挂拦截器（CubeNC/CubeDemoNC 不 Link 无副作用；WebAPI AddCube→WorkflowHost.Register 幂等挂 Global）
- [x] `[CronJob("WorkflowTimeoutTick", "0 */5 * * * ?")]` pass/reject/transfer（WorkflowEngine.TimeoutTick）

## T5 Cube 核心挂钩

- [x] `WorkflowPageOverlay.GetTypeBlock`：GetPage（ReadOnlyEntityController）PrepareFieldsForApi 后注入 `workflow` 块；匿名仅 `enabled`，登录加 definitionCount/lockPolicy/canStart（WorkflowPageOverlayTests 匿名矩阵绿）
- [x] `ApplyRows`：`__wfStatus` / `__wfInstanceId` / `__wfCanStart`；批量 IN 无 N+1（覆盖测试：running/none/approved 绿）
- [x] GetDetail 登录记录级：ApplyRow 注入 `__wfStatus/__wfInstanceId/__wfWritable`（行 JSON SetItem 平铺，序列化探针已证）
- [x] ~~CubeNC 双栈~~（Amd-1 取消）：仅 `NewLife.Cube`（WebAPI）`ReadOnlyEntityController` 挂钩，不改 CubeNC
- [x] `GET /Cube/Workflow/Meta`（WorkflowController.Meta：匿名 {enabled:true}；登录加 todoCount）

## T6 API

- [x] `WorkflowController` 全表 design §7（Definitions CRUD+发布、Instances 发起/撤回/作废/跳转/详情、Tasks 认领/同意/驳回/加签/转办/知会/回退、Todo/Started/Done、Meta）
- [x] BatchApprove ≤50 部分成功（逐条 try/catch 返回 {id,ok,error}）
- [x] `POST Entities/{typePath}/{key}/Patch` 仅节点可写字段（WorkflowWriteScope.Enter + 拦截器放行）
- [x] Phrases 读写 Parameter `Workflow.Phrase`（`tenant:{TenantId}`；空回落内置三条；Phrases_Roundtrip 单测绿）
  - 补录：GET /Phrases（IA 常用语管理读取需要，非 design §7 显式列出的只读端点在会话补录）
  - 说明：意见附件（attachmentIds）留待 T8 前端配合 Attachment 复用，接口已预留不报错

## T7 通知

- [x] 任务到达 / 知会 / 超时 / 驳回 / 办结 → NotificationRecord（引擎随 T3/T4 已落地，Action=Workflow、Channel=InApp）
- [x] 待办槽角标用 Task 计数（`GET /Cube/Workflow/Meta` 登录附 todoCount；WorkflowTask.CountTodoByUser），不写 Inbox 主时间轴作待办行

## T8 ArcoVue

- [ ] `useWorkflowList.ts` + spec：IA §4 按钮矩阵；embed 隐藏
- [ ] DefaultList 薄接入；工具栏顺序：自动化之后「提交审批」
- [ ] `SubmitApprovalDrawer` / `WorkflowProgressPanel` / `TodoPage`
- [ ] `WorkflowDesignerPage`：FlowGram 固定布局；节点仅 oa.*
- [ ] api-core `workflow.ts` + URL 单测
- [ ] `<1024` 设计器只读提示

## T9 文档

- [ ] 迁移方案 §8.5.5 **一句**本号 ID + 方案 A（最小增量）
- [ ] `Doc/功能清单.md` 增 WF 行
- [ ] `Doc/Api/核心接口架构.md` 路径表
- [ ] FAQ 41.9 一句平台级勿加业务 ApprovalStatus

## T10 测试与构建

- [ ] `WorkflowEngineTests`：§8 每格 + 回退 + 加签 + XOR + 空候选人
- [ ] `WorkflowLockTests` / `WorkflowExclusiveTests` / `WorkflowPageOverlayTests`
- [ ] 回归 `Osc260815` 至少接收人 + 一次执行
- [ ] Vitest `useWorkflowList.spec.ts`
- [ ] `dotnet build` Workflow + Cube 0 error；`pnpm` 相关 spec

## T11 手工冒烟（实现期）

- [ ] Demo 引用模块：Meta.enabled=true；Admin/User 发布定义；提交 2 条会签通过
- [ ] full 锁普通 PUT 失败；流程 Patch 成功
- [ ] 不引用模块的宿主：无待办槽
