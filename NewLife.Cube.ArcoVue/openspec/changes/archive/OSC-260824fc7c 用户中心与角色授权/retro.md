# OSC-260824fc7c Retro

> 复盘 2026-08-26 | 状态 Done
> 验收决策：无 P0/P1 实现缺口；P2（浏览器冒烟、CubeNC 双栈、列表壳与 03c0 混文件）仅记录不补齐

## 结果摘要

| 维度 | 结果 |
|------|------|
| AC 通过率 | 21/21 代码路径通过（AC-01～AC-21；浏览器端到端见残余） |
| 三步编排 | 实现审计 ✅ → 代码审查（🔴 无）→ 文档同步 ✅ |
| 自动化门禁 | api-core 37+51；arco-vue 670；vue-tsc/vite 0 error；dotnet Cube 0 error |
| 缺口 | 无 P0/P1；P2 残余已记 verify |

## 范围回顾

| 维度 | 计划 | 实际 |
|------|------|------|
| 权限编解码 | 纯函数 + spec | `rolePermission.ts` 18 测；叶子改为 `perms` 横排 |
| 角色树 UI | L2 FieldInput + 抽屉树 | 编辑/添加/详情只读树；节点全选 checkbox |
| 账号中心 | `/account` 四 Tab | 资料/密码/安全/绑定；旧 security 重定向 |
| SSO 外跳 | LoginConfig 两字段 | Setting 双栈 + Auth/Cube LoginConfig；仅资料/改密 |
| 系统角色 / 菜单目录 | 禁删锁名；目录只读 | WebAPI 已落地；CubeNC Menu/Role 列表未同步 |

## 实际完成范围

- 角色 `Permission`：`RolePermTree` 替代 textarea；保存仍 `menuId#flags`；`RoleController.Valid` 未改。
- 菜单源 `GET /Admin/Menu` 分页；401/403 告警不清空原串。
- `/account`：Info / ChangePassword / MFA / OAuth 绑定；payload 禁止登录名。
- 顶栏「个人信息」；SSO http(s) 外跳用户中心。
- 系统角色删除/批量删除/名称/`isSystem` 锁定；用户列表「仅自己」告警。
- 文档：功能清单 PERM-1/2、SPA-7；SSO 子应用文档；web README。

## 做得好的

1. **契约没被冲开**：方案一守住实体路由，没有 `/iam` 工作台，也没有把 MenuTree 当授权目录。
2. **编解码先于 UI**：纯函数 + spec 锁默认四叶、自定义 flag、非法段、升序序列化，树 UI 后来改自定义 checkbox 也不用改存储。
3. **账号中心复用 397e**：安全/绑定拆面板，没有第二套 MFA API。
4. **详情树补齐 AC-19**：执行期详情仍走 `formatDetail` 文本；验收前会话补成只读树，与编辑抽屉一致。
5. **SFC 薄壳守住**：`AccountCenter` / `RolePermTree` 无 `watch`/`cubeApi`；`sfcThin` 全量扫描通过。

## 待改进

1. **双栈非 Link 文件要写进 tasks**：B.8 只写 `MenuController.cs`，WebAPI 改了、CubeNC 独立副本未改。教训：Cube / CubeNC 分文件时 tasks 必须点名两处（沿用 e483）。
2. **详情布局与表单分叉**：`RecordDrawer` 详情不用 `FormContent`，design 写「readonly 时树 disabled」不够——必须写清详情槽位也挂同一组件。
3. **浏览器冒烟整包留到验收**：AC-01～18 只能代码路径放行（e483 lessons 已有，本号再犯）。
4. **工作区并行 OSC 未提交**：03c0 源码仍 `??`，列表壳与本号 B.4/B.5/D.1 缠在一起，复盘无法干净拆提交。

## 关键决策记录

| 决策 | 理由 |
|------|------|
| 方案一而非 /iam 工作台 | 遵守实体路由与 GetPage；授权树是 20% 覆写 |
| 菜单源用 /Admin/Menu 不用 MenuTree | MenuTree 已按当前用户裁剪，不能作为授权目录 |
| SSO 只外跳资料/改密 | MFA/绑定是本应用 Auth API |
| 权限叶改为节点 `perms` 而非 tree children | 勾选与菜单名同一行，避免 Arco Tree 把权限位当成子节点缩进 |

## 偏差

- Tree 交互：design `checkable` + `checked-strategy=child` → 自定义 checkbox + `perms`；存储契约未变，已补录 tasks B.1。
- 详情只读树：计划在 FormContent readonly，实际详情自定义布局，会话补 `RecordDrawer`。

## 遗留与后续

- CubeNC Menu `Permission` ReadOnly、Role 列表去 Permission 列：另号或随 CubeNC 同步。
- 浏览器实机：角色勾选保存往返、SSO 外跳、系统角色批量删除。
- 03c0 源码与列表壳应单独提交，勿与本号混打。
- 工作区并行：OSC-0018、地区级联、甘特、wwwroot 全量构建均排除。

## 过程备注

- 用户确认：方案一；创建 ID `OSC-260824fc7c`。
- 批准并执行 2026-08-24；验收并复盘 2026-08-26。
- 依赖 OSC-0003 / OSC-260813397e 已 Done；不落地 97c1 人员控件。
