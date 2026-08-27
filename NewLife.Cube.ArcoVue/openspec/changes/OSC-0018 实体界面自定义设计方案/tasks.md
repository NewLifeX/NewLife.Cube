# OSC-0018 Tasks — 实体界面自定义设计方案

> 依赖顺序：T1~T3 研究可并行（各自产出研究笔记）→ T4 基于 T1~T3 编写设计方案文档 → T5 评审定稿。
> 本号为纯文档 OSC，无代码测试；每项以「研究产出/文档产出」勾选，T5 以 verify.md 交叉核对为准。

## 研究阶段（T1~T3）

- [ ] **T1 研究 Cube.Vue 自定义列表/表单机制**
  - 通读 `NewLife.Cube.Vue/skills/**` 全部 8 个技能（cube-add-app / cube-add-page / cube-page-override / cube-layout / cube-lov / cube-add-api / cube-init / modal-organize），提取「能力清单 + 约定速查」（页面目录情形 A/B、Section 大写命名、registerLayout、usePageApi、token 规范）
  - 对照 `NewLife.Cube.Vue/web/apps/**`、`web/src/**` 源码核实技能描述与实现一致（抽查 index.vue 自动加载、Section 扫描注册、microAppConfig）
  - 产出：研究笔记（并入 T4 文档 §3 能力矩阵的 Cube.Vue 列）
  - [x] 研究完成（事实基线已固化于 design.md §1.1）
- [ ] **T2 研究 ArcoVue 动态页面框架现状**
  - `views/dynamic/DynamicPage.vue` / `views/crud/DefaultList.vue` 微内核 / `core/composables/useSections.ts`（11 SectionKey）/ `core/utils/menuRoutes.ts`（apps 覆写）/ `core/utils/fieldControl.ts`（控件矩阵）/ LOV 链路
  - 逐项核对 Section 覆盖点与 Cube.Vue 差异表（design §1.2），确认每个 ArcoVue Section 的 props/emits 契约与覆盖语义（含 OSC-0016 搜索改抽屉后的覆盖点语义）
  - 产出：能力清单 + 差异表（并入 T4 文档 §3/§6）
  - [x] 研究完成（事实基线已固化于 design.md §1.2）
- [ ] **T3 研究 GetPage 元数据能力边界**
  - `NewLife.Cube/Common/ReadOnlyEntityController.cs` GetPage/GetFields/OnGetFields、`NewLife.CubeNC/ViewModels/FieldCollection.cs`（ViewKinds/DataField/Map 候选）、`packages/api-core/src/types.ts`（DataField/PageSetting/PageInfo）
  - 产出：setting + 五分区 + DataField 属性 → 前端消费映射表（并入 T4 文档 §4）
  - [x] 研究完成（事实基线已固化于 design.md §1.3）

## 编写阶段

- [ ] **T4 编写设计方案文档（核心交付）**
  - 新建 `web/docs/实体界面自定义设计方案.md`，按 design.md §2 大纲成文：背景目标 / 自定义分层模型 L0~L4 / Cube.Vue↔ArcoVue 能力矩阵 / GetPage 元数据利用模型 / 自定义决策树 / Section 覆盖点速查 / AI 技能体系蓝图（≥5 技能含触发词/输入/产出/落地 OSC）/ 实施切片建议 / 边界与非目标
  - 引用规范：所有文件路径/SectionKey/API 名/技能名与工作区实际一致；运行期自定义仅边界引用
  - [x] 文档完成
- [ ] **T5 评审与定稿**
  - 按 verify.md 逐条自审（能力矩阵完整性 / 决策树可执行 / 元数据映射可追溯 / 技能蓝图覆盖 Cube.Vue 核心技能 / 实施切片与既有 OSC 无冲突）
  - 交叉核对：grep 验证文档引用的 SectionKey（useSections.ts）、技能名（Cube.Vue skills）、DataField 属性（types.ts）、Controller 方法（ReadOnlyEntityController.cs）
  - 修订后交付；必要时最小增量更新迁移方案 §9 docs 登记
  - [x] 自审通过 [x] 交叉核对通过

## 收尾

- [ ] **T6 归档登记**
  - `web/README.md` 登记 OSC-0018 交付物（可选最小增量）
  - 确认 status.md 推进 Validating 前置条件就绪
  - [x] 登记完成
