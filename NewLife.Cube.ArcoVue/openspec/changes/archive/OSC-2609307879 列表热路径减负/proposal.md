# OSC-2609307879 — 列表热路径减负

## 1. 目标愿景

打开实体列表时，地区和值集不再按行串行打接口；改一个开关或拖一张看板卡片时，筛选和排序不依赖该字段就不再整表重查；菜单和元数据走 XCode 已有的实体缓存，同一请求里不把数据源委托跑两遍。

- 目标 1：一页里的地区叶子只发一次名称查询；翻页后新出现的 LOV 值仍能翻译。
- 目标 2：布尔徽标与看板跨列在成功后，当前排序和 `viewFilter` 都不含该字段时不调用 `loadData`。
- 目标 3：`MenuTree` 与已发布流程定义走实体缓存；`ToDictionary` 在 `DataSourceMap` 已有内容时不再调用 `DataSource`。
- 目标 4：`heightMode=fit` 的表格宿主高度不超过视口；离开页签后监控图、数据列表自动滚动、甘特宽度轮询停止。

## 2. 为何做

列表 `bootstrap` 在 `GetList` 之后对每个地区 ID `getDetail`，对每个 LOV 字段串行 `BatchLabel`，且字段上已有任意 `dataSource` 就整列跳过，翻页会缺标签。布尔切换在乐观更新之后仍 `getDetail` + 整单 `Update` + `loadData`。日历固定第一页 1000 条，不带当前日/周/月区间。

`fit` 把 VTable 高度设成 `行数 × 40`，虚拟滚动失效。`keep-alive` 的 `cached` 无上限，定时器只在卸载时清。图表部件在启动时同步注册，`echartsTheme.ts` 顶层引用完整 echarts。

`CubeController.MenuTree` 使用 `Role.FindAll()`，同文件其它入口和用户/应用控制器已用 `FindAllWithCache()`。`WorkflowDefinition.FindPublished` 每次列表都打 SQL。`DataField.PrepareForApi` 已把委托写入 `DataSourceMap`，`ToDictionary` 仍优先再跑一遍委托。`GetPage` 对 List 分区调用两次 `OnGetFields`。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | 一个 OSC、三条线：A 请求、B 绘制与保活、C 元数据与菜单缓存。只改 ArcoVue 与 WebAPI `NewLife.Cube`。 |
| 2 | 不改 NewLife.XCode。地区名称用现成 `Area.FindAllWithCache()`，未命中再 `ID.In`。不新写 `ConcurrentDictionary`。 |
| 3 | 不做列表列裁剪，不新增 GetPage `projections`，不改 `Search` / `GetPage` / `Index` 签名。 |
| 4 | 流程定义：`Meta.Session.Count < 1000` 时 `Meta.Cache.FindAll`；否则保持现有 `FindAll` 表达式。门槛与 `MapAttribute.GetDataSource`、角色 Biz 一致。 |
| 5 | `DataField.cs` 是 CubeNC 链接进 Cube 的同一文件。`DataSourceMap` 为空时仍走委托，MVC 未 `PrepareForApi` 的行为不变。 |
| 6 | 日历仍最多 1000 条、`pageIndex` 仍为 0。可见区间并进**请求用**的 `viewFilter` 副本，不写回用户筛选、预定义查询或 lastQuery。用户筛选 `logic=any` 且已有条件时不附加区间。 |
| 7 | Enable 仍走 `EnableSelect` / `DisableSelect`。其它布尔走已有 `patchFields`，删除该路径上的 `getDetail` + 整单 `Update`。 |
| 8 | 页签条 `visited` 保留。`cached` 上限 8，挤掉最久未激活的组件名。关掉页签的现有逻辑不变。 |
| 9 | 保存、删除、批量、导入成功后仍 `loadData`。 |

## 4. 做什么

**A.** `POST /api/Cube/Area/Names` 一次取名称；LOV 只补缺值且字段间并行；布尔与看板按条件跳过整表刷新；日历在可下推时为请求附上当前日/周/月区间；GetFields 兜底改为并行。

**B.** `fit` 高度取内容高度与视口测量值的较小者；主题色与条件填色在绘制前算好；页签失活停表；图表运行时改为动态导入，四类图表部件异步注册。

**C.** `MenuTree` 改 `Role.FindAllWithCache()`；`FindPublished` 小表走实体缓存；`ToDictionary` 优先已物化的 `DataSourceMap`；`GetPage` 对 List 只 `OnGetFields` 一次。

## 5. 不做什么

- 不改 `Index` 的整实体序列化，不关默认总记录数，不改导出上限。
- 不改 Lov 远端 `List.*` 最多 20 页的扫描，不改批量 Patch/Delete 的逐行 `Find`。
- 不改 Cube.Vue / NaiveUI 页面，不改 XCode 源码。
- 不把 `fit` 改成页面级无限滚动，不取消页签，不改表格列顺序和工具栏 DOM 顺序。
- 不改图表选项编辑器「每次按键 dispose 再 init」的行为。
- 级联选择器 `useCascaderField` 仍按叶子 `getDetail` 取路径，本号只改列表注水与记录抽屉里的地区叶子标签。

## 6. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-2608139feb | 地区 `areaLabelCache` 与级联叶子展示。本号替换取名称的方式，不改 `mergeAreaLabel` 的键值语义 |
| OSC-260819e483 | 复用 `patchFields` 与 `viewFilter` 下推。不推翻「不新增 projections」 |
| OSC-260926c2b8 | 日历 1000 上限、看板 `patchFields`。本号只给请求附加区间，并在成功后有条件地跳过 `loadData` |
| OSC-26090347f1 | `FindPublished` / `WorkflowPageOverlay.ApplyRows` 调用点不变 |
| OSC-260830a1b2 | 区间条件仍受 search∪list 白名单约束；字段不在白名单则不加区间，避免 400 |

## 7. 测试范围

| 类型 | 是否做 | 说明 |
| --- | --- | --- |
| XUnit | 是 | 地区名称、`FindPublished` 小表过滤、`ToDictionary` 优先 map、`Role.FindAllWithCache` 与 `FindAll` 的 Resources 并集一致 |
| Vitest | 是 | 缺值 LOV、跳过刷新、日历窗口与 `logic=any`、`fit` 高度、`cached` 上限 8 |
| 构建 | 是 | `dotnet build NewLife.Cube`；web `vue-tsc --noEmit` |
| 手工 | 是 | 见 verify.md。执行阶段可留到验收 |

硬门禁：执行期跑本号新增单测；验收期新增单测全过，且上述构建无错误。

## 8. 成功标准

- [x] 列表打开时，同一页地区叶子对应一次 `POST /api/Cube/Area/Names`（ID 超过 200 时按 200 分片），不再对每个 ID `getDetail`。
- [x] 第一页已经写入 `dataSource` 的 LOV 字段，翻页出现新值时仍请求 `BatchLabel`，且只带缺失值。
- [x] 布尔徽标或看板跨列成功后，排序和筛选都不含该字段时，不再发 `GetList`。
- [x] 日历月/周/日切换或翻页后，请求里的 `viewFilter` 含开始字段的 `gte`/`lt`（用户筛选为 `any` 且已有条件时除外）；返回行数仍不超过 1000。
- [x] `MenuTree` 用 `Role.FindAllWithCache()`；`DataSourceMap` 非空时 `ToDictionary` 不调用委托。
- [x] `fit` 且行数很多时，表格宿主高度等于视口测量值，而不是 `行数 × 40`。
- [x] 打开监控工作台后切到另一页签，5 秒轮询停止；再回来后恢复。
