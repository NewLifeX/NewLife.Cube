# 设计规格检查单（spec-checklist）

> 每个维度对应一类"如果不定义就会不一致"的决策。本项目的**权威规则**落地在 [`web/docs/standards/ui-spec.md`](../../../web/docs/standards/ui-spec.md)，本文件只列"要覆盖哪些维度"和"不定义会怎样"，供新增/审查页面时逐条对照。

## 核心维度

| 维度     | 不定义会怎样                                     | 落地位置                                     |
| -------- | ------------------------------------------------ | -------------------------------------------- |
| 布局     | 每个页面 max-width、间距、网格列数都不同         | page-types.md + ui-spec.md                   |
| 组件选型 | 同样的数据，有人用 table 有人用 card 有人用 list | component-decision.md                        |
| 交互模式 | 加载有人用 spinner 有人用 skeleton 有人用文字    | interaction-patterns.md                      |
| 页面模板 | 页头结构、操作按钮位置、内容区组织方式各不相同   | page-types.md                                |
| 排版     | 标题字号、正文大小、辅助文字颜色随意             | ui-spec.md                                   |
| 色彩语义 | "成功"有人用绿有人用蓝                           | ui-spec.md（`--el-color-success` 等）        |
| 动效     | 有人加 hover 抬升，有人加辉光，有人什么都不加    | ui-spec.md（禁止清单）                       |
| 图标     | 有人用 Element 图标，有人用其他图标库            | ui-spec.md（统一 `@element-plus/icons-vue`） |
| 暗色模式 | 有人适配了有人没适配                             | ui-spec.md（`core/themes/*.css`）            |
| 无障碍   | 对比度不够、焦点不可见、图标按钮没有 aria-label  | ui-spec.md                                   |
| 表单规则 | label 位置、校验时机、必填标记不统一             | interaction-patterns.md                      |
| 导航模式 | 面包屑/tab/侧栏何时出现不清楚                    | interaction-patterns.md                      |
| 数据密度 | 表格行高、卡片信息层级不清                       | interaction-patterns.md                      |
| 空态分类 | "从未有数据"和"筛选无结果"混用同一文案           | interaction-patterns.md                      |
| 错误边界 | 组件崩溃白屏、断网无提示                         | interaction-patterns.md                      |

## 新页面提交前的自检清单

- [ ] 页面类型（四选一）已确定，骨架符合 page-types.md
- [ ] 是否需要子壳已判断，需要则复用共享导航组件
- [ ] 组件选型走完优先级（框架组件 → Element Plus → Tailwind 摆放 → 才允许自定义）
- [ ] 没有 `el-row`/`el-col` 用于页面级布局（表单内部除外）
- [ ] 没有硬编码色值（`#fff`、`rgba(...)` 等），全部走 `--el-*` / Tailwind 语义类
- [ ] 没有 hover `translateY`/位移动效
- [ ] 圆角未超过 12px（对应 `--el-border-radius-base`）
- [ ] 加载态、空态、错误态按 interaction-patterns.md 选取，未现场发明新表现
- [ ] 暗色模式下手动过一遍（切换 `core/themes/*.css` 对应主题）
- [ ] 如果做出了新决策（新页面类型、新组件、新交互），已写入 `web/docs/decisions/` ADR

## spec 缺口处理

遇到本清单和 `ui-spec.md` 都未覆盖的决策点：**不做主观判断**，按 [principles.md 第六节](./principles.md) 走"标记缺口 → 决策者拍板 → 写入 ADR 与 ui-spec.md → 必要时同步 principles.md"流程。

## 判断 spec 是否足够的测试

把 `ui-spec.md` 和一个新页面需求交给不了解项目的人或 AI：产出"看起来像同一个产品" → 够了；"结构对细节不同" → 补充维度；"完全不像" → spec 太抽象，需要加具体代码示例。
