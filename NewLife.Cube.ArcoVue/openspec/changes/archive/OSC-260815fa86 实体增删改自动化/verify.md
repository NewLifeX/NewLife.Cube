# OSC-260815fa86 Verify

> 状态：通过（openspec-verify）  
> 时间：2026-08-16T16:00+08:00  
> 触发：补齐所有缺口后验收 fa86。  
> 编排：implementation-audit → code-review → doc-sync

## 执行阶段记录（openspec-apply）

- 2026-08-15 Implementing：后端拦截+执行器+API、api-core、ArcoVue 抽屉/行按钮/Runs Tab、XUnit/Vitest/构建已通过。
- 2026-08-16 T12：P0.1 当时将 AutomationRun 落入 Log 库；P0.2–P0.6 / P1 / P2 缺口全部补齐（见 tasks T12.2）。
- 2026-08-19 事后修订（T12.3）：删除 `AutomationRun` 实体表；队列改回内存 POCO；`GET /Runs` / dateArrive once 只读系统 Log。**验收实现时以 design 文首现行约束为准，勿按 08-16 审计条目把表加回。**

## 验收阶段记录（openspec-verify）

### implementation-audit

- design §2.2 `AutomationRun`：**现行**内存 POCO + 系统 Log 审计（2026-08-19）。2026-08-16 验收曾记「xml ConnName=Log + 实体 `自动化运行.*`」，该项已废止，禁止按当时记录回滚。
- design §4.4 found 连续段：`AutomationExecutor` 对每条 found 执行整段 update/notify/addComment；空 found 跳过不 failed。
- findRecords：SQL `TryBuildWhere` 下推，否则分页扫描至 limit。
- Filter：缺字段 isNull、contains 大小写敏感、after/before CmpFlexible，与 `matchesViewFilter` 对齐。
- runAutomation：`ValidateForSave(selfId)` 拒绝自引用；运行时仍拒自身/深度。
- httpRequest：公网 http(s) + 拒 loopback/私网/SSRF。
- Hook：先查规则再限流；字典 Trim。
- target=created：保存归一 current；运行时不再取 Created。
- 工厂补挂：Worker 周期 `WrapAll`；批量 names/values Update/Delete/Insert After。
- debounce：内存 `queued|running` 窗口（不查运行表）。
- 通知展开：EnableTenant 时按 `TenantUser` 裁剪。
- 前端：飞书双栏、Recipients、Inbox remind、菜单无 runAutomation — 与 design/IA 一致。

### code-review

- 阻断项：无（先前缺口已落地）。
- 残余风险：纯 SQL 字符串 `Update/Delete(whereClause)` 仍不入队（无实体实例）；工厂注册后、Worker 下一拍前仍可能漏首次写入（窗口已缩小）。

### doc-sync

- 已有：`web/README.md`、`Doc/功能清单.md` DATA-13/SPA-20、`Doc/Api/核心接口架构.md`、迁移方案「自动化 ≠ FlowGram」。
- 本轮：`tasks.md` T11.4/T12.2 勾选；本 verify 成文。
- 2026-08-19：归档 design/proposal/tasks/verify/retro/IA 与 `harness/lessons.md` 同步现行约束（T12.3）；DATA-13 / 核心接口架构已写「Runs 读系统 Log」。

### 愿景对照

先前 P0/P1/P2 缺口清单均已补齐或在 verify 记残余风险；无新阻断缺口。用户已决策「补齐所有缺口后验收」。

## 验收标准

### Happy path

- [x] **AC-01 入口**：Admin/User、Cube/Area 顶栏「搜索」与「高级」之间有「自动化」（2026-08-16 浏览器快照）。
- [ ] **AC-02 创建线性流**：未在本环境点完创建表单（抽屉点击被壳层遮罩拦截）；由编译单测 + 保存 API 覆盖图结构。
- [ ] **AC-03 拦截触发**：未跑完整 UI insert→Run；由 `Osc260815` Trigger/Enqueue/Execute 单测覆盖。
- [x] **AC-04 飞书动作集**：编辑器菜单类型与 `AUTOMATION_MENU_ACTION_TYPES` 一致（无 runAutomation）；触发种类见 design。
- [x] **AC-05 按钮**：button 规则 API + 列表 `__ops` 接入（代码审计）。
- [x] **AC-06 Webhook**：HMAC/限流单测；Hook 控制器先校验 token。

### 权限 / 空 / 非法 / 旧数据

- [x] **AC-07 无 Update**：`AutomationAuth.CanConfigure`（代码）。
- [x] **AC-08 空列表**：抽屉 empty 文案（代码/IA）。
- [x] **AC-09 空 filter**：Compile 不写空 filter 节点。
- [x] **AC-10 非法 TriggerKind / 空 Name / actions>20**：Save 校验。
- [x] **AC-11 错误 Webhook token**：404。
- [x] **AC-12 签名**：HMAC 单测 + 401 分支。
- [x] **AC-13 循环**：自引用保存拒绝单测；Depth≥3 failed 单测。
- [x] **AC-14 旧 JSON**：未知 TriggerConfig 键保留；version>1 拒绝。
- [x] **AC-15 预留节点**：approval 执行 failed 单测。
- [x] **AC-16 fieldChange**：Dirtys 单测。
- [x] **AC-17 关闭 Enable**：FindEnabled 过滤。
- [x] **AC-18 租户**：TenantMismatch 单测。

### 必须保留

- [x] **AC-19～AC-21**：未删 FilterBuilder/matchesViewFilter/评论；无 FlowGram；Persistence 包装非仅 OnInsert。

### 测试与构建

- [x] **AC-22** api-core **50** pass；arco-vue **421** pass。
- [x] **AC-23** `pnpm --filter @cube/arco-vue build` 成功。
- [x] **AC-24** `dotnet test --filter Osc260815`：**12** pass；`dotnet build NewLife.Cube` / `NewLife.CubeNC` 0 error。
- [x] **AC-25** 文档四份已登记。

## 命令与预期

```text
pnpm --filter @cube/api-core test          → 50 pass
pnpm --filter @cube/arco-vue test          → 421 pass
pnpm --filter @cube/arco-vue build         → 0 error
dotnet test NewLife.Cube.Tests --filter Osc260815 → 12 pass
dotnet build NewLife.Cube / NewLife.CubeNC → 0 error
```

## 风险

- AC-02/AC-03 完整 UI 冒烟未点完；入口 AC-01 已确认。
- 字符串级批量 SQL、工厂注册后极短窗口仍可能漏触发（已记 code-review）。
