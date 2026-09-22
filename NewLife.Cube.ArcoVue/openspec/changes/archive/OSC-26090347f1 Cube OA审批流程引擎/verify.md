# OSC-26090347f1 Verify

> 状态：passed（Validating → 可复盘）  
> 时间：2026-09-22T08:40+08:00  
> 触发：按照 OpenSpec 验收并复盘 47f1。  
> 编排：implementation-audit → code-review → doc-sync  
> checklist: passed。AC-01/AC-16 与 T11 宿主冒烟仅记录，不阻断。

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

### T13 流程设计 / 审批界面（Amd-3，2026-09-19）

- 实现审计后补齐：`allowTransfer` 默认 true（false → 转办 403）；`GET Instances/{id}` 下发 `graphSnapshot`。
- D1–D10 已落地：XOR ViewFilter + 默认分支、顶栏写锁/发起条件、节点卡片徽章、节点间「+」、节点进度、回退入口、待办宽屏分栏、摘要 Markdown、常用语弹窗。
- 回归：`FullyQualifiedName~Workflow` 48 绿；web workflow vitest 57 绿；vue-tsc 0 error。


AC-01/16（无模块 404 + enabled false）、AC-02 非法图发布校验、AC-03 批量提交（Osc47f1 集成）、AC-04 或签、AC-05 会签、AC-06 依次、AC-07 写锁、AC-10 403、AC-11 排他 409、AC-13 撤回、AC-14 回退、AC-15 空候选人、AC-17 自动化回归 → 均由上述单测/集成测试覆盖（WorkflowEngineTests/WorkflowMatrixTests/WorkflowExclusiveTests/WorkflowLockTests/WorkflowPageOverlayTests + Osc260815 回归 33/33）。
AC-08 设计器保存/映射由 vitest + vue-tsc + vite build 覆盖；「运行时不在浏览器执行」为架构设计属性（设计评审）。
AC-12（混 TypePath/复合主键/N>100）、AC-18 由后端校验/引擎实现，前端 400 呈现待前端 E2E 或 T11 手工补充。

## 验收阶段记录（openspec-verify）

### implementation-audit

- **2026-09-10 审计 + 自动补齐**：对照 proposal/IA/design/功能清单 WF-1..4。
- **已修**：G-04 或签候选待办可见（`FindTodoByUser`/`CountTodoByUser`）；G-08 多定义 StartFilter 任一命中 + 锁策略不一致省略 `lockPolicy`；G-11 Claim/Transfer/Cc/超时 reject 单测；侧栏 `EnsureMenus`（既有未提交改动纳入）；顶栏审批槽+角标；列表 `__wfStatus` 列；RecordDrawer 审批 Tab。
- **回归**：`FullyQualifiedName~Workflow` 31/31；vitest workflow 相关 28/28。
- **2026-09-19 审计 + T13 D1–D10**：对照 design §13。补 `allowTransfer` + 实例 `graphSnapshot`；设计器 XOR/写锁/发起条件/卡片/[+]/常用语；进度节点流+回退+Markdown；待办宽屏分栏。
- **仍无法闭环**：G-01 跨进程 DB 唯一约束；G-07 T11 宿主冒烟。

### code-review（2026-09-22）

- `NewLife.Cube/Workflow` 抽查：无 C# 别名、无 `ThrowIfNull`、无 `new StringBuilder`。🔴 0。
- 🟡 `WorkflowDesignerPage.vue` 的 script 以解构绑定为主，行数超过约 20 行的建议值；业务仍在 `useWorkflowDesigner`。
- 🟡 `WorkflowHost.Register(Boolean ensureMenus = true)`：写锁单测传 false，避免与菜单播种抢同一棵 `Menu` 树。默认 true，宿主 `AddCube` 行为不变。

### doc-sync（2026-09-22）

- `Doc/功能清单.md` WF-4：常用语弹窗已交付；顶栏改为 `audit` 无旁字；设计器新建不再选依次签；意见附件仍记 G-05。
- proposal / design / FAQ / 迁移方案 §8.5.5 与「后端在 `NewLife.Cube/Workflow`、FlowGram 只做设计器」一致，本轮不改口径。

### 愿景对照（2026-09-22）

| 目标 | 结论 |
| --- | --- |
| 1 后端随 WebAPI 核心库，MVC 不 Link | 达成。类在 `NewLife.Cube/Workflow`。 |
| 2 一批 N 条、通过/驳回同时生效、同一键一条在途 | 达成（进程内 `_startGate`）。跨进程唯一约束仍是 G-01。 |
| 3 GetPage/GetList/GetDetail 挂钩，业务实体零改 | 达成。 |
| 4 FlowGram 只读写定义图，运行时是 C# 状态机 | 达成。 |

