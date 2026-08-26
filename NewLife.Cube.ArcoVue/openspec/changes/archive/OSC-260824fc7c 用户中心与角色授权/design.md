# OSC-260824fc7c Design — 用户中心与角色授权

适用前端：Arco Design Vue（https://arco.design/vue/docs/start），权限树用 [Tree](https://arco.design/vue/component/tree)（`checkable`、`checked-strategy=child`）。不涉及 VisActor 新配置、不涉及 FlowGram。`.vue` 薄 script，业务进同目录 `useXxx.ts` 或 `core/utils` 纯函数。

图标：顶栏「个人信息」用已注册 IconPark `user`；账号安全继续 `permissions`。不新增图标包。

## 0. 方案对照（实施勿扩范围）

| 项 | 本号 | 明确不做 |
| --- | --- | --- |
| 管理 IA | 现有 `Admin/*` DefaultList | `/iam` 聚合台 |
| 角色授权 | 抽屉内树 → `Permission` 字符串 | 改存储、改 EntityAuthorize |
| 菜单源 | `/Admin/Menu` 分页全量 | `/Cube/MenuTree` |
| 账号 | `/account` 四能力 Tab | 头像上传、97c1 人员控件 |
| SSO | Setting + LoginConfig 两字段 | 子应用直连用户中心 API 改用户 |

## 1. 状态唯一来源

| 状态 | 来源 | 禁止 |
| --- | --- | --- |
| 角色权限串 | 表单 `model.permission`（大小写随 GetPage 字段名，写入前归一到实体属性 `Permission`） | 组件内另存一份 bitmask 且保存时不用串 |
| 菜单目录 | `GET /Admin/Menu` 行的 `permission` | 前端写死仅 1/2/4/8 而丢掉目录自定义位 |
| 当前用户资料 | `GET /Admin/User/Info` | 用 `/Auth/Info` 当可写资料（缺字段且非 UserInfo 模型） |
| SSO 是否外跳 | `LoginConfig.redirectUserToSso` **且** `ssoUserCenter` 去空白后非空 | 前端写死域名 |
| 系统角色 | 行/表单 `isSystem` | `roleName === '管理员'` |
| CRUD 按钮 | 菜单 `PermissionFlags` ∩ setting | 前端自造 16/32 位 |

`typePath` 比较一律 `normalizeIamTypePath(s)`：去首尾 `/`、反斜杠改 `/`、小写。`Admin/Role` 与 `/Admin/Role` 等价。

## 2. 权限编解码（纯函数，先于 UI）

文件：[`web/src/core/utils/rolePermission.ts`](../../web/src/core/utils/rolePermission.ts) + `rolePermission.spec.ts`。

### 2.1 两种 Permission 字符串

| 实体 | 格式 | 例 |
| --- | --- | --- |
| **Role** | `menuId#flags[,menuId#flags]*` | `12#7,15#15` |
| **Menu** | `flag#name[,flag#name]*` | `1#查看,2#添加,4#修改,8#删除` |

禁止混用解析器。

### 2.2 目录解析 `parseMenuPermissionCatalog(raw)`

- 空 / 非字符串 → 默认四元组 `[{flag:1,name:'查看'},{2,'添加'},{4,'修改'},{8,'删除'}]`。
- 按逗号拆；每段 `Split('#', 2)`；`flag` 必须 `parseInt` 成功且 **> 0**；`name` trim，空则用 `String(flag)`。
- 重复 flag：后者覆盖前者。
- 忽略 `flag<=0`、缺 `#`、非数字 flag。
- **不要**把 Role 串拿来当目录解析。

### 2.3 Role 串 `parseRolePermission(raw): Map<number, number>`

- 空 → 空 Map。
- 逗号拆；`menuId#flags` 两段都为整数；`flags > 0` 才写入；同一 menuId 后者覆盖。
- 非法段丢弃，不抛。

`serializeRolePermission(map): string`

- 只输出 `flags > 0` 的项。
- **按 menuId 升序**，格式 `${id}#${flags}` 逗号拼接，无空格。
- 空 Map → `''`。

### 2.4 树节点

```ts
type PermTreeNode = {
  key: string;           // 菜单: `m:${id}`；权限叶: `p:${menuId}:${flag}`
  title: string;
  children?: PermTreeNode[];
  isPerm?: boolean;
  menuId?: number;
  flag?: number;
};
```

`buildPermForest(menus: MenuRow[]): PermTreeNode[]`

