# Status
- id: OSC-260830a1b2
- state: Done
- updated: 2026-08-30T19:45:00+08:00
- approvedBy: openspec-approve
- trigger: "请按照本项目 OpenSpec 规范，批准并执行变更 a1b2。"
- checklist: passed
- note: |
  批准通过（依赖 OSC-0016/0015/e483/0e9e/15a1 均 Done）。执行主要完成：
  - 后端：AutomationFilter 白名单/startsWith/复杂度 400 + 时间窗辅助；SearchData（白名单/时间窗/X-Cube-Filter-Narrowed/CORS Expose）；树控制器双份同步；WidgetQueryService（白名单/filterNarrowed）；CubeSetting.FilterWindowDays（默认 30）。
  - 前端：退役 SearchDrawer/SearchFieldInput；查询簇（Q+查询/自定义/▾）；FilterBuilder 标题「查询」+ startsWith；QueriesJson v2（q+filter，兼容 v1 迁 Q）；filterFields 与 search∪list 对齐；时间窗 Alert。
  - 测试：后端 Osc260830A1b2Tests 4/4；前端 754 passed + vue-tsc EXIT=0 + vite build 通过。
  已知项：
  - CubeNC 构建存在**预先**的 `EntityController2.cs ImportFile` 重复定义（CS0111），与本号无关（工作区未提交状态），需另行修复后复跑 T15。
  - 文档 T19（竞品报告）与 T20 的部分（字段组件规范/功能清单 SPA-7/核心接口架构）未完成。
  - 进入验收（Validating）。会话小任务已补录（T21–T25：日期时间分组、未命名查询持久化分层、数据更新重算分组/填色、重置刷新、UI 微调），见 tasks.md。
  - 验收发现缺口（前端时间窗提示链断裂 / AC-15 enableKey=false 按钮组隐藏 / 时间窗与透明下推单测缺失 / 文档回写不全 / 若干 P2），经用户决策「补齐全部 P1+P2」，状态回写 Implementing，追加任务 T26–T34。
  - 缺口补齐完成并复跑验收门禁：后端 9/9 + 0 error；前端 770/770 + vue-tsc EXIT=0 + vite build 0 error；AC-01~19 通过或标记残余（见 verify.md R1–R6）。checklist passed，状态回 Validating。
  - 复盘完成（retro.md + harness/lessons.md），状态置 Done，目录归档至 `archive/`。
