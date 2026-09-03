# OSC-26090347f1 Verify

> 状态：骨架（openspec-create）  
> 时间：2026-09-03T12:00+08:00  
> 触发：批准并落地后由 openspec-verify 填写。  
> 编排：implementation-audit → code-review → doc-sync

## 执行阶段记录（openspec-apply）

### T1-T9（后端核心 + 前端 + 文档）— 已完成并提交

- T1 数据模型（Workflow 五实体 + WorkflowGraph/Entity 注册）、T2 接收人公共化（regression 14/14）、T3 状态机引擎（or/and/sequence/403/409/撤回/热更新 GraphSnapshot）、T4 写锁 + 超时 Cron（full/Scope/解锁）、T5 挂钩（GetPage/GetList workflow 块 + Meta）、T6 WorkflowController、T7 前端路由占位 —— 全部勾选提交。
- T8 前端：api-core `createWorkflowApi`（vitest 51 绿）→ ArcoVue 列表「提交审批/流程进度」（useWorkflowList.spec 12 绿）→ 提交抽屉/进度面板/接收人选择/待办·已办·我发起 → **设计器切换 FlowGram.AI**（`@flowgram.ai/fixed-layout-editor`，React 桥 Vue，固定布局文档=有序节点、内置 start/end；`flowgramGraph.ts` 双向映射 spec 6 绿；vue-tsc 0 error；vite build 通过，FlowGram 懒加载 chunk ~895KB）。vitest 全仓 830 绿（87 文件）。
- T9 文档：`Doc/功能清单.md` WF-1..WF-4、`Doc/Api/核心接口架构.md` OA 审批流程行、`Doc/常见问题FAQ.md` §41.9、ArcoVue 迁移方案 §8.5.5 —— 4 处提交。

### T10 测试与构建 — 已完成并提交

- 新增 `WorkflowMatrixTests` 7 例：会签 quorum=0.6 两票过、or 驳回整单、依次首人驳回、回退清下游并重开目标（重开推进到下游）、前加签挂起→通过后恢复、XOR 缺 defaultTarget 发布校验失败、空候选人 Running+error 意见。
- 新增 `WorkflowExclusiveTests` 2 例：顺序二次 409；**并发双提**——实测复现 Start 在途检查非原子的双插缺陷 → `WorkflowEngine.Start` 进程内 `_startGate` 互斥（排他检查+事务落库）修复。跨进程多实例仍需 DB 唯一约束（测试类备注，实现审计关注项）。
- 回归：`Workflow*` + `Osc260815` 33/33 绿；`dotnet build` NewLife.Cube 0 error；web vitest 830 绿。

### T11 手工冒烟（实现期）— 环境阻塞，待人工

- **阻塞原因**：CubeDemo 宿主在本机构建失败——`MSB3552 找不到资源文件 "**/*.resx"`，来自 `NewLife.Cube.Vue/NewLife.Cube.React`（`EmbeddedResource Include="wwwroot\**\*"`）在 .NET SDK 10.0.400 下的回归（清除 obj/bin 复现、与本 OSC 改动无关；CubeDemo 引用全部主题皮肤）。宿主无法启动 → Meta enabled/发布定义/会签提交等 HTTP 冒烟无法在本会话执行。
- **待人工清单**（宿主就绪后逐条执行，对应 tasks.md T11）：
  1. CubeDemo `appsettings.json` ConnectionStrings 增加 `"Workflow": "Data Source=..\\Data\\Workflow.db;provider=sqlite"`（其余成员/Cube/Log 同款）；`dotnet run` 于 CubeDemo。
  2. `GET https://localhost:7116/Cube/Workflow/Meta` → `{ enabled: true }`；匿名 GetPage 仅 enabled。
  3. Admin/User 登录发布定义（start→approve(or)→end），非法图 400。
  4. 选 2 条 User 记录提交 → 1 Instance + 2 Subject；Inbox 候选人收 InApp；或签/会签/依次签通过路径。
  5. full 锁：Running 中普通 PUT 失败、流程 Patch 成功（Patch 已改 `Entities/{key}/Patch?typePath=`）。
  6. 不引用模块的宿主（CubeDemoNC）：无待办槽、Meta `{ enabled:false }`、POST /Instances 404。

### T12 收尾门禁修复批次（代码审查 + 实现审计）

- 代码审查（NewLife 规范）5 🔴 全修 + 实现审计 P1/P2 合并修复，见 tasks.md T12；本轮**修复了 2 个此前未被测出的真实缺陷**：
  1. **后加签整条断裂**：`#after#` 任务创建后无激活路径（原任务 Done 直接推进下游），现经 `TryActivateAfterSign` 激活等待，全部后加签完成后 `ContinueNode` 常规收尾；
  2. **超时永不下发**：sqlite 下 `DueTime > DateTime.MinValue` 比较恒 false 致 `TimeoutTick` 查询恒 0 命中——改 SQL 仅按到期过滤、MinValue/状态/可见性内存筛。
