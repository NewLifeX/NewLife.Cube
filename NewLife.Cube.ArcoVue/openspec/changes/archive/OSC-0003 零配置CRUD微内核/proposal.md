# OSC-0003 — 零配置 CRUD 微内核

## 1. 为何做

在 **仅 `UseArcoVue`** 时，菜单下任意 `EntityController` 自动获得与 **Cube.Vue 业务 CRUD 同级**的元数据管理页：列表/搜索/增删改、导入导出、字段控件矩阵、LOV、树形数据、图表、Section/整页覆写、**右侧**记录抽屉。  
布局/主题（OSC-0004）变化不得破坏该契约——CRUD 与壳解耦。

## 2. 范围（做）

- **DynamicPage** 作为宿主，落地 **Cube.Vue 微内核引擎**（GetPage → fieldControl → Section 列表/表单 → LOV → 命令式抽屉/弹层 → 菜单路由），UI 全部用 **Arco Design**（缺则自研于 `web/src/components`）。
- 路由 **B3**：`/Cube/MenuTree` → 叶节点 `addRoute` + `props: { type, authId }` + `beforeEach`；文件夹菜单不注册嵌套子路由（避免 NaiveUI 式嵌套坑）。
- 字段：Arco **本地适配层**（移植 Cube.Vue `fieldControl` 规则到 ArcoVue；**不**以扩展 `@cube/field-mapping` 为必选项）。
- LOV、GetChartData、树表（零配置识别层级/`children`）、Section + `apps` 整页覆写、**右侧**记录抽屉（表单为主；历史/评论见 design）。
- 冒烟实体：`Admin/User`、`Admin/Role`、`Admin/Menu`、`Admin/Log`。
- Vitest 关键路径 + `pnpm build` 硬门禁。
- **只改** `NewLife.Cube.ArcoVue/**`（及本 OSC 文档）；**禁止**改 Cube.Vue / NaiveUI / 其他皮肤前端代码。

## 3. 不做什么

- **不**做布局引擎 / 多布局 / 主题密度持久化 / TagsView 产品化（→ OSC-0004）。
- **不**接 VTable 多视图与 EntityViewProfile 列布局（→ OSC-0005/0006）；本号表格用 Arco `a-table`（可树）。
- **不**做 FlowGram、MFA UI。
- **不**改其他前端框架仓库代码；共享包 `@cube/*` **默认不改**（权限等在 Arco 侧适配；若批准阶段认定必须改共享包，另开附注任务）。

## 4. 依赖

| 依赖 | 关系 |
|------|------|
| OSC-0001 | Done（代理/通路） |
| OSC-0002 | 评论 Tab **消费** EntityComment：合并顺序仍建议 0002 先合；本号可先做抽屉骨架 + 历史 Tab，评论 Tab 待 0002 可用后接线 |
| 后端 | 既有 GetPage / Index / CRUD / Import/Export / GetChartData / Lov / MenuTree |

## 5. 与迁移方案的范围冲突（批准前必读）

原附录 OSC-0003 仅「page-logic / FieldMapping / 动态路由」，且 LOV/Section/抽屉分属后续号。  
**本次用户澄清**：能力 **A2** 且对 LOV / 树 / 图表 / Section / 记录抽屉 **不豁免** → 本 Draft 按 **加宽后的 M1** 编写。

OpenSpec「一号一事」建议（可选，批准时可拆）：

| 方案 | 说明 |
|------|------|
| **采用本号（默认）** | 单号交付 A2 微内核；后续 OSC-0005+ 在 Section 上替换表格/视图实现 |
| 拆号 A | OSC-0003 仅路由+微内核壳+基础控件+导入导出；OSC-0011 LOV；树并入 OSC-0006；抽屉并入 OSC-0007 |
| 拆号 B | OSC-0003a 列表引擎 / 0003b 字段+LOV / 0003c 抽屉+Section |

拆号需用户/批准 Agent 明确指令；**未拆则按本 proposal 全量执行**。

## 6. 验收 / 测试范围

| 类型 | 是否做 | 说明 |
|------|--------|------|
| **Vitest（新增）** | **是** | 路由构造、fieldControl/widget 推断、权限门闩、URL→type、序列化提交等纯逻辑 |
| **构建** | **是** | `pnpm build`（ArcoVue web）无错误 |
| 手工冒烟 | 是 | User / Role / Menu / Log 四页 CRUD 与菜单进入 |
| XUnit | 否 | 无后端代码（默认） |
| E2E | 否 | |

硬门禁：执行期跑新增单测；验收期**本 OSC 新增单测全过** + **构建无错误**。

## 7. 成功标准

- [ ] 叶菜单动态注册；守卫加载 MenuTree；`type`/`authId` 注入 DynamicPage
- [ ] 四冒烟实体可完成列表/搜索/增删改（权限与 pageSetting 生效）
- [ ] Admin/Menu 树形列表可用（零配置或等价识别）
- [ ] LOV 字段可选用；含 lovCode 的表单/搜索不落回纯文本
- [ ] 导入导出、批量删除、GetChartData 可用（有数据的控制器）
- [ ] Section 与 apps 整页覆写机制可演示至少一处
- [ ] 右侧记录抽屉可打开详情/编辑；与布局 store/主题 **零读取**
- [ ] 本 OSC 新增 Vitest 全过；`pnpm build` 无错误
- [ ] 迁移方案 §10.3 / §13 与本号范围已回写一致
