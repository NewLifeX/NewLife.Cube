# Status
- id: OSC-260922201a
- state: Done
- updated: 2026-09-25T18:05:00+08:00
- approvedBy: openspec-approve
- trigger: "按照 OpenSpec 规范，批准并执行 201a 变更 。"
- checklist: passed
- note: |
    批准检查通过。目标愿景原为 5 条，已收成 4 条（设计器选人与分支合并为一条），依赖 OSC-26090347f1 已归档。
    执行中。已落地：图校验（六种选人、level、办理节点禁依次、emptyPolicy）、引擎展开与空人自动通过、办理驳回 400、同一人跳过、在途占用表、效率菜单叶子。单测 WorkflowOsc260922 4 通过。
    未完成：设计器界面、发起预览、就地审批、进度、效率聚合接口与页面、意见附件、收尾门禁。下一步继续 T3–T6。
    续跑：T3–T7 全部落地并勾选（六选人设计器、发起步骤预览 picks、就地表单/批量驳回跳过、进度分支过滤与附件、效率页与六维聚合）。
    收尾门禁：实现审计补齐 T2/T6 缺口（建表夹具、manager/禁用改派/picks/field 用例、api-core 效率类型导出）；代码审查无 🔴，🟡（NodeId 加签后缀未归一化）已修并补测；🟡 效率聚合大 IN 分块/租户过滤记为后续 OSC 候选，🟢 不阻塞。会话小任务已补录。
    门禁结果：后端 66/66、前端 91/91、dotnet build 与 vue-tsc 0 error；下一步可 `验收 OSC-260922201a`（verify.md 已补 AC 证据）。
    反馈增强（T3d 会话补录）：条件分支改画布卡片——每条件一张卡（点卡编辑条件/则去/删除）、画布「＋ 添加条件」插在「其他情况」前、抽屉移除「添加条件」；块级 filter/target 通道保证增删分支不错位。门禁：前端 105/105、vue-tsc 0 error；浏览器全链路实测通过。
    反馈增强二（T3e 会话补录）：分叉上方留白修复（覆盖内置 blockIcon 占位）；删除按钮移到卡片标题栏；条件卡片支持自定义名称（cases[].name，实时同步+持久化）；条件编辑内嵌抽屉（FilterBuilderPanel 弹层/内嵌双模式，弹层回归通过）；移除「则去」选择器（目标由分支内节点决定）。门禁：前端 106/106、vue-tsc 0 error；浏览器全链路实测通过。
    反馈增强三（T3f 会话补录）：抽屉 Footer（分支条件重置/应用、流程属性保存草稿/发布）；单行条件布局 + 说明上移；流程属性控件分组卡片、流程名称置顶、发起条件内嵌；常用语就地编辑/删除/新增即时落库。修复：常用语保存「Value 长度限制 200 字符」——ToJsonString 将非 ASCII 转义为 \uXXXX 超长，改用 Parameter.LongValue + 纯字符串数组（读取兼容 Value 旧数据），20 条长文本通过。门禁：前端 121/121、vue-tsc 0 error；后端发布重启（Bin/CubeDemo）；浏览器全链路实测通过。
    再审计：进度页办理节点仍显示驳回；效率汇总未随部门/用户筛选收窄；按年全 0 仍画表。已补齐并加测。
    2026-09-25：画布手感写入 design §4.6 / T3b，引擎规则不动。节点属性改为右侧无遮罩抽屉，画布铺满。菜单短名、卡片两行、条件卡片标注仍待做。
    同日执行 T3b / T3c：菜单短名、卡片两行、条件卡片标注，以及 `oa.parallel`（命中分支同时进入，全部完成后汇合，任一审批驳回取消其余在途）。`oa.xor` 仍只走一条。    反馈增强四（T3d~T3f 后续补录）：xor/parallel 复查（配置同形系设计使然，补 joinTarget 缺省发布校验+单测）；隐藏并行分支菜单（`ENABLE_PARALLEL_BRANCH=false`，代码与引擎保留，存量并行节点兼容）。
    反馈增强五（2026-09-25 补录）：流程属性「新建审批流程」文案；接收人友好名（已选 id 合并显示名，修「1 ×」）；进度抽屉（我发起的/已办/待办）与实体列表「提交」抽屉的「查看流程」改用快照只读画布 `WfInstanceGraph`（FlowGram readonly + 节点状态徽标 已完成/当前/未处理 替代删除按钮，点击节点不开属性抽屉；提交抽屉用发布快照 publishedGraphJson，后端 ToDefView 下发该字段）。
    E2E 基建：`auth.setup.ts` 集成算术验证码模板识别（`e2e/helpers/captcha.ts`，8×8 内嵌字体滑窗匹配，一次通过）；新增 `workflow-progress-graph.spec.ts`（进度抽屉实例快照 + 提交抽屉发布快照，vtable 行按钮坐标点击）。
    门禁：vue-tsc 0 error；vitest 120/120；dotnet test Workflow 69/69；E2E 3 passed（setup+2）。后端发布重启（Bin/CubeDemo）。
    反馈五修复（2026-09-25 补录）：修复“进度抽屉只剩查看流程页签”回归——① 回退上轮顺手添加的抽屉宽度机制（WorkflowProgressPanel 恢复 560、SubmitApprovalDrawer 恢复 520、DefaultList 移除 :width 传参），避免宽于窄屏视口时抽屉左移出屏致页签不可见；② WfInstanceGraph 容器加 position:relative + isolation:isolate + z-index:0，约束 FlowGram 绝对定位浮层不再溢出容器遮挡页签栏；③ 两抽屉改为仅激活「查看流程」时才挂载画布（v-if activeTab）。E2E 补窄视口（632）与双向切换断言，连续 3 轮 3 passed；真实浏览器（645 面板）实测两页签在屏内、双向切换正常。
    反馈五优化（2026-09-25）：查看流程画布高度方案修订——从“按节点内容自适应裁剪”改为**拉伸填满抽屉内容区**（用户要求四边等距）：tabs 撑满 body → content flex:1 → content-list/content-item/pane height:100%（缺一不可）→ 画布 height:100%（min 240），移除内容自适应与 height prop；实测四边距 16/15/16/13。E2E 断言改为“画布底边贴近内容区底部”（≤ 24px）；三连跑稳定。
    反馈六（2026-09-25 补录）：① 深色主题适配——FlowGram 画布底色 `--g-editor-background`（原硬编码 #f2f3f5）由 `.wf-flowcanvas` 接管为 `var(--color-fill-2)`；FlowGramDesigner.tsx 节点卡/分支卡/插入菜单硬编码色改语义变量（--color-bg-2/--color-text-1|2|3/--color-border-1|2/--color-bg-popup，保留状态色 STATE_STYLE、+ 按钮、TYPE_COLOR fallback）。② 「新建审批流程」弹层：标题改名；字段顺序对调（流程名称在前、实体在后）；实体下拉友好名——后端 `AutomationController.Entities` 改 `AutomationAuth.FindMenu(typePath)?.DisplayName` 菜单中文名优先（回退实体 DisplayName→类名），前端 entityOptions 用 displayName 优先。E2E 新增 `workflow-designer.spec.ts`（深色两态精确色值 + 弹层标题/顺序/「部门」中文名断言；注：页面存在隐藏的「添加审批节点」modal，定位需按标题 filter）。门禁：vue-tsc 0 error；vitest 120/120；E2E 全量 25 passed / 16 skipped（0 failed）；后端发布重启（Bin/CubeDemo）。
    反馈七（2026-09-25 补录）：① 分支条件卡片条件值取用户友好名——wfFilterText 加 `FilterValueLabel` 回调（viewFilterSummary 第三参），`filterValueLabelResolver` 按字段 dataSource 映射（「类型 等于 2」→「类型 等于 部门」，与筛选器一致），branchInfosOf 透传，设计器与详情页筛选摘要接入；② 开始节点不允许删除——卡片删除按钮对 start/oa.start 隐藏（与 end 一致），FlowGramAPI.removeNode 对 start/end 拦截，Vue 纯函数 removeNode 既有 oa.start/oa.end 判断保留；③ 发布前确认开始条件语义=提交准入门槛（非自动发起），「发起条件」标签加 ? 说明符（tooltip：仅作提交准入校验…不会自动发起流程），只读摘要改为「未设条件，任意记录均可提交审批 / 已设条件，仅条件命中的记录可提交审批」。E2E designer spec 补断言。门禁：vue-tsc 0 error；vitest 126/126；E2E 25 passed。
    反馈八（2026-09-25 补录）：效率页指标卡对齐首页工作台部件卡规范（bg-2 + border-2 + radius var(--cube-radius-md,8px) + padding 10/12 + 主色图标+标题行 14px/500 + 数值 32px/700）——三卡图标 timer/check/remind；浅色实测 rgb(255,255,255)/8px/10px 12px，深色 rgb(35,35,36) 正常。门禁：vue-tsc 0 error；vitest 126/126；E2E 25 passed。
    反馈九（2026-09-25 补录）：效率指标卡数字居中 + 语义彩色——`--eff-accent` CSS 变量按卡注入（平均耗时=主色蓝、完成率=成功绿 `--success-6`、超 48h=警告橙 `--warning-6`），图标与数字同色；实测浅色 rgb(52,145,250)/rgb(0,180,42)/rgb(255,125,0) 居中，深色自动取 Arco 深色变体（ rgb(39,195,70)/rgb(255,150,38) ）。门禁：vue-tsc 0 error；vitest 126/126；E2E 25 passed。会话小任务已补录（反馈六至九）。
    验收（2026-09-25）：三步编排完成（实现审计无 P0、代码审查 1🔴+8🟡+8🟢、文档同步修正 16 处漂移）；门禁：dotnet build 0 error、Workflow 单测 69/69。发现 🔴：`validateGraph` 对 manager/starterPick/field 节点误报缺人导致发布被拦。用户决策「补齐 🔴 + 关键 🟡 后复盘」——缺口已按任务粒度追加 T3j（不勾选），state 回写 Implementing，等待补齐后再次验收。
    验收补齐与复验（2026-09-25）：T3j 三项已补齐——① validateGraph 按 kind 分支（manager 恒合法等）+ vitest 三 kind 新用例；② graphChangedFromCanvas 收窄引用条件（分流删除不再复活）；⓷ 死代码清理（flowNodes/definitionNodes/WfDefinitionNodes.vue）。复验门禁：vue-tsc 0 error；vitest 127/127（+1）；E2E 全量 25 passed / 16 skipped（0 failed）；后端 Workflow 69/69。遗留 🟡 风险已入 verify.md（后续 OSC 候选）。checklist: passed，可复盘。
    复盘（2026-09-25）：retro.md 完成（结果摘要/实际范围/好与改进/偏差）；harness/lessons.md 追加条目「OSC-260922201a — 2026-09-25」（含索引）。先置 Done 后整体搬迁至 archive/。