# OSC-0005 Retro

> 复盘时间：2026-08-01  
> 触发：对 OSC-0005 进行验收和复盘。

## 做得好的

- 线缆扩展干净：`ViewsJson`/`ActiveViewId` + FE `namedViews` 权威，与 OSC-0002 单行 Profile 模型一致。
- VTable 收敛在 `features/vtable/ListTable.vue`，DefaultList 换装未破坏搜索/抽屉/权限。
- 列 merge / 排序 payload / 命名视图生命周期有 Vitest；ViewsJson upsert 有 XUnit。
- 验收期抓住 `DataField.Nullable` 遮蔽 `System.Nullable`，避免 SPA 数据源物化拖垮 net10 构建。

## 问题与根因

1. **devProxy 只代理 Admin/Cube**：业务 Area 回 `index.html` → GetPage 全空；已修通配 + lessons。  
2. **左冻结 AC 与产品冲突**：实现中途按 UX 禁用入口，模型仍在 → 验收记残留而非回退。  
3. **「行点」→「双击」**：防误触改手势后文档一度滞后，验收 doc-sync 已对齐。  
4. **种子名「列表」→「默认列表」**：产品文案演进，文档/OpenSpec 初稿未同步。  
5. **范围蠕变**：视图 chrome / 徽章 / 表单分组等同会话抛光叠在 0005 上，验收需与 M3a 核心 AC 分列。

## 行动项 / lessons

- 已写入 `openspec/harness/lessons.md`（OSC-0005 正式条目 + 既有联调/字体待办）。
- 后续：OSC-0006 多视图类型；冻结入口重开；VTable 动态 import；操作列动作分发。

## 与迁移方案偏差

- 默认视图显示名：方案初稿「列表」→ 实现「默认列表」（读时迁移）。
- 打开详情：方案「行点」→ 实现「双击 + 操作列」。
- 左冻结：方案「可改」→ UI 暂不可用（偏好字段保留）。
