# OSC-0018 Design — 实体界面自定义设计方案

适用前端（文档描述用）：Arco Design Vue（https://arco.design/vue/docs/start）；多维视图 VisActor VTable（https://visactor.com/vtable/option/ListTable）；工作流设计器若被决策树提到则 FlowGram.AI，**本号不设计运行时**。无 `.vue` 改动，无 SFC 实现任务。

## 0. 本号零代码

交付物只有 Markdown。`tasks.md` 勾选以文件是否存在为准，禁止在无文件时勾完成。

## 1. 研究基线（2026-08-30 必须复核，不得沿用 08-08 过时句）

### 1.1 已变化（原 design 过时点）

| 旧基线 | 现态 |
| --- | --- |
| 搜索自定义 = SearchDrawer | OSC-260830a1b2 退役抽屉；工具栏查询簇（Q + 自定义 + 预定义） |
| 无插件手册 | 迁移方案 **§12** 已是 L0/L1 权威操作手册 |
| Insight = 搜索+图 | 0e9e WidgetHost；15a1 `/home` 工作台 |
| 技能落点未定 | 禁止复制 Cube.Vue `skills/**` 正文 |

T1～T3 旧笔记可作草稿，**编写 T4 前必须 grep 复核** SectionKey、GetPage 字段、§12 目录。

### 1.2 Cube.Vue（对照，不搬）

8 技能目录以 `NewLife.Cube.Vue/skills/` 为准（含 cube-add-app / add-page / page-override / layout / lov / add-api / init / modal-organize，以及后来的 cube-design 等）。约定：只建文件、token `--el-*`。Arco 用 `--cube-*` + Arco token。交付矩阵按**实际目录**列出，禁止抄 08-08 过时「恰好 8 个」清单而不核对。

### 1.3 ArcoVue 现状

| 能力 | 文件 |
| --- | --- |
| DynamicPage / DefaultList | `views/dynamic/DynamicPage.vue`、`views/crud/DefaultList.vue` + `useDefaultList.ts` |
| Section 11 Key | `core/composables/useSections.ts` |
| apps 整页 | `core/utils/menuRoutes.ts`；`apps/*/src/views/**/index.vue` |
| 字段控件 | `core/utils/fieldControl.ts` |
| 运行期 | `stores/viewProfile.ts`：ViewsJson / FormJson / FiltersJson / DashboardJson；QueriesJson **保留**（OSC-260830 升级为 Q+自定义条件，退役的是 SearchDrawer） |

Section 与 Cube.Vue 映射（定稿进交付文档，此处约束）：

| Cube.Vue | ArcoVue | 文档必须写清 |
| --- | --- | --- |
| ListSearchBar | `ListSearchBar` | 收口后=筛选条/Q，不是抽屉 |
| TableColumns | `ListTableContent` | 粒度不同 |
| FormFields | `FormContent` | |
| DetailHeader | 无 1:1；详情在右侧抽屉 | 禁止假装同名即同意 |

### 1.4 GetPage

`ReadOnlyEntityController.GetPage`：`setting` + `list`/`addForm`/`editForm`/`detail`/`search`。`search` 在 260830 后=可筛元数据。计算列：§12「可序列化属性 + AddField」；`GetValue` 仅 MVC。`sensitive` 在 OSC-2608273d95 落地后出现，文档用「若已有则前端藏列，不是授权」。

映射表必须可追溯到 `packages/api-core` `DataField` 与前端消费方（VTable / FieldInput / RecordDrawer / filterBuilder），**不要**写 SearchDrawer 为消费方终态。

## 2. 交付文档结构（T4）

`web/docs/实体界面自定义设计方案.md`：

1. 背景：开发期 vs 运行期；链接 §12、§8.2。
2. L0～L4：每层「做什么 / 不做什么 / 指向文档」。
3. Cube.Vue ↔ ArcoVue 矩阵（≥10 行）。
4. GetPage 映射表。
5. 决策树（场景→一层→动作）。至少：新增实体（§12）、改列（L1）、改筛选（筛选构建器，非抽屉）、改表单 L1 vs FormJson、整页 apps、改壳、计算列、操作链 `__ops`。
6. Section 11 Key 速查。
7. 技能蓝图 ≥5：`arco-add-page` / `arco-page-override` / `arco-layout` / `arco-lov` / `arco-add-app`（可选 `arco-theme`）。每项：触发词、输入、产出路径、与 Cube.Vue 差异、**落地另号（不写 OSC-00xx）**。
8. 后续切片建议（查询收口、行权、技能落地、BE-B2），无抢号。
9. 边界：§8.2.6；FormJson≠ACL；AppModule≠业务 CRUD 主路径。

## 3. 文件级改动地图

| 文件 | 改 | 不动 |
| --- | --- | --- |
| `web/docs/实体界面自定义设计方案.md` | 新建 | |
| `web/README.md` | 可选增加文档链接一行 | 其它规范 |
| `ArcoVue企业中后台迁移方案.md` | 可选 §9 docs 登记一句 | 不得改 §12 操作步骤使与本号打架 |
| 任意 `.cs/.ts/.vue` | **禁止** | |

无 JSON schema 变更。无 UI 产品实现（不建 `ui/`）。

## 4. 核心文档影响

| 文档路径 | 影响类型 | 说明 |
|----------|----------|------|
| `NewLife.Cube.ArcoVue/web/README.md` | 修改（可选） | 链到设计方案 |
| `NewLife.Cube.ArcoVue/web/docs/**` | 新增 | 本号唯一必选 |
| `Doc/Api/内置前端皮肤.md` | 无 | |
| `Doc/功能清单.md` | 无新编码 | 设计号不新增 PERM/SPA 功能点 |
| `Doc/Api/核心接口架构.md` | 无 | 无新 API |
| `ArcoVue企业中后台迁移方案.md` | 可选一句 | §9 拟建 docs；§14 附录 OSC-0018 仍 Draft 直至验收 |

## 5. 测试设计

N/A 单测。验收= grep 交叉核对（verify 命令）+ 结构检查清单。

## 6. 关键决策

| # | 选择 | 否决 |
| --- | --- | --- |
| D1 | L1 纯后端优先 | 不把改列默认成写 Vue |
| D2 | 技能重写 | 不 git 复制 Cube.Vue skills |
| D3 | 本号只文档 | 技能落地另 OSC |
| D4 | 以工作区+§12 为准 | 不凭 08-08 记忆写 SearchDrawer 教程 |
