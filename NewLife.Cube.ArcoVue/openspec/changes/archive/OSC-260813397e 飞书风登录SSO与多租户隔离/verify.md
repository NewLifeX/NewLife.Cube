# OSC-260813397e Verify

> 状态：**通过**（openspec-verify 终验 2026-08-17）  
> 编排：implementation-audit → code-review → doc-sync  
> 触发：验收和复盘 397e。

## 执行阶段记录（openspec-apply）

- 2026-08-13：首轮 Implementing / 单测构建。
- 2026-08-16：验收缺口 T7 全部补齐（P0～P2 + T6.3 自动化证据）。

## 验收阶段记录（openspec-verify）

### 会话小任务补录（2026-08-17）

- T8：登录后租户入口改用户菜单分组；去掉登录页「租户 Code」；`GetTenantId` Cookie 空引用；DrawingCaptcha `data:` 归一；用户菜单图标。
- status note：会话小任务已补录（终验）。

### implementation-audit

- 愿景成功标准代码侧对齐（登录契约 / MFA / 绑定 / Tenants+X-Tenant / TenantUser / UserTenantSearch / 藏 TenantId）。
- 相对初版 design：登录页不再展示租户 Code；租户切换由顶栏下拉改为用户菜单 `a-dgroup`（更贴近「登录后选组织」飞书路径）。已写入 T8 + IA/design 勘误。
- 无新增 P0/P1 实现缺口。

### code-review

- 阻断项：无。
- 残余：真实 IdP OAuth 回跳、EnableMfa 真人二步、两租户 UI 互不可见——依赖自动化等价覆盖，发版前建议手工抽测（同 08-16）。

### doc-sync

- IA / design §登录·壳租户入口已与实现一致（登录无 Code、用户菜单分组切换）。

### 愿景对照与缺口决策

| 级 | 缺口 | 决策 |
|----|------|------|
| — | 无 P0/P1 代码缺口 | — |
| P2 | 真实 OAuth / MFA / 双租户浏览器冒烟未在本环境点完 | **仅记录不补齐**（用户「验收和复盘」一并关闭） |
| 偏差 | 登录租户 Code / 顶栏下拉 → 用户菜单分组 | **接受为产品增强**（T8） |

## 验收标准

### 登录

- [x] AC-01～AC-08（代码 + loginConfig / captchaImage 单测）
- [x] AC-01b 登录页无「租户 Code」；品牌仍走全局 LoginConfig（T8）

### 账号安全

- [x] AC-09 MFA Setup 含二维码图 + 备用码
- [x] AC-10 / AC-11 绑定与 `/Auth/Binds`

### 多租户

- [x] AC-12 切换 + 401 清租户（用户菜单分组选项）
- [x] AC-12b EnableTenant 匿名 `LoginConfig` 不因缺 Cookie NRE（GetTenantId）
- [x] AC-13 EnsureManagerTenantUser 单测
- [x] AC-14 / AC-15 UserTenantSearch 单测
- [x] AC-16 / AC-17

### 边界

- [x] AC-18 无 OAuthPending SPA
- [x] AC-19 / AC-20

### 门禁

- [x] AC-21～AC-23：见下

## 自动化门禁（终验实跑 2026-08-17）

```text
pnpm exec vitest run（arco-vue/web）     → 54 files / 493 tests pass
pnpm exec vue-tsc --noEmit               → OK
dotnet build NewLife.Cube                → 0 error
dotnet test --filter Osc260813397e       → 2 pass
```

## 风险（仅记录）

- 真实 IdP OAuth 回跳、EnableMfa 真人二步、两租户 UI 互不可见：建议发版前手工抽测一次。
