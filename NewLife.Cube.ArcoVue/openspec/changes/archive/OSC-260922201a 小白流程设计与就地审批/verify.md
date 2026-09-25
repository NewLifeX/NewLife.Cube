# OSC-260922201a Verify

> 状态：AC 记录（openspec-apply 收尾门禁登记；交互项待验收复核）  
> 时间：2026-09-22T14:45+08:00  
> 触发：批准并落地后由 openspec-verify 填写。  
> 编排：implementation-audit → code-review → doc-sync  
> 说明：每条 AC 下「证据」为自动化用例；标 [ ] 者为纯浏览器交互行为，需验收阶段人工确认。

## 验收标准

### Happy path

- [x] **AC-01** 设计器新建「部门负责人 · 或签」不填用户 Id 可发布；发起后任务在该部门 `ManagerId`。
  - 证据：`WorkflowOsc260922Tests.OldGraph_WithoutKind_StillPublishes`（无 users 可发布）、`Manager_Resolves_To_DepartmentManager`、`EmptyPolicy_Manager_FallsBack_To_DepartmentManager`
- [x] **AC-02** 三个条件分支从左到右只执行第一条命中；都不命中走「其他情况」。
  - 证据：`WorkflowOsc260922Tests.Xor_MultiCase_FirstHit_ElseDefault`
- [ ] **AC-03** 待办点「同意」只出现气泡，不出现进度抽屉；提交后该行消失。
  - 证据（部分）：`useWorkflowTaskList.spec.ts` 行操作矩阵；气泡/行消失为交互行为，待浏览器验收。
- [x] **AC-04** 办理节点行上只有「已办理」；调用驳回返回 400，实例仍为审批中。
  - 证据：`WorkflowOsc260922Tests.HandleNode_Reject_Returns400`；前端 `rowActionsOf` 办理行 `canReject=false`（`useWorkflowTaskList.spec.ts`）
- [x] **AC-05** 发起抽屉展示步骤；提交人自选未选时不能提交；选后 `picks` 进入实例任务。
  - 证据：`WorkflowOsc260922Tests.StarterPick_Uses_Picks_Or_400`；`useSubmitApproval.spec.ts`（缺 picks canSubmit=false、`picks` 入请求体）

### 权限 / 空 / 非法 / 旧数据

- [x] **AC-06** 无 kind、无 emptyPolicy 的旧图发布成功；空候选人仍通知发起人且不自动通过。
  - 证据：`WorkflowOsc260922Tests.OldGraph_WithoutKind_StillPublishes`、`OldGraph_EmptyCandidate_NotifiesStarter`
- [x] **AC-07** `level` 为 2 或办理节点 `mode=sequence` 发布失败。
  - 证据：`WorkflowOsc260922Tests.Level2_AndHandleSequence_FailPublish`
- [x] **AC-08** `emptyPolicy=pass` 且无人：实例离开该节点，意见含「自动通过」。
  - 证据：`WorkflowOsc260922Tests.EmptyPolicyPass_ApprovesInstance`
- [x] **AC-09** 同一人已同意过的下一审批节点不产生待办；办理节点仍产生待办。
  - 证据：`WorkflowOsc260922Tests.SamePerson_SkipApprove_NotHandle`
- [x] **AC-10** 禁用用户的任务改派到其部门负责人。
  - 证据：`WorkflowOsc260922Tests.Disabled_User_Reroutes_To_DepartmentManager`
- [x] **AC-11** 同一 `(TypePath, EntityKey)` 并发第二次发起 409。
  - 证据：`WorkflowExclusiveTests.SameRecord_SecondStart_409`、`Concurrent_DoubleStart_OnlyOneRunning`
- [ ] **AC-12** 非候选人同意 403。驳回意见空白时气泡提交按钮禁用。
  - 证据（部分）：非候选人 403 = `WorkflowEngineTests`「非候选人操作 403」；气泡禁用为组件交互，待浏览器验收。
