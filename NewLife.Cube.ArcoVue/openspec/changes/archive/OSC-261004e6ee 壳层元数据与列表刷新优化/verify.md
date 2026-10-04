# OSC-261004e6ee Verify — 壳层元数据与列表刷新优化

## AC-1 壳层稳定元数据

- [x] AC1.1 并发去重 ✅
- [x] AC1.2 成功缓存；`force=true` 作废本资源 in-flight 后重取 ✅（T4-1）
- [x] AC1.3 失败不锁死 ✅
- [x] AC1.4 登出/`clear`/`resetSession`/注销均清会话元数据 ✅（T4-2）

## AC-2 动态角标

- [x] AC2.1～AC2.3 ✅ in-flight 去重、完成后可再刷、失败可重试

## AC-3 实体列表刷新

- [x] AC3.1～AC3.3 ✅；`listRefreshGate` 锁定入口（T2-5）
- [x] AC3.4 浏览器 Fetch/XHR 冒烟 ✅ **仅记录放行**（T3-3：本机无 Vite/API 登录环境；用户 2026-10-04 决策「验收并继续复盘归档」）

## AC-4 多维视图独立性与数据正确性

- [x] AC4.1～AC4.5 ✅ 视图级 lastQuery/activeQuery + 签名门控；legacy 清除同步删旧键

## AC-5 回归与文档

- [x] AC5.1 ✅
- [x] AC5.2 ✅ §2.3 性能口径、§7.1 签名复用、SPA-17a

## 固定编排摘要（2026-10-04 复验）

| 步 | 结论 |
| --- | --- |
| 实现审计 | T1–T4 与 design 对齐：app/tenant/userProfile 会话缓存 + per-resource `_xxxGen`；角标仅 in-flight；`listRefreshGate` / `listRequestSignature`；viewProfile 视图级查询；注销 `clearSessionMetadata`。执行期对照 CubeSetting 将 `GetMapConfig` 纳入同一缓存模型（见 T4）。 |
| 代码审查 | force 迟到写回双守卫；列表复用门槛含 tenant/viewFilter；无 P0。徽标就地刷新（`listRowPatch`）与地图散点视图 UI（d7f4）属并行 WIP，不纳入本号 AC。 |
| 文档同步 | 迁移方案 §2.3/§7.1、功能清单 SPA-17a / SPA-17·18 口径已对齐。 |

## 命令

```powershell
cd NewLife.Cube.ArcoVue/web
pnpm exec vitest run src/stores/app.spec.ts src/stores/tenant.spec.ts src/stores/userProfile.spec.ts src/core/utils/listRefreshGate.spec.ts src/core/utils/listRequestSignature.spec.ts
pnpm exec vue-tsc -b
```

## 验收缺口补齐记录（2026-10-04）

### 已修
| 项 | 处理 |
| --- | --- |
| P0 force/in-flight | 每资源 `_xxxGen`；force 递增并弃用旧 Promise；迟到响应双守卫（session+resource） |
| 注销清 app 元数据 | `useCloseAccount` → `appStore.clearSessionMetadata()` |
| legacy lastQuery | `_clearLastQuery` 同步删实体级旧键 |
| 文档 | 迁移方案 §2.3/§7.1；功能清单 SPA-17a |
| 测试 | force 竞态（app/tenant）；`listRefreshGate`；签名复用门控 |

### 测试与构建
- Vitest：相关 5 文件 / 11 例全过（全量相关此前 108 仍绿）。
- `vue-tsc -b`：0 错误。

### CubeSetting 对照结论

| 魔方设置 | 与本号关系 | 结论 |
| --- | --- | --- |
| `EnableTenant` | LoginConfig / Tenants 会话缓存 | 同会话复用正确；管理端改设置后需登出或 force 才刷新——符合「会话缓存」决策 |
| `AISwitch` / AI 配色 | `GetAiConfig` 会话缓存 | 同上；force/登出失效 OK |
| `MapProvider`/`MapKey`/`MapScriptUrl` | `GetMapConfig` 会话缓存 | 已纳入同一 force/失效模型 |
| `FilterWindowDays` | 服务端 GetList 收窄 | 不经 Pinia 缓存；签名含 viewFilter，复用门槛正确 |
| `SessionTimeout` / `TokenExpire` / `TokenRefreshThreshold` | 令牌/Cookie | 认证层处理，与元数据会话缓存无冲突 |
| `RefreshUserPeriod` | SSO 拉取用户周期（服务端） | 前端未误用该秒数做 LoginConfig TTL；无需改 |
| `SsoUserCenter` / `RedirectUserToSso` | LoginConfig 字段 | 会话缓存；改配置后需重新拉 LoginConfig |
| `ActivateUrl` / 验证码/MFA 等 | 激活与安全页 | 不走本号稳定缓存路径；无问题 |
| `EnableMultiDeviceLogin` / `LogoutAll` | 登出/注销 | 注销已对齐清 app/tenant/profile，避免复用前用户元数据 |

**未发现**与 CubeSetting 语义冲突的实现错误；会话缓存故意不实时跟随后台改配置，属本号锁定范围。

### 残留（仅记录）

| 缺口 | 级别 | 处置 |
| --- | --- | --- |
| T3-3 / AC3.4 浏览器 Fetch/XHR 冒烟 | P1 | **仅记录**：本机无登录联调环境；有环境后按 verify 命令段手工补做（实体刷新不重打 GetPage/ViewProfile；双视图签名门控；租户切换与角标刷新） |

**验收结论**：checklist **passed**（T3-3 用户决策仅记录放行）。状态 Validating → 可复盘归档。
