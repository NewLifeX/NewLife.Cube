# OSC-2608178bdb Retro

> 复盘在验收通过后由 openspec-retro 填写；归档后状态置 `Done`。

## 结果摘要

| 维度 | 结果 |
|------|------|
| AC 通过率 | AC-01～AC-16 通过；浏览器完整点验为 🟢 残余 |
| 三步编排 | 实现审计 ✅ → 代码审查（0🔴）→ 文档同步 ✅ |
| 自动化门禁 | arco-vue Vitest **472** · vue-tsc + vite build ✅ |
| 工期 | T1–T10 主实现 + 冒烟三项并入 T4/T7 |
| 手工冒烟 | 代码路径具备；User/CronJob 完整浏览器点验记残余 |

## 实际完成范围

- 方案 E：`hasTypeName` / `dataAction` 归一；`classifyListLink` / `partitionListFields`。
- 合成 Url / `dataAction` → `__ops`（直出≤2 +「更多」）；实体字段 Url → 单元格链接。
- table/tree + card/kanban 同源；calendar/gantt → RecordDrawer 标题区链接。
- 冒烟：`lookupRowField`（`{ID}`↔`id`）；卡片 ops 防竖排与按宽自适应直出。

## 做得好的

1. **分流真值表先锁纯函数**：单测钉死后再接线 UI，回归成本低。
2. **与自动化配额分离**：自定义直出 2、自动化直出 3，互不挤占语义。
3. **冒烟问题即时补进 T4/T7**：占位符大小写与卡片折行未另开 OSC，范围可控。
4. **后端零改**：只消费 GetPage 既有契约，迁移矩阵可标 Done。

## 待改进

1. **行数据键与 Url 模板大小写**：camelCase JSON vs `{ID}` 模板应在 page-utils 层默认容错（本号才补 `lookupRowField`）。
2. **卡片直出配额与常量**：VTable 固定 `OPS_LINK_INLINE_MAX`，卡片按宽动态——验收文档应写明双策略，避免误读为偏离 design。
3. **完整浏览器冒烟**：依赖真实菜单数据；验收期以单测+代码路径为主时须在 verify 标明残余。

## 偏差

- 卡片自定义链接直出数可小于 2（宽度不足）——产品增强，不违反「最多 2」上限。
- 同会话文档标题 / 卡片悬停阴影未纳入本号归档范围。

## 遗留与后续

- MapProvider 空 Url 前端补链（若需要）另立 OSC
- `{page:}` 占位增强可并入 page-utils 另号
- 页级批量启用/禁用工具条仍属迁移矩阵 P2
- User/CronJob 完整浏览器点验（🟢）
