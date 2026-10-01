# OSC-2610011cd6 Verify — 列表行内动作

## AC-1 马上执行

- [x] AC1.1 定时作业「马上执行」先确认，确认后请求方法为 POST，URL 含 `ExecuteNow`。证据：`CronJobController` DisplayName「马上执行」且 `DataAction=action`；`invokeOpsLink` 先 `confirmText` 再 `requestDataAction(..., POST)`。
- [x] AC1.2 取消确认不发请求。证据：`askActionConfirm` 返回 false 时 `invokeOpsLink` / `onCellLink` 直接 return。
- [x] AC1.3 `code!==0` 时提示 message 且不刷新。证据：api-core 对 `code!==0` reject 为 `ApiError`；`runOpsCustomLink` 的 catch 只 `Message.error`，不调用 `onDone`。

## AC-2 解封

- [x] AC2.1 访问规则过期时间链接指向 `/api/.../Unblock` 时发 GET，地址栏不变成该 api 路径。证据：`ExpireTime` 有 TypeName，分流为单元格；`isApiActionUrl` 为真时 `runCellFieldLink` GET，不 `router.push`。
- [x] AC2.2 `/Admin/Log?userId=1` 仍是站内导航。证据：`isApiActionUrl` 单测为 false；无 dataAction 时 `navigateResolvedUrl`。

## AC-3 用户安全动作

- [x] AC3.1 系统角色编辑已有用户时看到「清空密码」「吊销令牌」。证据：`showUserSecurityActions` 单测；`RecordDrawer.vue` 底部 `v-if`。
- [x] AC3.2 非系统角色、新增模式、主键 ≤0 时两个按钮都不渲染。证据：同一函数对 add、非系统、id=0、非 `admin/user` 返回 false。
- [x] AC3.3 确认后分别 POST ClearPassword 与 RevokeTokens。失败不关闭抽屉。证据：`confirmRecordAction` 只发 POST；成功 `Message.success`，不改 `visible`。

## AC-4 强制下线

- [x] AC4.1 用户在线 GetPage 的列表字段含「强制下线」，dataAction 为 action，Url 指向 Kick。证据：静态构造赋值；`UserOnline_KickField_IsDataAction` 通过。
- [x] AC4.2 点击确认后 POST。无 Delete 权限时操作列不出现该链接（沿用现有权限裁剪，本号不放宽）。证据：dataAction 走 POST；`visibleOpsLinks` 在 `canDelete===false` 时去掉 `/UserOnline/Kick`。

## 命令

```powershell
cd NewLife.Cube.ArcoVue/web
pnpm exec vitest run src/core/utils/opsRequest.spec.ts
pnpm exec vue-tsc -b
dotnet test "NewLife.Cube.Tests/NewLife.Cube.Tests.csproj" --filter Osc2610011cd6
dotnet build NewLife.Cube/NewLife.Cube.csproj -f net10.0
```

预期：vitest 与过滤后的测试全部通过；vue-tsc 与 build 无错误。测试项目路径若与仓库 csproj 名不一致，以 `NewLife.Cube.Tests` 目录下实际 csproj 为准，过滤名不变。

## 执行阶段记录（openspec-apply，2026-10-01）

| 命令 | 结果 |
| --- | --- |
| `pnpm exec vitest run src/core/utils/opsRequest.spec.ts` | 4/4 通过 |
| `pnpm exec vitest run src/core/utils/listLinkFields.spec.ts` | 8/8 通过 |
| `dotnet test NewLife.Cube.Tests/NewLife.Cube.Tests.csproj --filter Osc2610011cd6` | 通过 2，失败 0 |
| `pnpm exec vue-tsc -b` | 无错误 |
| `dotnet build NewLife.Cube/NewLife.Cube.csproj -f net10.0` | 成功，0 错误 |

代码路径：dataAction 确认后 POST 并按 `code` 决定提示与刷新；`/api/` 单元格确认后 GET，不 `router.push`；用户抽屉两个按钮按 `showUserSecurityActions` 渲染，成功不关抽屉；强制下线为合成列，无 Delete 权限不进操作列。

浏览器点击（马上执行、解封、清空密码、吊销令牌、强制下线）留验收。AC 勾选在验收阶段完成。

### 会话小任务补录

- T5：`useFieldInput.ts` 上传结果断言。vue-tsc 报 TS2352，改为 `as unknown as Record<string, unknown>`。

### 代码审查

- 🔴 无。
- 🟡 `Modal.confirm` 的 `content` 为空字符串。Arco 类型把 `content` 标成必填；确认句在 `title`，与 design 一致。
- 🟢 测试文件使用 `String` / `Object`。`Kick` 仍为 `[HttpPost]` + Delete。

### 实现审计

- 目标 1～4 均有实现与单测。无功能清单新接口。
- 无实现缺口。浏览器 AC 未在本阶段点选。

## 验收阶段记录（openspec-verify，2026-10-01）

会话小任务复核：自执行补录 T5 之后无新增，不重复建任务。

### 三步摘要

| 步骤 | 结果 |
| --- | --- |
| 实现审计 | 目标 1～4 均有代码。判定函数与合成列有单测。`requestDataAction` 的 HTTP 往返无集成测试，proposal 允许只反射 Kick。 |
| 代码审查 | 🔴 无。🟡 `Modal.confirm` 的 `content` 为空（类型必填，文案在 `title`）。测试方法无 XML，不阻断。 |
| 文档同步 | 迁移方案矩阵已写 dataAction POST 与 `/api/` 单元格 GET。`Doc/功能清单.md` 只把 SYS-10 测试列改为 🟡，编码未改。`web/README.md`、`Doc/Api/核心接口架构.md`、根 README、竞品分析无需改。 |

### 目标愿景对照

| 目标 | 结论 |
| --- | --- |
| 1 dataAction POST，马上执行成功提示并刷新 | 达成 |
| 2 `/api/` 单元格 GET，不导航 | 达成 |
| 3 系统角色用户抽屉可清空密码、吊销令牌 | 达成 |
| 4 强制下线确认后 POST，无 Delete 不显示 | 达成 |

缺口清单：无。未在浏览器点选四条动作，记为风险，不构成实现缺口（proposal §7 的验收门禁是本号单测、`vue-tsc -b`、`dotnet build`）。

### 测试与构建（验收重跑）

| 命令 | 结果 |
| --- | --- |
| `pnpm exec vitest run src/core/utils/opsRequest.spec.ts src/core/utils/listLinkFields.spec.ts` | 12/12 通过 |
| `dotnet test NewLife.Cube.Tests/NewLife.Cube.Tests.csproj --filter Osc2610011cd6` | 通过 2，失败 0 |
| `pnpm exec vue-tsc -b` | 无错误 |
| `dotnet build NewLife.Cube/NewLife.Cube.csproj -f net10.0` | 成功，0 错误 |

checklist: passed。随后进入复盘，state 置为 Done。