- [x] **AC-13** 批量驳回跳过办理任务，提示跳过条数。
  - 证据：`WorkflowMenuTests.BatchReject_SkipsHandleTask`；前端 `useWorkflowTaskList` 计算 skipped 条数与提示。
- [ ] **AC-14** 记录抽屉审批页签办理时不再打开第二层抽屉。
  - 证据：`WorkflowProgressPanel` `embed` 内嵌实现；第二层不开为交互行为，待浏览器验收。
- [x] **AC-15** 附件出现在该意见下；进度「查看流程」为只读画布——全图渲染，未命中分支显示「未处理」（2026-09-25 复核修正：原「进度不渲染未命中分支」随节点流改「时间线 + 只读画布」作废，见 T3g）。
  - 证据：`wfProgressFlow.spec.ts`（`splitAttachments` 三类归位、意见附件挂办理人、`definitionPaint` 已完成/当前/未处理三态；`executionOrder` 未命中分支过滤保留为纯函数用例，现无 UI 消费者）；`WorkflowRequestModelValidationTests`（`attachmentIds` 模型校验）
- [x] **AC-16** 有「效率」菜单 Detail 权的用户可按流程、部门、用户、节点、年、月看到平均耗时、中位耗时、超 48 小时占比。部门按办理人 `DepartmentID` 归堆，无部门的行标题为「未分配部门」。办理节点和「不计入效率」的节点不进任何一维。无该权返回 403。
  - 证据：`WorkflowMenuTests.Efficiency_Auth_And_Validation`（403/400）、`WorkflowOsc260922Tests.Efficiency_SamplesAndExclusions`（排除规则、overdue、slow；部门/年/月归堆在 `BuildEfficiency` 实现）
- [x] **AC-17** 点流程行后同一页改为该流程的节点表；点节点行就地展开最慢 10 条，不打开新页面。等待超过 48 小时的未办完单计入超长等待数，不计入平均耗时。按年固定为今年与前两年。
  - 证据：`useWorkflowEfficiency.spec.ts`（`drillOf` 流程→节点、年→月、月/节点就地展开、`buildQuery` 三年）、`Efficiency_SamplesAndExclusions`（50 小时未办完只进 `OverdueOpen`）；界面换聚合为交互行为，待浏览器验收复核。

### 反馈增强（T3d，2026-09-25 会话补录）

- [x] **AC-18** 添加「条件分支」后画布即显示「条件1」与「其他情况」两张卡片；点卡片右侧抽屉编辑该条件（点击设置条件/则去/删除该条件）。
- [x] **AC-19** 画布「＋ 添加条件」在「其他情况」前新增条件卡片（气泡不再出现在抽屉）；「其他情况」始终在最后。
- [x] **AC-20** 设置/删除条件后保存，GraphJson `cases[].filter`/`target` 与 `defaultTarget` 正确，重开定义完整还原。
  - 证据：浏览器实测（新增/编辑/删除/保存/重开全链路）；单测 `flowgramGraph.spec`（块 filter/target 通道、`nextCaseBlockId`、`xorSplitBlocks`）与 `wfNodeCard.spec`（`branchInfosOf`）。

### 反馈增强二（T3e，2026-09-25 会话补录）

- [x] **AC-21** 分叉节点与上一节点之间不再出现大面积空白（内置 `blockIcon` 占位 250x84 被覆盖为 0x0）。
  - 证据：浏览器截图（审批→分支卡片行间距恢复正常）。
- [x] **AC-22** 条件卡片标题栏提供删除按钮（其他情况没有）；卡片标题随「条件名称」输入实时变化；未命名回落「条件N」；保存后重开名称仍在。
  - 证据：浏览器实测（改名→卡片即时更新；删除→卡片消失；保存/重开 `cases[].name` 持久化）；单测 `wfNodeCard.spec`（自定义名优先）。
