# OSC-26090347f1 Verify

> 状态：骨架（openspec-create）  
> 时间：2026-09-03T12:00+08:00  
> 触发：批准并落地后由 openspec-verify 填写。  
> 编排：implementation-audit → code-review → doc-sync

## 执行阶段记录（openspec-apply）

- （空）Draft，未实现。

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
dotnet test NewLife.Cube.Tests --filter DisplayName~Workflow   # 测试项目引用 NewLife.Cube.ArcoVue（含 ArcoVue/Workflow 源码）
dotnet test NewLife.Cube.Tests --filter DisplayName~Osc260815
pnpm --dir NewLife.Cube.ArcoVue/web exec vitest run src/views/crud/useWorkflowList.spec.ts
dotnet build NewLife.Cube.ArcoVue/NewLife.Cube.ArcoVue.csproj --no-restore   # Amd-1：后端并入 ArcoVue，无独立 Workflow 项目
```

预期：相关测试全绿；构建 0 error。
