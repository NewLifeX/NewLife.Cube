# OSC-0017 Retro

> 复盘在 `Validating` 阶段（checklist passed）由 openspec-retro 填写；归档后状态置 `Done`。

## 结果摘要

| 维度 | 结果 |
|------|------|
| AC 通过率 | 17/17 通过（AC-01~17；「高级」按钮形态与 design §2.3④ 不同为验收期用户决策，记入风险） |
| 三步编排 | 实现审计（11✅/1⚠️用户决策）→ 代码审查（0🔴 + 6🟡全修）→ 文档同步（2✅/1🟡已修） |
| 自动化门禁 | Vitest **295/295**（含本 OSC 新增 20 项）· vue-tsc ✅ · vite build ✅（主包 682→324KB gzip，按需引入降级生效） |
| 代码质量 | 图标体系双源分层清晰（iconRegistry 名字串唯一事实源 + iconComponents 组件按需登记）；单测双向锁死图标名有效性 |
| 工期 | 12 个计划 Task + 3 轮执行期会话细化（并入 T2/T5/T6/T7）全部完成，执行 + 验收两阶段 |
| 手工冒烟 | ✅ CubeDemo 后端 + Vite dev 实测：视图 Tab 图标/more-one/菜单图标/高级按钮/详情字段（36 字段统一 120px + 图标垂直居中 offsetY=0）/自定义主色三行布局/side header 60px/右上角主题图标循环 |

## 做得好的

1. **按需引入降级执行到位**：design §10 预置的风险（全量 `install(app,'icon')` 使主包 +387KB gzip）在 T1 即量化触发（682→324KB），及时降级为 `iconComponents.ts` 具名按需登记 + `main.ts` 自定义 `<icon-park>` 动态组件，避免一次 387KB gzip 的无声回归。
2. **图标注册表双源分层**：`iconRegistry.ts` 只存名字串（纯函数，可单测）+ `iconComponents.ts` 只存组件映射（按需引入点），职责分离；`iconRegistry.spec.ts` 用 `ICON_COMPONENTS` 覆盖断言把「名字 ↔ 组件」双向锁死，新增图标漏登记会被测试拦截。
3. **执行期用户决策快速收敛**：视图图标/菜单图标/more-one/高级按钮形态/自定义主色布局等 3 轮细化，均在浏览器冒烟中即时验证（DOM class + 几何位置断言），避免纯代码正确但视觉偏差。
4. **自定义主色选中态二义及时修复**：代码审查发现自定义徽标恒 selected 与预置色选中同时亮 check，改为 `isPresetColorActive()` 条件化——仅非预置色时显示选中态，消除语义冲突。
5. **文档与实现同步维护**：doc-sync 发现 `web/README.md` 仍写「全量 install」与按需引入相悖，验收期即时修正，防止文档误导后续维护。

## 待改进

1. **T4 替换遗留缩进/注释瑕疵**：RecordDrawer 有一处 icon-park 缩进比兄弟节点少 6 空格、iconRegistry 头注释保留「install(app,'icon')」旧描述，均到验收代码审查才被捕获。**教训**：批量替换后应做一次格式与注释一致性自检（grep 关键注释关键词），再进入测试。
2. **「高级」按钮设计基线不稳**：design §2.3④ 提案文字前 `more` 图标，执行期被用户改为右侧 `down` 箭头又撤销 min-width，来回 2 次。**教训**：涉及既有控件形态的改动，design 阶段应先与用户确认最终形态再落 proposal，减少执行期返工。
3. **自定义主色布局 3 次调整**：独立行 → 第三行水平 → 徽标在标签下方，均属验收前用户微调。**教训**：这类纯视觉微调宜在 demo 时一次性对齐（提供 2~3 个候选布局让用户选），避免逐轮会话往返。
4. **死登记 `more` 未即时清理**：T4 替换后 `more` 不再使用，但 iconComponents 保留登记直到验收审查才删。**教训**：替换完成后应 grep 使用点，同步清理未用登记与对应 spec 条目。

## 遗留与后续

- 「高级」按钮形态（文字 + 右侧 down）与 design §2.3④ 提案不一致——已记 verify 风险，proposal 成功标准「高级（含菜单项）带图标」仍满足，无需回溯 design。
- IconPark 按需引入依赖 `iconComponents.ts` 登记完整性——openspec/README.md 已注明「新图标必须先经 IconPark 站点确认再登记」，由 spec 断言守护。
- `menuIcon` 对 `fa fa-user` 空格多类名已兼容（首 token 查表）；Bootstrap 4+ `fas fa-user` 前缀（`fa-` 前有 `fas `）仍只取首 token `fas` 会 miss → 落关键词兜底，后续如需可扩展剥离 `fa[sbdrl]? ` 前缀（🟢）。
- `estimateDetailLabelWidth` 下限 120px 对超短标签（如「编号」）略宽，属统一列宽设计取舍（🟢，可按需再调）。
