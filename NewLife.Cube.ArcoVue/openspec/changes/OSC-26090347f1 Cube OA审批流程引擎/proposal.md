# OSC-26090347f1 — Cube OA审批流程引擎

> **2026-09-03 架构修订（Amd-1）**：执行中将后端从“独立 `NewLife.Cube.Workflow` NuGet 模块”改为“并入 `NewLife.Cube.ArcoVue` 皮肤仓（WebAPI 版专属）”，命名空间保留 `NewLife.Cube.Workflow(.Entity)`；CubeDemoNC（MVC）不引用、不受影响。目标 1 与决策 6/9 及 §4/§5/§7 相应条目已同步改写。

## 1. 目标愿景

让任意已有 Cube 实体在**零改业务实体/零改实体控制器**的前提下，挂上飞书/钉钉式 OA 审批：提交即实例、待办可认领、或签/会签/依次签、加签/知会/回退/撤销，审批中写锁由定义策略与节点字段权共同约束。

- 目标 1：工作流后端随 `NewLife.Cube.ArcoVue`（WebAPI 版皮肤）交付——类/服务位于皮肤仓 `ArcoVue/Workflow`，命名空间 `NewLife.Cube.Workflow(.Entity)` 提供定义 / 实例 / 主体 / 待办 / 意见；仅 WebAPI 宿主（CubeDemo，经 ArcoVue）生效，CubeDemoNC/MVC 侧不引用，无入口、无待办槽（`GET /Cube/Workflow/Meta` 能力探测）。
- 目标 2：一次提交可挂 N 条同 TypePath 记录（一单一批，默认 N≤100）；通过/驳回对全部主体同时生效；同一 `(TypePath, EntityKey)` 同时最多一条在途实例。
- 目标 3：Cube 核心一次性挂钩——写入拦截、GetPage 类型级能力、GetList/GetDetail 记录级覆盖；业务 `Entity`/`EntityController` 零改动。
- 目标 4：设计器只用 FlowGram.AI 固定布局读写定义图；运行时是 C# OA 状态机，**禁止**浏览器执行、禁止把 OSC-260815fa86 自动化升级成审批引擎。

## 2. 为何做

迁移方案 [§8.5.5](../../../ArcoVue企业中后台迁移方案.md) 已冻结：**实体自动化 ≠ 流程引擎 ≠ FlowGram 运行时**。OSC-260815fa86 交付的是「记录变了就跑线性动作、跑完即终态」；审批要的是实例还在、待办能认领、能驳回。FAQ 41.9 在实体上加 `ApprovalStatus` 会侵入业务表，与「非侵入」冲突。Elsa/BPMN/Camunda/Warm-Flow 会引入第二套运行时与组织模型，和 Cube 用户/角色/部门、Inbox、Attachment 对不齐。

本号落实方案 **A：自研 OA 状态机**（会话已锁定，不复活 OSC-0010 顺序号）。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | **产品定位**：OA 审批引擎（飞书/钉钉：或签/会签/依次签、加签/知会/回退）。**不是** BPMN 编排；V1 不做子流程/服务任务/定时中间事件。 |
| 2 | **批量语义**：一个流程实例挂 N 条记录（`WorkflowSubject`）；通过/驳回一起生效。待办列表可勾多条**任务**批量同意/驳回（上限 300），不是「N 条各自一个实例」。 |
| 3 | **非侵入**：允许改 Cube 核心一次（拦截器 + GetPage/GetList/GetDetail 覆盖 + Meta 探测）。**禁止**改业务实体字段、禁止改业务 `EntityController`、禁止要求 `IApprovable`。 |
| 4 | **写锁**：定义上 `LockPolicy`=`full`（默认，审批中禁止普通 Update/Delete）或 `nodeFields`（仅当前节点可写字段、且仅审批人经流程通道 PATCH）。 |
| 5 | **组织**：只用 Cube `User` / `Role` / `Department`；不接飞书/钉钉通讯录。接收人展开**调用** `AutomationActions` 的用户/角色/部门解析，不复制第二套。 |
| 6 | **宿主（Amd-1）**：后端内置 `NewLife.Cube.ArcoVue`（非独立 NuGet），命名空间 `NewLife.Cube.Workflow(.Entity)` 预留未来抽包；仅 UseArcoVue 的 WebAPI 宿主（CubeDemo）生效——不引用则无表用途、无钩子副作用（拦截器 `Init` 返回 false 或模块未注册则不挂）。 |
| 7 | **V1 能力一次交付**：提交/撤回、同意/驳回、或签/会签/依次签、前加签/后加签、知会、回退到已办节点、指定节点跳转（管理员）、超时（通过/驳回/转交）、意见+附件、条件网关（XOR）、常用语。 |
| 8 | **自动化边界**：不改 `EntityAutomation` / `AutomationExecutor` / `AutomationPersistence` 产品语义。图 schema 可与自动化同形 `nodes/edges`，**节点 type 命名空间分离**（`oa.*` vs 自动化现有 type）；执行器遇自动化节点 type **失败并停止**。 |
| 9 | **单栈（Amd-1）**：WebAPI `NewLife.Cube` 挂钩与 API 为主，工作流仅随 ArcoVue/WebAPI 版交付；**取消**实体生成物 CubeNC Link 与 CubeDemoNC 引用——MVC 侧（CubeNC）不交付设计器、无工作流。 |
| 10 | **编号**：本号 `OSC-26090347f1` 替代历史上「给 FlowGram 留 OSC-0010」的叙事；**禁止**复活 `OSC-0010`。 |

