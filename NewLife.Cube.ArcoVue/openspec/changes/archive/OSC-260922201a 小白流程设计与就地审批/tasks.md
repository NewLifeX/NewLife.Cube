# OSC-260922201a Tasks

## T1 图与发布校验

- [x] `WorkflowGraph` 解析 `to.kind`、`emptyPolicy`、`oa.handle`；非法值列入发布错误
- [x] 无 kind 的旧节点仍按 users/roles/departments 校验
- [x] `level` 非 1 发布失败；办理节点 `sequence` 发布失败
- [x] 单测覆盖上表

## T2 引擎

- [x] 展开 manager / field / starterPick（发起 `picks`）
- [x] 禁用用户改派部门负责人后再套 emptyPolicy
- [x] `pass` / `manager` / `user` / 缺省四条路径
- [x] `oa.handle` 完成后续边；驳回接口对办理任务返回 400
- [x] 审批节点同一人自动跳过；办理节点不跳过
- [x] XOR 三分支顺序单测（引擎已有循环，补用例锁定）
- [x] `Workflow.xml` 增加 `WorkflowOccupancy` 并 xcode；Start 同事务插入；终态删除；冲突 409
- [x] 单测：旧图空候选人仍通知；双发起 409
- [x] 补齐（实现审计）：`WorkflowHost.Register` 触发六表建表 + 测试夹具 `WorkflowTestDb.EnsureWorkflowTables`（xUnit 下 `WorkflowOccupancy` 仅在查询路径建表）
- [x] 补齐（实现审计）：manager / `emptyPolicy=manager` / 禁用用户改派 / starterPick picks / field 取值用例（`WorkflowOsc260922Tests`）
- [x] 补齐（代码审查）：`Reject` 与 `TrySkipSamePerson` 的 NodeId 归一化（加签任务不可绕办理驳回；加签同意参与同人跳过），并补用例

## T3 设计器

- [x] 右侧选人六种，卡片人话（`wfNodeCard.ts`）
- [x] `setApproveMode` 接受 `or`/`and`/`sequence`
- [x] 新节点 `emptyPolicy=manager`；右侧可选四种空人策略
- [x] `flowgramGraph.ts` 支持 N 个条件支 + 末支「其他情况」；两支旧图可打开
- [x] 「+」可插入办理节点
- [x] vitest：卡片文案、模式、分支条数

## T3b 画布手感

引擎规则不改。对照飞书管理员手册第 4 章，只改设计器呈现。见 design §4.6。

- [x] 节点属性改为右侧抽屉（宽 400、无遮罩、关抽屉取消选中），画布不再让出固定侧栏
- [x] 连线「+」菜单挂到页面最外层，不被节点卡片挡住
- [x] 菜单改为短名：审批人、抄送人、办理人、条件分支、并行分支（并行分支的运行时见 T3c）
- [x] 卡片标题行：类型徽标 + 签核方式；内容行只写接收人；未指定时橙色提示
- [x] 条件卡片写出分支条数和「其他情况」是否已接上

## T3d 条件分支画布卡片（2026-09-25 会话补录 · 用户反馈）

- [x] 添加「条件分支」后画布即显示每个条件一张卡片（条件1…条件N、其他情况），卡片带筛选摘要与未接节点橙提示
- [x] 点条件卡片 → 右侧抽屉编辑该条件（点击设置条件、则去、删除该条件）；其他情况卡片编辑默认目标
- [x] 画布「＋ 添加条件」：在「其他情况」前插入新条件卡片（`addBlock` + `moveChildNodes` 归位），抽屉不再放「添加条件」
- [x] 分支数据通道：filter/则去目标以块上数据为准（`patchBranch`），增删分支后条件不错位；`branchInfosOf` 摘要含字段中文名
- [x] 单测：`flowgramGraph.spec`（块 filter/target、nextCaseBlockId、xorSplitBlocks）、`wfNodeCard.spec`（branchInfosOf）
- [x] 浏览器实测：新增条件/设置条件/删除条件/保存草稿/重开定义 全链路通过（GraphJson cases/target/defaultTarget 正确）

## T3e 分支卡片与属性面板打磨（2026-09-25 会话补录 · 用户反馈二）

- [x] 分叉节点上方留白修复：覆盖内置 `blockIcon` 占位（默认 250x84 + 间距）为 0x0 且无间距
- [x] 删除条件入口移到画布卡片标题栏垃圾桶（仅条件卡；其他情况不可删）；抽屉「删除该条件」移除
- [x] 条件卡片标题支持自定义名称（`cases[i].name` → 卡片标题），未命名回落「条件N」；抽屉新增「条件名称」输入
- [x] 条件编辑内嵌抽屉：筛选器内容抽为 `FilterBuilderPanel`（弹层/内嵌双模式），分支面板直接铺开不再弹层；窄容器下值控件自适应宽
- [x] 移除分支面板「则去」选择器：目标由画布分支卡片内拖入的节点自然决定（空分支保存不报错、发布时后端报「条件目标不存在」）
- [x] 单测：`wfNodeCard.spec` 增自定义名称用例（106 全绿）；`vue-tsc` 0 error
- [x] 浏览器实测：名称即时反映到卡片、卡片删除/新增条件、内嵌条件应用、保存后重开持久化（name/filter）、弹层模式（发起条件）回归

