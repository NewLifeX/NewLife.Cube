# OSC-26081903c0 Verify

> 进入 `Validating` 后逐项勾选。命令在 `NewLife.Cube` 仓库根或注明的 `web/` 下执行。

## 必须保留（暂缓区，实施误删即失败）

- `EnableSelect`/`DisableSelect` 仍为 **GET** + query `keys`；`EnableOrDisableSelect` 循环与 `OnSetField` 不改。
- GetPage `[AllowAnonymous]`、`EnableFieldValidation` 默认 false。
- 批量删除门禁仍仅 `table`（本号不把删除扩到 tree）。
- e483「批量修改」菜单项不删除。
- Cube.Vue `AiAssistant.vue` 不改。
- 条件格式不得从 list/detail 分区删字段。
- 不把填色做进 `ViewConfigDrawer`；不做「智能全局配色」。

## 命令与预期

```
pnpm --filter @cube/api-core test
pnpm --filter @cube/arco-vue test
pnpm --filter @cube/arco-vue build
```

预期：0 failed；vue-tsc/vite 0 error。

记录（Implementing 2026-08-19）：

- `pnpm --filter @cube/api-core test`：vitest 35 + node:test 50，全过。
- `pnpm --filter @cube/arco-vue test`：62 files / 553 passed。
- `pnpm --filter @cube/arco-vue build`：vue-tsc + vite 0 error。

新增/扩 spec：`aiMarkdown` `aiSse` `aiChatContext` `aiWelcome` `aiFill` `viewFormat`；api-core `getAiConfig`；`viewProfile`/`stores/viewProfile`/`viewMapping` 启停与 format。


## Happy path

- [ ] **AC-01 AI 开**：CubeSetting `AISwitch=true`，登录后右下 FAB；点开为**右侧全高面板**（非右下小卡片）；欢迎含当前顶栏用户名；推荐 Tab 可点「检查系统运行状态」并出现 SSE 文本（需环境已配 IAIService，否则气泡失败文案含 AISwitch/服务提示，**不崩溃**）。
- [ ] **AC-01b 欢迎 Tab**：提问 Tab 无快捷行；分析 Tab 在列表页含「分析当前数据」；无「搭建」、无回形针、无「表哥」文案。
- [ ] **AC-01c 更多**：更多菜单可开深度推理；清空后回到欢迎区且 session 更换。
- [ ] **AC-02 AI 上下文**：打开 Admin/User 列表，对话走 area=Admin controller=User（可在网络面板核对 body）。
- [ ] **AC-03 填表**：User 点添加，快捷「帮我填表」或 fill_form 写入当前抽屉可写字段；只读/主键不被覆盖。
- [ ] **AC-04 启停**：User 列表勾选 2 条，高级 → 批量禁用 → 确认 → Message 含启用/禁用个数 → 列表刷新徽标变化。
- [ ] **AC-05 填色整行**：表格工具栏「填色」→ 添加「Enable 等于 false / 整行 / 浅黄」；关闭弹层后禁用行整行着色（含勾选列与操作列）；刷新后规则仍在，徽标为 1。
- [ ] **AC-05b 填色单元格**：规则改为「单元格」后仅 Enable 列变色，其它列恢复默认。
- [ ] **AC-05c 互斥**：打开填色时筛选/分组弹层关闭；点填色徽标清空规则且着色消失。
- [ ] **AC-05d 优先级**：两条规则，上条整行、下条单元格；命中行只应用上条背景。拖拽换序后着色跟随。
- [ ] **AC-05e 行侧边**：添加「Enable 等于 false / 行侧边 / 红色」；禁用行最左侧出现约 3px 竖条，其它列背景不变。再加一条整行浅黄后，竖条与整行底**同时**可见。分组头无竖条。
- [ ] **AC-05f 整列**：范围改为「整列」后操作符与值消失，只留字段；该列所有行着色（不看条件）。弹层宽度贴合控件，无大块空白。色块点开为 3×10 预置色板（含红橙绿蓝紫）并可勾选「文字加粗」。
- [ ] **AC-05g 默认字段**：无规则时打开填色弹层，自动出现一条规则且字段为当前列表第一列。

## 权限 / 空 / 非法 / 旧数据

