# OSC-260819e483 Tasks

状态：Draft。默认本文件只规划。实施时按 **P1 → P5** 顺序。每阶段结束：该阶段新增单测全过 + 相关项目构建无错误。

硬约束：不改 design 冻结表中的方法签名与 `EnableFieldValidation` 默认值。**先复用现码，禁止平行造轮子。** 双栈对照 design「双栈文件地图」。

---

## 阶段 P1 — GetPage 契约加固

- [x] P1.1 WebAPI `ReadOnlyEntityController.PrepareFieldsForApi` 循环内按矩阵写 `Required`（不改 `Fill`；不必新类型）
- [x] P1.2 CubeNC `ReadOnlyEntityController.PrepareFieldsForApi` **同一矩阵**（GetPage 两栈对齐）
- [x] P1.3 WebAPI `Index.Stat = p.State as TEntity`（WhereBuilder 时 Stat=null、不抛）。MVC Index 不改
- [x] P1.4 WebAPI Insert/Update：`if (EnableFieldValidation || 请求头)` 仍调用现有私有 `ValidateEntityFields`（禁止复制校验类）。CubeNC 无此校验，不改
- [x] P1.5 XUnit：Required / Stat / 头开/关；`DataFieldTests` Fill 后 Required=false；布尔 false 不算缺必填
- [x] P1.6 ArcoVue `isFieldRequired` 认 `nullable===false` 或 `required===true`；POST/PUT/PATCH 附带 `X-Cube-Field-Validation`；读请求不加；补 spec
- [x] P1.7 文档：匿名 GetPage 不得下发权限表达式（并入收尾文档同步）
- [x] P1.8 跑测 + 冒烟：NOT NULL 列 `required:true`；有数据权限列表不 500；无头 PUT 与今日一致（自动化冒烟留待 verify）

## 阶段 P2 — 筛选 AST（复用）与 Sort

- [x] P2.1 共享 `SearchData`：`p["viewFilter"]` 用 `AutomationExecutor.DeserializeFilter` + `AutomationFilter.TryBuildWhere`，可下推则 `Factory ??= Factory` 后 `GetExpression() & viewExp` 赋给 `p.State`（不改 `CreateWhere`）
- [x] P2.2 `TryBuildWhere` 补 `notcontains`（与 `Match` 对齐）。数组 eq / 嵌套 groups 仍不下推
- [x] P2.3 WebAPI 与 CubeNC `EntityTreeController.Search`：缓存列表上 `AutomationFilter.Match`
- [x] P2.4 不新增 `sorts`。不在 SearchData 清空 `OrderBy`（Pager 已不绑请求 OrderBy）。非法 Sort 维持 XCode 抛错
- [x] P2.5 JSON 损坏或文本 >4096 → 400；无法下推则忽略服务端过滤（前端 `matchesViewFilter` 仍在；翻页不完整为已知限制）
- [x] P2.6 XUnit：all/any 不能绕过权限表达式；超长 400；notcontains 可下推（执行期发现 WhereBuilder 对常量表达式解析受限，SearchData 加防御：GetExpression 失败放弃下推不 500）
- [x] P2.7 ArcoVue：有条件才把 `viewFilter` 传给 getList/getChartData（logic=`all`/`any`）；`buildSortPayload` 维持单列；补 spec。**不做多列排序 UI**
- [x] P2.8 跑测 + 冒烟：默认列表可下推筛选翻页；部门 FindAll 吃到 State；树表内存过滤；`notContains` 不 500（冒烟留待 verify）

## 阶段 P3 — PATCH 与批量改字段

