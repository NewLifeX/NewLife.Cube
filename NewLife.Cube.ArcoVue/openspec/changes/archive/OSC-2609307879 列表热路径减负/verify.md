# Verify

验收命令（在对应项目目录执行。新增测试全过，构建无错误）：

- `dotnet test NewLife.Cube.Tests --filter "AreaNamesTests|WorkflowDefinitionFindPublishedTests|DataFieldDictionaryTests|RoleCacheMenuTests"`
- `web` 目录 `npx vitest run src/core/utils/areaLabels.spec.ts src/core/utils/patchReload.spec.ts src/features/views/calendarDay.spec.ts src/stores/tagsView.spec.ts src/views/crud/resolveFitHeight.spec.ts src/views/crud/missingLovValues.spec.ts`
- `dotnet build NewLife.Cube/NewLife.Cube.csproj`
- `web` 目录 `npx vue-tsc --noEmit`

`fit` 高度若测在 `listContext` 以外的纯函数文件，把该 spec 路径补进上面的 vitest 命令。日历窗口若单独成 `calendarWindow.spec.ts`，同样补进命令。

## AC

- [ ] AC1 `POST /api/Cube/Area/Names`，body `{ "ids": [] }` 返回 200 且 `data` 为 `{}`。
- [ ] AC2 一个存在的地区 ID 返回该 ID 到 `Name` 的映射；不存在的 ID 不出现；`"abc"` 被忽略且不 400。
- [ ] AC3 去重后 201 个 ID 返回 400，消息含 `200`。列表注水把 201 个未缓存 ID 分成两片请求。
- [ ] AC4 无 Detail 权限调用 `Names` 得到 401 或 403，不返回名称。
- [ ] AC5 列表含地区列时，网络里该页只有 `Names`（或按 200 分片），没有按 ID 的 `/Cube/Area/Detail`。`Names` 失败时表格仍显示，单元格为原始 ID。
- [ ] AC6 LOV 字段第一页已有 `dataSource` 时，第二页的新值仍发 `BatchLabel`，body 里的 values 不含已翻译的旧值。
- [ ] AC7 非 Enable 布尔徽标成功请求为 `PATCH`，body 只有该字段。当前排序和筛选都不含该字段时，成功后没有新的列表 GET。
- [ ] AC8 同上操作失败时徽标回到点击前的值，且没有列表 GET。
- [ ] AC9 Enable 徽标仍走 `EnableSelect` 或 `DisableSelect`。字段出现在当前排序中时，成功后有列表 GET。
- [ ] AC10 看板跨列成功且分组字段不在筛选和排序中时，没有列表 GET；失败时卡片回到原列。
- [ ] AC11 日历月视图、开始字段为 DateTime、用户筛选为 `all` 或空：列表请求的 `viewFilter` 含该字段 `gte` 当月 1 日与 `lt` 下月 1 日。保存预定义查询时不含这两条。
- [ ] AC12 用户筛选 `logic=any` 且已有一条条件时，日历请求不追加 `gte`/`lt`。`pageSize` 仍为 1000。
- [ ] AC13 开始字段不是日期，或当前不是日历：请求筛选与用户筛选相同。
- [ ] AC14 `FindPublished` 在少量数据下只返回启用且已发布、租户与路径匹配的定义；把 `Published` 改为 false 并保存后，再次调用不再包含它。
- [ ] AC15 `DataSourceMap` 已有一项时，`ToDictionary` 不调用 `DataSource` 委托；map 为空时调用。布尔字段不因 map 变成下拉数据源。
- [ ] AC16 测试库中 `Role.FindAllWithCache()` 与 `Role.FindAll()` 展开的 Resources 并集相同。`MenuTree` 源码使用 `FindAllWithCache`。
- [ ] AC17 `fit`：内容高度 200、视口 800 时宿主为 200；内容远高于视口时宿主等于视口测量值，且不小于 240。
- [ ] AC18 连续打开 9 个可缓存路由后 `visited` 为 9、`cached` 为 8。再次激活最早那个名字会把它移到 `cached` 末尾。关闭页签仍从两边删除。
- [ ] AC19 打开带监控图的工作台后切到别的页签，5 秒内不再请求该监控接口；回到该页签后轮询恢复。
- [ ] AC20 登录完成到进入无图表页之间，不加载 echarts 包。打开含迷你图或指标卡的页面后图表仍能画出。