## 4. 做什么

1. `NewLife.Cube.ArcoVue/Workflow`（并入皮肤仓，Amd-1）：`Workflow.xml`（ConnName=`Workflow`，命名空间 `NewLife.Cube.Workflow.Entity`）五表 + xcode 生成；禁止手写实体骨架。
2. `WorkflowModule : IModule`（与 ArcoVue 同程序集，随 WebAPI 宿主生效）+ `AddCubeWorkflow()`；`WorkflowEngine` 状态机；`WorkflowWriteInterceptor` 全局写入校验。
3. Cube 核心薄挂钩（模块存在才生效）：GetPage 类型级 `workflow` 块；GetList `__wf*`；GetDetail 字段三元组；拦截器注册。
4. API：定义 CRUD/发布、发起/撤回、任务同意/驳回/加签/转办/知会/回退、Inbox 待办、Meta、常用语 Parameter。
5. ArcoVue：实体列表提交/状态列；待办中心；FlowGram 固定布局设计器页；意见抽屉与附件（复用 Attachment）。
6. CronJob `WorkflowTimeoutTick`：扫描到期任务。
7. 单测（状态机矩阵、拦截器、排他实例、GetPage 匿名不泄漏）+ Vitest（入口显隐）+ 构建。

## 5. 不做什么

- 不引入 Elsa、WorkflowCore、Camunda、Flowable、Warm-Flow、BPMN XML。
- 不把 FlowGram 当运行时；不做自由布局/循环/子画布。
- 不升级自动化执行器以支持人工节点；不在 `EntityAutomation.GraphJson` 里画审批。
- 不在业务表增加 `ApprovalStatus`；不改 FAQ 41.9 为推荐路径。
- 不接飞书/钉钉/企微审批开放平台；企微 `ApprovalInfo` 仍只是 OAuth 补卡 DTO。
- 不实现角色×字段 ACL（OSC-2608273d95 字段矩阵另号）；本号字段权仅审批节点 JSON。
- 不把 GetPage `[AllowAnonymous]` 取消（e483 契约）；匿名只给能力开关，不下发可写字段名。
- 不改 `PermissionFlags` 枚举（无 Approve 位）；审批动作走 Workflow API + 候选人校验。
- 不混 TypePath 一批提交；不跨实体网关。
- 不手写实体骨架；不全量乱跑 xcode 导致重复中文文件。
- 不做 CubeNC/CubeDemoNC 双栈（Amd-1）：不把工作流实体 Link 到 `NewLife.CubeNC`，MVC 宿主不受影响。

## 6. 依赖

| 依赖 | 关系 |
| --- | --- |
| 迁移方案 §8.5.5 / §12.4 | 口径来源：独立 IModule、FlowGram 仅设计器、能力探测 |
| OSC-260815fa86 | **复用** Filter / 接收人展开 / Inbox / CronJob / GraphJson 外形；**不升级**执行器 |
| OSC-0015 | `AutomationFilter.Match` 与 ViewFilter 同构，发起条件与网关条件共用 |
| OSC-0008 | 意见区交互可参考评论抽屉；意见表独立，附件走 Attachment |
| OSC-0003 | GetPage / typePath / DefaultList |
| OSC-2608273d95 | 行权仍走 DataScope；本号拦截器在 Valid 阶段拒绝写，不替代行权 |
| Cube User/Role/Department | 唯一组织源 |
| NotificationRecord / Inbox | 待办到达、知会、超时提醒 |
| Attachment | `Category=WorkflowComment`，`Key=CommentId` |
| CronJob / JobService | 超时扫描 |
| FlowGram.AI | 设计器 npm；官方：https://flowgram.ai/guide/getting-started/introduction.html |

## 7. 测试范围

| 类型 | 是否做 | 说明 |
| --- | --- | --- |
| XUnit（Workflow） | 是 | 状态机：或签/会签/依次、回退清下游、加签、超时、排他实例、条件网关无匹配失败 |
| XUnit（Cube 挂钩） | 是 | 拦截器跳过流程自有表；full 锁拒绝 Update；nodeFields 仅审批 PATCH；GetPage 匿名无字段权 |
| Vitest | 是 | Meta.enabled 隐入口；列表提交按钮矩阵；待办空态 |
| 构建 | 是 | NewLife.Cube.ArcoVue（含 ArcoVue/Workflow 后端）+ NewLife.Cube + arco-vue |
| 手工 | 是 | 对 Admin/User 挂定义、提交 2 条、会签通过、驳回解锁 |

## 8. 成功标准

- [ ] 未引用模块：`GET /Cube/Workflow/Meta` → `{ enabled:false }`；列表无提交按钮、壳无待办槽。
- [ ] 引用模块后：管理员配置定义并发布；有 Detail 权的用户对筛选命中的记录提交一批，生成 1 实例 + N Subject + 首节点任务。
- [ ] 审批中 `LockPolicy=full`：实体普通 PUT/PATCH/Delete 返回业务错误；审批人经流程通道改节点可写字段成功。
- [ ] 或签一人同意即过；会签须全部同意（或达比例）；依次按序；驳回整单结束并解锁。
- [ ] 回退到已办节点：下游任务取消，目标节点重开。
- [ ] 同一记录第二条在途提交 409。
- [ ] 自动化规则仍按原语义触发；审批写入不误跑「把审批当自动化节点」。
