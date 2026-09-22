# OSC-26090347f1 Retro — Cube OA 审批流程引擎

> 复盘时间：2026-09-22T08:40:00+08:00  
> 触发：按照本项目 OpenSpec 规范，验收和复盘 47f1 变更。

## 摘要

| 项 | 结论 |
| --- | --- |
| 目标愿景 | 1–4 达成。跨进程排他（G-01）与宿主 HTTP 冒烟（G-07/T11）按仅记录放行 |
| 三步编排 | implementation-audit → code-review → doc-sync 完成。🔴 0 |
| 自动化门禁 | 后端 Workflow+Osc260815 62/62（并行两轮）；web workflow 相关 vitest 84/84；vue-tsc 0 error；`dotnet build` NewLife.Cube net10.0 0 error |
| 缺口处置 | G-01 / G-05 / G-07 / 设计器不再新建依次签 = 仅记录。验收中修了菜单单测并行打空库 |
| 归档 | 状态 → Done；目录移至 `changes/archive/` |

## 实际完成范围

- 后端在 `NewLife.Cube/Workflow`：定义/实例/主体/任务/意见，状态机（或签/会签/依次签、加签、转办、知会、回退、撤回、XOR、超时），写锁拦截器，GetPage/GetList/GetDetail 覆盖，`WorkflowController`。
- ArcoVue：列表提交与进度、FlowGram 固定布局设计器、待办/已办/我发起、顶栏 `audit` 图标（无旁字）、审批几何徽标、常用语弹窗。
- 设计器新建审批只保留或签/会签；引擎仍执行历史 `sequence` 图。
- 条件分支画布为「满足 / 不满足」两支。

## 做得好

- 运行时与设计器分开：浏览器只读写 GraphJson，状态机在 C#。
- 收尾门禁修过后加签不激活、sqlite 超时比较恒 false 两个真实缺陷，并补了矩阵测试。
- 会话增量（T12–T14）落在 tasks，而不是只留在对话里。

## 偏离与原因

| # | 偏离 | 原因 |
| --- | --- | --- |
| D1 | 后端从独立 NuGet / 皮肤仓改到 `NewLife.Cube/Workflow` | Amd-2。MVC 不 Link |
| D2 | 设计器不再提供依次签 | 会话产品决定。引擎与历史图保留，AC-06 仍有单测 |
| D3 | AC-01/AC-16、T11 未做 HTTP 冒烟 | CubeDemo 宿主构建被无关 MSB3552 挡住；WebAPI 引用核心库后 Meta 恒为 enabled |
| D4 | 跨进程双提仍可能双插 | 只有进程内锁。唯一索引留后续 OSC（G-01） |
| D5 | 意见附件 UI 未做 | V1 裁剪（G-05） |

## 教训（已写入 harness/lessons.md）

- `Menu.Meta.ConnName` 不能在别的用例已经访问过 Menu 之后再改：会话绑在第一次打开的库上，并行时断言会打到空文件。菜单播种单测应使用当前连接，写锁注册用 `Register(false)` 避开播种。
- 依次签这类能力下线要同时改设计器入口和文档；引擎兼容历史图，避免旧 GraphJson 无法打开。

## 遗留与后续

- G-01：`(TypePath, EntityKey)` 在途唯一约束。
- G-07：CubeDemo 上 Meta、发布、会签、写锁 PATCH 的 HTTP 冒烟。
- G-05：意见附件 UI。