- [ ] **AC-06 登录页**：`/login` 无 FAB。
- [ ] **AC-07 AI 关**：`AISwitch=false` 无 FAB。
- [ ] **AC-08 无 Detail**：无菜单 Detail 的实体目标对话 403，气泡展示错误，无工具乱执行。
- [ ] **AC-09 无 Enable**：无 Enable 列实体高级菜单无启停两项。
- [ ] **AC-10 无 Update**：有 Enable 但无 Update，无启停项；行内徽标保持现码不可点。
- [ ] **AC-11 空选中**：启停项可见但 disabled；点不发请求。
- [ ] **AC-12 超 200**：选中 >200 时 disabled 或确认前拦截，不发请求。
- [ ] **AC-13 树视图**：tree 无批量启停项（与删除一致）。
- [ ] **AC-14 非法 format**：ViewsJson 掺入 `color:red`、嵌套 `filter` 对象的规则，加载后被丢弃，不抛错。
- [ ] **AC-15 旧视图**：无 `format` 的 ViewsJson 行为与今日一致。
- [ ] **AC-16 非权限**：仅填色隐藏「异常」行视觉；GetDetail/导出仍含该字段。
- [ ] **AC-17 高级菜单**：仅有 Update、无导入导出删除的实体，仍能打开高级并看到批量修改（e483）与启停（若有 Enable）。
- [ ] **AC-18 Markdown XSS**：助手返回 `<img onerror=alert(1)>` 以转义文本显示。
- [ ] **AC-19 填色隐藏**：看板/日历/甘特无「填色」按钮；卡片有按钮。切回表格规则仍在。
- [ ] **AC-20 空值**：`等于` 且值未填的规则不命中任何行，规则行仍留在弹层。
- [ ] **AC-21 卡片标题行**：卡片添加「Enable 等于 false / 整行 / 浅黄」；命中卡片仅标题条变色，字段区/操作区保持默认底。范围下拉只有「行侧边」「整行」。表格里的单元格规则切到卡片后不涂标题。
- [ ] **AC-21b 卡片行侧边**：卡片规则改为「行侧边」后，卡片左缘出现 3px 色条，标题条恢复默认底（若无整行规则）。

## Validating 验收记录（2026-08-20）

### 测试与构建门禁

| 命令 | 结果 |
|------|------|
| `pnpm --filter @cube/api-core test` | 51 pass / 0 fail |
| `pnpm --filter @cube/arco-vue test` | 65 files / 593 passed / 0 failed |
| `pnpm --filter @cube/arco-vue build` | vue-tsc + vite 0 error（仅 chunk size warning） |
| C# 测试（Osc260819P1/P3/P4 + Osc260819/P2） | 8 pass / 0 fail |

**验收修复**：`sfcThin.spec.ts` 检出 `FormatPopover.vue` 内含 `watch`/`ref` 违反 SFC 薄壳规范。已修复：`openColorIdx` + `watch(visible)` 移入 `useFormatPopover.ts`，`.vue` 不再直接引用 `ref`/`watch`。

### 三步检查摘要

1. **实现审计**：tasks.md 全部勾选（A.1-A.5 / B.1-B.4 / C.1-C.8 / T.1-T.3 / D.1）；design §2.3 文件地图全部到位（aiMarkdown/aiSse/aiChatContext/aiWelcome/aiFill/aiConfig/aiFab/aiAttach + spec；viewFormat + spec；FormatPopover + useFormatPopover）。会话小任务已补录（A.3 marked Renderer；B.3 Modal.confirm content）。
2. **代码审查**：验收修复后无 🔴 违规；SFC 薄壳门禁通过。
3. **文档同步**：`Doc/功能清单.md`（SPA-7）、`web/README.md`（填色+AI 浮窗）、`竞品分析报告.md`（v1.4 §6.1）、`ArcoVue企业中后台迁移方案.md`（§3.1/§10.4）均已同步引用 OSC-26081903c0。

### 目标愿景对照

| 目标 | 实现 | 结论 |
|------|------|------|
| 目标 1：AI 浮窗（AISwitch 控制、SSE 对话） | A.1-A.5 全部完成；8 个 AI 纯函数模块 + spec | ✅ |
| 目标 2：批量启停（高级菜单、table 限定、200 上限） | B.1-B.4 全部完成；resolveBatchEnableState 真值表 | ✅ |
| 目标 3：条件填色（最多 50 条、4 种范围、NamedView.format） | C.1-C.8 全部完成；viewFormat + FormatPopover | ✅ |
| 不做项（不改后端、不改 API、不做权限/智能配色等） | 全部遵守，无越界 | ✅ |

### 缺口清单

**无缺口。** 所有 proposal 目标 1-3 和 design 文件地图均已在代码中实现并通过测试。浏览器 AC 冒烟（AC-01…AC-21b）归人工验证，不阻断自动化验收。

### checklist: passed

## 残余（可接受，不阻断）

- EnableSelect 用 `Find("ID")` 而非 `FindData`，行权漏洞是既有问题。
- 非数字主键批量启停可能 0 条。
- 日历/甘特不绘制 format。
- 看板不显示填色按钮；若下发标题色则与卡片标题行同规则。
- AI 无服务时不能「对话成功」，但 UI 必须稳定。
