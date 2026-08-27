# OSC-0018 — 实体界面自定义设计方案

## 1. 为何做

ArcoVue 已具备零配置 CRUD 微内核（`DynamicPage` → `DefaultList`）、11 个 Section 覆写点、`apps/` 整页覆写、多维视图（table/tree/card/kanban/calendar/gantt）与用户运行时自定义（ViewsJson/FiltersJson/FormJson/模板），但存在三处缺口：

1. **缺「开发期自定义」的方法论**：现有自定义能力分散在代码与迁移方案 §8 中，没有一份面向「实体对象多维视图/表单自定义界面」的完整设计方案，业务研发不知道「什么场景用什么方式、如何最小成本自定义」。
2. **缺 AI 技能体系**：Cube.Vue 已沉淀 8 个协作技能（`cube-add-app` / `cube-add-page` / `cube-page-override` / `cube-layout` / `cube-lov` / `cube-add-api` / `cube-init` / `modal-organize`），使 Copilot 通过调用技能即可完成页面创建、Section 覆盖、布局定制；ArcoVue 尚无对应技能，AI 协作只能依赖通用指令，效率与一致性低。
3. **缺 Cube.Vue 机制的显式对标**：Cube.Vue 的「页面组件自动加载/路由自动注册」「Section 命名大写约定」「token 样式规范」「usePageApi」等成熟约定未被 ArcoVue 系统性吸收；ArcoVue 的 SectionKey 与 Cube.Vue 同名但覆盖点不齐（Cube.Vue 有 `TableColumns`/`DetailHeader` 等，ArcoVue 覆盖点与之有差异），需要一张显式能力矩阵。

本号产出**一份设计方案文档**（交付物），完成上述三件事：系统研究 Cube.Vue 自定义列表/表单机制与 ArcoVue 动态页面框架，给出「实体对象多维视图/表单自定义界面」的完整设计方案（含分层自定义模型、Section/apps 应用、GetPage 元数据利用、AI 技能体系蓝图），并落地为可执行的后续 OSC 蓝图。

## 2. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | **交付物为设计方案文档**，不写业务代码；文档落位 `NewLife.Cube.ArcoVue/web/docs/实体界面自定义设计方案.md`（新建 `web/docs/` 目录，与迁移方案 §9 拟建 docs 计划一致）。 |
| 2 | 设计方案覆盖「**开发期自定义**」（业务研发/AI 通过技能定制界面）与「**运行期用户自定义**」（OSC-0012~0016 已落地的 ViewsJson/FiltersJson/FormJson/模板）的边界划分：本方案聚焦开发期自定义，运行期仅作边界引用。 |
| 3 | 自定义分层模型（拟定）：**L0 零配置默认**（GetPage 元数据自动渲染）→ **L1 控制器字段级定制**（ListFields/AddFormFields/EditFormFields/OnGetFields/Search 重写，纯后端）→ **L2 Section 局部覆写**（ArcoVue 11 SectionKey）→ **L3 apps 整页覆写** → **L4 布局/壳定制**。 |
| 4 | AI 技能体系蓝图：参照 Cube.Vue 8 技能，为 ArcoVue 规划技能清单（`arco-add-page` / `arco-page-override` / `arco-layout` / `arco-lov` / `arco-add-app` 等），明确每个技能的触发词、输入参数、产出、与既有 Section/文件的对应关系；技能落地另立 OSC，本号只产出蓝图。 |
| 5 | 文档必须含：Cube.Vue ↔ ArcoVue 自定义能力矩阵、GetPage 元数据利用模型（5 分区字段 + setting + DataField 属性 → 前端消费映射）、自定义界面决策树（场景 → 用哪层）、Section 覆盖点速查、技能体系蓝图、实施切片建议。 |
| 6 | 仅研究+文档；不改任何前端/后端代码；不实现技能；不迁移 Cube.Vue 技能文件（ArcoVue 技能为 Arco 栈重写）。 |
| 7 | 研究素材以工作区现有内容为准：`NewLife.Cube.Vue/skills/**`、`NewLife.Cube.Vue/web/**`（源码）、`NewLife.Cube.ArcoVue/web/**`、`NewLife.Cube/Common/*EntityController*.cs`（GetPage 后端）、迁移方案与既有 OSC 归档。 |

