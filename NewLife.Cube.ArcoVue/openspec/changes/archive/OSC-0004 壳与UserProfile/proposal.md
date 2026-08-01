# OSC-0004 — 壳引擎 + 消费 UserProfile

## 1. 为何做

将 ArcoVue 从「写死侧栏 + 本地暗色开关」升级为 **UserProfile 驱动的可配置壳**：布局 / 主题 / 密度 / 多页签按用户持久化，满足迁移方案个性化工作台（P0），且不破坏 OSC-0003 CRUD 微内核契约隔离。

## 2. 范围（做）

- **三布局**：`side` / `top` / `mix`，由 `userProfile.layout.mode` 动态切换（RootLayout）。
- **主题引擎**：`appearance` = light | dark | system；`primaryColor` / `radius` / `density` / `fontScale`；经 CSS 变量 + Arco ConfigProvider 注入。
- **TagsView** 多页签（`layout.showTabs`）；与 keep-alive 协同、关签 prune。
- **外观设置页** + 顶栏快捷入口；`GET/PUT /Cube/UserProfile`；localStorage 先行、服务端权威覆盖；「恢复默认」。
- Vitest 关键路径 + `pnpm build` 硬门禁。
- **代码范围**：优先仅 `NewLife.Cube.ArcoVue/**`；必要时 `packages/api-core` 增加薄 UserProfile 客户端封装；**不改**后端实体与其它皮肤。

## 3. 不做什么

- EntityViewProfile / VTable / 列表多视图（→ OSC-0005+）。
- 完整 i18n（本号仅中文；结构可扩展）。
- 记录抽屉增强 / 评论 Tab（→ OSC-0007 / 0008）。
- MFA UI；改 Cube.Vue / NaiveUI。

## 4. 依赖

| 依赖 | 关系 |
|------|------|
| **OSC-0002** | Done：`GET/PUT /Cube/UserProfile` |
| **OSC-0003** | Done（软依赖）：壳变化不得破坏 DynamicPage / 右侧抽屉契约 |

## 5. 验收 / 测试范围

| 类型 | 是否做 | 说明 |
|------|--------|------|
| **Vitest（新增）** | **是** | profile merge/默认回落、保存 payload 形状、layout mode 解析、theme token 纯函数 |
| **构建** | **是** | `pnpm build`（ArcoVue web）无错误 |
| 手工冒烟 | 是 | 三布局 / 三外观 / 两密度；刷新与重登仍生效；CRUD 页仍可用 |
| XUnit | 否 | 无后端代码（默认） |
| E2E | 否 | |

硬门禁：执行期跑新增单测；验收期**本 OSC 新增单测全过** + **构建无错误**。

## 6. 成功标准

- [ ] RootLayout 按 `layout.mode` 切换 side/top/mix（mix 至少最小可用）
- [ ] 主题 appearance（含 system）与密度立即生效并写入 UserProfile
- [ ] TagsView 可开关；关签与 keep-alive 一致
- [ ] 外观设置页 + 顶栏快捷；恢复默认可用
- [ ] GET 成功覆盖 localStorage；PUT 防抖；失败有提示
- [ ] CRUD 微内核不读 userProfileStore；抽屉仍右侧弹出
- [ ] 本 OSC 新增 Vitest 全过；`pnpm build` 无错误
- [ ] 迁移方案 / 对接文档 / README 已回写
