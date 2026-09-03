# OSC-26090347f1 Tasks

> 顺序：模型 → 引擎 → Cube 挂钩 → API → ArcoVue → 文档 → 测试构建。批准前不写业务代码。  
> **2026-09-03 Amd-1**：后端不再建独立 `NewLife.Cube.Workflow` 项目，改为并入 `NewLife.Cube.ArcoVue/Workflow`（命名空间不变，随 WebAPI/ArcoVue 生效）；CubeDemoNC 不引用。T1 勾选保留、语义以修订后为准；T2–T11 无实质拆分变化，代码落点均为 `NewLife.Cube.ArcoVue/Workflow`。

## T1 数据模型

- [x] 新建 `NewLife.Cube.ArcoVue/Workflow/Entity/Workflow.xml`（Amd-1 前建于独立项目 `NewLife.Cube.Workflow/Entity`，迁入皮肤仓后命名空间不变）：Definition / Instance / Subject / Task / Comment，列与索引对齐 design §3
- [x] 该目录执行 `xcode`；禁止改 `NewLife.Cube/Entity/Cube.xml`
- [x] Biz：Definition 发布校验入口；Subject 在途查询；Task 按用户待办
- [x] **Amd-1 架构修订**：删除独立 `NewLife.Cube.Workflow` 项目，后端并入 `NewLife.Cube.ArcoVue/Workflow`；`魔方.sln` 移除项目、`NewLife.Cube.Tests` 改引用 ArcoVue；**CubeDemoNC 移除引用**——工作流只随 CubeDemo（WebAPI）经 ArcoVue 生效

## T2 接收人公共化（不升级执行器）

- [x] 将 `AutomationActions.ResolveRecipientUserIds` 抽到 `NewLife.Cube/Membership/RecipientResolver.cs`
- [x] 自动化改为调用同一方法；`Osc260815` 接收人相关测试仍过（回归 14/14）
- [x] Workflow 只引用 Resolver，不引用 `AutomationExecutor`

## T3 状态机

- [ ] `WorkflowEngine.Start`：同 TypePath、keys≤100、EntityKey 归一、复合主键 400、排他 409、StartFilter 全主体 Match、钉扎 GraphSnapshot、首节点任务
- [ ] Approve/Reject：or/and/sequence 矩阵；乐观并发 409
- [ ] Rollback：下游 Cancelled + 目标重生
- [ ] AddSign 前/后临时节点；不写回 Definition
- [ ] Transfer / Cc / Withdraw / Cancel / Jump
- [ ] XOR：Filter 第一条主体；无命中走 default；运行期无 default 视为数据损坏失败停止
- [ ] `WorkflowWriteScope` AsyncLocal

## T4 拦截器与超时

- [ ] `WorkflowWriteInterceptor.Init/Valid` 矩阵 design §6.2
- [ ] 模块未注册不挂拦截器
- [ ] `[CronJob("WorkflowTimeoutTick", "0 */5 * * * ?")]` pass/reject/transfer

## T5 Cube 核心挂钩

- [ ] `WorkflowPageOverlay.ApplyType`：GetPage 在 PrepareFieldsForApi 之后；匿名仅 `enabled`
- [ ] `ApplyRows`：`__wfStatus` / `__wfInstanceId` / `__wfCanStart`；IN 查询无 N+1
- [ ] GetDetail 登录记录级 wfVisible/wfWritable
- [ ] ~~CubeNC 双栈~~（Amd-1 取消）：仅 `NewLife.Cube`（WebAPI）`ReadOnlyEntityController` 挂钩，不改 CubeNC
- [ ] `GET /Cube/Workflow/Meta`

## T6 API

- [ ] `WorkflowController` 全表 design §7；未引用模块非 Meta → 404
- [ ] BatchApprove ≤50 部分成功
- [ ] `POST .../Patch` 仅 Scope 内字段
- [ ] Phrases 读写 Parameter `Workflow.Phrase`

## T7 通知

- [ ] 任务到达 / 知会 / 超时 / 驳回 / 办结 → NotificationRecord；渠道与自动化 notify 相同默认 InApp
- [ ] 待办槽角标用 Task 计数，**不**把待办行写入 Inbox 主时间轴

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
