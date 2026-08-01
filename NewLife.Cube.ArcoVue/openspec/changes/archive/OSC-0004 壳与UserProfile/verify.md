# OSC-0004 Verify

> 验收时间：2026-08-01T09:08:10+08:00  
> 编排：implementation-audit → code-review → doc-sync  
> 触发：验收并复盘 OSC-0004。

## 硬门禁

- [x] 本 OSC **新增** Vitest 全过（`userProfile.spec.ts` 7 + `tokens.spec.ts` 3，套件合计 32）
- [x] `pnpm build`（ArcoVue web）无错误（含先重建 `packages/api-core` dist）

### 测试记录

```text
> pnpm test  (@ NewLife.Cube.ArcoVue/web)
 Test Files  10 passed (10)
      Tests  32 passed (32)

> pnpm build (@ packages/api-core) → ok
> pnpm build (@ NewLife.Cube.ArcoVue/web) → vue-tsc + vite build ok
```

## 功能验收

| 项 | 结果 | 说明 |
|----|------|------|
| side/top/mix 切换；刷新保持 mode | ✅ 代码路径 | RootLayout + merge/persist；浏览器联调冒烟本环境未跑 |
| light/dark/system | ✅ 代码路径 | applyTheme + media 监听；OS 实时切换依赖浏览器 |
| density default/compact | ✅ 代码路径 | density.css + class 注入 |
| TagsView + 关签 prune | ✅ 代码路径 | tagsView + 路由具名包装 + keep-alive include |
| 外观设置页 / 恢复默认 | ✅ 代码路径 | `/settings/appearance`；workspace 仅存（设计约定） |
| GET 覆盖 local / PUT 重登 | ✅ 代码路径 | store 语义完备；需后端联调确认端到端 |
| PUT 失败 toast + dirty | ✅ | Message.error；dirty 保留 |
| CRUD + 右侧抽屉 | ✅ | crud/core 无 userProfileStore；`placement="right"` |
| 壳/CRUD 隔离 | ✅ | grep 无违规 import |

## 三步摘要

### 1. implementation-audit

AC 与 design 对齐：api-core `profile`、userProfile/tagsView、RootLayout 三布局、主题/TagsView/设置页、登录与路由加载、文档回写均到位。  
未做项：无（相对范围）；手工浏览器冒烟本会话未联后端，记为代码路径验收 + 残留联调项。

### 2. code-review

- **已修（验收期）：** `onUnauthorized` 清 `cube.arco.userProfile` localStorage，避免 401 串用户壳偏好。
- **可接受后续：** ConfigProvider 仅包壳，主色/圆角主要靠 CSS 变量（非全量 Arco token）；`showTabs=false` 仍累计 cache。
- **无契约级缺陷。**

### 3. doc-sync

- [x] 迁移方案 M2 出口说明
- [x] `前端对接指南` UserProfile 消费约定
- [x] ArcoVue `web/README.md` 壳说明

## 风险 / 残留

- 三布局/主题/重登一致性建议在本地起后端后点一次冒烟（可并 OSC-0010）。
- 主色对全部 Arco 组件覆盖可能不全，后续可加深 ConfigProvider theme。

## 验收结论

**通过**（checklist: passed）。硬门禁与契约 AC 满足；联调 UI 冒烟记环境受限/代码路径已验。可进入复盘。
