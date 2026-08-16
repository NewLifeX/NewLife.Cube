# OSC-260815fa86 — 实体增删改自动化

## 1. 为何做

Cube 实体增删改（含导入、启用禁用、作业、`entity.Insert()`）目前没有可配置的事后自动化。业务管理员只能改代码或靠外部脚本，无法达到飞书多维表格「自动化」的配置能力：记录增改删、指定字段变化、日期到达、定时、按钮、入站 Webhook，以及发通知、增改查找记录、HTTP、延时、执行另一条自动化。

方案 C 已锁定：**配置存统一 `GraphJson`（nodes/edges）于 Cube；V1 用线性表单编译为 `Start → [Filter] → Action* → End`；C# 执行器跑图；不把 FlowGram 当运行时。** 本号一次交付方案 C 的全部功能述求（含原先拟延后的按钮/Webhook/查找/延时/再跑一条），并把触发与动作对齐飞书「自动化」（不是飞书「工作流」画布）。

## 2. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | **一个实现 OSC**：不拆「V1 增改删 + 二期按钮/Webhook」。本号做完方案 C 全部述求，并达到飞书多维表格**自动化**能力。 |
| 2 | 对标飞书帮助中心的 **自动化**（线性触发+动作）。**不对标**飞书「工作流」If/Else 画布、会签/加签/审批待办。 |
| 3 | 数据模型用方案 C：`GraphJson` 含 `nodes`/`edges`；表单编译线性图。图 schema **声明** `condition`/`switch`/`loop`/`approval`，本号 UI 不提供这些节点；执行器遇未知/预留类型 **失败并停止**（不得静默跳过）。 |
| 4 | 配置者：业务管理员、无代码。主路径为 **飞书式双栏表单**（触发+条件 | 动作），不引入 FlowGram 依赖、不画布。 |
| 5 | 运行时：**C# 执行器**。触发挂 XCode 持久化拦截（与 `UserModule`/`TenantModule` 同层），**禁止**只挂钩 `EntityController.OnInsert/OnUpdate/OnDelete`。SQL 成功后 **异步**入队，失败不回滚业务写入。 |
| 6 | 筛选条件 JSON 与 OSC-0015 `ViewFilter`（`logic: all\|any`，ops `eq/neq/contains/...`）同构；C# `AutomationFilter.Match` 与前端 `matchesViewFilter` 对齐。FilterBuilder 列表过滤仍为纯前端，本号不改其语义。 |
| 7 | 入口：实体列表 `DefaultList` 顶栏「自动化」。**不**做 ObjectController / DefaultObject / DefaultHome。 |
| 8 | 租户：定义行带 `TenantId`；执行用定义上的租户。不把本号与 OSC-260813397e 的登录/SSO/租户头 WIP 混提交。 |
| 9 | 通知：写 `NotificationRecord`，渠道 InApp/Mail/Sms/DingTalk/WeCom；接收对象为 **用户 / 角色 / 部门三选一**（多选 ID）；模板仅 `{{Field}}` 白名单，无脚本。飞书 IM 入站触发 **不做**。壳顶栏提供「站内通知」抽屉（读 InApp）。 |
| 10 | 本号 **不**做保存前同步拦截、不改单例 Object PUT、不接任意非 Area URL 级联。 |
| 11 | 配置 UI 对标飞书「自动化」双栏：左触发+字段条件卡片，右动作卡片；**不**用 Steps 三步向导。动作菜单 **不**再提供「执行自动化」添加入口（执行器仍支持旧图中的 `runAutomation`）。 |

### 飞书自动化 ↔ 本号

| 飞书自动化 | 本号 |
| --- | --- |
| 添加记录时 | `insert` |
| 修改记录时 | `update` |
| 添加或修改且满足条件 | `insertOrUpdateIf` + filter 节点 |
| 修改了指定字段 | `fieldChange` + `watchFields` + Dirtys 快照 |
| 到达记录中的时间 | `dateArrive` + Cron 扫描 |
| 定时 | `schedule` + CronJob |
| 点击按钮 | `button` + 行操作 |
| 接收 webhook | `webhook` 入站 |
| 收到飞书消息 | **不做** |
| 发送消息/邮件 | `notify`（NotificationRecord 渠道；接收人=用户/角色/部门三选一） |
| 新增记录 | `createRecord`（实体下拉：有 Insert 权限） |
| 修改记录 | `updateRecord`（目标 `current`\|`found`；`found` 须前置 findRecords） |
| 查找记录 | `findRecords`（实体下拉：有 Update 权限 + ViewFilter） |
| HTTP 请求 | `httpRequest` |
| 执行自动化 | 执行器支持 `runAutomation`（深度 ≤3）；**配置菜单不提供添加入口** |
| 延时 | `delay` |
| AI 生成文本 | `aiText`（无 AI 配置则该节点失败） |
| 添加评论 | `addComment`（复用 EntityComment；目标 `current`\|`found`） |
| If/Else 画布、审批 | schema 预留，本号不实现 |
| （壳）站内通知 | 顶栏 `remind` 图标 + 抽屉时间轴；读 Inbox API |

