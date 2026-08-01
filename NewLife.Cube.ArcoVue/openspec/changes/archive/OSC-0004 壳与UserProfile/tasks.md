# OSC-0004 Tasks

## P0 — Store + API 接线

- [x] `stores/userProfile.ts`：系统默认、merge、localStorage、脏标记
- [x] `GET/PUT /Cube/UserProfile` 客户端（`api-core` `createProfileApi` → `cubeApi.profile`）
- [x] 登录后加载；变更防抖 PUT（400ms）；失败 toast
- [x] 「恢复默认」写入系统默认并 PUT

## P1 — RootLayout + 三布局

- [x] `RootLayout.vue` 按 `layout.mode` 动态挂载
- [x] 从 `default.vue` 拆分 `side.vue` / `top.vue` / `mix.vue`（菜单复用现有节点组件）
- [x] Mix 最小可用：顶一级 + 侧二级
- [x] 路由父级改为 RootLayout；`siderCollapsed` / `siderWidth` 生效

## P2 — 主题与密度

- [x] `theme/`：appearance（含 system + media 监听）、primaryColor、radius、fontScale、density class
- [x] Arco ConfigProvider / `arco-theme` 注入
- [x] 顶栏快捷：主题、密度、进设置；去掉仅本地 darkMode 半成品

## P3 — TagsView

- [x] `TagsView.vue`：开签/关签/切换；受 `showTabs` 控制
- [x] 与 keep-alive 协同：关签 prune cache（路由组件具名包装）

## P4 — 外观设置页

- [x] `views/settings/appearance.vue`：布局 / 主题 / 密度 / 页签 / 恢复默认
- [x] 静态路由 `/settings/appearance`（无需菜单权限）
- [x] 顶栏入口可达

## P5 — 契约、测试、文档

- [x] CRUD 路径无 `userProfileStore` import；抽屉仍右侧
- [x] Vitest：merge / payload / themeTokens / layoutMode
- [x] `pnpm build` 无错误
- [x] 手工冒烟（见 verify.md）— 代码路径验收；浏览器联调记残留
- [x] 回写迁移方案 M2、对接指南/README

## 测试记录（执行期）

- 命令：`pnpm test`（`NewLife.Cube.ArcoVue/web`）→ **32 passed**
- 命令：`pnpm build`（api-core 后 + ArcoVue web）→ **成功**
- 新增测试：`src/core/utils/userProfile.spec.ts`、`src/theme/tokens.spec.ts`

## 建议顺序

P0 → P1 → P2 → P3 → P4 → P5（P2/P3 可部分并行；测与文档收尾）。