- 回归：`Workflow*`（含新增 4 例矩阵）23/23 + `Osc260815` 14/14 = **37/37**；web vue-tsc 0 error、workflow spec 32 绿；api-core 3 绿。
- 无法闭环项（已记录 status.md）：G-01 跨进程唯一约束（后续 OSC）、G-04 候选可见语义（待确认）、G-05 附件 V1 范围外裁剪、G-06 Meta 恒 true（随 T11）、G-07 HTTP 冒烟（T11）、G-08/G-11 后续批次。

### 已由自动化覆盖的验收标准（供 verify 阶段引用）

AC-01/16（无模块 404 + enabled false）、AC-02 非法图发布校验、AC-03 批量提交（Osc47f1 集成）、AC-04 或签、AC-05 会签、AC-06 依次、AC-07 写锁、AC-10 403、AC-11 排他 409、AC-13 撤回、AC-14 回退、AC-15 空候选人、AC-17 自动化回归 → 均由上述单测/集成测试覆盖（WorkflowEngineTests/WorkflowMatrixTests/WorkflowExclusiveTests/WorkflowLockTests/WorkflowPageOverlayTests + Osc260815 回归 33/33）。
AC-08 设计器保存/映射由 vitest + vue-tsc + vite build 覆盖；「运行时不在浏览器执行」为架构设计属性（设计评审）。
AC-12（混 TypePath/复合主键/N>100）、AC-18 由后端校验/引擎实现，前端 400 呈现待前端 E2E 或 T11 手工补充。

## 验收阶段记录（openspec-verify）

### implementation-audit

- （空）

### code-review

- （空）

### doc-sync

- （空）

### 愿景对照

对照 proposal §1 四条目标；未达成即缺口。

## 验收标准

### Happy path

- [ ] **AC-01 模块探测**：未引用 Workflow → `GET /Cube/Workflow/Meta` 返回 `{ enabled: false }`（可匿名）；壳无待办槽、列表无「提交审批」。
- [ ] **AC-02 发布定义**：引用模块后，管理员对 `Admin/User` 保存 Graph（start→approve or→end）并 Publish；非法图（无 end / 自动化 type）400。
- [ ] **AC-03 一批 N 条**：选 2 个 User 提交同一 definitionId → 1 Instance + 2 Subject + 首节点任务；Inbox 候选人收到 InApp。
- [ ] **AC-04 或签**：两人候选人，一人同意 → 实例 Approved，另一任务 Cancelled，主体解锁。
- [ ] **AC-05 会签**：mode=and 须全部同意才 Approved；中途一人驳回 → Rejected 解锁。
- [ ] **AC-06 依次签**：第二人任务 Visible=false 直至第一人同意。
- [ ] **AC-07 写锁 full**：Running 时实体普通 PUT 失败；`POST /Entities/.../Patch` 在 writable 内成功。
- [ ] **AC-08 设计器**：FlowGram 固定布局保存 GraphJson；运行时不在浏览器执行。

### 权限 / 空 / 非法 / 旧数据

- [ ] **AC-09 匿名 GetPage**：`workflow` 仅 `enabled`，无 writable 字段名、无 canStart。
- [ ] **AC-10 非候选人 Approve**：403。
- [ ] **AC-11 排他**：同一 User 第二条 Running 提交 409。
- [ ] **AC-12 混 TypePath / 复合主键 / N>100**：400。
- [ ] **AC-13 撤回**：无人同意可撤；已有同意 400。
- [ ] **AC-14 回退**：下游任务 Cancelled，目标重生。
- [ ] **AC-15 空候选人节点到达**：不自动通过；通知发起人。
- [ ] **AC-16 未引用模块**：`POST /Instances` 404。
- [ ] **AC-17 自动化回归**：既有 EntityAutomation 插入仍入队；审批 Graph 不含 notify 节点。
- [ ] **AC-18 定义热更新**：发布 V2 不影响 V1 在途 GraphSnapshot。

### 必须保留（防误删）

- `GetPage` `[AllowAnonymous]`
- `AutomationExecutor` 线性语义与 `AutomationRun` 内存队列
- `PermissionFlags` 四位枚举（不增 Approve）
- 业务实体无 `ApprovalStatus` 列
- FlowGram 不作为运行时
- OSC-0010 目录不复活

## 命令

```powershell
dotnet test NewLife.Cube.Tests --filter DisplayName~Workflow   # 工作流源码位于 NewLife.Cube/Workflow，测试项目直接引用 NewLife.Cube
dotnet test NewLife.Cube.Tests --filter DisplayName~Osc260815
pnpm --dir NewLife.Cube.ArcoVue/web exec vitest run src/views/crud/useWorkflowList.spec.ts
dotnet build NewLife.Cube/NewLife.Cube.csproj -f net10.0   # Amd-2：后端并入 WebAPI 核心库 NewLife.Cube/Workflow
```

预期：相关测试全绿；构建 0 error。
