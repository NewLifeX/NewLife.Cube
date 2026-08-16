# OSC-260815fa86 Verify

> 进入 Validating 后勾选。创建阶段只列 AC，不勾选。

## 执行阶段记录（openspec-apply）

- 2026-08-15 Implementing：后端拦截+执行器+API、api-core、ArcoVue 抽屉/行按钮/Runs Tab、XUnit/Vitest/构建已通过。
- 命令：`pnpm --filter @cube/api-core test`；`pnpm --filter @cube/arco-vue test`；`pnpm --filter @cube/arco-vue build`；`dotnet test NewLife.Cube.Tests --filter Osc260815`；`dotnet build NewLife.Cube`；`dotnet build NewLife.CubeNC`。0 failed / 0 error。
- 手工 AC-01～AC-03 未在本机打开 Admin/User 浏览器；verify 阶段补冒烟或记跳过。

## 验收阶段记录（openspec-verify）

- （待填）

## 验收标准

### Happy path

- [ ] **AC-01 入口**：实体 DefaultList（如 Admin/User）顶栏在「搜索」与「高级」之间有「自动化」；Object/Home 页没有该按钮。
- [ ] **AC-02 创建线性流**：三步保存「新增记录 → 条件可选 → 发站内信」，库中 GraphJson 为 start→[filter?]→notify→end，edges 单链。
- [ ] **AC-03 拦截触发**：该规则 Enable=true 时，经列表新增一条记录后出现 Status=succeeded 的 AutomationRun（允许异步，验收最多等 10s 刷新）。
- [ ] **AC-04 飞书动作集**：编辑器「添加动作」仅出现 notify/updateRecord/createRecord/findRecords/httpRequest/delay/runAutomation/addComment/aiText；触发单选含 insert/update/delete/insertOrUpdateIf/fieldChange/dateArrive/schedule/button/webhook。
- [ ] **AC-05 按钮**：button 规则启用后，行操作出现自定义文案；点击后新增 Run。
- [ ] **AC-06 Webhook**：用 HookToken POST JSON 返回 runId；能在 Runs 看到 TriggerKind=webhook。

### 权限 / 空 / 非法 / 旧数据

- [ ] **AC-07 无 Update**：无该实体 Update 权限的账号不显示顶栏「自动化」；直接 POST `/Cube/Automation` 返回 403。
- [ ] **AC-08 空列表**：无规则时抽屉 `a-empty`「还没有自动化，点击下方创建」，有「创建自动化」。
- [ ] **AC-09 空 filter**：无条件保存不写 filter 节点（start 直接连第一个动作或 end）；执行视为通过。
- [ ] **AC-10 非法 TriggerKind / 空 Name / actions>20**：保存 400 或前端拦截且不落库。
- [ ] **AC-11 错误 Webhook token**：POST 返回 404，不新建 Run。
- [ ] **AC-12 签名**：`requireSignature=true` 且头错误 → 401，无 Run。
- [ ] **AC-13 循环**：runAutomation 指向自身保存拒绝；Depth≥3 的入队产生 failed「超过最大深度」且不再连锁写入。
- [ ] **AC-14 旧 JSON**：TriggerConfig 含未知键，保存后仍在；Graph version 缺省当 1；`version=2` 保存拒绝。
- [ ] **AC-15 预留节点**：手工把 GraphJson 写成含 `approval` 的链，执行该 Run 为 failed 且 Error 含「未实现」，不静默跳过。
- [ ] **AC-16 fieldChange**：watchFields=`["Name"]` 时只改其它字段不入队，改 Name 入队。
- [ ] **AC-17 关闭 Enable**：Enable=false 的规则不入队、不出现行按钮。
- [ ] **AC-18 租户**：规则 TenantId=A 在租户 B 上下文插入不入队（EnableTenant 开启时）。

### 必须保留（暂缓区，实施不得删）

- [ ] **AC-19** 不得删除或改语义：`DefaultList.vue` 的 FilterBuilderPopover / GroupPopover / 搜索 / 高级；`matchesViewFilter`；`FilterBuilderPopover`；历史与讨论 Tab；`createCommentApi`。
- [ ] **AC-20** 不得引入 FlowGram npm 依赖；不得把执行放到 Node 运行时。
- [ ] **AC-21** 不得只挂钩 `EntityController.OnInsert` 而不挂 XCode 模块（导入/EnableDisable/直接 Insert 也须能触发，单测或手工至少覆盖 `entity.Insert()` 或导入之一）。

### 测试与构建

- [ ] **AC-22** `pnpm --filter @cube/api-core test` 与 `pnpm --filter @cube/arco-vue test` 全过，含本号新增 spec。
- [ ] **AC-23** `pnpm --filter @cube/arco-vue build` 无错误。
- [ ] **AC-24** `dotnet test NewLife.Cube.Tests --filter Osc260815` 全过；`dotnet build NewLife.Cube` 与 `dotnet build NewLife.CubeNC` 无错误。
- [ ] **AC-25** 文档四份已登记：web README、功能清单、核心接口架构、迁移方案（写明自动化 ≠ FlowGram 运行时）。

## 命令与预期

```text
pnpm --filter @cube/api-core test
pnpm --filter @cube/arco-vue test
pnpm --filter @cube/arco-vue build
dotnet test NewLife.Cube.Tests --filter Osc260815
dotnet build NewLife.Cube
dotnet build NewLife.CubeNC
```

预期：测试 0 failed；构建 0 error。Webhook/定时手工依赖运行中的 WebAPI；CI 以单测为准。
