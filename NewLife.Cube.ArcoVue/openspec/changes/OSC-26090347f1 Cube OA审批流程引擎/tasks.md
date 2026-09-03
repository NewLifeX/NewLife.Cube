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

- [x] `useWorkflowList.ts` + spec：IA §4 按钮矩阵；embed 隐藏
- [x] DefaultList 薄接入；工具栏顺序：自动化之后「提交审批」
- [x] `SubmitApprovalDrawer` / `WorkflowProgressPanel` / `TodoPage`
- [x] `WorkflowDesignerPage`：FlowGram.AI 固定布局（自绘链式已替换为 FlowGram，见下）；节点仅 oa.*
  - 补录（2026-09-03）：OA 独立页（Designer/Todo/Done/Started）静态注册于 Layout children（`router/index.ts` oaLeafRoutes），URL 直达不再依赖菜单播种/embed 兜底——菜单未播种时直达曾 No match 白屏（menuRoutes 特判组件仅菜单命中才注册）；vue-tsc 0 error
  - 补录（2026-09-03 修复批次，宿主真机验证）：**画布空白/无法插入节点**三根因——① 画布挂载 div 在 `v-if="!graph"` 分支，`ensureCanvas` 仅 onMounted 执行一次被跳过（初始 graph=null 无 div）→ `watch([graph, canvasEl])` flush post 就绪挂载；② FlowGram `materials.components` 空 `{}` 缺官方 `drag-node` 渲染 key → `PlaygroundReactRenderer` 抛 `Unknown render key drag-node` 整树卸载 → 合并 `defaultFixedSemiMaterials`（新依赖 `@flowgram.ai/fixed-semi-materials` + `styled-components`）+ `history.enable`（撤销/onApply 镜像）+ `onAllLayersRendered` fitView；③ 后端 Int64 雪花主键字符串序列化，前端 `Number()` 精度丢失致定义打开/保存失配 → id 全程字符串透传（openDefinition `String()` 比较、select、`?id=` 直达——顺带实现注释承诺的直达）。实测 start→审批(n1)→end 渲染、属性面板、插入 n2、保存草稿全通；web spec 834 全绿
- [x] api-core `workflow.ts` + URL 单测
- [x] `<1024` 设计器只读提示

> **T8 实现说明（2026-09-03）**：提交 88f02037（api-core 客户端+URL 单测）、T8b（useWorkflowList+矩阵 spec）、T8c（提交抽屉/进度面板/待办·已办·发起页）、T8d（DefaultList 薄接入：工具栏「提交审批」在高级-自动化之后、行提交/进度按 __wf* 过滤、提交后清勾选刷新）、T8e（设计器）。
> **T8e 修订（FlowGram.AI）**：先前以自绘链式布局临时交付并记录偏差；现按设计 §3.4 **引入 FlowGram.AI 固定布局**（依赖：`@flowgram.ai/fixed-layout-editor` + `react`/`react-dom`，见 web/package.json；peer 仅 react，未引 Semi 材料包）。`WorkflowFlowCanvas`（views/workflow/flowgram/FlowGramDesigner.tsx）经 react-dom 桥接挂载到 DesignerPage；结构增删/拖拽/缩放由 FlowGram 承担（start/end 用其内置类型，业务节点仅 oa.approve/cc/xor），节点卡片外部 labels 展示；每次历史 onApply → 业务序 doc 回调 → Vue 镜像重建 GraphJson（保存/发布权威仍为后端 GraphJson，浏览器不执行引擎）。<1024 不挂画布、回退只读链式预览。纯转换 helper（flowgramGraph.ts graphToFlowDoc/flowDocToGraph 双向无损 + 虚拟节点过滤）spec 6 例；web vitest 830、vue-tsc 0、vite build 通过（FlowGram chunk 懒加载）。",

## T9 文档

- [x] 迁移方案 §8.5.5 **一句**本号 ID + 方案 A（最小增量）
- [x] `Doc/功能清单.md` 增 WF 行
- [x] `Doc/Api/核心接口架构.md` 路径表
- [x] FAQ 41.9 一句平台级勿加业务 ApprovalStatus

## T10 测试与构建

- [x] `WorkflowEngineTests`：§8 每格 + 回退 + 加签 + XOR + 空候选人
- [x] `WorkflowLockTests` / `WorkflowExclusiveTests` / `WorkflowPageOverlayTests`
- [x] 回归 `Osc260815` 至少接收人 + 一次执行
- [x] Vitest `useWorkflowList.spec.ts`
- [x] `dotnet build` Workflow + Cube 0 error；`pnpm` 相关 spec

