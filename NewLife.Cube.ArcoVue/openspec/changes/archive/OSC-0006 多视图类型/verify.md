# OSC-0006 Verify

> 状态进入 Validating 后勾选；全部通过再 Done。

## 验收标准（AC）

### Tab 与配置

- [x] AC-01：列表页展示视图 Tab + `···` + `+`；切换视图刷新舞台且持久化 `activeViewId`
- [x] AC-02：`+` 菜单按门禁禁用不可建类型；树无 Parent/children 元数据时不可创建
- [x] AC-03：ViewConfigDrawer「列表区」随活跃 `view` 切换为对应字段映射表单项
- [x] AC-04：日历配置：开始必填、结束可选、标题、颜色；保存进 `NamedView.mapping`

### 渲染与操作

- [x] AC-05：卡片/看板卡片左下：有权则 **详情 + 编辑 + 删除**（与表格操作列一致）
- [x] AC-06：看板按分组字段分列；**不可**拖拽改分组写回
- [x] AC-07：日历月视图按 start/(end) 落点；点击打开详情抽屉
- [x] AC-08：甘特绑定起止/标题/颜色；点击详情；独立 chunk 不拖垮首屏
- [x] AC-09：kanban/calendar/gantt 请求 GetList 使用放大 pageSize（约 200–500），非仅当前小页

### 契约与质量

- [x] AC-10：映射只写 `viewsJson`（NamedView.mapping）；前端不读写 `ganttJson`/`cardJson`
- [x] AC-11：`pnpm test` 与 `pnpm build` 通过；新增映射/门禁单测全绿（61 tests）
- [x] AC-12：核心文档（迁移方案 M3b、对接指南、web README）已同步 ViewKind

## 手工冒烟（可选）

- [ ] 真实实体：建卡片/看板/日历/甘特各一，刷新后 Tab 与配置仍在
- [ ] 无图字段时卡片仍可渲染；无色字段时用主题色

## 门禁命令

```bash
cd NewLife.Cube.ArcoVue/web && pnpm test && pnpm build
```

## 执行记录

| 项 | 结果 |
|----|------|
| `pnpm test` | 15 files / 61 tests passed（含 viewMapping 8） |
| `pnpm build` | vue-tsc + vite ok；`vtable-gantt` 独立 chunk ~229KB |
| 日期 | 2026-08-01 |