## 3. 做什么

1. **研究 Cube.Vue 自定义机制**（T1）：通读 8 个技能与 `web/apps/**`、`web/core/**` 中页面创建/Section/布局/LOV 实现，沉淀「能力清单 + 约定速查」。
2. **研究 ArcoVue 动态页面框架现状**（T2）：`DynamicPage` / `DefaultList` 微内核 / `useSections`（11 SectionKey）/ `apps` 覆写 / `fieldControl` / LOV 链路，产出「已具备能力清单 + 与 Cube.Vue 覆盖点差异表」。
3. **研究 GetPage 元数据能力边界**（T3）：`GetPage` 输出（setting + list/addForm/editForm/detail/search 五分区）、`DataField` 属性全集、`OnGetFields`/`ListFields` 定制点、Map 候选/LOV 体系，产出「元数据 → 前端消费映射表」。
4. **编写设计方案文档**（T4，核心交付）：按 §2 决策 5 的结构成文（矩阵/元数据模型/决策树/Section 速查/技能蓝图/实施切片）。
5. **评审与定稿**（T5）：对照验收标准自查，修订后交付。

## 4. 不做什么

- 不改任何前端/后端代码（纯设计文档 OSC；文档本身是唯一交付物）。
- 不实现 AI 技能（仅产出技能蓝图，技能落地另立 OSC）。
- 不迁移/复制 Cube.Vue 技能文件（ArcoVue 技能按 Arco 栈重写，不直接搬 Element Plus 实现）。
- 不做运行期用户自定义的扩展（OSC-0012~0016 已覆盖，本方案仅边界引用）。
- 不评审/修改既有 OSC-0012~0017 设计。

## 5. 依赖

| 依赖 | 关系 |
| --- | --- |
| Cube.Vue 技能体系 | `NewLife.Cube.Vue/skills/**`（8 技能，能力与方法论参照） |
| OSC-0003 | Done：ArcoVue 微内核 / Section 同名 / apps 覆写 |
| OSC-0005~0016 | Done：多维视图 / 视图自定义 / 表单布局 / 模板 / 筛选 / 查询（运行期自定义现状） |
| GetPage 后端 | `ReadOnlyEntityController.GetPage` / `FieldCollection` / `DataField`（元数据利用） |

## 6. 测试范围

| 类型 | 是否做 | 说明 |
| --- | --- | --- |
| 代码测试 | 否 | 本号零代码改动，声明 N/A |
| 文档自审 | 是 | 设计方案文档按 verify.md 逐条自审：能力矩阵完整、决策树可执行、元数据映射逐项可追溯、技能蓝图覆盖 Cube.Vue 8 技能、实施切片与既有 OSC 无冲突 |
| 交叉核对 | 是 | 文档中引用的文件路径/SectionKey/API 名必须与工作区实际一致（实现审计式核对） |

## 7. 成功标准

- [ ] `web/docs/实体界面自定义设计方案.md` 交付，结构完整（见 §2 决策 5）。
- [ ] Cube.Vue ↔ ArcoVue 自定义能力矩阵：逐能力标注 Cube.Vue 现状 / ArcoVue 现状 / 差距 / 建议。
- [ ] GetPage 元数据利用模型：setting + 5 分区字段 + DataField 属性 → 前端消费映射表完整可追溯。
- [ ] 自定义决策树：覆盖「新增实体页 / 改列表列 / 改搜索 / 改表单 / 整页特殊交互 / 改壳」等场景，每场景明确落到 L0~L4 某层。
- [ ] Section 覆盖点速查：11 SectionKey 的名称/作用/覆盖方式/与 Cube.Vue 对应关系。
- [ ] AI 技能体系蓝图：为 ArcoVue 规划 ≥5 个技能（对应 Cube.Vue 核心技能），含触发词/输入/产出/对应文件，并明确落地为后续 OSC。
- [ ] 实施切片建议：按依赖排序给出后续 OSC 建议（如技能落地、缺省 Section 补点、能力矩阵缺口修复）。
- [ ] 文档中所有引用（路径/SectionKey/API/技能名）与工作区实际一致。
