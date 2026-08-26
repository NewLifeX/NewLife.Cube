# OSC-260824fc7c Tasks



## A 编解码与守卫



- [x] A.1 新增 `web/src/core/utils/rolePermission.ts`：`parseMenuPermissionCatalog` / `parseRolePermission` / `serializeRolePermission` / `buildPermForest` / `checkedKeysFromRole` / `roleMapFromCheckedKeys` / `normalizeIamTypePath` / `isRolePermissionField` / `isMenuPermissionField`。行为锁在 design §2。

- [x] A.2 `rolePermission.spec.ts`：默认四叶、自定义 flag、Role 升序序列化、非法段丢弃、有子菜单不挂权限叶、checked 往返、typePath `/Admin/Role`。

- [x] A.3 新增 `web/src/core/utils/iamGuards.ts`：`isIamRowActionDisabled(typePath, record, action)`、`isSystemRoleNameLocked(typePath, model, fieldName)`。spec：系统角色 delete；非 Role 不拦截；`isSystem` 缺省当 false。

- [x] A.4 新增 `web/src/core/utils/accountCenter.ts`：`parseAccountTab`（非法→`profile`）、`resolveSsoAccountUrl`。spec：缺开关、空地址、无 http(s)、profile/password 路径。



## B 角色树 UI



- [x] B.1 `RolePermTree.vue` 薄模板 + `useRolePermTree.ts`：`getList('/Admin/Menu')` 分页；工具条四按钮；spin/alert/空态。emit `update:modelValue` 为序列化串。禁止在 `.vue` 写 fetch。
  - 叶子权限位挂在节点 `perms` 横排勾选（非 tree children）；`a-tree` `selectable=false`，勾选走自定义 checkbox。
  - **会话补录**：分组/菜单项标题前增加全选勾选框（半选 indeterminate）；`collectPermKeysUnderNode` / `nodePermCheckState` / `toggleNodePerms`。

- [x] B.2 `FieldInput.vue`：`isRolePermissionField` 优先渲染 `RolePermTree`（`span` 由 FormContent 拉满）。`isMenuPermissionField`：只读文本 + 说明句。

- [x] B.3 `FormContent.vue`：角色 Permission 或菜单 Permission 说明控件 `a-col :span=24`。系统角色编辑锁 `Name`（调用 `isSystemRoleNameLocked`）。`isSystem` 编辑态 disabled。

- [x] B.4 `useListTable.ts`：`action==='delete'` 且 `isIamRowActionDisabled` 则不创建删除链接。卡片/其它视图若复用同一 ops 渲染则同判。

- [x] B.5 `useListCrud.ts`：批量删除前若选中任一行 `isSystem` 且 typePath 为 Role → Message 拒绝，不请求。

- [x] B.6 `useRecordDrawer.ts`：系统角色不提供删除（与列表一致）。抽屉无实体删除按钮；`entityDeleteLocked` 与列表 `handleDelete`/`onCardDelete` 同判。
  - **会话补录**：详情态 `isRolePermissionField` 渲染只读 `RolePermTree`（`disabled`），不走 `formatDetail` 编码文本。

- [x] B.7 `RoleController.cs`：`ListFields.RemoveField("Permission")`。不改 `Valid` 解析。

- [x] B.8 `MenuController.cs`：表单字段 `Permission` `ReadOnly = true`（`OnGetFields` 或等价）。不改菜单扫描生成目录。



## C 账号中心



- [x] C.1 api-core `createUserApi`：`profile` GET `/Admin/User/Info`；`updateProfile` POST `/Admin/User/Info`；`changePassword` POST `/Admin/User/ChangePassword`。`api.spec.ts` 断言 method/url。

- [x] C.2 `LoginConfig` 增 `ssoUserCenter` / `redirectUserToSso`。`CubeSetting`（`NewLife.Cube/Setting.cs` 与 `NewLife.CubeNC/Setting.cs` 同步）两属性。`AuthController.BuildLoginConfigPayload` 与 NC `CubeController` 登录配置附加这两项。

- [x] C.3 路由：`/account` → `AccountCenter.vue`；`/account/security` redirect `tab=security`。

- [x] C.4 `AccountCenter.vue` + `useAccountCenter.ts`：Tab 顺序 **资料 / 密码 / 安全 / 绑定**。资料字段 design §4.4；密码 §4.5。安全/绑定复用现 MFA 与 OAuth 逻辑（可从 `useSecuritySettings` 拆面板，禁止复制一套 API）。SSO 外跳按钮文案固定「前往用户中心」。

- [x] C.5 `SecuritySettings.vue`：改为可被 Tab 嵌入的面板，或删除独立页只留 composable。不得残留第二套 `/account/security` 组件路由。

- [x] C.6 `ShellToolbar.vue` / `useShellToolbar.ts`：账号分组增加「个人信息」（图标 `user`），位于「账号安全」之上。`goProfile` 按 `resolveSsoAccountUrl(..., 'profile')`。



## D 用户列表说明



- [x] D.1 `DefaultList.vue` + composable：`admin/user` 且非系统用户且仅一行且 id=自己 → `a-alert` 文案与 design §5.1 逐字一致。不改 User `DataPermission`。



## 测试 / 构建 / 文档



- [x] T.1 `pnpm --filter @cube/api-core test` 新增用例全过。

- [x] T.2 `pnpm --filter @cube/arco-vue test` 本号 spec 全过。

- [x] T.3 `pnpm --filter @cube/arco-vue build` 0 error。

- [x] T.4 若改了 C#：`dotnet build` NewLife.Cube 无 error。

- [x] T.5 手工冒烟对照 verify AC（单元覆盖编解码/守卫/SSO URL；浏览器端到端冒烟待验收环境）。

- [x] Doc.1 同步 `Doc/功能清单.md` PERM-1/2 与 SPA-7、`Doc/Api/SSO子应用跳转用户中心.md` 状态、`web/README.md` 账号路由、迁移方案开箱即用权限一句。