- `MenuRow`：`id`（number）、`parentId`/`parentID` 任一、`displayName`/`name`、`permission`。
- 先建 `id → 节点`（无 children）；再按 parent 挂接；`parentId` 缺省/0/找不到父 → 根。
- 对每个 **无真实子菜单** 的节点，附加权限叶（catalog）。有子菜单的目录节点 **不** 挂权限叶（权限落在叶子菜单上，与 Cube.Vue 一致）。
- 权限叶不出现在 `id` 映射里，避免和真实菜单冲突。

`checkedKeysFromRole(map, forest): string[]`

- 仅权限叶：`map.get(menuId)` 含该 `flag` 位则加入 `p:menuId:flag`。
- 父菜单 key **不**写入受控 checked（交给 Tree 半选）。

`roleMapFromCheckedKeys(keys, forest): Map<number, number>`

- 只读 `p:` 前缀；按 menuId OR 各位；未知 key 忽略。
- 不根据父节点勾选推断（`checked-strategy=child` 保证 keys 只有叶）。

### 2.5 菜单列表加载（composable，非纯函数）

`loadAllMenusForPermTree(getList)`：

- `type = '/Admin/Menu'`（与 `cubeApi.page.getList` 其它实体一致）。
- `pageIndex` 从 1，`pageSize = 200`；累加 `data`；用 `page.totalCount`（缺省则本页 length）判断是否继续。
- 最多 **10** 页（2000 行）后停止。
- 401/403：返回 `{ menus: [], error: '无权加载菜单目录' }`，不把角色 Permission 清空。
- 树在 Role 抽屉 **打开编辑/新增** 时加载一次，缓存于 composable 实例；关闭抽屉丢弃。

## 3. 角色 UI

### 3.1 文件地图

| 文件 | 改动 | 勿动 |
| --- | --- | --- |
| `web/src/core/utils/rolePermission.ts` | 新建编解码 | — |
| `web/src/views/crud/RolePermTree.vue` | 薄模板：`a-tree` + 全选/展开按钮 | 不调 Role PUT |
| `web/src/views/crud/useRolePermTree.ts` | 加载菜单、受控 checked、emit 字符串 | 不改 Menu 实体 |
| `web/src/components/FieldInput.vue` | `isRolePermissionField` 时渲染 `RolePermTree`，否则原控件 | 其它 ControlType 分支 |
| `web/src/views/crud/FormContent.vue` | Permission 树 `a-col :span=24` | 不改校验框架 |
| `web/src/core/utils/iamGuards.ts` | `isIamRowActionDisabled`、系统角色锁名 | 不改 DataPermission |
| `web/src/features/vtable/useListTable.ts` | 删除链接：禁用则不渲染 | 操作列 customLayout 契约 |
| `web/src/views/crud/useRecordDrawer.ts` | 系统角色隐藏/禁用抽屉删除 | 历史/评论 Tab |
| `NewLife.Cube/Areas/Admin/Controllers/RoleController.cs` | `ListFields.RemoveField("Permission")` | `Valid` 解析逻辑 |

不新增 `apps/admin/role/index.vue`（L3），除非验收时抽屉放不下树——那时另开号，本号禁止擅自升 L3。

### 3.2 何时渲染树

`isRolePermissionField(typePath, field)`：

- `normalizeIamTypePath(typePath) === 'admin/role'`
- `field.name` 大小写不敏感等于 `permission`

此时 **不要**走 textarea（即便 length≥300）。

`isFullWidth`：该字段强制 24 栅格。

### 3.3 Tree 交互

- 组件：`a-tree` `checkable` `checked-strategy="child"` `default-expand-all` 否；默认展开 **第一层根**。
- 工具条（树上方，顺序固定）：全部选中 / 全部取消 / 展开全部 / 收起全部。
- 高度：树容器 `min-height: 240px; max-height: 420px; overflow: auto`。
- `disabled`：表单 `readonly` 或字段 `readOnly` 时整树禁用（详情态）。
- 加载中：`a-spin` 包树；失败：`a-alert` type=warning 文案用 `error`，树空，**不改**当前 `model-value`。
- 空菜单且无 error：展示「暂无菜单」。
- 新增角色：`model-value` 空 → 全不选。后端对空串 + 插入 `IsSystem` 会赋全权限——**前端不预勾全树**（与现网 JSON 行为一致：插入后再编辑可见全勾）。

### 3.4 系统角色矩阵

