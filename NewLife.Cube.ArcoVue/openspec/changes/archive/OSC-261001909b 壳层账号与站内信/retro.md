# OSC-261001909b Retro — 壳层账号与站内信

## 摘要

| 项 | 结论 |
| --- | --- |
| 目标愿景 | 达成：公开激活页与注销确认可用；站内信带 `target` 可跳转并识别 `?id=`；租户列表有「成员」导航列；纯函数与后端投影有测试 |
| 归档 | 已归档（本次提交） |

## 实际完成范围

- T1 激活页；T2 站内信（含 T2-6 分桶双层时间轴，执行期补录）；T3 注销；T4 租户成员列与测试。
- 文档同步 4 处：design §1 文件地图、`ui/information-architecture.md`、proposal §4、`Doc/功能清单.md` SPA-7。
- 验收：AC1–AC4 全通过（AC2.2 由用户决定补齐，一次性账号端到端实测）。

## 测试与构建

- 前端：vitest 全量 121 文件 /1096 例中 1092 过（失败 4 例为存量 `sfcThin`，与 HEAD 基线一致）；本号涉及 inboxTarget 3、closeAccount 1、inboxBucket 7、iconRegistry 含分桶图标，全过；`vue-tsc -b` 0 错误。
- 后端：XUnit `Osc261001909b` 2/2；`dotnet build NewLife.Cube -f net10.0` 0 错误。
- 端到端（Playwright，dev 5184）：激活 mock 链路、注销弹窗三段 + 真实注销、站内信跳转/已读、分桶时间轴、TenantUser 通用列表。

## 坑（经验）

1. **VTable canvas 断言陷阱**：实体列表行/单元格由 VTable canvas 渲染，不在 DOM → 断言改用截图 + API 元数据；DOM 可断言的只有工具栏/分页等外壳文本。
2. **`.record-drawer` 类挂在 `.arco-drawer-container`（全屏包裹层）**：可见性用尺寸探测；`locator.isVisible({timeout})` 是即时探针**不等待**（曾用它误判"登录失败"，实际已成功）。
3. **演示环境无租户菜单**（多租户未启用，MenuTree 无 Tenant）→ `/Admin/Tenant` UI 不可达；成员列验收改为真实 `GetPage` 元数据 + 后端单测。
4. **后端登录风控对 localhost 同样生效**：`MaxLoginError=5`、`LoginForbiddenTime=600s`；验证码识别重试会累积用户名与 IP 双计数触发锁定（TTL 自"首次错误"起算，锁定期重试 +计数但不续期）。脚本策略：每次仅提交"图片刷新稳定约 800ms"后的识别结果，遇「错误过多」立即停止。
5. **Playwright 启动清空 `test-results/`**：跨脚本共享临时文件放 `playwright/` 下；`--no-deps` 跳过 auth.setup 以匿名登录。
6. **主题色取 `rgb(var(--primary-6))`**（body 上挂 RGB 三元组），分组图标颜色随之。
7. 双层时间轴：外层分组节点用 `#dot` 插槽自定义图标；条目内容不再画竖线（`.inbox-item` 无边框）。

## 风险与后续

- AC2.2 再登录拒绝实测文案为「账号…被禁用！」（与注册未激活路径的「账号未激活」不同名）；注销后 `Detail.online` 快照短暂为 true（在线记录按心跳超时释放，令牌已吊销，接口不可再用）。
- AC1.1 真实邮件链路未实测（演示无邮件服务）：接入邮件后建议补一次真实激活闭环。
- AC4.1 UI 级验收待在启用多租户的部署环境补验。
- 临时账号 `tmp909b*` 已在验收后清理。

