# 前端文档

> 这是 `web/` 的唯一文档入口。先按你要完成的任务找文档，不要从目录名猜实现。

## 先从这里开始

| 你要回答的问题                     | 先读                                                                                                               |
| ---------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| 项目要往哪里演进，AI 应如何选择实现路径？ | [product/vision-and-roadmap.md](./product/vision-and-roadmap.md) 与 [guides/ai-iteration.md](./guides/ai-iteration.md) |
| 这是怎样的项目，如何启动？         | [architecture/overview.md](./architecture/overview.md) 与 [guides/getting-started.md](./guides/getting-started.md) |
| 路由、菜单和应用是怎样协作的？     | [architecture/routing.md](./architecture/routing.md)                                                               |
| 配置、请求和状态由谁负责？         | [architecture/state-and-data.md](./architecture/state-and-data.md)                                                 |
| 新增页面、接口或主题怎样做？       | [guides/](./guides/)                                                                                               |
| 新增 / 改写一个通用组件（含样式）？ | [guides/add-component.md](./guides/add-component.md) + [standards/ui-spec.md](./standards/ui-spec.md)             |
| 新增一个接口 / API？               | [guides/add-api.md](./guides/add-api.md)                                                                           |
| 新代码必须遵守什么？               | [standards/](./standards/)（总纲见 [frontend-testable-development.md](./standards/frontend-testable-development.md)） |
| 当前已有何种组件、配置和路由约定？ | [reference/](./reference/)                                                                                         |
| 如何测试、构建、发布和排障？       | [operations/](./operations/)                                                                                       |
| 写组件时怎么看渲染效果 / 做视觉回归？ | [guides/component-visual-dev.md](./guides/component-visual-dev.md)                                                 |
| 怎么测组件在「页面 + 逻辑」综合下的完整闭环（请求/提交/调接口）？ | [guides/ct-page-mode-proposal.md](./guides/ct-page-mode-proposal.md)（草案） |
| 为什么曾作出某项关键选择？         | [decisions/](./decisions/)                                                                                         |

## 文档分层

| 目录            | 回答的问题                 | 维护规则                             |
| --------------- | -------------------------- | ------------------------------------ |
| `product/`      | 项目要解决什么、如何演进   | 方向或阶段优先级变化时更新           |
| `architecture/` | 系统怎样组织、组件怎样协作 | 结构或边界变化时更新                 |
| `standards/`    | 必须遵守什么               | 只写强约束；改规则前先写决策         |
| `guides/`       | 怎样完成一类任务           | 每步必须可从代码或命令验证           |
| `reference/`    | 当前事实是什么             | 紧贴源码；不写理由和长教程           |
| `decisions/`    | 为什么选 A 而非 B          | 追加 ADR，不把历史结论改写为现状     |
| `operations/`   | 怎样运行、测试、发布和排障 | 命令或运行环境变化时更新             |
| `changes/`      | 一次已完成变更发生了什么   | 只保留对维护仍有价值的迁移记录       |
| `archive/`      | 已失效但仍值得追溯的材料   | 不作为新开发依据；没有追溯价值则删除 |

## 权威来源

- 运行行为以 `core/`、`apps/`、`configs/`、`vite.config.ts` 和 `package.json` 为准。
- 产品方向以 [product/vision-and-roadmap.md](./product/vision-and-roadmap.md) 为准；AI 迭代按 [guides/ai-iteration.md](./guides/ai-iteration.md) 执行。
- UI 规则以 [standards/ui-spec.md](./standards/ui-spec.md) 为准；Element Plus / Tailwind 分工见 [decisions/0003](../decisions/0003-element-plus-tailwind-design-system.md)。
- 文档放置、状态、更新和删除规则以 [standards/documentation-standard.md](./standards/documentation-standard.md) 为准。
- 组件视觉开发的操作细节（Gallery 预览、`?story=` 深链、CT 编写、类人点击验证、CT 环境从零搭建）不在本文档展开，统一收口于项目技能 `vue-component-visual-loop`（`skills/vue-component-visual-loop/`）；本文档只阐述规范、使用流程与达成效果。
- 文档与代码冲突时，先以代码为事实修正文档；若代码行为本身不明确，补 ADR 后再改实现。

## 已迁移的旧文档

根目录不再放置设计草案、一次性重构总结或通用模板。原 `design.md`、`theme-guide.md`、`project.md`、`project-specification.md`、`menu-driven-routing.md`、`page-override-system-design.md`、`api-response-*.md`、`user-page-refactor-demo.md` 已按实际价值重写到上述目录，或删除并交由 Git 历史保留。
