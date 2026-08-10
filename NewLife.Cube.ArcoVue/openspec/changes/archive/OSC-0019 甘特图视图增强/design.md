# OSC-0019 Design — 甘特图视图增强

## 0. 适用框架与官方资料

| 场景 | 框架 | 官方资料 |
| --- | --- | --- |
| 甘特图（基线任务条/定位/拖拽宽度） | VisActor VTable Gantt `@visactor/vtable-gantt` | [Gantt option](https://visactor.io/vtable/option/Gantt)（taskBar.baselineStartDateField / baselineEndDateField / baselinePosition / locateIcon / frame.verticalSplitLineMoveable）· [任务条定位示例](https://visactor.io/vtable/demo/gantt/gantt-locate-taskbar) · [拖拽表格宽度示例](https://visactor.io/vtable/demo/gantt/gantt-interaction-drag-table-width) · [Gantt 教程](https://visactor.io/vtable/guide/gantt/introduction) |
| 配置抽屉/颜色选择（a-color-picker） | Arco Design Vue | https://arco.design/vue/docs/start（`ColorPicker`/`Select`/`Drawer` 组件 API，实现前必须查阅） |

**已核实的关键事实**（VTable 源码 `packages/vtable-gantt/`）：
- `taskBar.locateIcon`：任务条横向超出可视区时在左右边缘显示定位图标，hover 高亮、点击滚动到可视区（官方示例 gantt-locate-taskbar）。
- `frame.verticalSplitLineMoveable: true`：左侧任务表格与右侧时间轴的分割线可拖拽调整表格宽度；`verticalSplitLineHighlight` 为拖拽高亮线样式；宽度范围由 `taskListTable.minTableWidth/maxTableWidth` 约束（官方示例 gantt-interaction-drag-table-width；源码 `state-manager.ts` 有 `startResizeTableWidth/dealResizeTableWidth/endResizeTableWidth`）。
- `taskBar.baselineStartDateField/baselineEndDateField/baselineStyle/baselinePosition('top'|'bottom'|'overlap')`：基线任务条相对主任务条垂直位置，默认 bottom。
- `taskBar.barStyle.barColor`：任务条颜色（可函数按任务返回）。
- `taskListTable` 为完整 ListTable 实例配置（columns/tableWidth/minTableWidth/maxTableWidth 等）。

## 1. 目标与契约边界

在**保持甘特图只读**的前提下，用 VTable 原生能力补齐：计划/实际双条对比（实际主条 + 计划基线 overlap）、任务条定位、拖拽宽度持久化、固定任务条颜色。

**契约边界**：
- mapping 仍经 `patchActiveMapping` → ViewsJson 持久化（OSC-0012/0015 既有链路），`tableWidth` 随 mapping 一并保存。
- 甘特图保持只读：`moveable/resizable/scheduleCreatable/progressAdjustable` 均 false；拖拽仅限「左侧表格宽度」（视图级交互，非数据编辑）。
- 旧数据兼容：已保存 gantt mapping 的 `startField/endField` 迁移为 `plannedStartField/plannedEndField`；`colorField` 忽略（行为变更：按字段着色 → 固定色）。
- 后端零改动。

**与既有机制的职责分离**：
| 机制 | 归属 | 本号关系 |
| --- | --- | --- |
| 命名视图/mapping 持久化 | `viewProfile.ts` patchActiveMapping → ViewsJson | tableWidth 随 mapping 保存，复用链路 |
| 视图配置抽屉 | `ViewConfigDrawer.vue` 甘特区 | 4 项扩为 6 项 |
| 甘特图渲染 | `GanttView.vue` | 双条/定位/拖拽/固定色 |
| 只读交互 | 现状 moveable/resizable=false | 保持，新增分割线拖拽（视图级） |

## 2. 文件级改动地图

### 2.1 类型与归一化

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `web/src/core/utils/viewMapping.ts` | ①`GanttMapping` 扩展：`plannedStartField/plannedEndField`（必填）、`actualStartField?/actualEndField?`、`barColor?`、`tableWidth?`，移除 `startField/endField/colorField`；②`seedMapping` gantt 分支：计划=前两个日期字段（`dates[0]/dates[1]`）、实际/barColor/tableWidth 缺省；③`normalizeMapping` gantt 分支：读旧 `startField/endField` → 迁移为 planned，`colorField` 忽略，新字段按候选校验 | calendar 等其它视图分支、`VIEW_KIND_LABEL`、`canCreateViewKind`（甘特仍要求 ≥2 日期字段） |
| `web/src/core/utils/viewMapping.spec.ts` | 更新甘特用例：seedMapping 新结构；normalizeMapping 旧 `startField/endField/colorField` 迁移与忽略；非法字段回退 seed | 其余视图用例 |

### 2.2 甘特图渲染

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `web/src/features/views/GanttView.vue` | ①`buildRecords`：行级预处理——实际有值（`actualStart/actualEnd` 均非空）→ 主条 `__actualStart/__actualEnd` = 实际值，否则回退计划值；基线 `__plannedStart/__plannedEnd` = 计划值；计划起止均为空的行过滤；②`taskBar`：`startDateField/endDateField = __actual*`、`baselineStartDateField/baselineEndDateField = __planned*`、`baselinePosition:'overlap'`、`baselineStyle.barColor`（中性浅色，如 `rgba(134,144,156,0.55)`）、`barStyle.barColor = mapping.barColor ?? themeColor('--primary-6', ...)`、`locateIcon:true`、`moveable/resizable/scheduleCreatable/progressAdjustable:false`；③`frame.verticalSplitLineMoveable:true` + `verticalSplitLineHighlight`（主题主色）；④`taskListTable`：`columns` = 标题（displayName）+ 计划开始 + 计划结束（列 title 用字段 displayName）、`tableWidth = mapping.tableWidth ?? 380`、`minTableWidth:280`、`maxTableWidth:640`；⑤新增 `emit('mapping-change', mapping)` 与宽度持久化监听（见 §4） | 点击事件（click_cell/click_task_bar → detail）、`themeColor`/`resolveCellBadge` 引用、宿主高度、防抖重建 |
| `web/src/views/crud/DefaultList.vue` | `GanttView` 新增 `@mapping-change="onGanttMappingChange"` → `patchActiveMapping(typePath, mapping)` | 其余视图接线 |

### 2.3 配置界面

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `web/src/views/crud/ViewConfigDrawer.vue` | 甘特区（`viewKind === 'gantt'`）由 4 项扩为 6 项：标题（titleCandidates 下拉）、计划开始*（dateCandidates 下拉）、计划结束*（dateCandidates 下拉）、实际开始（dateCandidates 下拉 allow-clear）、实际结束（dateCandidates 下拉 allow-clear）、任务条颜色（`a-color-picker` hide-trigger + hex 文本，缺省主题色）；`localMapping` 增 planned/actual/barColor/tableWidth 字段；`emitMapping` 输出新结构 | 其它视图分支、`resetMapping`/`patchMapping` 逻辑（同步新字段） |

## 3. GanttMapping schema 与兼容

```ts
export type GanttMapping = {
  kind: 'gantt';
  titleField: string;
  plannedStartField: string;
  plannedEndField: string;
  actualStartField?: string;
  actualEndField?: string;
  barColor?: string;    // hex；缺省主题主色
  tableWidth?: number;  // 缺省 380；拖拽后持久化
};
```

**归一化（normalizeMapping gantt 分支）**：
1. `plannedStartField = pickFirst([o.plannedStartField, o.startField], names) || dates[0]?.name`（旧 `startField` 迁移）。
2. `plannedEndField = pickFirst([o.plannedEndField, o.endField], names) || dates.find(f => f.name !== plannedStart)?.name`（旧 `endField` 迁移）。
3. `actualStartField/actualEndField = pickFirst([o.actualStartField], names)` 且必须成对（仅一个配置则忽略，视为未配置实际）。
4. `barColor`：合法 hex（`/^#[0-9a-fA-F]{6}$/`）才保留，否则 `undefined`；**`colorField` 直接忽略**。
5. `tableWidth`：合法正整数（280~640 夹取）保留，否则 `undefined`。
6. 计划字段缺失 → `seedMapping('gantt', fields)` 回退。

**旧数据兼容声明**：已保存 ganttJson 的 `startField/endField` 自动迁移为计划字段；`colorField`（按字段着色）不再生效，改由固定色（缺省主题色）——行为变更登记到 `web/README.md`/ChangeLog。

## 4. 渲染与持久化细节

### 4.1 buildRecords 行级预处理（GanttView.vue）

```
对每行：
  plannedStart = toDateStr(row[plannedStartField])
  plannedEnd   = toDateStr(row[plannedEndField])
  实际有值判定：actualStartField && actualEndField 均配置且行值非空
  __actualStart = 实际有值 ? toDateStr(row[actualStartField]) : plannedStart   // 主条回退计划
  __actualEnd   = 实际有值 ? toDateStr(row[actualEndField])   : plannedEnd
  __plannedStart = plannedStart   // 基线（计划）
  __plannedEnd   = plannedEnd
  barColor 固定 = mapping.barColor ?? 主题主色（barStyle 全局，无需行级）
过滤：__plannedStart 或 __plannedEnd 为空的行不渲染（现状逻辑保留）
```

> 无实际的行：主条=计划、基线=计划（overlap 完全重合 → 视觉单条），满足「无实际只显计划」。

### 4.2 VTable option 关键配置

```ts
taskBar: {
  startDateField: '__actualStart',
  endDateField: '__actualEnd',
  baselineStartDateField: '__plannedStart',
  baselineEndDateField: '__plannedEnd',
  baselinePosition: 'overlap',
  baselineStyle: { barColor: 'rgba(134,144,156,0.55)', width: 18, borderLineWidth: 0 },
  barStyle: { width: 18, barColor: mapping.barColor ?? themeColor('--primary-6', '22, 93, 255'), borderLineWidth: 0 },
  moveable: false, resizable: false, scheduleCreatable: false, progressAdjustable: false,
  locateIcon: true,
},
frame: {
  verticalSplitLineMoveable: true,
  verticalSplitLineHighlight: { lineColor: themeColor('--primary-6', '22, 93, 255'), lineWidth: 2 },
  outerFrameStyle: { ...现状 },
},
taskListTable: {
  tableWidth: mapping.tableWidth ?? 380,
  minTableWidth: 280,
  maxTableWidth: 640,
  columns: [标题列, { 计划开始, title=plannedStartField displayName }, { 计划结束, title=plannedEndField displayName }],
},
```

### 4.3 宽度持久化

- **监听**：甘特图实例上「表格宽度拖拽结束」事件。官方事件名以执行时查 VTable Gantt API/源码为准（源码 `state-manager.ts` 有 `endResizeTableWidth`，可能暴露为 `resize_table_width` 事件）；若官方无该事件，**兜底**：轮询 `gantt.taskTableWidth`（拖拽结束 300ms 后与 `mapping.tableWidth` 比较，变化则上报）。
- **上报**：`emit('mapping-change', { ...mapping, tableWidth: 新宽度 })` → `DefaultList.onGanttMappingChange` → `patchActiveMapping` → ViewsJson 持久化（既有 debounce/保存链路）。
- 边界：宽度在 `minTableWidth(280)~maxTableWidth(640)` 内由 VTable 原生夹取；只上报有效整数。

## 5. 配置 UI 规格（ViewConfigDrawer 甘特区）

```
┌─ 甘特图区 ───────────────────────────┐
│ 标题        [ 字段下拉 ]             │
│ 计划开始 *  [ 日期字段下拉 ]          │
│ 计划结束 *  [ 日期字段下拉 ]          │
│ 实际开始    [ 日期字段下拉 ▾可清空 ]  │
│ 实际结束    [ 日期字段下拉 ▾可清空 ]  │
│ 任务条颜色  [ a-color-picker ] #xxxxxx│  ← 缺省主题主色；勾选「无」回缺省
└──────────────────────────────────────┘
```

- 计划开始/结束必填（缺任一不渲染，面板顶部提示「请在自定义配置中设置计划开始/结束日期字段」——复用现状 alert 文案改造）。
- 实际开始/结束**成对可选**：仅配一个视为未配置（归一化忽略）。
- 任务条颜色：`a-color-picker`（hide-trigger、format hex、disabledAlpha）+ 当前 hex 文本；清空/恢复默认 → `barColor = undefined`（主题主色）。

## 6. 状态与唯一来源

| 状态 | 唯一来源 | 说明 |
| --- | --- | --- |
| 甘特 mapping（标题/计划/实际/颜色/宽度） | `activeMapping`（viewProfile store）→ ViewsJson | patchActiveMapping 持久化 |
| 表格宽度（拖拽后） | mapping.tableWidth | 会话内由 GanttView 上报，随保存链路落 ViewsJson |
| 任务条颜色 | mapping.barColor ?? 主题主色 | 固定色，无行级差异 |
| 主题色 | `themeColor('--primary-6', ...)` | canvas 渲染需解析 CSS 变量（VTable 不支持 CSS 变量） |

## 7. 测试设计

### 7.1 Vitest（viewMapping.spec）
- `seedMapping('gantt', fields)`：返回 `{ kind:'gantt', plannedStartField: dates[0], plannedEndField: dates[1], titleField, actualStartField/actualEndField/barColor/tableWidth 缺省 }`。
- `normalizeMapping('gantt', ...)`：
  - 旧结构 `{ startField, endField, colorField }` → `plannedStartField/plannedEndField` 迁移、`colorField` 忽略。
  - 新结构 round-trip（planned/actual/barColor/tableWidth）。
  - 实际仅配一个 → 忽略（视为未配置）。
  - barColor 非法 hex → undefined；tableWidth 越界/非数 → undefined。
  - 计划字段非法 → 回落 seedMapping。
- `canCreateViewKind('gantt')` 保持 ≥2 日期字段门禁。

### 7.2 构建与冒烟
- `npm.cmd --prefix NewLife.Cube.ArcoVue/web run test|build` 全绿；`wwwroot` 重新生成。
- 手工冒烟（verify.md 清单）：双条 overlap、无实际只显计划、定位图标、拖拽宽度持久化、明暗主题颜色、旧视图配置迁移。

## 8. 核心文档影响

| 文档 | 影响 |
| --- | --- |
| `NewLife.Cube.ArcoVue/web/README.md` | 登记 OSC-0019 能力（甘特图计划/实际双条/定位/拖拽宽度）+ `colorField → 固定色` 行为变更 |
| `Doc/功能清单.md` | 甘特图相关条目增补 OSC-0019 状态 |

## 9. 风险

| 风险 | 缓解 |
| --- | --- |
| baseline 无行级开关，无实际行基线=主条重叠 | 视觉单条可接受；主条回退计划保证始终有渲染 |
| 宽度持久化事件名不确定 | 执行时查官方 API；无事件则轮询 `taskTableWidth` 兜底（300ms 比较） |
| `colorField` 行为变更（按字段着色→固定色） | proposal/design 显式登记；normalizeMapping 忽略旧字段，不报错 |
| overlap 双条视觉拥挤 | baselineStyle 用中性浅色 + 主条主题色，区分明显；冒烟验证可读性 |
| VTable option 类型与运行时不完全对齐 | 沿用现状 `Record<string, unknown>` 宽松传入 + `as never`；冒烟逐项验证 |
