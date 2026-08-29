# 2026-08-27：邮箱/手机验证（账号激活）前端实现

## 目的

为三代 API 版实现 GitHub issues [#112 邮箱验证](https://github.com/NewLifeX/NewLife.Cube/issues/112) / [#113 手机验证](https://github.com/NewLifeX/NewLife.Cube/issues/113) 的 Vue 前端：注册表单（用户名/邮箱/手机三方式）、注册后待激活提示、激活页、安全中心、登录页重发激活。

后端端点契约见仓库根 `Doc/AUTH-邮箱手机验证.md`（其它皮肤实现依据）。

## 变更

| 文件 | 变更 |
|------|------|
| `core/pages/LoginPage.vue` | 新增登录/注册 Tab、注册待激活提示卡、登录失败「未激活」检测 + 重发激活入口 |
| `core/pages/RegisterForm.vue`（新） | 注册表单展示组件：用户名/邮箱/手机三方式，按 `register.requireMailVerify/requireMobileVerify` 强制联系方式，发码走 `sendCode(action=register)` |
| `core/pages/ActivatePage.vue`（新） | `/activate` 激活页：邮箱链接直达（`GET /Auth/Activate`）+ 邮箱/手机验证码激活（`POST /Auth/Activate`） |
| `core/pages/ProfileSecurity.vue`（新） | `/profile/security` 安全中心：展示邮箱/手机验证状态，验证/更换走 `verifyContact`（发码 `action=bind`） |
| `core/routes/index.ts` | 新增 `/activate`（免登录无布局）、`/profile/security`（需登录） |
| `core/components/UserMenu.vue` | 用户菜单新增「安全中心」入口 |
| `core/utils/loginApi.ts` | 新增 `registerByForm/sendCode/fetchCaptcha/activateByLink/activateByCode/sendActivateCode/verifyContact`；`normalizeLoginResult` 透传 pendingActivation 字段 |
| `core/stores/user.ts` | `UserInfo` 新增 `mailVerified/mobileVerified` |
| `core/__tests__/RegisterForm.spec.ts`（新） | 9 个用例：三方式注册、必填校验、发码 |

## 依赖变更

`packages/api-core`（类型 `RegisterAbility.requireMailVerify/requireMobileVerify`、`UserInfo.mailVerified/mobileVerified`、`LoginResult.pendingActivation/channels/targets`，新增 `activateByLink/activateByCode/sendActivateCode/verifyContact`）、`packages/auth-logic`（`AuthLogic` 新增 `register/activateByLink/activateByCode/sendActivateCode/verifyContact`）。

## 验证

- `pnpm type-check`（vue-tsc）通过
- `pnpm test:unit`：165 通过（含新增 9 个 RegisterForm 用例）；2 个套件因 `sass-embedded` 依赖问题失败（预存环境问题，与本次无关）
- `pnpm build` 被 `sass-embedded` 缺 `@bufbuild/protobuf/codegenv2` 阻塞（预存环境问题）

## 说明

- 注册表单暂未接入图片验证码 UI（现有登录页亦未接入，属既有缺口；`CaptchaScene` 默认关闭不受影响），后续可在 `RegisterForm` 增补
- 后端构建被用户 S3 存储 WIP（`S3ObjectStorage` 等未提交文件）阻塞，后端测试待 WIP 解决后运行
