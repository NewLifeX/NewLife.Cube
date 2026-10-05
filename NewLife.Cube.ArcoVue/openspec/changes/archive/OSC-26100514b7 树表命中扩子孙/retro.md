# OSC-26100514b7 Retro — 树表命中扩子孙

## 摘要

| 项 | 结论 |
| --- | --- |
| 目标愿景 | 达成：树表 + 树实体时命中行扩全部子孙；TotalCount=命中数；Data 可大于 pageSize；封顶默认 100000 |
| 缺口处置 | 部门树表手工冒烟 **仅记录**；P2（控制器集成测 / 缓存阈值 / 取子 catch）仅记录 |
| 归档 | 已归档（本次提交） |

## 实际完成范围

- T1 后端：`TreeDescendantExpand`（缓存索引 / 分层 BFS、去重、环、`canView`、默认 100000）；`Index` 在 SearchData 后挂钩；`EntityTreeController` 短路；`GetPage.setting.isTreeEntity`；`CubeSetting.TreeExpandMaxRows`（筛选时间窗前天，默认 100000）。
- T2 前端：树表 GetList `viewKind=tree`；`listRequestSignature` 纳入；树表跳过会裁掉扩入行的 `matchesViewFilter`；`canCreateViewKind('tree')` 优先 `pageSetting.isTreeEntity`。
- T3：XUnit 7、Vitest 签名/门禁、迁移方案 §7.1 / 树表行、功能清单 SPA-18。
- **未纳入本号提交**：地图散点增强（查询卡片/添加弹层等 WIP）、洞察面板空态隐藏（已另 commit `8e056f39`）。

## 测试与构建

- XUnit `TreeDescendantExpand` 7/7；Vitest 58；`vue-tsc -b` 0 错误；`dotnet build` net10.0 0 错误。
- 部门树表浏览器冒烟：仅记录。

## 做得好

1. 扩子孙做成与 Search 解耦的纯函数，Index 一层挂钩，EntityTree 虚方法短路，避免菜单全树二次遍历。
2. TotalCount 仍表示命中数、Data 可大于 pageSize，分页语义对普通 table 零影响。
3. 前端签名与 `viewKind` 同进同出，切换树/表不会误复用对方结果集。

## 偏离与教训

1. **缓存阈值写死 10000**：design 写 `Meta.Session.Count < MaxCacheCount`，实现用 `CacheCountThreshold = 10_000`；量级接近但不是同一符号，后续若改 MaxCacheCount 需对齐。
2. **验收冒烟依赖登录环境**：与 e6ee 相同，组合指令「验收和复盘」对浏览器 AC 按仅记录放行，须在 lessons 标明补做入口。
3. **工作区混 WIP**：地图添加弹层、lastQuery 清空、d7f4 二轮 lessons 与本号并行；归档提交按白名单，忌 `git add .`。

## 风险与后续

- 有登录环境后补部门树表：Network `viewKind=tree`、命中父可见全部下级、切回表格无该参数。
- 宽树高命中靠 100000（设置可夹到 1e6）与行权收敛；大表无缓存时 BFS 往返次数 = 深度。
- 不做懒加载「点开再取 children」（proposal 不做什么）。