- [x] **AC-23** 分支面板不再有「则去」选择与抽屉「删除该条件」；条件编辑直接内嵌抽屉（重置/应用，无“取消”），不弹层；弹层模式（发起条件/列表筛选）不受影响。
  - 证据：浏览器实测（内嵌面板应用→卡片摘要更新；发起条件弹层打开/取消正常）；`FilterBuilderPanel` 弹层/内嵌双模式单一事实源。

### 反馈增强三（T3f，2026-09-25 会话补录）

- [x] **AC-24** 分支条件抽屉：单字段条件（字段/操作符/值/删行）在同一行；抽屉整体说明文字位于条件名称编辑框下方；重置/应用在抽屉 Footer。
  - 证据：浏览器实测（单行布局；Footer 重置清空草稿、应用更新卡片摘要“类型 等于 3”）。
- [x] **AC-25** 流程属性抽屉：保存草稿/发布在 Footer；控件分组卡片样式对齐实体编辑抽屉；流程名称置顶；发布条件改内嵌条件工具。
  - 证据：浏览器实测（Footer 两按钮、名称第一位、发起条件内嵌面板可重置/应用）。
- [x] **AC-26** 常用语就地管理：不再弹层；列表行内可编辑/删除、底部行内新增，变更即时落库并刷新。
  - 证据：浏览器实测（编辑→列表刷新且 `PUT /Cube/Workflow/Phrases` code=0；新增/删除往返无错误提示）。
- [x] **AC-27** 常用语保存不再受 200 字符限制（原：4 条中长文本即报错）；改存 `Parameter.LongValue` + 纯字符串数组，20 条长文本保存通过；旧 `Value` 数据可读。
  - 证据：接口实测（修复前 A 版 4 条 code=500、修复后同内容 code=0；20 条压力 code=0 且落库完整）；SQL 日志确认 `Update Parameter Set LongValue=...`。

### 复查：条件分支 vs 并行分支（2026-09-25）

- [x] **AC-28** 条件分支与并行分支的配置 UI 一致为设计使然（design §4.7「画布形状与条件分支相同」）；差异在运行语义：xor 顺序取第一条命中/空条件跳过；parallel 无条件恒进入、全部命中进入、`joinTarget` 汇合记账、驳回取消其余在途分支；两侧均“其他情况”兜底。
  - 证据：引擎 `PickXor`/`PickParallelArms`/`EnterParallel`/`HoldForParallelJoin`/`ReleaseParallel` 代码核对；面板 hint 与卡片 badge 分别文案；后端 69/69（并行两条都进/只进一条/都不进/驳回取消）。
- [x] **AC-29** 并行分支缺 `joinTarget` 在发布校验即拦（原实现空值静默通过，运行期会停在分支完成、无法汇合）。
  - 证据：`WorkflowGraph.Validate` 新增“缺少 joinTarget”错误；`WorkflowOsc260922Tests.Parallel_MissingJoin_FailValidation` 通过。

### 必须保留

- GetPage `[AllowAnonymous]`
- 无 `emptyPolicy` 时停住并通知发起人
- `_startGate` 仍在
- 不新增业务表审批状态列
- 不出现并加签、减签、委托入口

## 命令

```powershell
dotnet test NewLife.Cube.Tests --filter "FullyQualifiedName~Workflow"
dotnet build NewLife.Cube/NewLife.Cube.csproj -f net10.0
pnpm --dir NewLife.Cube.ArcoVue/web exec vue-tsc --noEmit
pnpm --dir NewLife.Cube.ArcoVue/web exec vitest run src/views/workflow src/features/vtable/wfStatusMark.spec.ts
```

预期：测试全绿；构建与 vue-tsc 0 error。

## 实际结果（2026-09-22 apply 收尾门禁）