| 输入 | 输出 |
| --- | --- |
| 列表/树表 `isSystem===true` 且 action=`delete` | 不渲染「删除」 |
| 其它 typePath 或 `isSystem` 假 | 删除可见性仍只看 `canDelete` |
| 编辑表单 `isSystem` 且字段 `name`/`Name` | `FieldInput` disabled（`isSystemRoleNameLocked`） |
| 编辑表单字段 `isSystem` | disabled，禁止关掉系统标记 |
| 新增表单 `isSystem` | 可开（与 Cube.Vue 一致） |
| 批量删除选中含系统角色 | 确认框排除系统角色或整批拒绝：采用 **整批拒绝**（Message「含系统角色，无法批量删除」），不发请求 |

卡片/看板/甘特：Role 实体极少用这些视图；若操作列存在，同一 `isIamRowActionDisabled`。

### 3.5 提交

现有 RecordDrawer PUT 整表。Permission 已是字符串，无需新 API。空字符串表示清空授权（非系统角色允许）。

## 4. 个人用户中心

### 4.1 路由

| 路径 | 行为 |
| --- | --- |
| `/account` | 新页 `AccountCenter.vue`，query `tab=profile\|security\|password\|binds`，缺省 `profile` |
| `/account/security` | `redirect: { path:'/account', query:{ tab:'security' } }` 保留书签 |

`router/index.ts` 在 Layout children 增加 `/account`；改写 security 为 redirect。

### 4.2 文件地图

| 文件 | 改动 |
| --- | --- |
| `packages/api-core/src/api.ts` | `user.profile` GET `/Admin/User/Info`；`user.updateProfile` POST 同 URL；`user.changePassword` POST `/Admin/User/ChangePassword` |
| `packages/api-core/tests` | URL 断言 |
| `web/src/core/utils/accountCenter.ts` | `parseAccountTab`、`resolveSsoAccountUrl` |
| `web/src/views/account/AccountCenter.vue` | 薄：`a-tabs` |
| `web/src/views/account/useAccountCenter.ts` | tab、资料、改密、调用现有 MFA/绑定逻辑 |
| `web/src/views/account/useSecuritySettings.ts` | 抽 MFA/绑定供 Tab 复用，删除独立页路由后可保留组件作 security 面板 |
| `web/src/layouts/useShellToolbar.ts` | `goProfile`；SSO 则 `window.location.assign` |
| `web/src/layouts/ShellToolbar.vue` | 「个人信息」在「账号安全」之上 |
| `NewLife.Cube/Setting.cs` | 两字段 |
| `NewLife.CubeNC/Setting.cs` | 同步同名字段（MVC 设置页） |
| `NewLife.Cube/Controllers/AuthController.cs` | `BuildLoginConfigPayload` 附加两字段 |
| `NewLife.CubeNC/Areas/Admin/Controllers/CubeController.cs` | 若仍拼 LoginConfig，同步附加 |

### 4.3 LoginConfig / Setting schema

CubeSetting（Category=`用户登录`，紧接 `LogoutAll` 之后）：

| 属性 | 类型 | 默认 | 说明 |
| --- | --- | --- | --- |
| `SsoUserCenter` | string | `""` | 用户中心根 URL，无尾斜杠亦可 |
| `RedirectUserToSso` | bool | `false` | 是否外跳资料/改密 |

LoginConfig 增量（camelCase）：

```ts
ssoUserCenter?: string;
redirectUserToSso?: boolean;
```

未知旧配置：缺字段视为 `false` / `''`。禁止删其它 LoginConfig 字段。

`resolveSsoAccountUrl(cfg, kind): string | null`

- `redirectUserToSso !== true` → `null`
- `ssoUserCenter` trim 空 → `null`
- 去掉尾部 `/` 后拼接：`profile` → `/Admin/User/Info`；`password` → `/Admin/User/ChangePassword`
- 非法 URL（无 `http://` 或 `https://` 前缀）→ `null`（避免 `javascript:`）

顶栏：个人信息 → url 非空则 assign，否则 `router.push('/account?tab=profile')`。
密码 Tab：url 非空则 **不渲染本地表单**，只显示按钮「前往用户中心修改密码」。
资料 Tab：url 非空同样外跳按钮，不提交本地 POST。
MFA / 绑定：忽略 SSO 开关。

### 4.4 资料表单

GET `/Admin/User/Info` → `data`。可编辑绑定：`displayName`、`sex`、`mail`、`mobile`（兼容 PascalCase 读入，提交 camelCase 与 Pascal 双写不要求，跟现网 JSON camelCase）。

只读展示：`name`、`roleNames`/`roleName`。

