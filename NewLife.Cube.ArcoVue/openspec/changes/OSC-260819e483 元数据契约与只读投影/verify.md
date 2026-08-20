# OSC-260819e483 Verify

状态：Draft / 骨架。

## 签名与复用（全程）

- [x] `Fill` / `EnableFieldValidation` 默认 false / `Search` / `GetPage` / `Insert`/`Update`/`OnUpdate` / `AddComment` 六参数 / `WriteLog` 均未改签名
- [x] 未新增 `EntityListFilter`、`CubeFieldDiffLogProvider`、`EntityFieldChange`、`MentionsJson`、GetPage `projections`、`sorts`
- [x] 筛选走 `AutomationFilter`；排序走现有 `Sort`/`BuildOrder`；diff 解析现有 Remark
- [x] GetPage 仍 `[AllowAnonymous]`
- [x] PATCH Action 不在 CubeNC；`SearchData` 只在共享 `ReadOnlyEntityController2` 接了一次 viewFilter

## P1

- [x] GetPage（WebAPI 与 CubeNC）：非 PK、非只读、Nullable=false → `required:true`；Fill 后 Required 仍 false
- [x] 布尔 NOT NULL 可出现星号；提交 false 成功（单测覆盖 isFieldRequired 布尔矩阵；真实表单冒烟待用户）
- [x] 数据权限列表 WebAPI Index 不 500；`RetrieveState` 统计对象若不是 TEntity 则 Stat=null
- [x] 无校验头时写入与改前一致；有头则缺必填失败；读请求无该头

## P2

- [x] `viewFilter` logic=`all`/`any`；可下推时服务端过滤且 AND 数据权限
- [x] `logic=any` 不能放大 `CreateWhere` 范围
- [x] 非法 JSON 或长度 >4096 → 400；无法下推不 500；当前页前端复核仍工作
- [x] 未新增 `sorts`；前端仍单列 `sort`/`desc`；请求无法再绑 `OrderBy`
- [x] 两栈 EntityTree 内存 Match；空条件不传 viewFilter
- [x] `notContains` 可下推（本号补 TryBuildWhere）或不下推时不 500

## P3

- [x] `PatchFields` 为 PATCH + `{id,values}`；`BatchUpdateFields` 为 POST + `{keys,field,value}`；均需 Update 权限
- [x] 只改白名单；逐行 Valid+OnUpdate；部分失败返回 `{ok,fail,errors}`
- [x] 空 keys / 超 500 / 未知字段 → 400
- [x] PUT、EnableSelect（仍为 GET）与今日一致
- [x] CubeNC 无这两个 Action

## P4

- [x] XCode 与 Log 表未改；Remark 仍为 `Field=old -> new` 文法；`LogOnChange` 未全局打开
- [x] 历史 Tab：标量 Update（已开日志的实体）能看出字段新旧；长名优先；逗号值/旧散文/自动化 JSON 不崩
- [x] 评论可带最多 20 个 mentionUserIds 写 NotificationRecord（InApp/Mention）；非法 Id 跳过；不传则与今日一致；未改 AddComment 签名
- [x] 通知失败不导致评论 500

## P5

- [x] 无 GetPage `projections` 键；无 `autoChart` 查询参数；`GetChartData` 签名未改
- [x] `insight.chartOption` 可经 ViewProfile 保存/读回；保存后无 `series.data`/`dataset.source` 快照
- [x] 超 32KB 或非对象 → 不写入
- [x] showChart + 用户 option：Insight 出图且不依赖 GetChartData 空数组
- [x] 子类 `OnGetChartData` 非空 → 仍用后端 option
- [x] §8.2.2 / §8.2.3 已改为允许一张用户 option；§8.2.6 只读例外已写

## 冒烟验收（真实运行环境，验收阶段缺口）

