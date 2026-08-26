# OSC-260824fc7c Verify

> 进入 `Validating` 后逐项勾选。命令在仓库 `NewLife.Cube` 根或注明的 `web/` 下执行。

## 必须保留（暂缓区，实施误删即失败）

- `RoleController.Valid` 仍按 `MenuID#Flags` 解析并 `entity.Set`；禁止改成只写字符串不更新 `Permissions` 字典。
- `PermissionFlags` 仍为 1/2/4/8；禁止为导入导出新增 16/32 并要求本号 UI 勾选。
- `/Cube/MenuTree` 仍只服务导航，**不得**改成角色授权数据源。
- 不新增 `/iam` 路由，不整页覆写 `apps/**/admin/role/index.vue`。
- 不实现字段级权限、不把 `ColumnsJson` 当 ACL。
- 不改 Cube.Vue；不迁 Element Plus 角色页。
- GetPage `[AllowAnonymous]` 与 `DataPermission` 表达式不下发浏览器。
- OSC-260813397e 的 MFA / OAuth 绑定 / 租户切换行为保持；本号只搬家到 Tab。
- 不做头像上传；不做 97c1 人员控件。

## 命令与预期

```
pnpm --filter @cube/api-core test
pnpm --filter @cube/arco-vue test
pnpm --filter @cube/arco-vue build
```

预期：0 failed；vue-tsc/vite 0 error。改 C# 后另：`dotnet build NewLife.Cube/NewLife.Cube.csproj` 0 error。

## Happy path

- [x] **AC-01 角色树**：`isRolePermissionField` + `FieldInput` 渲染 `RolePermTree`；编解码单测往返；保存仍为 `menuId#flags`（`RoleController.Valid` 未改）。浏览器端到端见残余。
- [x] **AC-02 新增角色**：空 `model-value` → 全不选（`checkedKeysFromRole` 空 Map）；前端不预勾全树。
- [x] **AC-03 目录自定义位**：`parseMenuPermissionCatalog` 保留非 1/2/4/8（spec 含 16）。
- [x] **AC-04 目录空**：空串回退查看/添加/修改/删除四叶（spec）。
- [x] **AC-05 列表无 bitmask**：`RoleController` `ListFields.RemoveField("Permission")`（WebAPI / CubeDemo）。
- [x] **AC-06 账号资料**：`/account` 默认 `parseAccountTab`→`profile`；`buildProfilePayload` 不含 `name`；保存后写 `userStore.displayName`。
- [x] **AC-07 改密**：`savePassword` 两次不一致 `Message.warning` 且不请求；一致才 `changePassword`。
- [x] **AC-08 旧链**：`/account/security` redirect `tab=security`。
- [x] **AC-09 顶栏**：`ShellToolbar`「个人信息」在「账号安全」之上，图标 `user`；`goProfile`。

## 权限 / 空 / 非法 / 兼容

- [x] **AC-10 无菜单权**：`loadAllMenusForPermTree` 401/403 → `{ menus:[], error:'无权加载菜单目录' }`；`emitFromKeys` 在 `disabled` 或未改时不写空串。
- [x] **AC-11 系统角色**：`isIamRowActionDisabled` / `isIamBatchDeleteBlocked` / 名称与 `isSystem` 锁定；文案「含系统角色，无法批量删除」。
- [x] **AC-12 菜单目录只读**：Cube `MenuController.OnGetFields` `ReadOnly=true`；前端 `MENU_PERMISSION_HINT`。
- [x] **AC-13 仅自己**：`shouldShowSelfOnlyUserAlert` + 文案与 design §5.1 逐字一致（spec）。
- [x] **AC-14 SSO 关**：`resolveSsoAccountUrl` redirect 假 → null，走本地表单。
- [x] **AC-15 SSO 开**：合法 https 拼 `/Admin/User/Info` 与 `ChangePassword`；MFA Tab 忽略 SSO。
- [x] **AC-16 SSO 非法**：`javascript:` → null（spec）。
- [x] **AC-17 禁止改登录名**：资料字段 `name` readOnly；payload 无 `name`/`roleID`/`enable`。
- [x] **AC-18 未登录**：`/account` 为 Layout 子路由，非 public（与其它壳页一致，401 进登录）。
- [x] **AC-19 详情只读**：`RecordDrawer` 详情对 `isRolePermissionField` 渲染 `RolePermTree` `disabled`；`emitFromKeys` 在 disabled 时 return。
- [x] **AC-20 旧 Role 串**：非法段丢弃（spec `12#x,15#7`）。
- [x] **AC-21 节点全选**（会话补录）：分组/菜单项前 checkbox + indeterminate；`collectPermKeysUnderNode` / `nodePermCheckState` spec。

## 构建门禁

- [x] 本号新增 `*.spec.ts` 全过。
- [x] `sfcThin.spec.ts`：本号新增 `.vue` 无业务 `watch`/`cubeApi`（64 SFC 扫描通过）。

## 执行记录（Implementing）