## T3f 抽屉 Footer 与常用语就地管理（2026-09-25 会话补录 · 用户反馈三）

- [x] 节点属性（分支条件）：单字段条件单行展示（字段/操作符/值收紧宽度不换行）；说明文字移到条件名称下方；重置/应用移到抽屉 Footer
- [x] 流程属性：保存草稿/发布移到抽屉 Footer；控件包入分组卡片（对齐 FormContent .form-group）
- [x] 流程属性：流程名称置顶；「发起条件」改内嵌条件面板（原弹层按钮）
- [x] 常用语不再弹层：就地可编辑/删除列表（行内编辑态 + ✓/✕、删除即时、底部行内新增），变更即时落库
- [x] `FilterBuilderPanel` 增 `showActions` 与 `defineExpose(reset/apply)`，供宿主 Footer 触发
- [x] 修复：常用语保存报「Value 长度限制 200 字符」——`ToJsonString` 将非 ASCII 转义为 \uXXXX 导致超长且 XCode 后置校验抛错（SQL 已执行）；改用 `Parameter.LongValue` 存储 + 纯字符串数组，读取兼容 Value 旧数据
- [x] 浏览器实测：内嵌条件应用/重置（Footer 触发）、常用语编辑/新增/删除（无错误提示，落库往返）、20 条长文本压力测试通过（前端 121/121、vue-tsc 0）
- [x] 流程属性去除分组方框；「新建流程」移到抽屉 Footer 左侧（无 current 也显示 Footer）
- [x] 「新建流程 / 发布」按钮使用主题主色（primary）
- [x] 内嵌条件面板不显示「查询」标题（且/或切换左侧；弹层模式保留标题）
- [x] 「发起条件」的且/或开关移到标签行右侧（FilterBuilderPanel 增 `showLogic` + `expose draft` 读写草稿）；无 label 的 form-item 内容强制纵向堆叠
- [x] 「不纳入效率统计」改用实体编辑表单同款 `a-switch`（label 在左）

## T3c 并行分支

`oa.xor` 保持只走一条。新增 `oa.parallel`，规则见 design §4.7。本段已实现（「并行分支」入口暂由 `ENABLE_PARALLEL_BRANCH=false` 开关隐藏，见下）。

- [x] 加号菜单增加「并行分支」，画布形状与条件分支相同（若干分支 + 最右「其他情况」）
- [x] 无条件的分支都进入；有条件的按现有过滤、第一条记录判断；都不进入时走「其他情况」
- [x] `joinTarget`：进入的分支全部完成后才继续；任一审批驳回则整单驳回并取消其余在途任务
- [x] 进度只画进入的分支；旧图无此节点时行为不变（2026-09-25 复核修正：节点流改只读画布后，未进入的分支显示「未处理」——见 T3g）
- [x] 引擎单测：两条都进并汇合、只进一条、都不进走其他情况、一条驳回取消另一条
- [x] 入口开关：`ENABLE_PARALLEL_BRANCH=false` 暂时隐藏「并行分支」插入菜单项（引擎与存量节点保留，恢复时改 true）
- [x] 复查（2026-09-25）：xor 与 parallel 配置同形系设计使然（design §4.7），差异在运行语义/文案/badge/汇合；补 joinTarget 缺省发布校验 + 单测（后端 69/69）

## T4 发起抽屉

- [x] `useSubmitApproval` 在同一抽屉渲染步骤；`starterPick` 行内选人
- [x] 未选齐禁用提交；请求体带 `picks`
- [x] 有 XOR 时显示「条件按第一条记录判断，将走：…」
- [x] 不新增子弹窗
- [x] vitest：缺 picks 时 canSubmit 为 false

## T5 就地审批与进度

- [x] 待办行同意/驳回/已办理走 popover（&lt;768px 为全宽 modal），不打开进度抽屉
- [x] 驳回意见为空不可提交；办理行无驳回
- [x] 批量驳回跳过办理任务并提示条数
- [x] 更多里的转交/加签/回退用同一行的第二个 popover
- [x] 标题或「进度」才打开 `WorkflowProgressPanel`
- [x] 记录抽屉审批页签底部就地办理，`embed` 内嵌进度面板，不再叠抽屉（2026-09-25 复核：原节点流已由时间线 + 只读画布取代，见 T3g）
- [x] 进度隐藏未命中分支；自动通过/跳过/转交写在节点上（2026-09-25 复核修正：未命中分支在只读画布显示「未处理」；自动文案随引擎意见进时间线——见 T3g）
- [x] 意见气泡上传并提交 `attachmentIds`；进度能列出 `WorkflowComment` 附件
- [x] vitest：按钮矩阵与分支过滤纯函数

