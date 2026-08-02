# OSC-0007 Retro

> 复盘时间：2026-08-02  
> 触发：对 OSC-0007 进行验收和复盘。

## 结果摘要

- 状态：**Done**
- 实际完成范围：图表入口暂撤（实现保留）、高级菜单（导入/导出/批删）、表格选择与批删门禁、工具栏精简、卡片三布局 + 正文列数/排版、语义字体 Token、相关 Vitest/文档。
- 与 proposal/design 的偏差：
  1. CardMapping 增补 `bodyColumns` / `fieldOrientation`（实施期产品反馈，已回写 design/对接指南）。
  2. `fullRow` 额外覆盖备注/说明/评论等语义字段名。
  3. 导/导入权限对菜单位做兼容回退（`VIEW`/`ADD`），否则「高级」在常见账号下几乎不可见。
  4. 曾试做列表/树拖拽排序与改父，**验收前按用户要求整段撤销**，不进入交付。

## 做得好的

- 批删门禁抽到 `resolveBatchDeleteState`，条件矩阵可单测，模板与 handler 双重防御。
- 图表能力「藏入口不删实现」，为后续独立 OSC 留接线（`void openChart`）。
- 卡片布局用 CSS 变量 + remount key，避免 scoped/异步组件导致配置「看似保存、视觉无感」。
- VTable 勾选/排序交互坑（`cellType`、勿对 selectedKeys 全量 refresh、`sort_click` 勿 `return false` 跳过状态）有可复用教训。

## 问题与根因

1. **权限枚举漂移**：前端 `Auth.EXPORT/IMPORT` 与后端菜单位不一致 → 高级菜单项大量隐藏；用 VIEW/ADD 兼容回退修复。
2. **配置控件「死」感**：Arco radio 在部分嵌套下体感差；列数/排版改用 seg-group 按钮，并以 mapping 为真源。
3. **VTable 状态被冲掉**：`watch(selectedKeys)` 触发 `updateOption` 导致勾选丢失；改为仅清空时轻量同步。
4. **范围诱惑**：拖拽排序/改父超出 proposal「不做什么」，试做后撤销——验收需严守冻结范围。

## 验证证据

| 项 | 实际结果 | 证据/日期 |
|---|---|---|
| 新增 Vitest | viewMapping / cardHelpers / tokens 相关断言绿 | 2026-08-02 |
| `pnpm test` | 18 files / 113 passed | 2026-08-02 |
| `pnpm build` | vue-tsc + vite ok | 2026-08-02 |
| 手工权限/布局冒烟 | 代码路径验收；浏览器端到端待本地补点 | 残留 |
| 旧 ViewsJson 兼容 | normalize 回落 + chrome 旧键保留（单测/类型） | 2026-08-02 |

## 行动项 / lessons

- 已写入 `openspec/harness/lessons.md`（OSC-0007 条目）。
- 后续候选：图表区独立 OSC；分组/排序真实能力；列表/树拖拽排序另立变更（若产品仍需要）。

## 度量（可选）

| 项 | 值 |
|----|-----|
| 单测套件 | 113 |
| 构建 | pass |
| 后端变更 | 无 |