| 命令 | 结果 |
| --- | --- |
| `dotnet test …~Workflow` | 66 通过 / 0 失败（含本轮补测 7 条） |
| `dotnet build … -f net10.0` | 0 error |
| `vue-tsc --noEmit` | 0 error |
| `vitest run src/views/workflow src/features/vtable/wfStatusMark.spec.ts` | 105 通过（10 文件；本轮新增 flowgramGraph/wfNodeCard 分支卡用例 6 条） |

新增/更新测试文件：`WorkflowOsc260922Tests.cs`（补 manager/空策略/禁用改派/starterPick/field、加签驳回与同人跳过归一化）、`WorkflowMenuTests.cs`（Efficiency 权限与参数校验、BatchReject 跳过）、`useWorkflowTaskList.spec.ts`、`wfProgressFlow.spec.ts`、`useWorkflowEfficiency.spec.ts`（新增）。

## 反馈五验收（2026-09-25）

| 项 | 证据 | 结果 |
| --- | --- | --- |
| 「新建审批流程」文案 | 浏览器实测（流程属性抽屉底部） | ✅ |
| 接收人友好名 | 浏览器实测：审批人下拉显示「管理员」而非「1 ×」 | ✅ |
| 进度抽屉「查看流程」= 实例快照只读画布 | E2E「我发起的」+ 浏览器实测：状态徽标（已完成/未处理）、无删除按钮、无节点属性抽屉 | ✅ |
| 提交抽屉「查看流程」= 发布快照只读画布 | E2E：全「未处理」徽标、无删除按钮；API 验证 publishedGraphJson（部门流程 v6，1197 字符） | ✅ |
| 回归 | vue-tsc 0；vitest 120/120；dotnet test Workflow 69/69；E2E 3 passed | ✅ |
| 修复：窄视口下页签完整可见可双向切换 | E2E 632 视口 + 真实浏览器（645 面板）实测：两页签在屏内、双向切换正常、画布卸载后原内容恢复 | ✅ |
| 优化：画布拉伸填满抽屉内容区（四边等距） | 实测四边距 16/15/16/13；E2E 断言“画布底边贴近内容区底部（≤ 24px）”两抽屉均验 | ✅ |

## 反馈六验收（2026-09-25）

| 项 | 证据 | 结果 |
| --- | --- | --- |
| 深色主题画布同步（设计器 + 只读实例画布） | E2E `workflow-designer.spec.ts` 两态断言：浅色画布 rgb(242,243,245)/卡片 #fff，深色画布 rgba(255,255,255,0.08)/卡片 rgb(35,35,36)；集成浏览器实测进度抽屉画布随深浅切换 | ✅ |
| 「新建审批流程」弹层文案与字段顺序 | E2E：`.arco-modal-header` 含「新建审批流程」；form-item 顺序 流程名称 < 实体 | ✅ |
| 实体下拉用户友好名 | E2E：下拉选项含「部门」等菜单中文名（后端 FindMenu DisplayName 优先链路） | ✅ |
| 回归 | vue-tsc 0 error；vitest 120/120；E2E 全量 25 passed / 16 skipped（0 failed） | ✅ |

## 反馈七验收（2026-09-25）

| 项 | 证据 | 结果 |
| --- | --- | --- |
| 分支条件卡片条件值友好名 | E2E + 浏览器实测：条件卡片显示「类型 等于 部门」（枚举 2→部门，与筛选器一致）；新增单测（wfFilterText.spec 5 条 + wfNodeCard 1 条） | ✅ |
| 开始节点不允许删除 | 浏览器实测 start/end 均无删除按钮、业务节点保留；E2E 断言 toHaveCount(0/1)；removeNode API 拦截 + Vue 纯函数判断 | ✅ |
| 发起条件文案去歧义 | E2E：标签「发起条件」+ ? 说明符 hover 显示「…不会自动发起流程」；只读摘要改为「未设条件，任意记录均可提交审批 / 已设条件，仅条件命中的记录可提交审批」 | ✅ |
| 回归 | vue-tsc 0 error；vitest 126/126（+6）；E2E 25 passed / 16 skipped | ✅ |

