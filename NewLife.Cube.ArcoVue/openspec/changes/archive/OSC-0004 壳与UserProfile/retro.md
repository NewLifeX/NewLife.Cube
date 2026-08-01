# OSC-0004 Retro

> 复盘时间：2026-08-01  
> 触发：验收并复盘 OSC-0004。

## 做得好的

- 契约隔离清晰：壳读 UserProfile，CRUD 不碰 store；延续 OSC-0003 微内核边界。
- 线缆模型与逻辑 DTO 分层干净：`api-core` 只认 Json 字符串列，FE `mergeProfile` / `prefsToWire` 可单测。
- TagsView 与 keep-alive 用「路由具名包装」解决 DynamicPage 同名缓存裁剪。
- 验收期补上 401 清 localStorage，避免多用户同浏览器串偏好。

## 问题与根因

1. **api-core 走 dist**：改 `src` 后未 rebuild 则 `vue-tsc` 看不到 `UserProfileModel` / `profile`——workspace 包以 `dist` 为 types 入口。  
2. **暗色半成品遗留**：原 `appStore.darkMode` 只改 DOM 不落库；本号已替换为 theme.appearance 持久化。  
3. **ConfigProvider 偏薄**：设计写「ConfigProvider + CSS vars」，实现以 CSS/`arco-theme` 为主，主色未必刷满全部 Arco 控件。  
4. **浏览器冒烟**：验收环境未起后端，UI 联调记代码路径 + 残留。

## 行动项 / lessons

- 已写入 `openspec/harness/lessons.md`（见 OSC-0004 条目）。
- 后续：OSC-0005+ 消费 EntityViewProfile；OSC-0010 补壳偏好端到端冒烟；可选加深 Arco 主题 token。

## 与迁移方案偏差

- 逻辑 DTO 密度/圆角/字号采用 OSC-0004 design（`default`/`number`），并对迁移方案 `comfortable`/`sm|md|lg` 等别名做 merge 兼容。
- `workspace.*` 本号只存不驱动视图（按设计，留给 VTable OSC）。
