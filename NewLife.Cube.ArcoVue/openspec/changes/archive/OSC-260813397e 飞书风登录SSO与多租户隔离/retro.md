# OSC-260813397e Retro

> 复盘在验收通过后由 openspec-retro 填写；归档后状态置 `Done`。

## 结果摘要

| 维度 | 结果 |
|------|------|
| AC 通过率 | AC-01～AC-23 通过；真实 OAuth/MFA/双租户浏览器点验为 🟢 残余 |
| 三步编排 | 实现审计 ✅ → 代码审查（0🔴）→ 文档同步 ✅ |
| 自动化门禁 | arco-vue Vitest **493** · vue-tsc ✅ · Cube build 0 error · Osc260813397e **2** pass |
| 工期 | T1–T7 主实现 + 终验 T8 会话打磨 |
| 手工冒烟 | 代码路径具备；真人 IdP/MFA 记残余 |

## 实际完成范围

- 飞书风登录/注册/忘记密码：嵌套 `LoginConfig`、验证码、Challenge、MFA 二步、OAuth `source=front-end` + `#token=`。
- 账号安全：TOTP MFA + `/Auth/Binds` 第三方绑定/解绑。
- 多租户：`/Auth/Tenants`/`SwitchTenant`、`X-Tenant`、`TenantManagerHelper`、`UserTenantSearch`、表单藏 `TenantId`、新实体接入文档。
- T8：登录后用户菜单分组切租户；去掉登录页租户 Code；修 GetTenantId Cookie NRE；DrawingCaptcha data URI 归一。

## 做得好的

1. **契约先于页面**：loginConfig / tenantHeader / mfaQr 纯函数单测钉死后接线，回归便宜。
2. **租户隔离后端齐平**：TenantUser + UserTenantSearch 用 XUnit 锁死，不依赖 NuGet JOIN 修复。
3. **401 清租户会话**：`clearTenantSession` 避免下一用户沿用旧 `X-Tenant`。
4. **验收缺口 T7 一次补齐**：P0～P2 未拖到下号。

## 待改进

1. **Cookie API 空值**：`Cookies[key]` 可为 null，禁止直接 `.ToString()`——应在引入 EnableTenant 中间件路径的首日加匿名 LoginConfig 冒烟。
2. **验证码载荷形态**：DrawingCaptcha 回 `data:image` 而 UI 按 SVG `v-html`，跨实现须归一层（`normalizeCaptchaImageHtml`）。
3. **租户入口位置**：初版顶栏下拉 + 登录 Code 与飞书「登录后选组织」不一致，终验才迁用户菜单——宜在 design 锁定交互稿时一次对齐。
4. **完整浏览器冒烟**：仍依赖真实 IdP / MFA 账号；验收应以自动化等价 + 明确残余清单为准。

## 偏差

- 登录页「租户 Code」、顶栏租户下拉 → 用户菜单 `a-dgroup`（T8，接受）。
- 同会话非本号：导航 Logo/favicon、系统信息列宽、混合布局菜单 `..`、CubeSetting 多项接线——**不进本号归档提交**。

## 遗留与后续

- 真实 OAuth 回跳 / EnableMfa 二步 / 双租户 UI 互不可见：发版前手工抽测
- NuGet `User.SearchWithTenant` JOIN 歧义：Cube 已绕行 `UserTenantSearch`；上游修复可另跟
- OAuthPending / ForceBind 首次 SSO 强制注册：明确不做，另案
