# OSC-0018 — 实体界面自定义设计方案

## 1. 目标愿景

业务研发和 AI 能按一张决策树选对自定义层级：多数实体只靠后端控制器 + §12 插件手册零前端；特殊页才用 Section/apps。文档与已归档运行期能力、§8.2「呈现≠ACL」一致，不再把 SearchDrawer 当长期搜索面。

- 目标 1：交付 `web/docs/实体界面自定义设计方案.md`（唯一代码外交付物），结构含 L0～L4、能力矩阵、GetPage 映射、决策树、Section 速查、技能蓝图、后续 OSC 切片。
- 目标 2：决策树每条场景落到唯一层级；L0/L1 以迁移方案 **§12** 为操作手册（本号不重复写第二份插件教程）。
- 目标 3：运行期（ViewsJson/FormJson/筛选/仪表盘/工作台）只作边界引用，并写明 FormJson/藏列不是 ACL。
- 目标 4：技能蓝图 ≥5 项且 **重写不迁移** Cube.Vue Element 技能；落地另立 `OSC-YYMMDDxxxx`，禁止预留 OSC-0010 空洞号。技能对照目录以工作区 `NewLife.Cube.Vue/skills/` 实际名为准（含 `cube-design` 等，不虚构已删除的技能名）。

## 2. 为何做

ArcoVue 微内核与 Section/apps 已存在，但缺少面向「实体多维视图/表单怎么定制」的单一入口文档。Cube.Vue 有 8 个技能，Arco 栈没有对应蓝图。本号创建于 2026-08-08，此后已归档 Widget、工作台，迁移方案已写 **§12 业务功能插件开发**，且 **OSC-260830a1b2** 将退役 SearchDrawer。原草案任务勾选与 `web/docs` 文件均不存在，本次修订把研究基线对齐到 2026-08-30，并清零虚假完成勾。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | **纯设计文档 OSC**：唯一必选交付 `NewLife.Cube.ArcoVue/web/docs/实体界面自定义设计方案.md`。零 `.ts/.vue/.cs` 业务改动。测试 N/A。 |
| 2 | 分层：**L0** 零配置（§12.3 宿主引用 Area+EntityController）→ **L1** 控制器字段（ListFields/OnGetFields/计算列可序列化）→ **L2** Section（11 Key）→ **L3** apps `index.vue` → **L4** 壳（布局/主题，UserProfile）。 |
| 3 | **§12 优先**：L0/L1 步骤、验收十分钟、计算列/`__ops`、禁止 GetValue 当 API，全部指向 §12，本号文档用链接+摘要表，禁止另写一套互相打架的 cookbook。 |
| 4 | 运行期只引用已归档：OSC-0012～0016、260819e483、2608280e9e、OSC-26082815a1。**不**在本号扩展 FormJson 权限语义。 |
| 5 | 搜索自定义：决策树按 **OSC-260830a1b2** 写——退役 SearchDrawer；工具栏查询簇（Q + 自定义条件 + 预定义）。不得再教「覆盖 SearchDrawer」。 |
| 6 | 技能：规划 `arco-*` 蓝图即可；文件落点建议 `NewLife.Cube.ArcoVue` 或 Skills **薄壳**，**禁止**复制 `NewLife.Cube.Vue/skills` Element 正文。 |
| 7 | 历史 ID `OSC-0018` 豁免不改名。实施切片建议必须用 `OSC-YYMMDDxxxx` 占位描述，禁止 `OSC-0010`/`max+1`。 |
| 8 | 不评审已归档 OSC 的设计对错；发现冲突时以迁移方案 §8.2 / §12 / 归档 OSC 为准并在文档「边界」列出。 |

## 4. 做什么

1. 按 design 研究基线（须复核 2026-08-30 源码：SectionKey、GetPage、§12）编写交付文档。
2. 能力矩阵：Cube.Vue vs ArcoVue，每项采用/重写/不采用。
3. 决策树：新增实体页 / 改列 / 改筛选 / 改表单 / 整页交互 / 改壳 / 计算列 / 操作链。
4. 技能蓝图：触发词、输入、产出路径、与 Cube.Vue 差异、建议后续 OSC 主题（无号）。
5. 可选最小增量：`web/README.md` 链到该文档；迁移方案 §9 拟建 docs 登记一句。

## 5. 不做什么

- 不改业务代码；不实现技能；不搬 Cube.Vue 技能文件。
- 不实现查询收口、DataScope、字段矩阵、FlowGram。
- 不把 ViewProfile / FormJson / 筛选当 ACL。
- 不设计整页画布、用户脚本公式、第三方市场（§8.2.6）。
- 不把 AppModule zip 当业务 CRUD 插件主路径（§12 已否定）。

## 6. 依赖

| 依赖 | 关系 |
| --- | --- |
| 迁移方案 §12 / §8.2 | L0～L1 与呈现≠ACL 的事实源 |
| OSC-0003 | Done：微内核 / Section / apps |
| OSC-0005～0016、e483、0e9e、15a1 | Done：运行期边界引用 |
| OSC-260830a1b2 | Draft：退役抽屉；查询簇 + QueriesJson 预定义 |
| Cube.Vue `skills/**` | 对照，不迁移 |

## 7. 测试范围

| 类型 | 是否做 | 说明 |
|------|--------|------|
| 代码测试 | 否 | 声明 N/A |
| 文档自审 | 是 | verify AC |
| 交叉核对 | 是 | SectionKey / DataField / GetPage / §12 标题与工作区一致 |

## 8. 成功标准

- [ ] `web/docs/实体界面自定义设计方案.md` 存在且章节齐全。
- [ ] L0/L1 明确「详见 §12」，无第二套打架步骤。
- [ ] 决策树覆盖约定场景且每条唯一层级。
- [ ] 11 SectionKey 与 `useSections.ts` 一致。
- [ ] ≥5 技能蓝图；声明重写不迁移；无 OSC-00xx 新号。
- [ ] 非目标引用 §8.2.6；FormJson≠ACL。
- [ ] git 本号仅新增/改 markdown（及可选 README 链接）。