## 3. 做什么

1. `Cube.xml` 增加 `EntityAutomation`（ConnName=Cube）与 `AutomationRun`（ConnName=Log），xcode 生成；CubeNC `Link` 同步。
2. 全局持久化拦截：Insert/Update/Delete 成功后按 TypePath 匹配启用规则并入队；Update 前快照 Dirtys。
3. C# 图执行器：实现 start/filter/notify/updateRecord/createRecord/findRecords/httpRequest/delay/runAutomation/addComment/aiText/end。
4. Cube API：CRUD 规则、运行历史、按钮手动跑、入站 Hook、字段 Meta、实体列表（Entities）、接收人搜索（Recipients）、站内信 Inbox。
5. ArcoVue：列表顶栏「自动化」抽屉 + **飞书双栏**编辑器（触发/条件 | 动作）编译 GraphJson；行按钮触发；流程「运行日志」Tab；壳顶栏站内通知。
6. CronJob `EntityAutomationTick`：定时触发、日期到达、延时续跑。
7. 循环防护、租户隔离、权限、单测与文档。

## 4. 不做什么

- 不引入 FlowGram Runtime/画布/npm 包；迁移方案里 FlowGram 仍是后期「工作流」样例（OSC-0010 叙事），与本号自动化平台分离。
- 不实现审批待办、会签、加签、人工节点等待。
- 不做飞书 IM/机器人入站触发。
- 不把 FilterBuilder 改成后端查询；自动化条件只在执行器匹配。
- 不在本号混入 OSC-260813397e 登录/SSO/`X-Tenant`/SecuritySettings 的未提交改动。
- 不手写实体骨架（必须 xml + xcode）。

## 5. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-0003 | DefaultList / GetPage / typePath |
| OSC-0008 | EntityComment（addComment 动作） |
| OSC-0009 | 字段元数据驱动选择器 |
| OSC-0015 | ViewFilter JSON 与 FilterBuilder 操作符矩阵；`matchesViewFilter` |
| OSC-2608139feb | pageKind：仅 entity 页挂入口 |
| CronJob / JobService | schedule、dateArrive、delay 续跑 |
| NotificationRecord / MailConfig / SmsConfig | notify 渠道 |
| IAIService | aiText；未启用则节点失败 |
| OSC-0018 / OSC-260813397e | **不依赖、不混提交** |

## 6. 测试范围

| 类型 | 是否做 | 说明 |
| --- | --- | --- |
| Vitest | 是 | 表单→Graph 编译、TriggerConfig 归一、ops 按钮拼装、filter 与 C# 对齐样例、found 目标链路校验 |
| api-core 单测 | 是 | `createAutomationApi` URL/方法（含 recipients/entities/inbox） |
| XUnit | 是 | Graph 校验、Filter.Match、循环深度、Dirtys 快照入队、Webhook token、租户隔离 |
| 构建 | 是 | `pnpm --filter @cube/arco-vue test` + `build`；`dotnet test NewLife.Cube.Tests`；`dotnet build NewLife.Cube` |
| 手工/E2E | 是 | User 列表打开自动化抽屉保存一条 insert+notify（用户接收人）；无 Update 权限不显示按钮；顶栏站内通知可读。CubeDemo 无菜单则记环境跳过，不删用例 |

## 7. 成功标准

- [ ] 实体列表（非 Object/Home）有「自动化」入口；无该实体 Update 权限时不显示配置入口。
- [ ] 飞书双栏表单可配置对照表中的本号触发与动作（添加动作菜单 8 项，不含 runAutomation 入口），保存后 GraphJson 为线性 nodes/edges。
- [ ] 字段条件为飞书式字段卡片（勾选展开；运算符≈4/12、条件值≈8/12；字段名与条件区有分隔线）；notify 接收人为用户/角色/部门按钮组三选一且可下拉实体数据。
- [ ] 动作卡片：标题栏动作名 + `⋯` 菜单（上移/下移/删除带图标）+ 右侧放大收起；`target=found` 无前置 findRecords 时保存阻断并卡片告警。
- [ ] 经控制器、导入、Enable/Disable、直接 `entity.Insert()` 的写入，只要拦截器覆盖到，均能触发（自表与运行表除外）。
- [ ] 按钮、Webhook、定时、日期到达、查找、延时均可用；预留节点类型跑一次即失败并写入 Run；旧图 `runAutomation` 仍可执行。
- [ ] 壳顶栏「站内通知」用 remind 图标；抽屉无底部按钮，点遮罩关闭。
- [ ] 深度>3 或自触发风暴被挡住；模板无法注入脚本。
- [ ] 本号新增单测全过，前端与 `NewLife.Cube` 构建无错误；README / 功能清单 / 核心接口架构 / 迁移方案已回写「自动化 ≠ FlowGram 工作流」。