- 2026-08-24：`pnpm --filter @cube/api-core test` 通过（含 profile / updateProfile / changePassword URL）。
- 2026-08-24：`pnpm --filter @cube/arco-vue test` 72 files / 654 tests 通过（含 `rolePermission.spec.ts`、`iamGuards.spec.ts`、`accountCenter.spec.ts`、`sfcThin.spec.ts`）。
- 2026-08-24：`pnpm --filter @cube/arco-vue build` vue-tsc + vite 0 error。
- 2026-08-24：`dotnet build NewLife.Cube/NewLife.Cube.csproj` 0 error。
- 浏览器端到端 AC-01～AC-20 留待验收环境勾选。

## Validating 验收记录（2026-08-26 openspec-verify）

> 触发：严格按照本项目 OpenSpec 规范，验收和复盘 fc7c 变更。

### 会话小任务补录

执行期已注明「会话小任务已补录」。验收核对后新增（并入既有项，不新建）：

- B.1：叶子 `perms` 横排勾选 + 分组/菜单项全选 checkbox
- B.6：详情抽屉只读 `RolePermTree`（兑现原 AC-19）

### 测试与构建门禁

| 命令 | 结果 |
|------|------|
| `pnpm --filter @cube/api-core test` | vitest 37 + node:test 51，全过（含 profile/updateProfile/changePassword URL） |
| `pnpm --filter @cube/arco-vue test` | 72 files / **670** passed / 0 failed（本号 `rolePermission` 18、`iamGuards` 7、`accountCenter` 8、`loginConfig` SSO 缺省、`sfcThin` 64） |
| `pnpm --filter @cube/arco-vue build` | vue-tsc + vite **0 error**（仅 chunk size / dynamic import 警告） |
| `dotnet build NewLife.Cube/NewLife.Cube.csproj` | **0 error**（493 warning，既有） |

首次 vue-tsc 因并行 WIP `useCascaderField.ts` TS2345 失败；已最小收窄 `modelValue == null`（**非本号交付**，复盘提交排除该文件）。

### 三步编排摘要

| 步骤 | 结论 |
| --- | --- |
| 实现审计 | 目标 1–4 均有代码落点；design 文件地图到位；tasks A–D / T / Doc 已勾选。禁止项未越界（无 `/iam`、无 L3 `apps/admin/role`、未改 `PermissionFlags` 1/2/4/8、未改 `RoleController.Valid`、授权源为 `GET /Admin/Menu` 非 MenuTree、未改 Cube.Vue）。功能清单 PERM-1/2 / SPA-7 已同步本号事实。 |
| 代码审查 | 本号 C# 使用 `String`/`Boolean`/`Int32`；无硬编码密钥。SFC 薄壳门禁通过。🔴 **无**。🟡 CubeNC `MenuController` 未设 Permission `ReadOnly`、CubeNC `RoleController` 未 `RemoveField("Permission")`（CubeDemo 走 NewLife.Cube，不阻断 ArcoVue）。🟢 树交互由 `a-tree checkable` 演进为自定义 checkbox + `perms`（功能等价，已补录 B.1）。 |
| 文档同步 | `Doc/功能清单.md` PERM-2/SPA-7；`Doc/Api/SSO子应用跳转用户中心.md` 已标已实现；`web/README.md` `/account`；迁移方案 §开箱即用「权限」一句。未改 `Doc/PERM-权限系统.md`。 |

### 目标愿景对照（proposal §1）

| 目标 | 结论 |
| --- | --- |
| 1 角色编辑抽屉权限树，保存仍 `menuId#bitmask` | ✅ FieldInput + RolePermTree；Valid 解析路径未改 |
| 2 `/account` 资料/改密/MFA/绑定；SSO 外跳资料与改密 | ✅ AccountCenter 四 Tab；`resolveSsoAccountUrl` |
| 3 系统角色不可删、名称不可改；菜单目录只读；仅自己说明 | ✅ iamGuards + Menu ReadOnly（WebAPI）+ 告警文案 |
| 4 不新增 `/iam`、不改 PermissionFlags、不把 ViewProfile 当授权 | ✅ 已遵守 |

### 缺口清单

**无 P0 / P1 实现缺口。** 下列为 P2 残余（用户本次要求验收并复盘，按既有号惯例**仅记录不补齐**）：

| 级 | 项 | 证据 |
| --- | --- | --- |
| P2 | 浏览器端到端未在本会话实机点击 AC-01～AC-18 | 无 browser MCP；以单测 + 代码路径验收 |
| P2 | CubeNC 双栈未同步 Menu ReadOnly / Role 列表去 Permission 列 | `NewLife.CubeNC/.../MenuController.cs` 无 `OnGetFields`；CubeDemo 引用 NewLife.Cube |
| P2 | 列表壳 B.4/B.5/D.1 与未入库的 03c0 填色/启停同文件 | 工作区已实现；复盘提交排除混改，避免把 03c0/地区/甘特打进本号 |

### checklist: passed

保持 `Validating`，可复盘归档。

## 残余（可接受，不阻断）

- CubeNC MVC 宿主菜单 Permission 仍可编辑、角色列表仍可能显示 bitmask（非 ArcoVue CubeDemo 路径）。
- 浏览器 SSO 真用户中心、无 Menu 权账号、系统角色批量删除的实机点击。
- design §2.4 仍写权限叶为 tree children + `checkable`；实现为叶子 `perms` 横排，已在 tasks B.1 补录。

