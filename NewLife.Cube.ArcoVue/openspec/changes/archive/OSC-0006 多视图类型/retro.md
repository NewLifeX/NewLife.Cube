# OSC-0006 Retro

## 做得好

- mapping / 门禁 / pageSize 抽到 `viewMapping.ts`，单测覆盖清晰
- 视图舞台 async + `vtable-gantt` 独立 chunk，首屏不被甘特拖垮
- 澄清结论（看板只读、大 pageSize、树门禁、卡片操作）一次锁死，实现少返工

## 可改进

- DefaultList 仍同步引入 ViewConfigDrawer，chunk 偏大，可再拆
- Gantt 与 VisActor 类型不完全对齐，暂用 `as never`；后续可跟官方类型收紧
- NamedViewsToolbar 旧文件可删除或标 deprecated

## 残留 / 下一号

- 筛选·分组持久化进 NamedView（飞书对齐后续）
- 看板/甘特/日历拖拽写回
- pageSize>500 或全库流式加载
- 保护视图 / 分享链接
- 手工冒烟真实实体（verify 可选项）

## 度量（可选）

| 项 | 值 |
|----|-----|
| 单测增量 | +8（viewMapping）→ 总计 61 |
| 构建 | pass；vtable-gantt ~229KB gzip 50KB |
