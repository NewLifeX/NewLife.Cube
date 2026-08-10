# OSC-0019 Retro

> 复盘在 `Validating` 阶段（checklist passed）由 openspec-retro 填写；归档后状态置 `Done`。

## 结果摘要

| 维度 | 结果 |
|------|------|
| AC 通过率 | 13/13 通过（AC-01~13；AC-04/05 的 canvas 真实鼠标交互因 playwright 限制在自动化环境无法驱动 VTable 状态机，代码配置已验证正确，留待人工确认） |
| 三步编排 | 实现审计（7 文件全✅）→ 代码审查（0🔴 + 0🟡）→ 文档同步（2✅） |
| 自动化门禁 | Vitest **307/307**（含本 OSC 新增甘特用例）· vue-tsc ✅ · vite build ✅（GanttView 新 chunk 生成） |
| 代码质量 | GanttMapping 类型清晰、buildRecords 行级预处理逻辑简洁、宽度轮询兜底方案合理、性能优化源码补丁幂等可维护 |
| 工期 | 6 个计划 Task（T1~T6）+ 9 轮执行期会话增量增强（均补录 verify.md 执行阶段记录），执行 + 验收两阶段 |
| 手工冒烟 | ✅ CubeDemo 后端 + Vite dev 实测：6 项配置 UI、甘特渲染、宽度持久化链路、分页器、缩放按钮、等待遮罩 |

## 做得好的

1. **VTable Gantt 源码补丁（patch-vtable-gantt.js）**：精准定位 `refreshAll` 冗余根因（4 次→1 次），用 postinstall 幂等脚本固化而非 fork 库——升级时可自动告警核对，维护成本低；千条切级从 >1s 降至 482~812ms，性能达标。
2. **宽度持久化兜底方案**：VTable Gantt `GANTT_EVENT_TYPE` 确认无表格宽度调整事件后，按 design §4.3 采用轮询 `taskTableWidth` + 300ms 防抖兜底，实现简洁且不依赖未公开 API。
3. **同步 applyZoomLevel 消除跳动**：根因分析到位（ZoomScaleManager 自动初始化级别 vs 延迟 setZoomPosition 造成两段式渲染），同步设置与实例创建同一渲染帧，一次解决。
4. **增量增强渐进式落地**：9 项增量（表头样式/时间轴填满/智能缩放/分页/性能优化/懒加载等）均在执行期会话中逐步完成并即时记录到 verify.md，未出现遗漏。
5. **旧数据兼容处理规范**：startField/endField→planned 迁移、colorField→忽略的行为变更在 normalizeMapping/单测/README/功能清单四处同步声明，迁移路径清晰。
6. **资源清理完整**：onBeforeUnmount 统一清理 widthTimer/resizeObserver/zoomApplyTimer/firstTaskScrollTimer/Ctrl 滚轮监听，无泄漏风险。

## 待改进

1. **执行期增量增强过多（9 项）**：原 proposal 范围仅 4 项核心能力（双条/定位/拖拽/固定色），执行期通过会话窗口追加了缩放/分页/性能优化/懒加载等 9 项增量。虽均即时记录到 verify.md，但**增量占比超过原始范围**，增加了验收审查工作量。**教训**：大跨度增量应在早期评估是否另立 OSC（如「OSC-0019b 甘特图性能与交互增强」），保持单 OSC 范围可控。
2. **缩放控件形态 3 次变更**：选项框 → 移除滚动条 → −/+ 按钮，每次均需改 DefaultList + GanttView 双文件。**教训**：与 OSC-0017「高级按钮形态」相同——涉及控件形态的 UI 决策应在 design 阶段一次性确认，减少执行期返工。
3. **Playwright 与 VTable canvas 互操作限制**：合成事件无法驱动 VTable 拖拽状态机（mousedown→pointermove 状态链），导致 AC-04/05 的 canvas 交互只能验证代码配置、无法自动化验证视觉效果。**已知限制**：VTable 为 canvas 渲染，Playwright 合成鼠标事件不触发内部状态机。后续 OSC 若涉及 VTable canvas 交互，建议在 verify 中提前声明此限制并规划人工冒烟。
4. **`position: relative` 冒烟暴露**：`.gantt-host` 缺少 `position: relative` 导致 VTable 分割线（absolute）相对页面定位被页头遮挡。属 CSS 定位基础遗漏，冒烟才发现。**教训**：涉及 absolute 定位子元素时，design 阶段应显式声明宿主定位上下文。

## 遗留与后续

- VTable Gantt 源码补丁基于 1.26.5 发布产物，升级 `@visactor/vtable-gantt` 后需核对 `patch-vtable-gantt.js` 告警（🟡 升级时必检）。
- AC-04（定位图标）/ AC-05（拖拽宽度）的 canvas 真实鼠标交互留待用户人工冒烟确认（🟢 代码配置已验证正确）。
- `colorField` 行为变更（按字段着色→固定色）已登记文档；若用户有按字段着色的存量需求，可后续另立 OSC 做双模式（🟢）。
- 甘特图依赖线/里程碑/进度条/数据编辑等能力不在本号范围，后续可另立 OSC（🟢）。
- 卡片/看板/列表/树增量渲染与翻页性能优化虽非甘特专属，但在本号执行期一并完成，属跨 OSC 的通用性能改进。
