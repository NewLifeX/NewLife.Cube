# Docs 规范审查待办（TODO）

> 来源：2026-08-02 三侧面审查（① 文档一致性与代码真实性 ② AGENTS.md 合规有效性 ③ 测试与可视化落地可行性）。
> 状态图例：🔴 阻断 / ⚠️ 高·中 / 🅸 低 / ✅ 已完成 / ⏳ 暂缓。
> 本文汇总所有「待办 / 待更新 / 待落实」项并跟踪落地状态；规则细节仍在各 `docs/` 文件，本文只做索引。

## 一、待落实（需决策或装依赖，非纯文档）

- [ ] **T0 覆盖率门禁工具落地**（🔴 阻断，二选一）
  - **路径 A（真装）**：`pnpm add -D @vitest/coverage-v8 husky lint-staged`；新增 `test:coverage` 脚本（`vitest run --config vitest.config.unit.ts --coverage`）；把总纲 §5.2 的 coverage 块接进 `vitest.config.unit.ts`。
  - **路径 B（标注）**：暂不装，统一把 `testing-standard.md` / `build-release.md` 的 `test:coverage` 标注「待 §5.2 工具落地后生效」。
  - 现状：文档两说并存（总纲诚实写「未安装」，但 testing-standard / build-release 当已生效）。**本次已采用路径 B 的标注（见 D1）**，安装决策留待后续。
- [ ] **T1 实际落地 CubeTable 引擎**（P0–P6，ADR 0005）：当前 `core/views/index.vue` 仍是旧 Section 实现；文档描述的 `CubeTable` / `createCubeEngine` / `useCubeEngine` 为未来目标。属长期路线图，不在本次文档修正范围。

## 二、文档待更新（可立即修，本次已批量处理）

| ID  | 严重度 | 位置                                                            | 问题                                                              | 状态        |
| ---- | ------ | --------------------------------------------------------------- | ----------------------------------------------------------------- | ----------- |
| D1  | 🔴     | testing-standard.md:97,120；build-release.md:25,28             | `test:coverage` 当已生效但工具未装，与总纲「未安装」自相矛盾       | ✅ 已标注待工具落地 |
| D2  | ⚠️ 高  | route-conventions.md:11                                         | 把 `<CubeTable />`（P3 目标）写进「当前约定」表，与自身遗留标注矛盾 | ✅ 已标注 P3 目标   |
| D3  | ⚠️ 高  | cube-engine.md:72,188-190,327-328,397                           | 代码示例错用别名 `@/core`，真实为 `@newlifex/cube-vue/core`        | ✅ 已全局替换      |
| D4  | ⚠️ 高  | web/AGENTS.md:39                                                | 派发表漏草稿旧名 `CubeSearch`/`CubeFormDialog`（总纲已废弃「四件套」） | ✅ 已改 CubeTable 子组件 |
| D5  | ⚠️ 中  | README.md:38；ui-spec.md:3；component-catalog.md:31-33          | 死链 `../skills/`（skills 目录不存在）                            | ✅ 已改指 ADR 0003 / 删技能链接 |
| D6  | ⚠️ 中  | cube-engine.md:632；ui-spec.md                                  | CubeTable↔CSS 规范内链缺失（脱离 AGENTS 入口不可互达）            | ✅ 已互加交叉链接  |
| D7  | ⚠️ 中  | web/AGENTS.md 派发表；docs/README.md                            | 孤儿文档 `add-component.md`/`add-api.md` 未入分发                 | ✅ 已接入派发表与 README |
| D8  | ⚠️ 中  | frontend-testable-development.md:233（及 :227）                | 引擎测试示例指向不存在文件 `core/engine/__tests__/createCubeEngine.spec.ts` | ✅ 已标注待 P1 落地 |
| D9  | ⚠️ 中  | component-visual-dev.md                                        | CT 需本机 Google Chrome（`channel:'chrome'`）未注明               | ✅ 已补前置说明    |
| D10 | 🅸 低  | component-catalog.md:26,30                                     | 仍指导用 Section 覆盖 / 旧封装，与 ADR 0005 冲突                  | ✅ 已改 CubeTable 插槽 |
| D12 | 🅸 低  | web/AGENTS.md 硬约束                                           | 数值（≤50、80/75/80/80）抄入，有陈旧风险                          | ⏳ 暂缓（低优先级，后续统一为纯引用） |
| D13 | 🔴 待统一 | component-visual-dev.md, testing-standard.md, testing.md, SKILL.md | 引用未注册脚本 `test:ct:headed`、`pnpm vitest run`，需补 `package.json` 脚本或统一改为 `pnpm test:ct --headed`、`pnpm test:unit -- <path>` |
| D11 | 🅸 低  | web/AGENTS.md:44                                               | 决策引用用短名（0002/0004/0005）                                  | ✅ 已改全名        |
| D12 | 🅸 低  | web/AGENTS.md 硬约束                                           | 数值（≤50、80/75/80/80）抄入，有陈旧风险                          | ⏳ 暂缓（低优先级，后续统一为纯引用） |

## 三、附加说明

- 所有文档更新只改 `docs/` 与 `web/AGENTS.md`，未把规则回抄到别处；AGENTS.md 保持索引角色。
- 覆盖率「实际安装」（T0）与 CubeTable 引擎「实际落地」（T1）为代码 / 依赖层待落实项，不属本次文档修正。
- 复核方式：改完用 `grep -rn "skills/\|@/core\|test:coverage\|CubeSearch" web/docs web/AGENTS.md` 确认无残留死链与旧别名。
