# OSC-260903e2a4 Retro — 实体部件查询条件与宿主联动

> 复盘时间：2026-09-22T12:00:00+08:00
> 触发：验收和复盘 e2a4 变更（验收发现缺口，用户授权「全部补齐」后复验放行）

## 摘要

| 项 | 结论 |
| --- | --- |
| 目标愿景 | 1–5 全部达成；AC 10 项全过（实现审计/代码审查/doc-sync 三步复核） |
| 三步编排 | implementation-audit → code-review → doc-sync 完成（验收轮发现 P1×2 + P2×3，补齐后复验关闭） |
| 自动化门禁 | 复验：XUnit 44/44（e2a4 16 + 回归 28）；api-core 54/54；arco-vue 889/889；`dotnet build` 0 error；`vue-tsc -b + vite build` 退出码 0 |
| 缺口处置 | G1–G5 全部补齐（用户决策） |
| 归档 | 状态 → Done；目录移至 `changes/archive/` |

## 实际完成范围

- 后端保存端：`DashboardJson.TryNormalize` 增 `hostTypePath` 重载 + `ValidateWidgetFilter`（logic/复杂度 ≤10、any ≤5、源实体 search∪list 白名单、14 操作符、`$host` 单键/表面/宿主字段、非 `$host` 对象拒绝）；补齐后增加：非对象 `extraFilter` 与 conditions 元素非对象 → 保存即 400、纯静态条件不强制解析宿主实体、hostTypePath 归一。
- 后端取数：`WidgetQueryService` `$host` 预解析（从 `req.HostFilter` 等值条件取值、克隆不写回、失败跳过、`hostFilterApplied` 落点）；`GetAllowedNames` 提取供两端复用；`FindEntityType` 改名 internal 共享。
- 控制器：`CubeController` ViewProfile/Template 传 `typePath`；`WorkbenchController` 各域传 null（工作台禁 `$host`）。
- api-core：`HOST_REF_KEY` / `isHostRefValue` / `hostRefField` / `hasHostRefFilter`（补齐后拒绝空串/空白串，与后端一致）；dist 重建。
- ArcoVue：`FilterBuilderPopover` 可选 `hostFields`/`showSaveView`（列表页零改动）；配置抽屉「查询条件」区（draft 还原/保存、save() 合并保留 extraFilter）；`loadEntityFilterFields`（补齐后 AutomationMeta 仅富化不扩充）；`hostFilterFields` 表面注入；未联动判定扩展。
- 测试：XUnit `Osc260903WidgetQueryTests` 16 例；api-core widget.spec；arco-vue `widget.spec`/`listFieldMeta.filter.spec`/`viewProfile.spec`。
- 文档：`功能清单.md` DASH-1/2/4、`核心接口架构.md`、`前端对接指南.md`、`web/README.md`（补齐后限定工作台、补「条件跳过」语义与复杂度数字）。

## 做得好

- 协议零扩展：复用 `query.extraFilter` + `FilterBuilderPopover`，未新增持久化字段与第二套条件 UI；`AutomationFilter` 保持不动（自动化/工作流零影响）。
- 三步编排 + 门禁重跑抓到 2 个 P1（候选超集、畸形形状放行）与 3 个 P2，未流入归档。
- `$host` 解析语义与 `linkFilter` 未联动角标统一（`hostFilterApplied`），失败 fail-closed（非法对象 400、空白拒绝）。
- 缺口补齐同轮闭环并复验，全程证据落 `verify.md`（命令 + 结果 + 文件位置）。

## 偏离与原因（记录）

| # | 偏离 | 原因 |
| --- | --- | --- |
| D1 | design §7.1 XUnit 构成（13 例）执行时为 13 例；验收补 3 例至 16 例 | 原计划的时间窗组合条目语义不成立于当前 fixture（$host 仅等值解析）；改为「group/$host 时间字段（等值生效、after 跳过）」实义覆盖 |
| D2 | design §3.2「无查询条件（显示源实体全部匹配数据）」文案未按原文实现 | 实现用计数按钮 + 固定提示；纯文案差异，记录不追溯 |
| D3 | design §6.1 列 `WorkbenchController` 为改动文件，实际未改 | 5 参重载已委托 null，语义满足 |
| D4 | wwwroot 产物未纳入本号复盘提交 | 工作区 wwwroot 由并行变更 OSC-26090347f1 的构建更新，已排除无关 WIP；本号复验构建已通过（当前 wwwroot 与源码一致） |

## 教训（已写入 harness/lessons.md）

- 部件条件候选 = search∪list 白名单，附源合并只可「按名富化」，严禁整集合并入（AutomationMeta kind=all 是超集）。
- 保存端校验只写「合法分支」不够，畸形形状（非对象/元素非对象）必须显式 400；否则查询期才炸。
- `$host` 只认宿主筛选的等值（eq/空）条件；非等值（after/gt）不解析 → 条件跳过，不是「按时间过滤」。
- 测试的 `$host` 字段要与断言语义一致（引用字段、宿主条件、期望过滤三者对齐）。
- git worktree 快照缺 gitignore 内容；历史提交的包名可能落后（@cube→@newlifex）；门禁以当前树为准。
- 目录联接（junction）补齐快照依赖时，清除须先删联接再 `worktree remove`，避免误删主仓 node_modules。

## 遗留与后续

- 手工冒烟 10 项（verify.md 清单）待 CubeDemo 宿主环境：跨实体 `$host` 收敛/未联动角标、工作台 400 拒绝、编辑保留、越权 400 等。
- CubeNC 兼容层 ViewProfile 保存路径未接入 `DashboardJson.TryNormalize`（既有差异，非本号范围）；建议后续另号补齐。
- 轻微项（未随本号补齐）：`showSaveView` 改 computed 响应式、`FilterBuilderPopover` 缩进整块重排、既有 private 方法补 `<param>`。
