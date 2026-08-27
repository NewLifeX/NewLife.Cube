# OSC-0018 Design — 实体界面自定义设计方案

## 0. 适用框架与官方资料

| 场景 | 框架/资料 | 说明 |
| --- | --- | --- |
| 研究方法论参照 | Cube.Vue 技能体系 `NewLife.Cube.Vue/skills/**` | 8 技能：cube-add-app / cube-add-page / cube-page-override / cube-layout / cube-lov / cube-add-api / cube-init / modal-organize |
| 前端实现参照 | Cube.Vue `NewLife.Cube.Vue/web/**` | apps 微前端 / Section / token 规范 / usePageApi |
| 目标框架 | ArcoVue `NewLife.Cube.ArcoVue/web/**` | DynamicPage / DefaultList / useSections / apps 覆写 |
| 元数据来源 | `NewLife.Cube/Common/ReadOnlyEntityController.cs`、`NewLife.CubeNC/ViewModels/FieldCollection.cs` | GetPage / DataField / ViewKinds |
| 规范依据 | `NewLife.Cube.ArcoVue/ArcoVue企业中后台迁移方案.md` §8~§9 | 固定容器契约 / 覆写优先级 / docs 建设计划 |

**本号零代码改动**：以下「研究结论」直接支撑 T4 编写的设计方案文档，是文档的权威事实源。

## 1. 研究结论（T1~T3 预期产出的事实基线）

### 1.1 Cube.Vue 自定义机制（T1 研究基线）

Cube.Vue 自定义能力由「8 个技能 + Section 覆盖机制 + 微前端 + token 规范」构成：

| 能力 | 机制 | 关键约定 | 触发词（技能） |
| --- | --- | --- | --- |
| 新增子应用 | `apps/{app-name}/` + `microAppConfig.json` 注册（4 步） | 框架自动加载路由/菜单/侧栏 | cube-add-app |
| 新增页面 | 按 Area/Controller 判断情形 A（区域为文件夹）/B（区域即应用）创建 `index.vue` | **只需建文件**，路由/菜单自动注册；自定义页须先有原型 | cube-add-page |
| 列表字段定制 | 后端 `ListFields`（Clear/AddListField/GetField/Url/Target/DisplayName/Align/Width/Header） | 配置在 Controller 静态构造 | cube-add-page（字段段） |
| 菜单图标 | 后端 `[Menu(Icon = "Element图标名")]` | 可在菜单管理覆盖 | cube-add-page |
| Section 覆盖 | `views/{Controller}/` 下大写开头文件（ListSearchBar/ListToolbar/TableColumns/FormFields/DetailHeader/Pagination） | Vite 插件扫描大写开头 .vue 自动注册；父组件经 `defineEmits` 通信 | cube-page-override |
| 布局定制 | `registerLayout(option, setAsCurrent)` | 双 Token 架构：`--el-*` + `--cube-layout-*`；禁止硬编码色值/自定义 token | cube-layout |
| LOV | LOV 选择器与值集 | — | cube-lov |
| API 封装 | `usePageApi(area, controller)` → getPage/getFields/getList/getDetail/add/update/remove/uploadFile/importFile/getChartData | 基于全局 `cubeApi`（@cube/api-core） | cube-add-api |
| 样式规范 | 页面/布局**必须**用 Element Plus token（`--el-*`）或 Cube Layout token（`--cube-layout-*`） | 禁止 `#fff`/`rgba` 硬编码、禁止自定义 `--xxx` | 全部技能通用 |

> 关键洞察：Cube.Vue 的「**只建文件、框架自动接线**」模型（页面/Section/布局均如此）+「**token 规范**」约束，是技能体系能高效驱动 AI 的根本原因——AI 只需遵守目录约定与 token 规范，无需理解框架内部路由/注册机制。

### 1.2 ArcoVue 动态页面框架现状（T2 研究基线）

