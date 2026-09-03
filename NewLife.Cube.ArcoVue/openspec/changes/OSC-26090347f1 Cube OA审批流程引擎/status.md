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
    **2026-09-03 Amd-1（执行期架构修订）**：后端从「独立 NewLife.Cube.Workflow NuGet 模块」改为
    「并入 NewLife.Cube.ArcoVue/Workflow（WebAPI 版专属），命名空间 NewLife.Cube.Workflow(.Entity) 不变」；
    CubeDemoNC（MVC）不引用、不受影响。proposal 目标1/决策6/9、design §1/§3/§6.1/§11/§12、tasks T1、verify 命令已同步改写。
