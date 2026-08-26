# OSC-26081903c0 Retro

> 复盘 2026-08-20 | 状态 Done

## 范围回顾

| 维度 | 计划 | 实际 |
|------|------|------|
| A AI 浮窗 | 5 项（A.1-A.5） | 5 项完成 + 会话补录 2 项（marked Renderer、Modal.confirm content） |
| B 批量启停 | 4 项（B.1-B.4） | 4 项完成 |
| C 条件填色 | 8 项（C.1-C.8） | 8 项完成 |
| 测试/构建/文档 | 4 项（T.1-T.3、D.1） | 4 项完成 |
| 新增 spec | ~6 个 | 8 个 AI 纯函数 spec + viewFormat spec + 既有 spec 扩展 |

## 做得好的

- **纯函数先行**：AI 8 个纯函数模块（aiMarkdown/aiSse/aiChatContext/aiWelcome/aiFill/aiConfig/aiFab/aiAttach）各自独立可测，spec 锁死边角。填色同理（viewFormat）。
- **竞品截图对照表**：design §2.0 把「抄什么/砍什么」逐行锁定，执行期零范围蠕变。
- **不做什么清单**：proposal §5 明确 12 项不做，有效防止了 AI 浮窗扩展到历史/附件/搭建。
- **SFC 薄壳门禁**：`sfcThin.spec.ts` 在验收阶段检出 FormatPopover.vue 内的 `watch`，确保规范不退化。
- **文档同步及时**：4 份关联文档（功能清单/README/竞品分析/迁移方案）在实施期内同步更新。

## 改进项

- **SFC watch 遗漏**：`FormatPopover.vue` 的 `openColorIdx` + `watch(visible)` 应在实施期就放入 `useFormatPopover.ts`，而非等验收门禁检出。原因：`openColorIdx` 是纯 UI 状态，直觉上觉得放 `.vue` 里方便，但违反了 SFC 薄壳规范。→ 教训已固化：**所有 `ref` + `watch` 组合，无论多小，一律进 composable**。
- **验收门禁与实施的反馈环**：本次 sfcThin 违规如果在 commit hook 或 CI 中跑，可在实施期就发现，减少验收返工。

## 关键决策记录

| 决策 | 理由 |
|------|------|
| AI 右侧停靠 380px 而非右下小卡片 | 更接近竞品体验；面板可最大化 |
| 填色一条规则一个条件（非 AND/OR） | 降低实现复杂度；满足 90% 场景 |
| `column` 范围无条件铺满整列 | 整列填色不需要条件，否则语义混乱 |
| 卡片仅 side + row 两种 apply | cell/column 在卡片语义不适用 |
| `marked` Renderer 须 `new Renderer()` | marked 18 不允许传残缺 renderer 对象 |

## 统计

- 前端测试：65 files / 593 passed（含本号新增 ~80 用例）
- 后端测试：8 passed（Osc260819 P1/P2/P3/P4，本号不改 C# 行为，共享测试集合）
- 构建：vue-tsc + vite 0 error
- 代码审查：0 🔴（验收修复后）
- 文档同步：4 份文档已同步