| 能力 | 现状 | 文件 |
| --- | --- | --- |
| 薄宿主 | `DynamicPage`：解析 `DefaultListPage` Section 覆写 → 否则挂 `DefaultList` | `views/dynamic/DynamicPage.vue` |
| 微内核 | `DefaultList`：GetPage → fieldControl → 列表/搜索/LOV → 右抽屉（RecordDrawer） | `views/crud/DefaultList.vue` |
| Section | 11 个 SectionKey（与 Cube.Vue **同名**）：DefaultListPage/PageNotFound/ListPageHeader/ListSearchBar/ListToolbar/ListTableContent/ListPagination/ListPageFooter/FormPageHeader/FormContent/FormActions | `core/composables/useSections.ts` |
| Section 注册 | `registerPageSectionsFromGlob` 扫描 `apps/*/src/views/**/[A-Z]*.vue`，路径段映射 typePath；同名大写文件自动注册 | `main.ts` |
| apps 整页覆写 | `apps/*/src/views/**/index.vue` 匹配菜单 path → 替换 DynamicPage | `core/utils/menuRoutes.ts` |
| 字段控件 | `fieldControl.ts`：resolveListControl/resolveSearchControl/resolveFormControl（20+ 控件） | `core/utils/fieldControl.ts` |
| LOV | LovSelect/LovSelectTable + lov-api（含 OSC-0016 `entity:` 内部值集） | `components/LovSelect.vue` 等 |
| 运行期自定义 | ViewsJson/FiltersJson/FormJson/QueriesJson/模板（OSC-0012~0016） | `stores/viewProfile.ts` 等 |
| 样式 | `--cube-*` 语义 token + Arco `--color-*`/`--primary-*`（主题/密度可配） | `theme/tokens.ts` |

**Section 覆盖点与 Cube.Vue 差异表**（T2 需逐项核对后定稿）：

| Cube.Vue Section | ArcoVue 对应 | 差异/建议 |
| --- | --- | --- |
| ListSearchBar | `ListSearchBar`（同名） | 已具备；搜索已随 OSC-0016 改为 SearchDrawer，覆盖语义需在文档中明确 |
| ListToolbar | `ListToolbar`（同名） | 已具备 |
| TableColumns | `ListTableContent` | 覆盖点粒度不同（表内容 vs 列），文档给出映射建议 |
| FormFields | `FormContent` | 表单整体 vs 字段粒度，文档给出映射建议 |
| DetailHeader | `ListPageHeader` | 语义不同，文档给出映射建议 |
| Pagination | `ListPagination`（同名） | 已具备 |
| （无） | DefaultListPage / PageNotFound / ListPageFooter / FormPageHeader / FormActions | ArcoVue 独有扩展点 |

### 1.3 GetPage 元数据能力边界（T3 研究基线）

`ReadOnlyEntityController.GetPage()` 输出（`ReadOnlyEntityController.cs` L101-142）：

- **setting**：NavView / EnableNavbar / EnableToolbar / EnableAdd / EnableKey / EnableSelect / EnableFooter / IsReadOnly / EnableTableDoubleClick / OrderByKey / DoubleDelete / masterTimeName / masterTimeDisplayName。
- **五分区字段**：`list` / `addForm` / `editForm` / `detail` / `search`，经 `PrepareFieldsForApi(OnGetFields(ViewKinds.X))` 输出。
- **DataField 属性**（`api-core/types.ts`）：name / displayName / description / category / typeName / itemType / length / precision / scale / primaryKey / nullable / readOnly / required / visible / mapField / url / target / dataAction / header / maxWidth / textAlign / dataSource / dataSourceMap。
- **定制点**：`ListFields` / `AddFormFields` / `EditFormFields` 静态配置、`OnGetFields(kind)` 重载（按上下文定制字段集）、`Search(Pager)` 重写、`FixSearchMapCandidates`（Map 外键候选，OSC-0016）。
- **LOV/字典**：`LovCode` / `DataSourceMap` / `Entity.` 内部值集（OSC-0016）→ LovSelect 渲染。

**元数据 → 前端消费映射**（T3 定稿，示例骨架）：

| 元数据 | 前端消费 | 用途 |
| --- | --- | --- |
| setting.EnableAdd / EnableToolbar / EnableKey… | DefaultList chrome/flags | 工具栏显隐 |
| setting.masterTimeName | SearchDrawer 主时间控件（OSC-0016） | 搜索 |
| list[] → DataField | fieldControl.resolveListControl + VTable 列 | 列表 |
| addForm[]/editForm[] → DataField | FieldInput 控件矩阵 | 表单 |
| detail[] → DataField | RecordDrawer 详情分组渲染 | 详情 |
| search[] → DataField | SearchDrawer 条件渲染 + Map 候选 | 搜索 |
| category | 表单/详情分组折叠 | 布局 |
| itemType/typeName | 控件选择 + 详情图标（OSC-0017） | 控件/图标 |

## 2. 设计方案文档结构（T4 交付物大纲）

`web/docs/实体界面自定义设计方案.md` 章节规划：

