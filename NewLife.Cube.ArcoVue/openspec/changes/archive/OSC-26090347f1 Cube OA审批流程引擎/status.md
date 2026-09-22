# Status
- id: OSC-26090347f1
- state: Done
- updated: 2026-09-22T08:40:00+08:00
- approvedBy: openspec-approve
- trigger: "请按照本项目OpenSpec 规范，批准并执行 47f1 变更。"
- checklist: passed
- note: |
    批准检查表全部通过后直接进入执行。
    依赖 OSC-260815fa86/0015/0008/0003 已 Done；OSC-2608273d95 仍 Draft 但不阻塞（本号不替代行权）。
    范围单一（自研 OA 状态机）；proposal 愿景 4 条 + 不做什么 + 测试范围齐全；
    design 含技术方案/文档影响/测试设计；tasks 可勾选；ui/ 存在；ID 为 OSC-YYMMDDxxxx。
    **Amd-1（2026-09-03）→ Amd-2（定稿）**：后端先后从「独立 NewLife.Cube.Workflow NuGet」改为「并入 NewLife.Cube.ArcoVue 皮肤仓」，**Amd-2 最终并入 WebAPI 核心库 `NewLife.Cube/Workflow`**（命名空间 NewLife.Cube.Workflow(.Entity) 不变）；NewLife.CubeNC/CubeDemoNC 不 Link，MVC 版不受影响。proposal 目标1/决策6/9、design §1/§3/§6.1/§11/§12、tasks T1、verify 命令已同步改写。
    **执行进度**：Amd-2 迁移已落地并提交；T1 数据模型完成；T2 接收人公共化完成（回归 14/14）；
    T3 状态机引擎核心已实现（WorkflowEngineTests 5/5 绿：or/and/sequence/403/409/撤回）；
    T4 写锁拦截器+超时 Cron 已实现（WorkflowLockTests 绿：full/Scope/解锁）。全部 Workflow 测试 6/6。
    编译：NewLife.Cube/ArcoVue/CubeDemo/NewLife.Cube.Tests 0 error；CubeDemoNC 有**预存在** EntityController ImportFile 重复（与本号无关）。
    下一步 T5 核心挂钩（GetPage/GetList workflow 块 + Meta）→ T6 WorkflowController → T8 UI → T9 文档 → T10 全矩阵单测。
    **T5-T10 全部完成**：T5 挂钩（GetPage/GetList workflow 块 + Meta）、T6 WorkflowController 全 API、T7 前端路由占位、
    T8 前端（api-core createWorkflowApi 51 绿 → ArcoVue 列表提交审批/流程进度 + 提交抽屉/进度面板/接收人选择/待办·已办·我发起；
    **设计器切换 FlowGram.AI**（fixed-layout-editor + React 桥 Vue，graph 双向映射 spec 6 绿，vue-tsc 0 error，vite build 通过，chunk ~895KB 懒加载）；vitest 全仓 830 绿）。
    T9 文档 4 处（功能清单 WF-1..4 / 核心接口架构 / FAQ §41.9 / 迁移方案 §8.5.5）。
    T10 补测：WorkflowMatrixTests 7 例（会签 quorum、or/sequence 驳回、回退重开、前加签、XOR 发布校验、空候选人）
    + WorkflowExclusiveTests 2 例——**并发双提实测复现 Start 双插 race**，WorkflowEngine.Start 增加进程内 _startGate 互斥修复
    （跨进程需 DB 唯一约束，实现审计关注项）；Workflow*+Osc260815 回归 33/33；dotnet build 0 error。
    **T11 阻塞（待人工）**：CubeDemo 宿主本机 MSB3552（NewLife.Cube.Vue/React wwwroot 嵌入在 SDK 10.0.400 回归，清除 obj 复现，
    与本号无关，CubeDemo 引用全部主题），HTTP 冒烟无法自动执行 → 详细清单见 verify.md「T11 手工冒烟」。
    状态：实现期全部代码/测试/文档工作完成并提交；待宿主就绪人工冒烟 → 验收。
    **收尾门禁（T12，2026-09-03）**：代码审查 5🔴 + 实现审计 P1/P2 合并修复批次已提交——
    后加签激活、多前加签等齐+复活保护、Rollback allowRollback 403、AllowAddSign/AllowRollback 默认 true、
    超时仅可见任务 + 修复 sqlite DateTime.MinValue 比较致超时永不下发的真实缺陷、
    Start Detail 权限、Patch typePath 移 query（多段断裂）、前端 before 透传；新增矩阵测试 4 例。
    回归 Workflow*+Osc260815 37/37；web vue-tsc 0 error、workflow spec 32 绿、api-core 3 绿。会话小任务已补录（tasks T12）。
    无法闭环项已记录：G-01 跨进程唯一约束（后续 OSC）、G-04 候选可见语义（待确认）、G-05 附件裁剪、
    G-06 Meta 恒 true（随 T11）、G-07 HTTP 冒烟（T11）、G-08/G-11 后续批次。收尾门禁无 🔴、无实现缺口。
    **会话小任务补录（2026-09-03）**：宿主冒烟发现 OA 独立页（Designer/Todo/Done/Started）菜单未播种时
    URL 直达白屏（路由由菜单动态注册）→ `router/index.ts` 静态注册 oaLeafRoutes 修复（tasks T8 补录子条目）。
    **实现审计补齐（2026-09-10）**：侧栏菜单已由 `WorkflowHost.EnsureMenus` 播种（一级「流程审批」+五叶子）；
    G-04 或签候选待办可见已修；G-08 多定义 StartFilter 任一命中；G-11 Claim/Transfer/Cc/超时 reject 单测；
    顶栏审批槽+角标、列表 `__wfStatus` 列、RecordDrawer 审批 Tab。
    仍无法闭环：G-01 跨进程唯一约束、G-05 附件、G-06/AC Meta 文档对齐、G-07 T11 冒烟、常用语管理 UI。
    **Amd-3（2026-09-19）**：对照飞书审批 + 当前实现，补充流程设计/审批界面（design §13、ui/process-and-approval.md、IA §3）。
    产品边界写死：实体表单=单据，GraphJson=流程；不复制飞书表单设计器。
    **Amd-3 实现（2026-09-19）**：实现审计后补齐 D1–D10。引擎补 `allowTransfer`（缺省 true，false 转办 403）；实例详情下发 `graphSnapshot` 供节点进度组装。
    设计器：顶栏写锁/发起条件/常用语；XOR 条件+默认分支；节点卡片徽章；节点下方「+」。
    办理：进度按节点+候选人；更多含回退；待办宽屏左右分栏；摘要 Markdown；审批 Tab 内嵌进度。
    仍无法闭环：G-01 跨进程唯一约束、G-07 T11 宿主冒烟。
    **验收（2026-09-22）**：三步编排完成，🔴 0。Workflow+Osc260815 62/62，workflow vitest 84/84，vue-tsc 与 net10.0 构建 0 error。AC-01/AC-16 与 T11 仅记录。checklist passed。会话小任务已补录（T14）。
    **复盘**：状态 Done，目录归档。
    **归档后清尾补录（2026-09-22）**：工作区残余的命名卡片「图标 + Admin 徽标」对齐样式此前未入库，补录 T15 并随装置产物提交；纯样式，无功能影响。
