# OSC-2610011ff2 Design — 隐藏功能入口

## 0. 适用框架与官方资料

| 场景 | 框架 | 资料 |
| --- | --- | --- |
| 页签、菜单、警告 | Arco Design Vue | https://arco.design/vue/docs/start |
| 用户统计图 | 现有 ECharts 封装 | 与 `ListChartModal` 同一套 option，不新引入图表库 |
| SFC | README 规则 | `.vue` 不写请求 |

## 1. 文件级改动地图

| 文件 | 改动 | 保留 |
| --- | --- | --- |
| `web/src/core/utils/hiddenEntryRoutes.ts` | 新建。导出白名单路径数组与 `isHiddenEntryPath(path)` | — |
| `web/src/core/utils/hiddenEntryRoutes.spec.ts` | 新建 | — |
| `web/src/router/index.ts` | 用白名单生成静态子路由，组件均为 `DynamicPage`，`meta.typePath` 为去掉首斜杠的路径。`oaLeafRoutes` 增加 Efficiency | 登录、账号、已有 Admin 静态路由 |
| `web/src/views/account/AccountCenter.vue` | 增加页签「关联」 | 资料/密码/安全/绑定 |
| `web/src/views/account/useAccountLinks.ts` | 新建。按 `userInfo.id` 生成 5 个链接 | — |
| `web/src/core/utils/accountLinks.ts` | 纯函数 `buildAccountLinks(userId: number \| undefined)` | — |
| `web/src/core/utils/accountLinks.spec.ts` | 新建 | — |
| `web/src/views/object/useDefaultObject.ts` | 左侧列表在对象项后追加「更多」 | 对象探测与保存 |
| `web/src/core/utils/objectMoreLinks.ts` | 纯函数：给定菜单展平列表，返回应显示的更多项 | — |
| `web/src/core/utils/objectMoreLinks.spec.ts` | 新建 | — |
| `web/src/views/crud/DefaultList.vue` | 向 `InsightPanel` 传入 `developerCharts` / 错误文案 | 其它实体传空 |
| `web/src/views/crud/useUserStatChart.ts` | 仅当 type 匹配时请求 GetChartData | — |
| `web/src/core/utils/chartOptions.ts` | option 归一、`mergeDeveloperCharts`、`prepareFixedChartOption`（boxplot 升序） | 弹窗交互 |
| `web/src/features/search/useInsightPanel.ts` / `InsightPanel.vue` | 合并开发者固定图；其后保留自定义部件 | — |
| `web/src/features/widget/LegacyChartWidget.vue` 等 | 固定图壳层、tall 高度、无设置 | — |
| `Areas/Admin/Controllers/UserStatController.cs` | boxplot 五数 `Array.Sort`；紧凑 grid | `[Menu]` 不变 |

不改 `buildLeafRoutes` 的 `visible===false` 跳过规则。静态路由负责白名单。

不加载、不保存 `Menu` 实体，不给任何菜单行赋值 `Visible`。不改白名单控制器上 `[Menu]` 的可见参数与 `LastUpdate`。`MenuHelper` 在特性更新时间新于行 `UpdateTime` 且（不可见或非必要）时会把 `att.Visible` 写回实体；本号避开这条回写路径。侧栏仍读菜单树里的 `visible`，管理员在菜单管理里改可见性后，侧栏随菜单接口结果变化，本号不代改。

## 2. 白名单

```
Admin/UserOnline
Admin/UserStat
Admin/UserToken
Admin/UserConnect
Admin/OAuthConfig
Admin/OAuthLog
Admin/AccessRule
Admin/SmsConfig
Admin/MailConfig
Admin/Parameter
Admin/TenantUser
Cube/AppLog
```

`isHiddenEntryPath` 去掉首尾斜杠后与上表忽略大小写比较。多一段或少一段为 false。

## 3. 账号中心链接

| 标题 | 路径 | userId |
| --- | --- | --- |
| 在线会话 | `/Admin/UserOnline` | query `userId` |
| 用户统计 | `/Admin/UserStat` | 无 |
| 访问令牌 | `/Admin/UserToken` | query `userId` |
| 登录日志 | `/Admin/Log` | query `userId` |
| OAuth 日志 | `/Admin/OAuthLog` | query `userId` |

`userId` 不是有限正整数时，除用户统计外 `disabled=true`，点击不导航。

## 4. 对象「更多」

固定四项，仅当展平菜单里存在 `url` 规范化后等于该路径的节点（不论 visible）才显示：

| 标题 | 路径 |
| --- | --- |
| 短信设置 | `/Admin/SmsConfig` |
| 邮件设置 | `/Admin/MailConfig` |
| OAuth 设置 | `/Admin/OAuthConfig` |
| 访问规则 | `/Admin/AccessRule` |

点击只 `router.push`。不调用 `detectPageKind` 把它们并进对象缓存。

## 5. 用户统计图表

| GetChartData | 界面 |
| --- | --- |
| 非数组或空数组 | 不注入开发者固定图 |
| 数组长度 ≥ 1 | 洞察区最多 2 张固定 `legacyChart`，整行 w=12；折线体高约 140、箱线/K 线约 220；标题在部件条 |
| 请求失败 | 洞察区警告，列表照常 |

查询参数与当前列表关键字、筛选一致（复用 `useListQuery` 已有的请求参数对象）。切换筛选后重新请求。

## 6. 效率页

`oaLeafRoutes` 增加 `{ sub: 'Efficiency', title: '效率' }`。`resolvePageComponent` 已识别 `efficiency`，不改该函数的分支条件，只确认静态路径能解析到 `WorkflowEfficiencyPage.vue`。

## 7. 核心文档影响

| 文档 | 影响 |
| --- | --- |
| `ArcoVue企业中后台迁移方案.md` | 修改：隐藏菜单白名单与效率页静态路由一句 |
| `web/README.md` | 无 |
| `Doc/功能清单.md` | 无 |
| `Doc/Api/核心接口架构.md` | 无 |

## 8. 测试设计

- 白名单 12 条为 true；`/Admin/User`、`/Admin/UserOnline/Kick`、空串为 false。
- `buildAccountLinks(undefined)` 除用户统计外 disabled。
- `buildAccountLinks(3)` 的令牌链接含 `userId=3`。
- 菜单无短信 url 时更多项不含短信；`visible:false` 仍包含。
- 图表归一：`null` → `[]`；已是 option 数组则原样。
