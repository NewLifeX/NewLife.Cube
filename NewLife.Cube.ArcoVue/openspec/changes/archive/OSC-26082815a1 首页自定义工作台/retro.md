# OSC-26082815a1 Retro

> 复盘时间：2026-08-29T01:30:00+08:00  
> 触发：验收并复盘（用户授权夜间自动补齐缺口）

## 摘要

| 项 | 结论 |
| --- | --- |
| AC 通过率 | 愿景 1–4 / 冻结项 / 自动化门禁通过；浏览器冒烟 S.1–S.10 未测（仅记录） |
| 三步编排 | implementation-audit → code-review → doc-sync 完成 |
| 自动化门禁 | Osc260828 22；api-core / arco-vue 753；Cube+CubeNC 0 error |
| 缺口 | P1 角色空墙已修；冒烟留给人工 |

## 实际完成范围

- `/home` 自定义工作台：`UserProfile.HomeJson` > `Parameter Workbench.Role` > Admin/Member 种子  
- 14 named + Inbox；工作台栅格 w∈{2,3,4,6,8,12}、上限 16  
- 实体部件：数据看板 / 数据列表 / 数据卡片（仅 workbench）；insight 仅指标卡+迷你图表  
- 列表紧凑斑马纹、7 行视口、窗口轮播；拉取数量含「全部」(-1)  
- 角色模板空保存改为清除；交叉单测锁定用户>角色  

## 做得好

- Resolver `IsConfigured` 明确区分空串与显式空数组，与提案一致  
- surface 分支贯穿 Catalog / TryNormalize / Host / api-core  
- 验收期用单测补上「用户压角色」交叉，避免只测单层命中  

## 教训（写入 harness/lessons.md）

- 角色模板「尚未配置」预览空墙时，Save 空 `widgets:[]` 会被当成有效角色域，阻断系统种子；空保存应 Clear 或拒绝  
- 列表自动滚勿依赖 Arco Scrollbar `scrollTop`；离散窗口轮播更稳  
- insight 禁 kind 扩到 dataList/dataCard 时必须同步 proposal 目标 4 / Catalog / PUT / Host，只写 miniKanban 会漏  

## 风险与后续

- 浏览器冒烟未跑：明早优先 S.1–S.4、S.6  
- 角色页尚未预载系统种子可视化（清除后预览空壳）  
