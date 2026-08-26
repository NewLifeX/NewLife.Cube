# SSO 子应用管理页面跳转用户中心

> ArcoVue 已实现配置与外跳（OSC-260824fc7c）。其它皮肤仍可按同一 LoginConfig 字段适配。

---

## 需求背景

在典型的魔方 SSO 部署中，一般使用一个纯净的魔方（CubeSSO）作为用户中心，管理所有用户的准入以及角色控制。其它多个子应用引用 NewLife.Cube，关闭密码登录并指向用户中心。

用户在各子系统通过 SSO 登录后，个人信息及角色部门会同步到子应用。因此在子应用里修改个人信息、角色、部门实际没有意义——数据源头在用户中心。

### 问题描述

当前子应用的用户菜单（如"个人信息"链接）仍指向子应用本地页面，用户在子应用中看到的个人信息、角色等管理入口实际无法产生有效修改。用户期望这些链接能够直接跳转到 SSO 用户中心对应的管理页面。

### 期望效果

- 子应用的"个人信息"等用户管理链接，可配置为跳转到 SSO 用户中心对应页面
- 提供开关配置，允许管理员选择是否启用此跳转行为（非所有场景都需要）
- **不**要求子应用前端直连用户中心 API 改用户（资料/改密走整页外跳）

---

## 功能设计

### 配置项

`CubeSetting`（Category=`用户登录`，紧接 `LogoutAll`）：

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `SsoUserCenter` | String | 空 | SSO 用户中心根 URL，无尾斜杠亦可 |
| `RedirectUserToSso` | Boolean | false | 是否将资料、修改密码跳转到 SSO 用户中心 |

`GET /Auth/LoginConfig` 附加 camelCase 字段：`ssoUserCenter`、`redirectUserToSso`。缺字段视为关 / 空。

### 跳转路径

仅当 `redirectUserToSso === true` 且 `ssoUserCenter` 为 `http://` 或 `https://` 前缀时：

- 资料：`{SsoUserCenter}/Admin/User/Info`
- 改密：`{SsoUserCenter}/Admin/User/ChangePassword`

非法协议（如 `javascript:`）视为不外跳，走本地 `/account`。

### 影响范围

1. **用户菜单**：ArcoVue 顶栏「个人信息」
2. **账号中心**：`/account` 资料 Tab、密码 Tab（安全/绑定仍本页）
3. MFA / 第三方绑定忽略本开关

### 实现思路

1. 读取 `SsoUserCenter` 与 `RedirectUserToSso`，经 LoginConfig 下发 SPA
2. 前端 `resolveSsoAccountUrl` 拼接路径；非空则 `window.location.assign`
3. 其它皮肤（ACE、Tabler、LayuiAdmin 等）可按同一配置适配

---

## 参考信息

- 相关 Issue：[sso求助](https://github.com/NewLifeX/NewLife.Cube/issues/128)
- 相关部署模式：CubeSSO 用户中心 + 多子应用 SSO 登录
- 关联项目：CubeSSO 示例项目
- OpenSpec：`OSC-260824fc7c`

---

## 状态

- [x] 配置项设计与实现（`CubeSetting.SsoUserCenter` / `RedirectUserToSso`）
- [x] 后端 API 支持（`/Auth/LoginConfig` 返回用户中心地址与开关）
- [x] 前端皮肤适配（ArcoVue 用户菜单与 `/account` 资料/改密）
- [x] 文档更新