缺口（沿用执行期「无法闭环」，本轮按仅记录放行，不回写 Implementing）：

- P1 G-07 / T11：CubeDemo 宿主 HTTP 冒烟未在本会话执行（历史 MSB3552，与本号无关）。
- P2 G-01：跨进程排他缺 DB 唯一约束。
- P2 G-05：意见附件 UI 未做。
- P2 设计器新建节点不再提供依次签；引擎与历史图仍支持 `sequence`（会话产品决定，AC-06 仍由单测覆盖）。

### 测试与构建（2026-09-22）

- `dotnet test NewLife.Cube.Tests --filter FullyQualifiedName~Workflow|FullyQualifiedName~Osc260815`：62/62，连续两轮并行通过。验收中先暴露菜单用例改 `Menu.ConnName` 在并行下打到空库（`Childs` 计数 5≠1 或 FindCount=0），已改为不切换连接，并让写锁用例 `Register(false)`。
- `dotnet build NewLife.Cube/NewLife.Cube.csproj -f net10.0`：0 error。
- `pnpm exec vitest run`（workflow / useWorkflowList / wfStatusMark / iconRegistry）：84/84。
- `pnpm exec vue-tsc --noEmit`：0 error。

### 会话小任务补录

- 已补录 tasks T14（依次签入口下线、XOR 两支、几何徽标、三列表列、顶栏 audit、菜单单测隔离）。核对无另增未落盘事项。

### 归档后清尾补录（T15，2026-09-22）

- 工作区残余的 `WidgetConfigDrawer.vue` 命名卡片「图标 + Admin 徽标」对齐样式（`.wd-named-head` flex 两端对齐）此前未随任何提交入库；本轮补录 T15 并同步 `wwwroot/index.html` 装置哈希。
- 核查：与 47f1 流程功能无耦合（纯模板/样式）；随本轮 arco-vue 全量 vitest 889/889 与 vite build 0 error 通过。

## 验收标准

### Happy path

- [ ] **AC-01 模块探测**：未引用 Workflow → `GET /Cube/Workflow/Meta` 返回 `{ enabled: false }`（可匿名）；壳无待办槽、列表无「提交审批」。（与 AC-16 同为 T11，仅记录。）
- [x] **AC-02 发布定义**：引用模块后，管理员对 `Admin/User` 保存 Graph（start→approve or→end）并 Publish；非法图（无 end / 自动化 type）400。
- [x] **AC-03 一批 N 条**：选 2 个 User 提交同一 definitionId → 1 Instance + 2 Subject + 首节点任务；Inbox 候选人收到 InApp。
- [x] **AC-04 或签**：两人候选人，一人同意 → 实例 Approved，另一任务 Cancelled，主体解锁。
- [x] **AC-05 会签**：mode=and 须全部同意才 Approved；中途一人驳回 → Rejected 解锁。
- [x] **AC-06 依次签**：第二人任务 Visible=false 直至第一人同意。（引擎单测仍覆盖；设计器新建入口已下线，见 T14。）
- [x] **AC-07 写锁 full**：Running 时实体普通 PUT 失败；`POST /Entities/.../Patch` 在 writable 内成功。
- [x] **AC-08 设计器**：FlowGram 固定布局保存 GraphJson；运行时不在浏览器执行。

### 权限 / 空 / 非法 / 旧数据

- [x] **AC-09 匿名 GetPage**：`workflow` 仅 `enabled`，无 writable 字段名、无 canStart。
- [x] **AC-10 非候选人 Approve**：403。
- [x] **AC-11 排他**：同一 User 第二条 Running 提交 409。
- [x] **AC-12 混 TypePath / 复合主键 / N>100**：400。
- [x] **AC-13 撤回**：无人同意可撤；已有同意 400。
- [x] **AC-14 回退**：下游任务 Cancelled，目标重生。
- [x] **AC-15 空候选人节点到达**：不自动通过；通知发起人。
- [ ] **AC-01 / AC-16 宿主冒烟**：未引用模块时 Meta `{enabled:false}` 且 `POST /Instances` 404。Amd-2 后 WebAPI 宿主引用 `NewLife.Cube` 即带工作流；MVC 不 Link。本会话未起 CubeDemo（T11 / G-07），仅记录。
- [x] **AC-17 自动化回归**：既有 EntityAutomation 插入仍入队；审批 Graph 不含 notify 节点。
- [x] **AC-18 定义热更新**：发布 V2 不影响 V1 在途 GraphSnapshot。

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