## T6 效率页

- [x] `EnsureMenus` 增加「效率」叶子，URL `/Cube/Workflow/Efficiency`，排序 10
- [x] 设计器顶栏「不纳入效率统计」、审批节点「不计入效率」写入 `excludeStats`；办理节点不显示
- [x] `GET /Cube/Workflow/Efficiency`：无 Detail 权 403；`groupBy` 非法、`days` 与 `year` 同传、按节点却无 `definitionId` 均 400
- [x] 聚合六种：流程、部门（`DepartmentID`，0 为「未分配部门」）、用户、节点、年（今年与前两年）、月；排除规则与或签/会签计时不变
- [x] 点流程行改为按节点，点部门行改为按用户，点年行改为按月；月和节点就地展开最慢 10 条
- [x] `formatDuration` 单测：30 分钟、3.5 小时、2.0 天
- [x] 引擎或控制器单测覆盖排除规则与 403
- [x] 补齐（实现审计）：部门/用户筛选后完成率与超长等待跟着收窄；按年三行全 0 不画表；进度与记录页签对办理节点只显示「已办理」

## T7 文档与门禁

- [x] `Doc/功能清单.md` WF-2、WF-4 各补一句，并写效率页
- [x] `dotnet test` 过滤 `FullyQualifiedName~Workflow` 全绿
- [x] `dotnet build NewLife.Cube/NewLife.Cube.csproj -f net10.0` 0 error
- [x] `pnpm exec vue-tsc --noEmit` 0 error
- [x] 相关 vitest 全绿

## T3g 反馈五（只读实例画布与文案）

- [x] 流程属性抽屉「新建流程」改为「新建审批流程」
- [x] 接收人选择：已选值合并显示名（修复「1 ×」裸 id），卡片/下拉框友好名称
- [x] 「我发起的 / 已办 / 我的待办」进度抽屉「查看流程」：实例快照（graphSnapshot）只读画布；节点状态徽标（已完成/当前/未处理）替代删除按钮；点击节点不打开属性抽屉
- [x] 实体列表「提交」抽屉「查看流程」：发布快照（publishedGraphJson）只读画布（单一可用定义自动选中）；后端 `ToDefView` 下发 `publishedGraphJson`
- [x] E2E 基建：`auth.setup.ts` 算术验证码模板识别（`e2e/helpers/captcha.ts`）；`workflow-progress-graph.spec.ts` 两场景
- [x] 门禁：vue-tsc 0 error、vitest 120/120、dotnet test Workflow 69/69、E2E 3 passed

## T3h 反馈六（主题适配与弹层文案）

- [x] 深色主题：`.wf-flowcanvas` 接管 FlowGram `--g-editor-background`；FlowGramDesigner.tsx 节点卡/分支卡/插入菜单硬编码色语义变量化（浅色不变、深色跟随）
- [x] 「新建审批流程」弹层：标题改名、流程名称/实体顺序对调
- [x] 实体下拉友好名：后端菜单 DisplayName 优先（`AutomationAuth.FindMenu`）、前端 displayName 优先
- [x] E2E 新增 `workflow-designer.spec.ts`（深色两态 + 弹层标题/顺序/中文名）
- [x] 门禁：vue-tsc 0 error、vitest 120/120、E2E 25 passed / 0 failed；后端发布重启

## T3i 反馈七/八（条件友好名、开始节点禁删、文案、效率卡样式）

- [x] 分支条件卡片条件值友好名（FilterValueLabel 回调 + dataSource 映射，设计器/筛选摘要接入）
- [x] 开始节点不允许删除（卡片按钮隐藏 + FlowGramAPI/Vue 双层拦截）
- [x] 发起条件文案去歧义（? 说明符 tooltip + 只读摘要改写）
- [x] 效率页指标卡对齐工作台部件卡规范（bg-2/border-2/radius-8/padding + 图标标题行 + 32px 数值）
- [x] 效率指标卡数字居中 + 语义彩色（主色/成功绿/警告橙，图标同色；反馈九）
- [x] 门禁：vue-tsc 0 error、vitest 126/126、E2E 25 passed / 0 failed

## T3j 验收缺口补齐（2026-09-25 验收发现，用户选择「补齐后复盘」）

- [x] 修复前端 `validateGraph` 对 `kind ∈ {manager, starterPick, field}` 审批节点误报「缺少审批人」（按 `wfToKindOf` 分支校验；补 vitest 三 kind 用例）——验收发现 🔴
- [x] 修复设计器删除分流节点后被 Vue 镜像复活（`graphChangedFromCanvas` 收窄为仅保留被现存节点 target 引用的旧节点）——验收发现 🟡
- [x] 清理「节点流」移除后的死代码（`flowNodes`/`definitionNodes` 计算与返回、`WfDefinitionNodes.vue` 无引用文件）——验收发现 🟡（文档同步已先行修正文档侧）
- [x] 复验门禁：vue-tsc 0 error、vitest 127/127、E2E 25 passed、后端 Workflow 69/69
