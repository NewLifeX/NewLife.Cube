# OSC-0019 — 甘特图视图增强

## 1. 为何做

ArcoVue 多维视图的甘特图（`GanttView.vue`，基于 `@visactor/vtable-gantt`）为**单任务条只读**实现，存在 4 处缺口：

1. **缺计划/实际双任务条对比**：只能配置单组开始/结束字段，无法同时展示「计划 vs 实际」；项目排期场景需要对比计划排期与实际执行，判断延期/提前。
2. **缺任务条定位**：时间轴较长时任务条可能超出可视区，用户无法感知到「该行任务条在可视区外」，也没有一键定位入口。
3. **缺表格宽度拖拽**：左侧任务列表宽度固定（380px），用户无法按需调整信息区/时间区占比。
4. **颜色配置单一且偏「字段映射」**：现有 `colorField` 按字段值着色，用户需求为**固定颜色**（所有任务条统一色），且无颜色选择控件。

本号利用 VTable 甘特图原生能力补齐上述缺口：基线任务条（`baseline*`）承载计划 vs 实际、`locateIcon` 定位、`frame.verticalSplitLineMoveable` 拖拽宽度，并将宽度持久化到视图配置。

## 2. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | **主任务条 = 实际**（`actualStartField/actualEndField`），**基线任务条 = 计划**（`plannedStartField/plannedEndField`），`baselinePosition: 'overlap'`（重叠居中，直接对比差异）。 |
| 2 | 实际字段**可选**：未配置或行数据无实际值时，主任务条**回退用计划字段渲染**（buildRecords 行级预处理，不配置基线）→「无实际只显计划」。 |
| 3 | 任务条颜色 = **固定颜色**（`barColor: hex`），缺省主题主色；旧 `colorField` 在 `normalizeMapping` 中忽略（行为变更，登记文档）。 |
| 4 | 拖拽表格宽度**持久化**：`tableWidth` 随 mapping 存 ViewsJson（`patchActiveMapping` 既有链路），刷新恢复。 |
| 5 | 左侧任务列表列 = **标题 + 计划开始 + 计划结束**（不显示实际列）。 |
| 6 | 甘特图保持**只读**：`taskBar.moveable/resizable/scheduleCreatable/progressAdjustable` 均 false（不引入数据编辑）。 |
| 7 | 任务条定位：`taskBar.locateIcon: true`（超出可视区左右边缘定位图标，hover 高亮、点击滚动）。 |
| 8 | `GanttMapping` 字段命名：`titleField/plannedStartField/plannedEndField/actualStartField?/actualEndField?/barColor?/tableWidth?`；旧数据 `startField/endField` → 迁移为 planned，`colorField` 忽略。 |
| 9 | 仅 ArcoVue 前端；不改后端；不改 Cube.Vue / NaiveUI 等其他皮肤。 |

## 3. 做什么

### `GanttMapping` 类型扩展（viewMapping.ts）
```ts
export type GanttMapping = {
  kind: 'gantt';
  titleField: string;            // 标题
  plannedStartField: string;     // 计划开始（基线，必填）
  plannedEndField: string;       // 计划结束（基线，必填）
  actualStartField?: string;     // 实际开始（主条，可选）
  actualEndField?: string;       // 实际结束（主条，可选）
  barColor?: string;             // 固定任务条颜色（hex），缺省主题主色
  tableWidth?: number;           // 左侧表格宽度（拖拽持久化），缺省 380
};
```

### 甘特图渲染（GanttView.vue）
- `buildRecords` 行级预处理：实际有值 → 主条用实际；无实际 → 主条回退计划；统一输出 `__actualStart/__actualEnd`（主条）+ `__plannedStart/__plannedEnd`（基线）；计划起止均为空的行过滤。
- VTable option：`taskBar` 配置 `startDateField/endDateField=__actual*`、`baselineStartDateField/baselineEndDateField=__planned*`、`baselinePosition:'overlap'`、`baselineStyle`（中性浅色）、`barStyle.barColor = mapping.barColor ?? 主题主色`、`locateIcon:true`、只读开关全 false；`frame.verticalSplitLineMoveable:true` + `verticalSplitLineHighlight`；`taskListTable` 列（标题+计划开始+计划结束）与 `tableWidth/minTableWidth(280)/maxTableWidth(640)`。
- 拖拽宽度持久化：监听甘特图宽度调整事件（`resize_table_width`，执行时以官方 API 为准；无事件则轮询 `taskTableWidth` 兜底）→ `emit('mapping-change', { ...mapping, tableWidth })`。

### 配置界面（ViewConfigDrawer.vue 甘特区）
- 6 项配置：标题（字段下拉）、计划开始*（字段下拉）、计划结束*（字段下拉）、实际开始（字段下拉可清空）、实际结束（字段下拉可清空）、任务条颜色（`a-color-picker` + hex 显示，缺省主题主色）。

### 持久化接线（DefaultList.vue）
- `GanttView` 新增 `@mapping-change` 事件 → `patchActiveMapping`（既有 ViewsJson 持久化链路）。

## 4. 不做什么

- 不启用任务条拖拽/调整（`moveable/resizable/scheduleCreatable/progressAdjustable` 保持 false）。
- 不做依赖线/里程碑/进度条/甘特图编辑数据（超本次需求范围，后续可另立 OSC）。
- 不改后端（无新接口）。
- 不改 Cube.Vue / NaiveUI 等其他前端。
- 不做 `colorField` 按字段着色的保留/双模式（用户确认固定颜色替代）。

## 5. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-0005/0006 | Done：6 视图 + 命名视图 Tab + 视图配置抽屉 |
| OSC-0012/0015 | Done：mapping 持久化（patchActiveMapping → ViewsJson）、ViewConfigDrawer 现有甘特区 |
| `@visactor/vtable-gantt` | 已依赖（^1.26.5）；baseline/locateIcon/verticalSplitLineMoveable 能力 |

## 6. 测试范围

| 类型 | 是否做 | 说明 |
| --- | --- | --- |
| Vitest | 是 | `viewMapping.spec`：seedMapping 甘特新结构（计划=前两日期字段、实际/barColor/tableWidth 空）；normalizeMapping 旧数据迁移（startField/endField→planned、colorField 忽略）、非法字段回退 |
| 构建 | 是 | `npm.cmd --prefix NewLife.Cube.ArcoVue/web run test|build`（vue-tsc + vite），`wwwroot` 重新生成 |
| 手工冒烟 | 是 | 双条 overlap 对比、无实际只显计划、定位图标、拖拽宽度持久化、明暗主题颜色 |

## 7. 成功标准

- [ ] 甘特图配置 6 项（标题/计划开始/计划结束/实际开始/实际结束/固定颜色）后正确显示：实际主条 + 计划基线 overlap 重叠对比。
- [ ] 未配置实际或无实际数据时，只显示计划条（主条回退计划），无空白/报错。
- [ ] 时间轴超出可视区时，甘特图左右边缘出现定位图标，hover 高亮、点击滚动到任务条。
- [ ] 左侧表格宽度可拖拽（分割线），拖拽后宽度持久化，刷新/切换视图恢复。
- [ ] 任务条颜色为固定色（缺省主题主色），明暗主题下正常（`themeColor` 兼容）。
- [ ] 旧 gantt 视图配置（startField/endField/colorField）无损迁移到新结构（startField/endField→planned，colorField 忽略）。
- [ ] 甘特图保持只读（无任务条拖拽/调整/新建）。
- [ ] 本 OSC 新增单测全部通过，相关构建无错误，`web/README.md` 等事实文档最小同步。