POST body 必须带 `id`/`ID` = 当前用户；**不得**提交 `name`、`roleID`、`enable`、`password`。失败展示 `message`。

成功：Message.success；可选刷新 `userStore` 显示名（若 store 有 `displayName` 则写入，无则忽略）。

性别：`a-radio-group` 未知/男/女，值与 `SexKinds` 0/1/2 对齐。

### 4.5 改密表单

字段顺序：原密码、新密码、确认新密码。`password` 输入框。

提交前本地：新密码非空；两次一致。强度提示文案「8 位起且含数字、大小写字母和符号」（与后端 `_passwordService.Valid` 一致），前端 **不重复实现正则**（避免与服务器漂移）；弱密码以后端 message 为准。

SSO 会话免原密码：后端看 Session，SPA 无此 Session 时仍显示原密码；后端若报原密码空，展示原文。

### 4.6 Tab 与权限

`/account` 登录即可（Layout 子路由，非 public）。无 `Admin/User` 菜单权仍允许 Info/ChangePassword（现网 `[EntityAuthorize]` 无 flags 即登录用户）。403 则 Message。

## 5. 用户列表说明与菜单目录

### 5.1 User 仅自己

`web/src/views/crud/DefaultList.vue` 在 `typePath` 为 `admin/user` 且满足：

- `userStore.userInfo?.isSystem !== true`
- 列表 `totalCount === 1`（或 data.length===1 且无 page）
- 唯一行 `id` 等于 `userStore.userInfo.id`（兼容 `ID`）

则在工具栏下展示 `a-alert`：「当前数据权限仅允许查看自己的账号。管理全部用户需要系统角色或调整数据权限。」关闭按钮无（一直显示直到条件不成立）。

系统用户不显示该条。

### 5.2 关联入口

本号 **不做** UserConnect/Token 深链。PERM-1 第三方链接仍靠实体自定义链接（已有 OSC-2608178bdb）。

### 5.3 Menu.Permission 只读

`MenuController`：`OnGetFields`（或静态构造后对 FormFields）将 `Permission` 的 `ReadOnly=true`。前端 `isMenuPermissionField` 时在控件下增加灰色说明：「权限子项由控制器 EntityAuthorize 生成，请勿手改。」即使有人改 JSON，只读字段不进脏写（现有提交归一化）；后端 ReadOnly 为对称。

列表可继续显示 Permission 短目录（与 Role 列表相反）。

## 6. 条件矩阵（穷尽）

### 6.1 角色树控件

| typePath | field.name | readonly | 渲染 |
| --- | --- | --- | --- |
| admin/role | permission | false | RolePermTree |
| admin/role | permission | true | 树 disabled |
| admin/role | remark | * | 原控件 |
| admin/menu | permission | * | 只读文本+说明，不是树 |
| 其它 | permission | * | textarea/input 原逻辑 |

### 6.2 SSO

| redirectUserToSso | ssoUserCenter | 个人信息 | 资料 Tab | 密码 Tab | MFA |
| --- | --- | --- | --- | --- | --- |
| false | * | `/account?tab=profile` | 本地表单 | 本地表单 | 本地 |
| true | 空/非法 | 同上（null url） | 本地 | 本地 | 本地 |
| true | `https://sso.example` | assign Info | 外跳按钮 | 外跳按钮 | 本地 |

## 7. 核心文档影响

| 文档 | 变更 |
| --- | --- |
| `Doc/功能清单.md` | PERM-1/2 说明补 ArcoVue 树/账号中心；SPA-7 记本号 |
| `Doc/Api/SSO子应用跳转用户中心.md` | 配置项改为已实现，路径与本号一致 |
| `ArcoVue企业中后台迁移方案.md` | §开箱即用「权限」一行：角色树已接线（不改容器模型） |
| `web/README.md` | 账号路由 `/account` |

不改 `Doc/PERM-权限系统.md` 模型（无新契约）。不改竞品分析字段级权限大 OSC 口径。

## 8. 测试设计

| 模块 | 用例要点 |
| --- | --- |
| `rolePermission.spec.ts` | 默认目录；自定义 16 位；Role 串排序；非法段；父子菜单只叶有权限；checked 往返 |
| `iamGuards.spec.ts` | 系统角色删；typePath 斜杠 |
| `accountCenter.spec.ts` | tab 非法→profile；SSO url；javascript: 拒绝 |
| api-core | 三 URL |
| `loginConfig.spec.ts` | 新字段缺省不外跳 |

命令见 `verify.md`。