> **T10 实现说明（2026-09-03）**：新增 `WorkflowMatrixTests`（会签 quorum=0.6 两票过、or 驳回整单、依次首人驳回、回退清下游并重开目标、前加签挂起/通过后恢复、XOR 缺 defaultTarget 发布失败、空候选人 Running+error 意见——空候选人以不存在的角色触发，图校验要求 to 非空）7 例 + `WorkflowExclusiveTests`（顺序二次 409 + 并发双提）2 例。并发双提实测复现**双插缺陷**（Start 在途检查非原子）→ `WorkflowEngine.Start` 增加进程内 `_startGate` 互斥包裹“排他检查 + 事务落库”修复（跨进程/多实例仍需 DB 唯一约束，见 WorkflowExclusiveTests 备注，列为实现审计关注项）。`Workflow*` + `Osc260815` 回归 33/33 通过；`dotnet build` 0 error；web vitest 830 全绿。

## T11 手工冒烟（实现期）

- [ ] Demo 引用模块：Meta.enabled=true；Admin/User 发布定义；提交 2 条会签通过
- [ ] full 锁普通 PUT 失败；流程 Patch 成功
- [ ] 不引用模块的宿主：无待办槽

> **T11 阻塞注记（2026-09-03）**：CubeDemo 宿主在本机 SDK 10.0.400 构建失败——`MSB3552 找不到资源文件 "**/*.resx"` 源自 `NewLife.Cube.Vue/NewLife.Cube.React`（`EmbeddedResource Include="wwwroot\**\*"`，清除 obj/bin 后复现，与本 OSC 改动无关，CubeDemo 引用全部主题皮肤）。宿主无法启动，HTTP 冒烟无法自动执行。逐条待办 + 命令见 verify.md「T11 手工冒烟」，宿主就绪后人工验证并在验收阶段勾选。

## T12 收尾门禁修复批次（代码审查 + 实现审计驱动）

> 会话小任务补录（收尾门禁第 1 步）核对：FlowGram 切换（T8e 修订）、GET /Phrases（T6 补录）、并发 race（T10 说明）均已记录于既有任务项，无需新建。本批次为收尾门禁代码审查 🔴 + 实现审计 P1/P2 合并修复，已全部勾选并验证。

- [x] **后加签激活**（CR#1/G-02）：`AfterApprove` 原任务 Done 后经 `TryActivateAfterSign` 激活本任务 `#after#` 挂起任务（Visible+通知）并等待；全部后加签完成后 `ContinueNode` 回原节点常规收尾（依次放行/通过/下游）。重构抽取 `ContinueNode` 统一 AfterApprove 与合成任务收尾
- [x] **多前加签等齐 + 复活保护**（CR#2）：`TryHandleSynthetic(addsign)` 等本节点全部前加签完成才恢复原任务；原任务已 Done/终态不复活
- [x] **Rollback allowRollback**（CR#3）：当前节点 `AllowRollback=false` → 403（与 allowAddSign 对称）
- [x] **WorkflowGraph 默认值**：`AllowAddSign/AllowRollback` 默认 true（对齐 design 节点示例 266-267 行）
- [x] **超时可见性 + MinValue 缺陷**（CR🟡1/G-11）：`TimeoutTick` 只处理 Visible 任务；修复 sqlite 下 `DueTime > DateTime.MinValue` 比较恒 false 致超时永不下发的**真实引擎缺陷**（SQL 只按到期过滤，MinValue/状态/可见性内存再筛）
- [x] **Start Detail 权限**（CR#4）：`WorkflowController.Start` 补 `CanDetail` 校验（design §6.4「发起=Detail」，行级 StartFilter 引擎已判）
- [x] **Patch 路由多段 typePath**（CR#5）：`Entities/{typePath}/{key}/Patch` 改 `Entities/{key}/Patch?typePath=`（typePath 含 `/` 致路由断裂 404）；api-core 契约 + spec 同步
- [x] **前端 before 透传**（G-03）：`useWorkflowProgress.transferAction` addSign body 补 `before`（原先面板前/后加签选择被丢弃，UI 前加签实际走后加签）
- [x] 新增矩阵测试 4 例：后加签激活推进 / 多前加签等齐 / Rollback 403 / 超时仅可见任务；回归 Workflow*+Osc260815 37/37；web vue-tsc 0 error + workflow spec 32 绿 + api-core 3 绿

> **无法在本会话闭环（记录为后续/待确认，见 status.md）**：G-01 跨进程排他需 DB 唯一约束（表结构决策，后续 OSC）；G-04 待办候选可见语义（design 未明示，待确认）；G-05 意见附件按 V1 范围外裁剪记录（retro）；G-06 Meta 恒 true（Amd-2 语义，随 T11 宿主验证）；G-07 HTTP 冒烟（T11 阻塞）；G-08 多定义 defs[0] 行级口径；G-11 动作级测试（Claim/Transfer/Cc/超时 transfer-reject 分支）留后续批次。