## 反馈八验收（2026-09-25）

| 项 | 证据 | 结果 |
| --- | --- | --- |
| 效率页指标卡对齐工作台部件卡规范 | 浏览器实测：浅色 bg rgb(255,255,255)、border-2 rgb(229,230,235)、radius 8px、padding 10px 12px、主色图标 + 14px/500 标题、「2.0 天」32px/700；深色 bg rgb(35,35,36) 正常 | ✅ |
| 效率指标卡数字居中 + 彩色（反馈九） | 浏览器实测：三卡数字居中且分别为 rgb(52,145,250)/rgb(0,180,42)/rgb(255,125,0)（图标同色）；深色自动取 Arco 深色变体 | ✅ |
| 回归 | vue-tsc 0 error；vitest 126/126；E2E 25 passed / 16 skipped | ✅ |

## 正式验收（2026-09-25，openspec-verify）

### 三步编排摘要

| 步骤 | 结论 |
| --- | --- |
| 实现审计 | 无 P0；愿景 4 条主体达成；1 项 P1（进度「节点流」已由「时间线 + 只读画布」取代、文档未同步，由文档同步环节修正）＋ 11 项 P2 证据/覆盖类 |
| 代码审查 | 1 🔴（validateGraph 对 manager/starterPick/field 误报缺人）+ 8 🟡 + 8 🟢 |
| 文档同步 | 修正 16 处文档漂移（design §3.5/§4.2/§4.3/§4.5/§4.7/§8、tasks T3c/T5、verify AC-11/14/15、ui/process-inplace、WorkflowExclusiveTests 陈旧注释） |

### 门禁记录

| 命令 | 结果 |
| --- | --- |
| `dotnet test --filter ~Workflow`（NewLife.Cube.Tests） | 69/69 通过（含本 OSC 新增套件） |
| `dotnet build`（NewLife.Cube，Debug） | 0 error（Bin3/net10.0） |
| `pnpm exec vue-tsc --noEmit` | 0 error |
| `pnpm exec vitest run src/views/workflow src/views/crud` | 127/127（T3j 后；此前 126） |
| `pnpm exec playwright test`（全量） | 25 passed / 16 skipped / 0 failed |

### 缺口决策与补齐（用户决策：补齐 🔴 + 关键 🟡 后复盘）

| 缺口 | 等级 | 修复 | 验证 |
| --- | --- | --- | --- |
| `validateGraph` 对 `kind ∈ {manager, starterPick, field}` 误报「缺少审批人」，设计器发布会拦 | 🔴 | 按 `wfToKindOf` 分支：仅 users/roles/departments 校验 Id；manager 恒合法；starterPick 交提交侧；field 交后端 | vitest 新用例（三 kind 零错误）+ 127/127 全绿 |
| 删除分流节点后被 Vue 镜像复活（保存后回归） | 🟡 | `graphChangedFromCanvas` 收窄为「仅保留被现存节点 target 引用的旧节点」 | 127/127 回归 + E2E 25 passed |
| 「节点流」移除后的死代码（`flowNodes`/`definitionNodes`/`WfDefinitionNodes.vue`） | 🟡 | 删除无消费计算与返回、删除无引用组件 | vue-tsc 0 error |

### 遗留风险（仅记录，后续 OSC 候选）

- 🟡 占用表 409 依赖错误文案匹配 “UNIQUE”（MySQL/MariaDB 下可能退化为 500，未运行验证）；
- 🟡 效率统计以意见文案匹配排除「自动跳过/自动通过」（字面量耦合，存在误伤面）；
- 🟡 E2E 假绿/假红面（设计器 spec 硬编码定义 id；进度 spec skip 兜底）；
- 🟡 `GetDefinitions` 对任意登录用户下发全部定义（含未发布草稿）；效率接口透传原始异常消息；
- 🟢 其余轻微项（硬编码语义色残留、模块级单例 refs、N+1 角标等）见审查报告。