1. **背景与目标**：ArcoVue 自定义现状、本方案定位（开发期自定义，运行期边界引用）。
2. **自定义分层模型（L0~L4）**：
   - L0 零配置默认：实体 + EntityController + 菜单 → GetPage 自动渲染。
   - L1 控制器字段级定制：ListFields/AddFormFields/EditFormFields/OnGetFields/Search（纯后端，前端零改动）。
   - L2 Section 局部覆写：11 SectionKey 覆盖点速查（名称/作用/props-emits 契约/与 Cube.Vue 对应）。
   - L3 apps 整页覆写：目录约定、原型要求、token 规范。
   - L4 布局/壳定制：RootLayout 布局模式（side/top/mix 配置化）、主题/密度、登录/首页。
3. **Cube.Vue ↔ ArcoVue 自定义能力矩阵**：逐能力（页面创建/Section/列定义/表单字段/详情头/分页/布局/LOV/API 封装/样式 token）对比现状与差距、给出「ArcoVue 采用/重写/不采用」结论。
4. **GetPage 元数据利用模型**：setting/五分区/DataField → 前端消费映射表 + 定制点速查（§1.3 展开）。
5. **自定义决策树**：场景 → 层级 → 具体动作（含示例：新增实体页、改列表列、改搜索条件、改表单字段、整页特殊交互、改壳）。
6. **Section 覆盖点速查**：11 SectionKey 的覆盖方式、与 Cube.Vue 差异及映射建议（§1.2 差异表展开）。
7. **AI 技能体系蓝图**：为 ArcoVue 规划技能清单（对照 Cube.Vue 8 技能）：
   - `arco-add-page`：按后端 Controller 在 `apps/**` 建 `index.vue` / Section 组件（Arco token 规范）。
   - `arco-page-override`：11 SectionKey 覆盖方法（Arco 组件 + `--cube-*` token）。
   - `arco-layout`：RootLayout 布局定制（side/top/mix 配置 + 新布局接入）。
   - `arco-lov`：LovSelect 值集接入（含 `entity:` 内部值集）。
   - `arco-add-app`：整页覆写应用创建（apps 目录 + 菜单 path 匹配）。
   - （可选）`arco-theme`：主题预置色/图标接入（对齐 OSC-0017）。
   每个技能给出：触发词、输入参数、产出文件、与 Cube.Vue 同名技能的差异、落地所属 OSC。
8. **实施切片建议**：按依赖排序的后续 OSC 蓝图（技能落地、Section 覆盖点补齐、能力差距修复、样例工程）。
9. **边界与非目标**：不做的能力（低代码画布/拖拽/跨实体/公式字段，引用迁移方案 §8.2.6）。

## 3. 关键设计决策

| # | 决策 | 理由 |
| --- | --- | --- |
| D1 | 自定义分层 L0~L4，**L1 为纯后端**（ListFields/OnGetFields），L2/L3 为前端 | 与迁移方案 §8「固定容器 + 覆写优先级」一致；L1 零前端成本最高 |
| D2 | ArcoVue 技能**重写不迁移**（Arco 组件 + `--cube-*`/Arco token，非 Element Plus） | Cube.Vue 技能含 Element Plus 专属内容（el-* token），直接搬会误导 Arco 开发 |
| D3 | 技能蓝图落地**另立 OSC**，本号只交付文档 | 本号定位设计研究；技能落地是编码 OSC，需独立批准执行 |
| D4 | 能力矩阵以「工作区实际代码」为准（非想象），所有引用经交叉核对 | 防文档虚构；verify.md 设交叉核对 AC |
| D5 | 运行期自定义（OSC-0012~0016）只作边界引用，不展开 | 已有 OSC 文档权威，避免重复/冲突 |

## 4. 文档影响

| 文档 | 影响 |
| --- | --- |
| `web/docs/实体界面自定义设计方案.md`（新建） | 本号唯一交付物 |
| `ArcoVue企业中后台迁移方案.md` | 最小增量：§9「拟建 web/docs」落地登记（可选，评审后定） |
| `NewLife.Cube.ArcoVue/web/README.md` | 登记 OSC-0018 交付物（可选） |

## 5. 风险

| 风险 | 缓解 |
| --- | --- |
| 文档与工作区实际不一致（虚构路径/Section/API） | verify 设交叉核对 AC（逐项 grep 引用）；T1~T3 以源码为准 |
| 能力矩阵遗漏/过时（Cube.Vue 持续演进） | 标注研究基线日期；矩阵「ArcoVue 采用/重写」结论为主，细节以落 OSC 时复核 |
| 方案过度设计（低代码画布倾向） | 决策树与边界章节显式引用迁移方案 §8.2 非目标，防止越界 |
| 技能蓝图与既有 OSC 冲突（如搜索已改抽屉） | 蓝图技能（arco-add-page 等）以当前实现（SearchDrawer/InsightPanel 现状）为基线撰写 |
