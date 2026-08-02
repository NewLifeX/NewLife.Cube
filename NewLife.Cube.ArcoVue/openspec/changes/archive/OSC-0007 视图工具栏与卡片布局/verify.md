# OSC-0007 Verify

> 验收时间：2026-08-02T16:30:00+08:00  
> 编排：implementation-audit → code-review → doc-sync  
> 触发：对 OSC-0007 进行验收和复盘。

## 硬门禁

- [x] 本 OSC **新增** Vitest 全过（`viewMapping` 批删/布局/列数/排版；`cardHelpers` fullRow；`tokens` 语义字体）
- [x] `pnpm test` 全绿：**18 files / 113 tests**
- [x] `pnpm build`（ArcoVue web）vue-tsc + vite **无错误**
- [x] XUnit：N/A（proposal 声明无后端契约变更）

### 测试记录

```text
> pnpm test  (@ NewLife.Cube.ArcoVue/web)
 Test Files  18 passed (18)
      Tests  113 passed (113)

> pnpm build (@ NewLife.Cube.ArcoVue/web)
  vue-tsc -b && vite build → ok
```

## 验收标准（AC）

### 图表范围冻结

- [x] AC-01：搜索面板和列表顶栏均不再渲染“图表”按钮（模板无入口；`void openChart` 保留接线）。
- [x] AC-02：`ListChartModal`、`chartVisible`、`chartList`、`openChart()`、`getChartData` 均保留。

### 高级菜单与权限

- [x] AC-03：「高级」仅在导入/导出/批量删除至少一项可见时出现；无独立 `.page-tools`。
- [x] AC-04：导入门禁 `flags.canImport`（`IMPORT || ADD` 且非只读）。
- [x] AC-05：导出门禁 `flags.canExport`（`EXPORT || VIEW`）；格式走 `EXPORT_FORMATS`。
- [x] AC-06：`resolveBatchDeleteState` 矩阵单测覆盖；模板 `v-if`/`disabled` + `confirmBatchDelete`/`handleBatchDelete` 双重门禁；成功清空选择并 reload。

### 表格选择

- [x] AC-07：仅 `activeViewKind === 'table'` 传 `show-checkbox`。
- [x] AC-08：代码路径沿用 VTable checkbox/表头全选 → `selectionChange`（浏览器冒烟见残留）。
- [x] AC-09：`loadData`/切视图/创建删除重置/路由切换/`handleBatchDelete` 清空 `selectedKeys`。

### 工具栏配置

- [x] AC-10：配置抽屉折叠标题为「工具栏」。
- [x] AC-11：无“允许添加记录/按钮文字/自定义按钮”表单与运行入口；添加记录仅 `flags.canAdd`。
- [x] AC-12：分组/排序仅 `isTableLikeViewKind`（table/tree）。
- [x] AC-13：`ViewChrome` 仍保留旧键 normalize；不主动清洗 `ViewsJson`。

### 卡片布局与兼容

- [x] AC-14～17：三布局 + RecordCard 标题→图片→字段；large 图高 180；row ≥640 侧栏/窄屏回退；无图不留空区（视觉冒烟见残留）。
- [x] AC-18：正文最多 8 字段；排除 title/image/看板 group。
- [x] AC-19：`fullRow`：多行控件 / 长度≥33 码位 / 备注类语义字段；恰 32 码位非整行（单测）。
- [x] **接受增强**：`bodyColumns`（1/2/3）与 `fieldOrientation`（横/竖）写入 CardMapping，CSS 变量驱动列数；标准/偏大时 3→2。

### 字体与质量

- [x] AC-20：tokens 五语义字体变量 + fontScale 等比（`tokens.spec`）。
- [x] AC-21：DefaultList / ViewConfigDrawer / RecordCard 触及处使用 `--cube-font-*`。
- [x] AC-22：`pnpm test` + `pnpm build` 通过。

## 三步摘要

### 1. implementation-audit

| 范围 | 结论 |
|---|---|
| T1 图表入口 / 高级菜单 | ✅ 对齐 design 冻结区与菜单矩阵 |
| T2 表格选择 / 批删门禁 | ✅ 纯函数 + 双重防御 + 选择生命周期 |
| T3 工具栏精简 | ✅ 文案/控件/运行时入口已移除；旧 chrome 键保留 |
| T4 卡片布局 + 字体 | ✅ layout 三态；增强列数/排版；fullRow；Token |
| 范围外尝试后撤销 | 列表/树行拖拽排序曾试做后**已整段撤销**，不纳入本号交付 |

### 2. code-review

- **已修（实施期）：** Auth.EXPORT/IMPORT 与菜单位不一致 → `canExport`/`canImport` 兼容回退，避免「高级」菜单空壳；VTable checkbox 用 `cellType`；勾选不触发全表 `updateOption`；表头排序自管升降循环；卡片列数用 CSS 变量 + remount key。
- **无必须阻塞项。**
- **建议残留：** 分组/排序顶栏仍为 `Message.info` 占位（迁移方案差距表 #7）；浏览器手工冒烟未在本会话完整点验。

### 3. doc-sync

- [x] 迁移方案 §13 OSC-0007 → ✅；M4a 与 OSC-0007 解绑说明保留
- [x] `前端对接指南` CardMapping 补 `bodyColumns` / `fieldOrientation`
- [x] ArcoVue `web/README.md` 高级菜单 + 卡片排版
- [x] 本变更 `design.md` CardMapping schema 回写增强字段

## 风险 / 残留

1. **浏览器手工冒烟**（高级菜单交互、表头全选、三布局视觉、旧 ViewsJson 刷新持久化）建议本地起后端再点验一遍；本验收以代码路径 + 单测/构建为主。
2. table/tree「分组」「排序」工具入口仍为占位提示，非本号硬 AC。
3. 列表/树拖拽排序与改父级**明确不在本号范围**（用户验收前已要求撤销试做）。

## 验收结论

**通过**（checklist: passed）。硬门禁满足；AC-01～22 代码与单测可判定项通过；视觉/浏览器类 AC 记残留。可进入复盘。
