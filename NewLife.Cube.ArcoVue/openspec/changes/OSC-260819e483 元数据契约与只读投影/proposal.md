# OSC-260819e483 — 元数据契约与只读投影

## 1. 目标愿景

在**不修改通用基类方法签名**、不翻转 Cube.Vue / NaiveUI / MVC 默认写入行为的前提下，按阶段补齐竞品分析 §8.6 第 1/2/3/4/6 项。能复用 XCode/Cube 现码的不平行造轮子。

- 目标 1（P1）：SPA GetPage 的 `required` 与校验规则对齐；`Index.Stat` 不再误转 `WhereBuilder`；字段校验仅请求头选择加入；匿名 GetPage 边界成文。
- 目标 2（P2）：`viewFilter` 复用 `AutomationFilter` 写入 `p.State`；前端维持单列 `sort`/`desc`；无法下推时本页复核（翻页不完整为已知限制）。
- 目标 3（P3）：WebAPI 新 Action（PATCH body / POST 批量 body）逐行改字段；现有 PUT / EnableSelect 语义不变；MVC 不加这些 Action。
- 目标 4（P4）：历史 Tab **解析** XCode 已写入的 `Field=old -> new`；不改 Log 写入。评论 POST 可选提及，复用 `NotificationRecord`，不改 `AddComment` 与评论表。
- 目标 5（P5）：查找展示/公式用现有 Map 与 C# 扩展属性；InsightPanel 用户配置的 ECharts option 写入 ViewProfile `ViewsJson`（`insight.chartOption`）。不新增 `autoChart`。

## 2. 为何做

报告 §8.6 将后端增强拆成多步。实施上这些步骤共享同一冻结约束（不改 `Search`/`Update`/`Fill`/`GetPage` 签名），适合**一个 OSC、任务分阶段**，避免多号交叉依赖。字段级权限（§8.6-5）仍排除在外。

曾草案分号 `OSC-2608198ccf` / `11c1` / `45c2`，按用户要求合并为本号后删除，未执行代码。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | **一个 OSC**；`tasks.md` 分 P1→P5，后者可依赖前者产物，禁止跨阶段改已冻结签名。 |
| 2 | **不改方法签名**：`DataField.Fill`、`EnableFieldValidation`（默认仍 false）、`GetPage`/`GetFields`/`OnGetFields`/`Search`/`SearchData`/`CreateWhere`/`Insert`/`Update`/`OnUpdate`/`EnableSelect`、`WriteLog(...)`、`EntityComment.AddComment(userId,userName,category,linkId,content,parentId)`。 |
| 3 | **禁止**给基类新增 `protected virtual` 强迫子类重写。优先方法体内接线与复用现有类型；仅 P3 允许 `partial` 新 Action。 |
| 4 | 不取消 GetPage `[AllowAnonymous]`；**不改** XCode 源码与 `Log` 表。P4 **解析**现有 `WriteLog(entity)` Remark，不装饰 LogProvider、不改 Remark 格式。 |
| 5 | 不含字段级角色 ACL（§8.6-5）、双向链接写回、用户脚本公式、画布工作流。 |
| 6 | Cube.Vue / NaiveUI / 外部客户端：**不传新参数 / 不调新 Action / 不加校验头则行为与今日一致**（P1 的 `required` JSON 加法除外，见 design 影响表）。 |
| 7 | P5 不新增 GetPage `projections`。公式/查找用现有扩展属性与 Map。图表 option 存 **ViewProfile.ViewsJson**，不改 Cube.xml、不改 `GetChartData` 签名、不新增 `autoChart`。 |
| 8 | **双栈**：共享 `SearchData`（`ReadOnlyEntityController2`）一处接线；`PrepareFieldsForApi`、EntityTree `Search`、评论 POST 在 WebAPI 与 CubeNC **各改一份**。PATCH 仅 WebAPI `partial`。 |
| 9 | 前端本号**不**做多列排序 UI；Cube `Pager` 已不绑定请求 `OrderBy`，不再为此清空。 |

## 4. 做什么（按阶段）

**P1 契约加固**：两栈 PrepareFieldsForApi 写 Required；仅 WebAPI Index Stat 安全转换；Insert/Update/PATCH 请求头选择加入现有校验。

**P2 筛选与排序**：复用 `AutomationFilter` + `SearchData` 的 `p.State`；两栈 EntityTree 用 `Match`。不新增 `sorts`；前端单列 `sort`/`desc`。可给 `TryBuildWhere` 补 `notcontains`。

**P3 PATCH/批量**：WebAPI 薄 Action；POST/PATCH + JSON body（不要 GET）；白名单逐行 `Valid`+`OnUpdate`。

**P4 审计与提及**：前端解析现有 Log.Remark；评论 POST 可选 `mentionUserIds` → `NotificationRecord`。

**P5 只读与图表**：文档 §8.2.2 / §8.2.3 / §8.2.6；Insight 配图持久化到 NamedView.insight。不新增 projections / autoChart。

## 5. 不做什么

- 不改 PUT 整单 `CopyFrom` 语义、不把校验默认打开给全体 API。
- 不把全部 `override Search` 一次改完。走 `FindAll(..., p)` 的在 `SearchData` 自动吃 `viewFilter`；仅两栈 EntityTree 补 `Match`。无法下推时不假装跨页完整。
- 不改 XCode 库与 Log 表；不装饰 LogProvider；不新建变更表/MentionsJson；不改 `AddComment` 六参数。
- 不新增 `EntityListFilter` / `sorts` / GetPage `projections`。
- 不做 NaiveUI/Cube.Vue 功能页（除非 JSON 加法自然生效）。
- 不做 §8.6-5 字段权限、对外表单、甘特依赖写回。

## 6. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-0009 / OSC-0015 / OSC-0008 / OSC-0002 | GetPage、ViewFilter、历史评论、EntityComment |
| OSC-260815fa86 | P3 走 `OnUpdate`→`entity.Update()`，自动化由已有 `AutomationPersistence` 入队；子类 override OnUpdate 仍生效 |
| 迁移方案 §8.2.2 / §8.2.3 / §8.2.6 | P5 修订洞察允许一张用户 option；只读公式例外 |
| OSC-0018 | 无代码依赖 |

## 7. 测试范围

| 类型 | 是否做 | 说明 |
|------|--------|------|
| XUnit | 是 | 每阶段帮助类 + 关键接线；Fill/PUT/EnableSelect 回归 |
| Vitest | 是 | isFieldRequired、viewFilter、PATCH 提交体、**logRemarkDiff 解析** |
| 构建 | 是 | NewLife.Cube + NewLife.CubeNC + arco-vue |
| 手工 | 是 | 每阶段一条冒烟（见 verify） |
| Cube.Vue/NaiveUI 改代码 | 否 | 回归：无新头/新参时写入与列表 |

硬门禁：本 OSC 新增单测全过 + 构建无错误。阶段未完成不得宣称该阶段 AC 通过。

## 8. 成功标准

- [ ] P1–P5 在 verify 中均有可判定 AC，且签名冻结表未被破坏。
- [ ] 无新查询参数时 GetList/PUT 与今日一致（P1 `required` 字段除外）。
- [ ] ArcoVue：可下推筛选翻页正确；PATCH/批量；历史 diff；评论提及站内信；Insight 配图写入 ViewProfile 刷新仍在。
- [ ] 文档：核心接口架构、功能清单、迁移方案 §8.2.6、web/README、竞品分析 §8.6 回写本号。