## 暂缓（本号不得改掉）

- `Index` 仍返回完整实体，`RetrieveTotalCount` 默认仍为 true。
- `LovController` 远端 `List.*` 的 `maxPages = 20` 保持。
- `BatchUpdateFields` / `DeleteSelect` 仍按主键循环。
- `useCascaderField` 仍 `getDetail`。
- 图表选项编辑器仍在文本变化时 `dispose` 再 `init`。
- 保存、删除、批量、导入成功后仍 `loadData`。

## 执行记录（2026-09-30）

- `dotnet test NewLife.Cube.Tests --filter "AreaNamesTests|WorkflowDefinitionFindPublishedTests|DataFieldDictionaryTests|RoleCacheMenuTests"`：通过 7。
- `npx vitest run` 上述 spec（含 `resolveFitHeight.spec.ts`、`missingLovValues.spec.ts`）：通过。日历窗口断言在 `calendarDay.spec.ts`。
- `dotnet build NewLife.Cube/NewLife.Cube.csproj`：成功（既有 XML 注释警告）。
- `npx vue-tsc --noEmit`：通过。
- 浏览器与接口冒烟（AC1/AC4/AC5/AC7–AC13/AC19/AC20）未跑，留验收。

## 验收（2026-09-30）

### 三步

- 实现审计：proposal §1 四条目标均有对应代码与单测。无 P0/P1 实现缺口。
- 代码审查：无命名、兼容性、安全类必须修复项。表头排序图标仍调用 `themeColor`，它在 `useListTable` 模块顶层注册，不在单元格 `style` 热路径。
- 文档同步：迁移方案四处已写「请求附带当前日/周/月区间；`logic=any` 且已有条件时不附带」。功能清单、需求文档、架构设计没有列表热路径条目，design 规定不新增。`Doc/Api/前端对接指南.md` 没有与本号相反的结论，不改。

### 愿景对照

| 目标 | 结论 |
| --- | --- |
| 1 地区一次取名、翻页 LOV 仍翻译 | 达成。`areaNames` + `pendingAreaIds`/`chunkAreaIds`；`missingLovValues` 在 dataSource 已有旧值时仍返回新值。 |
| 2 布尔与看板按条件跳过 `loadData` | 达成。`shouldReloadAfterPatch`；Enable 仍走启停接口。 |
| 3 菜单与流程定义走实体缓存，字典不重复跑委托 | 达成。`Role.FindAllWithCache`、`FindPublished` 小表分支、`ToDictionary` 非布尔且 map 非空不调委托。 |
| 4 fit 封顶，离页停表 | 达成。`resolveFitHeight`；监控图、自动步进、甘特宽度轮询有 `onDeactivated`。 |

### 缺口（用户本句同时要求复盘，P2 按仅记录继续）

- P0：无。
- P1：无。
- P2：没有跑浏览器和真实 HTTP。AC4 的 401/403、AC5 的网络面板、AC19/AC20 的 echarts 加载时机没有集成测试。逻辑由单测和源码审查覆盖。

### 门禁

- 后端过滤测试：第一次重跑失败 1（`Area.ID=78790001` 唯一约束，Membership 连接没有换成独立内存库，上次插入还在）。插入前删除并在断言后清理后，7 通过。
- 前端 6 个 spec、29 个测试通过。
- `dotnet build NewLife.Cube/NewLife.Cube.csproj` 成功，0 错误（既有 XML 注释警告）。
- `npx vue-tsc --noEmit` 通过。
- checklist: passed
