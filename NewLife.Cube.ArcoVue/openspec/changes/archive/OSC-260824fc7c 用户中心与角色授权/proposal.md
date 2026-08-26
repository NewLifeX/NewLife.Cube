# OSC-260824fc7c — 用户中心与角色授权

## 1. 目标愿景

让管理员能在 ArcoVue 里用菜单树勾选角色权限，让登录用户能维护自己的资料与密码，而不离开 Cube「实体 + 菜单 + 动作位」契约。

- 目标 1：`Admin/Role` 编辑抽屉用权限树代替 `Permission` 文本框，保存仍为 `menuId#bitmask` 字符串，后端 `RoleController.Valid` 解析路径不变。
- 目标 2：顶栏进入 `/account` 可改资料、改密、MFA、绑定；`RedirectUserToSso` 开启时资料/改密外跳 CubeSSO。
- 目标 3：系统角色不可删、名称不可改；菜单实体的权限目录只读；非系统用户在用户列表仅见自己时有说明。
- 目标 4：不新增 `/iam` 聚合台、不改 `PermissionFlags`、不把 ViewProfile 藏列当成授权。

## 2. 为何做

ArcoVue 的 User / Role / Menu / Department 全部走 `DynamicPage` → `DefaultList`。角色 `Permission` 是 `12#7,15#15` 文本，管理员无法勾选。个人资料与已登录改密后端已有（`/Admin/User/Info`、`ChangePassword`），SPA 未接线。Cube.Vue 与 MVC `SetPermission` 已证明树交互足够；缺口只在默认皮肤。

功能清单 PERM-1/2 后端 ✅、ArcoVue 无专用 UI。竞品（Arco/Ant Design Pro、RuoYi）都是「系统管理菜单 + 角色页内授权树」，与方案一一致。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | **方案一**：管理侧保留 `Admin/User|Role|Menu|Department` 实体路由与 DefaultList；只对授权树、账号中心、系统角色保护、菜单目录只读做覆写。 |
| 2 | **不做方案二**：不新建 `/iam` 或「组织与权限」工作台替代实体页。 |
| 3 | 权限存储不变：`Role.Permission` = `MenuID#Flags` 逗号串；Flags 为 `PermissionFlags` 位或。`Menu.Permission` 是目录 `flag#显示名`，禁止当角色授权串编辑。 |
| 4 | 授权树叶子来自该菜单 **目录**（解析 `Menu.Permission`）；目录空则回退 `1#查看,2#添加,4#修改,8#删除`。自定义位（非 1/2/4/8）只要出现在目录里就必须可勾选。 |
| 5 | 菜单数据源为 **`GET /Admin/Menu` 全量分页**，不是 `/Cube/MenuTree`（后者已按当前用户裁剪）。无 Menu 查看权则树空并提示。 |
| 6 | 角色页优先 **L2**：`FieldInput` 在 `Admin/Role` + 字段 `Permission` 时渲染树，不整页覆写 `apps/`。 |
| 7 | 个人中心扩现有 `/account/security` 为 `/account` 多 Tab（资料 / 安全 / 绑定）；旧路径重定向到 `?tab=security`。 |
| 8 | SSO 外跳：本号补齐 `CubeSetting.SsoUserCenter` + `RedirectUserToSso`，经 `LoginConfig` 下发。仅资料与改密外跳；MFA/绑定仍走本应用。 |
| 9 | 资料可写：`DisplayName` / `Sex` / `Mail` / `Mobile`。禁止改 `Name`/`RoleID`/`Enable`（后端已拒）。本号 **不做头像上传**。 |
| 10 | **不落地** OSC-26082097c1 人员/角色/部门专用控件；User 的 `RoleIds` 仍用现有 select/LOV。 |
| 11 | 字段级权限、DataPermission 表达式下发、导入导出 16/32 位，均不在本号。 |

## 4. 做什么

1. 纯函数：目录解析、树构建、勾选 ↔ `Role.Permission` 编解码；IAM 行操作禁用（系统角色删）。
2. `RolePermTree` 接入 Role 表单；列表去掉 Permission 列；系统角色禁删、编辑时锁名称。
3. `/account`：资料 GET/POST `/Admin/User/Info`；改密 POST `/Admin/User/ChangePassword`；安全/绑定迁入 Tab。
4. LoginConfig + CubeSetting SSO 两字段；顶栏「个人信息」；外跳 URL 拼接。
5. Menu 表单 Permission 只读提示；User 列表「仅自己」说明。
6. api-core 补 Info / ChangePassword；单测 + 构建；同步功能清单 / SSO 文档状态。

## 5. 不做什么

- 不新建平行 ACL、字段矩阵、视图级权限。
- 不改 `EntityAuthorize` / `DataPermissionAttribute` / 多角色 OR 聚合 / `IsSystem` 跳过行权。
- 不把 Menu 页做成第二套授权器。
- 不整页覆写 User/Menu/Department；不拖拽菜单排序。
- 不实现 Cube.Vue Element Plus 页面搬运；Arco 重写。
- 不做 FlowGram、VTable 新视图、头像 multipart。
- 不改 Cube.Vue。

## 6. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-0003 | DefaultList / DynamicPage / Section 扫描 |
| OSC-260813397e | `/account/security`、LoginConfig、顶栏用户菜单 |
| OSC-0018 | L2 字段覆写，不升 L3 除非树无法塞进抽屉 |
| OSC-26082097c1 | 正交；本号不实现人员控件 |
| Cube.Vue `admin/role` | 交互对照，不迁代码 |
| `RoleController.Valid` | JSON 下已解析 Permission 字符串 |

## 7. 测试范围

| 类型 | 是否做 | 说明 |
| --- | --- | --- |
| Vitest | 是 | 编解码、目录回退、合成节点 id、SSO href、系统角色禁删、typePath 归一化 |
| api-core | 是 | `profile` / `updateProfile` / `changePassword` URL |
| XUnit / build | 是 | LoginConfig 含 SSO 字段；`dotnet` 能编过（不强制新控制器单测若无现成夹具） |
| 手工冒烟 | 是 | Role 勾选保存再打开一致；账号资料/改密；SSO 开关外跳；系统角色无删除 |

## 8. 成功标准

- [ ] 管理员打开角色编辑，Permission 为可勾选树而非 textarea；保存后再进仍勾选一致。
- [ ] 空目录菜单仍出现查看/添加/修改/删除四叶；目录有自定义位则出现对应叶。
- [ ] 系统角色：列表无删除；编辑名称禁用；`isSystem` 编辑不可改。
- [ ] `/account` 可改昵称邮箱手机；改密校验两次一致；旧 `/account/security` 仍进入安全 Tab。
- [ ] `RedirectUserToSso=true` 且 `SsoUserCenter` 非空：个人信息与改密走用户中心 MVC 路径；MFA 仍本页。
- [ ] 菜单 Permission 只读；角色列表不展示 bitmask 列。
- [ ] 本号新增单测全过；`pnpm --filter @cube/arco-vue build` 无 error。