- [ ] 已 `LogOnChange=true` 实体改字段 → 历史 Tab 出字段 diff 表（长名优先、忽略大小写、自动化 JSON/逗号值回落原文）
- [ ] 评论 @ 人 → 有站内信（Channel=InApp，Action=Mention，最多 20 去重，非法/禁用/自己跳过）；不 @ 与今日一致
- [ ] 筛选构建器条件 → 默认列表可下推翻页；`notContains` 不 500；`logic=any` 不放大权限范围
- [ ] 高级菜单「批量修改」→ 多字段行可用（添加/删除行、typeName 自适应控件：状态/枚举/值集→下拉且元数据 dataSource/lovCode 自动填充、数值→数字框、日期→选择器、长文本→textarea）；确定后一次性更新；fail>0 显示首条失败明细
- [ ] 双击可编辑单元格 → 字段编辑弹窗调 PatchFields；布尔仍走 EnableSelect 徽标
- [ ] 配置一张柱状 option → 刷新后仍在；改搜索后图随当前页数据变；超大 JSON（>32KB）保存失败有提示
- [ ] 开发者 `OnGetChartData` 非空的实体仍走后端图（用户 option 不覆盖）
- [ ] PUT / EnableSelect（GET）/ 无校验头写入与改前一致（回归确认）

### 冒烟操作指南

**前置**：启动 CubeDemoNC 或 CubeDemo（WebAPI 模式），浏览器打开默认列表。

| # | 操作 | 预期 |
|---|------|------|
| S.1 | 找到一个 `LogOnChange=true` 的实体（如角色/菜单），修改某一字段 → 点击该记录 → 右侧抽屉「历史」Tab | 出现字段 diff 表：旧值（删除线）→ 新值（高亮）；不是原文 |
| S.2 | 打开任意支持评论的实体详情页 → 在评论框输入 `@` 触发提及 → 选中某人 → 提交 | 提交成功；被 @ 的人收到站内信（通知中心） |
| S.3 | 工具栏「筛选」→ 添加条件（如 `Name contains 测试`）→ 应用 → 翻页 | 翻页后仍过滤；`notContains` 条件不 500 |
| S.4 | 勾选多行 → 高级菜单「批量修改」→ 添加多行字段（状态→下拉、名称→文本框、日期→日期选择器）→ 确定 | 一次性更新全部字段；toast 提示成功 N 条 |
| S.5 | 双击一个可编辑单元格（非布尔）→ 修改值 → 回车 | 弹字段编辑弹窗 → 保存后单元格刷新 |
| S.6 | 视图配置抽屉 → 开启「固定图表」→ 点击「配置图表」→ 粘贴柱状 option JSON → 保存 → 刷新页面 | 图表刷新后仍在；修改搜索条件后图表数据跟随变化 |
| S.7 | 用老客户端（不带 `X-Cube-Field-Validation` 头）调用 POST/PUT | 与改前行为完全一致；不校验必填 |

## 风险记录（验收阶段）

- **外部 WIP 文件 sfcThin 违规**（非本 OSC）：`FormatPopover.vue` / `useFormatPopover.ts` 未跟踪（`??`），.vue 内使用 watch 违反 sfcThin 规范。建议完成该功能后提取 watch 到 useFormatPopover.ts 或加入白名单。本 OSC 592/593 Vitest 通过（1 个外部失败）。

## 测试门禁（准确命令）

在仓库根 `NewLife.Cube`：

```
dotnet test XUnitTest --filter FullyQualifiedName~Osc260819
dotnet test NewLife.Cube.Tests --filter FullyQualifiedName~Osc260819
dotnet build NewLife.Cube/NewLife.Cube.csproj --no-restore
dotnet build NewLife.CubeNC/NewLife.CubeNC.csproj --no-restore
```

在 `NewLife.Cube.ArcoVue/web`：

```
npm test
npm run build
```

- [x] 本 OSC 新增 XUnit / Vitest 全过（XUnitTest 8/8；NewLife.Cube.Tests 15/15；arco-vue 510；api-core 全过）
- [x] NewLife.Cube、NewLife.CubeNC 与 arco-vue 构建无错误

> 实施期将单测类名写入 filter；若实际类名不同，以 `tasks.md` 新增测试文件名为准，不得用「差不多过了」代替。
>
> 冒烟项（真实运行环境）待用户验收：已 `LogOnChange=true` 实体改字段历史 Tab 出 diff；@ 人有站内信；默认列表 viewFilter 翻页完整；PATCH/批量修改在页面可用；配置图表刷新后仍在；超大 JSON 保存失败提示.