- [x] P3.1 仅 WebAPI `partial` 文件增加 `PatchFields`（**PATCH** + `{id,values}`）与 `BatchUpdateFields`（**POST** + `{keys,field,value}`）。权限 Update。勿写入被 CubeNC Link 的 `EntityController2.cs`
- [x] P3.2 白名单 = EditForm ∩ !ReadOnly ∩ 非 PK；keys 空或 >500 → 400；未知字段 400；逐行 FindData + Valid + ChangeType + OnUpdate；部分失败 `{ok,fail,errors[]}`
- [x] P3.3 写请求带头时走现有 `ValidateEntityFields`。不改 PUT / EnableSelect。布尔 UI 仍 EnableSelect。不必 `EntityFieldWriteGuard`
- [x] P3.4 XUnit：白名单拒绝、部分失败、超 500、空 keys、GET 不能改字段（执行期修复：EnableFieldValidationRequested 在 Request==null 时 NRE，已防御）
- [x] P3.5 ArcoVue：高级菜单「批量修改」（BatchUpdateFields）+ 双击可编辑单元格 → 字段编辑弹窗（PatchFields）；布尔仍 EnableSelect；PATCH 也带头（onRequestHook 已覆盖）
- [ ] P3.6 跑测 + 冒烟：PATCH / 批量 / PUT / 徽标启停（自动化门禁已跑，冒烟留待 verify）

## 阶段 P4 — 解析现有 Log diff 与评论提及

- [x] P4.1 **不改** XCode、`Log` 表、`WriteLog`、不装饰 `LogProvider`、不建新表、不改 Remark 格式、不全局打开 `LogOnChange`
- [x] P4.2 前端 `logRemarkDiff.ts`：字段名**长名优先**、忽略大小写，解析 `Field=old -> new`（Update/修改/Edit）；失败走现有 `historyRemark`；禁止 `JSON.parse` 整段 Remark。Vitest：标量成功、逗号值失败回退、Insert 无箭头、自动化 JSON、Name vs DisplayName
- [x] P4.3 历史 Tab 有变更则渲染字段表（锚点用抽屉 `fields`），否则原文
- [x] P4.4 评论：不改 Cube.xml / `AddComment`。WebAPI 与 CubeNC POST 改用控制器内 DTO（多 `mentionUserIds`）。成功后写 `NotificationRecord`（Channel=`InApp`，Action=`Mention`，Target=`category#linkId`，抄自动化 Insert）
- [x] P4.5 最多 20、Distinct；非法/禁用/自己跳过；通知失败不回滚评论；无该字段行为与今日一致
- [ ] P4.6 冒烟：对已 `LogOnChange=true` 的实体改一字段，历史 Tab 出 diff；@ 人有站内信；未开日志实体历史仍可空；旧散文日志仍可读

## 阶段 P5 — 只读文档 + ViewProfile 图表 option

- [x] P5.1 修订迁移方案 §8.2.2 / §8.2.3：允许当前 NamedView 持久化 **一张** 用户 ECharts option；仍禁止多图看板/拖拽。§8.2.6：查找展示用现有 Map/lov；公式用 C# 扩展属性；禁止双向写回与用户脚本。**不**加 GetPage `projections`，**不**加 `autoChart`
- [x] P5.2 `ViewInsight` 增加 `chartOption`；`normalizeInsight` / `serializeInsight` round-trip；保存前剔除 `dataset.source` 与 `series[].data`；>32KB 拒绝。Vitest 覆盖
- [x] P5.3 InsightPanel：showChart 且无开发者 GetChartData 时，用 `chartOption` + 当前列表行 `applyChartData` 后 `setOption`；无 option 显示配置入口。ViewConfigDrawer 开关旁可打开同一配置。保存走现有 `updateInsight`
- [x] P5.4 `GetChartData` / `OnGetChartData` **不改签名**；子类非空数组仍优先于用户 option
- [ ] P5.5 跑测 + 冒烟：配置一张柱状 option → 刷新后仍在；改搜索后图随当前页数据变；超大 JSON 保存失败；开发者 OnGetChartData 非空的实体仍走后端图

## 收尾

- [x] C.1 竞品分析 §8.6 与本号 design 一致（diff=解析现有 Remark；前端单列 sort；PATCH 仅 WebAPI；图表=ViewProfile 非 autoChart）
- [x] C.2 本 OSC 新增单测 + 构建 `NewLife.Cube` + `NewLife.CubeNC` + arco-vue 无错误
- [ ] C.3 `verify.md` 勾选后方可 Validating / Done
