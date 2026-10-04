# OSC-261004e6ee Retro — 壳层元数据与列表刷新优化

## 摘要

| 项 | 结论 |
| --- | --- |
| 目标愿景 | 达成：壳层稳定元数据会话复用与并发去重；角标仅 in-flight；列表普通刷新只 `loadData`；视图级查询隔离 + 请求签名门控 |
| 缺口处置 | T4 代码/文档缺口已补齐；T3-3 浏览器冒烟 **仅记录** |
| 归档 | 已归档（本次提交） |

## 实际完成范围

- T1 壳层：`app`/`tenant`/`userProfile` 成功缓存、Promise 去重、失败可重试；动态 Inbox/Workflow 仅 in-flight；`force` 用 per-resource `_xxxGen` 作废本资源 in-flight（不误伤角标）。
- T2 列表：`listRefreshGate` 锁定 bootstrap vs loadData；`listRequestSignature` + `tableDataRequestSignature`；`viewProfile` 按 `typePath+viewId` 隔离 lastQuery/activeQuery，legacy 清除同步删旧键。
- T3 文档：迁移方案 §2.3/§7.1、功能清单 SPA-17a；Vitest/`vue-tsc` 门禁。
- T4 验收补齐：注销对齐 `clearSessionMetadata`；CubeSetting 对照；执行期将 `GetMapConfig` 纳入与 Login/AI 同一会话缓存模型（含后端 `GetMapConfig` 与 `CubeSetting` 地图三项），超出草案「不改后端」字面、但与对照结论一致。
- **未纳入本号提交**：地图散点视图 UI（OSC-261004d7f4 WIP）、列表徽标就地刷新（`listRowPatch`/`useListCrud`）等并行改动。

## 测试与构建

- Vitest：`app`/`tenant`/`userProfile`/`listRefreshGate`/`listRequestSignature`（及 mapConfig 解析相关）通过；`vue-tsc -b` 0 错误。
- 浏览器 Fetch/XHR 冒烟：环境未起，verify 仅记录。

## 做得好

1. force 与会话失效分层：`_sessionGeneration` 挡登出迟到写回，`_xxxGen` 只作废本资源，避免「刷新 LoginConfig 误杀角标请求」。
2. 列表复用门槛写成纯函数 + 单测，签名含租户与 `viewFilter`，避免「看起来同页实则条件不同」的脏复用。
3. 验收对照 CubeSetting，主动发现 Map 配置应进同一缓存模型，而不是事后再开洞。

## 偏离与教训

1. **force 初版误伤**：曾用抬升会话世代作废 in-flight，会连坐其它资源；改为 per-resource gen。
2. **注销漏清 app**：登出路径清了，注销 initially 未调 `clearSessionMetadata`；壳层失效矩阵要覆盖「登出 + 注销」。
3. **草案 vs 对照**：proposal 写「不修改后端」，CubeSetting 对照后为 MapConfig 补了 `GetMapConfig`——执行期扩范围须即时改三件套并在 retro 标明。
4. **工作区混 WIP**：同目录并行 d7f4/徽标修复；归档提交须按文件白名单，忌 `git add .`。

## 风险与后续

- T3-3 有登录环境后补 Fetch/XHR 冒烟（实体刷新、双视图签名、租户/角标）。
- `GetMapConfig` 会话缓存与即将落地的地图散点视图（d7f4）衔接：视图 UI 另号验收，勿把散点渲染误记入本号。
