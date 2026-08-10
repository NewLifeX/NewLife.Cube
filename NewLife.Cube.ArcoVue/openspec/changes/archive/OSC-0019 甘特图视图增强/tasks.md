# OSC-0019 Tasks — 甘特图视图增强

> 依赖顺序：T1 类型与归一化先行 → T2 渲染 + T3 配置可并行（均依赖 T1）→ T4 持久化接线 → T5 门禁 → T6 收尾。
> 每项含补测/跑测勾选；执行阶段必须跑单元测试（前端 Vitest + 构建）。

## 类型与归一化

- [x] **T1 GanttMapping 扩展 + seed/normalize 兼容**
  - `viewMapping.ts`：`GanttMapping` 改为 `titleField/plannedStartField/plannedEndField/actualStartField?/actualEndField?/barColor?/tableWidth?`；`seedMapping('gantt')` 计划=前两个日期字段、实际/颜色/宽度缺省；`normalizeMapping` gantt 分支支持旧 `startField/endField` 迁移为 planned、`colorField` 忽略、实际成对校验、barColor hex 校验、tableWidth 280~640 夹取、计划缺失回落 seed
  - 更新 `viewMapping.spec.ts`（seed 新结构 / 旧数据迁移 / 实际成对 / barColor/tableWidth 校验 / 回落）
  - [x] 测试通过 [x] 构建通过

## 渲染与配置

- [x] **T2 GanttView 双条渲染 + 定位 + 拖拽宽度**
  - `GanttView.vue`：`buildRecords` 行级预处理（实际有值→主条用实际，否则回退计划；`__actualStart/__actualEnd/__plannedStart/__plannedEnd`；计划空行过滤）
  - `taskBar`：`startDateField/endDateField=__actual*`、`baselineStartDateField/baselineEndDateField=__planned*`、`baselinePosition:'overlap'`、`baselineStyle`（中性浅色）、`barStyle.barColor = mapping.barColor ?? 主题主色`、`locateIcon:true`、只读开关全 false
  - `frame.verticalSplitLineMoveable:true` + `verticalSplitLineHighlight`（主题主色）
  - `taskListTable`：列=标题+计划开始+计划结束（displayName）、`tableWidth = mapping.tableWidth ?? 380`、min 280 / max 640
  - 宽度持久化：**确认 VTable Gantt 无 `resize_table_width` 事件（GANTT_EVENT_TYPE 无宽度事件）→ 轮询 `gantt.taskTableWidth` 防抖 300ms 兜底**；`emit('mapping-change', { ...mapping, tableWidth })`
  - 点击事件（click_cell/click_task_bar → detail）保留
  - [x] 测试通过 [x] 构建通过
- [x] **T3 ViewConfigDrawer 甘特区 6 项配置**
  - 甘特区：标题 / 计划开始* / 计划结束* / 实际开始（可清空）/ 实际结束（可清空）/ 任务条颜色（`a-color-picker` hide-trigger + hex 文本，清空回主题色）
  - `localMapping` 同步新字段；`emitMapping` 输出新结构；缺计划字段时 alert 文案改为「计划开始/计划结束」
  - `a-color-picker` 实现前查阅 Arco 官方组件 API（`emit('change', value)`）
  - [x] 测试通过 [x] 构建通过
- [x] **T4 持久化接线**
  - `DefaultList.vue`：`GanttView` 接 `@mapping-change="onGanttMappingChange"` → `patchActiveMapping(typePath, mapping)`（ViewsJson 持久化）
  - [x] 测试通过 [x] 构建通过

## 门禁与收尾

- [x] **T5 全量门禁**
  - `npm.cmd --prefix NewLife.Cube.ArcoVue/web run test` 全绿；`run build`（vue-tsc + vite）通过，`wwwroot` 重新生成
  - [x] 测试通过 [x] 构建通过
- [x] **T6 文档同步**
  - `web/README.md`：登记 OSC-0019 能力 + `colorField → 固定色` 行为变更
  - `Doc/功能清单.md`：甘特图条目增补 OSC-0019 状态
  - 手工冒烟（verify.md 清单）：门禁（User 拒绝/Tenant 允许）、视图创建、6 项配置 UI、甘特渲染、宽度持久化链路（手动触发验证）通过；真实鼠标拖拽（AC-05）与双条视觉对比（AC-01）因 playwright 合成事件无法驱动 VTable 状态机，留待验收阶段人工确认
  - [x] 文档完成 [x] 冒烟（部分）
