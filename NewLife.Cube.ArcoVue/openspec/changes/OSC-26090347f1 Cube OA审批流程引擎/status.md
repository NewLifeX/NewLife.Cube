# Status
- id: OSC-26090347f1
- state: Implementing
- updated: 2026-09-03T18:48:20+08:00
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
