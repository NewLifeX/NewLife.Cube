# OSC-26082815a1 UI 信息架构

框架：Arco Design Vue（https://arco.design/vue/docs/start）。图表：ECharts（`echartsTheme.ts`）。图标：IconPark，先查 https://iconpark.oceanengine.com/official 再注册。业务在 `useXxx.ts`，`.vue` 保持薄。

## 路由与页面角色

| 路径 | 页面 | 谁用 |
|------|------|------|
| `/home` | 我的工作台（本号） | 全员登录后落地 |
| `/Admin/Index` | 运维监控 DefaultHome | 菜单进系统信息；工作台 SysInfo/CpuRate 链到此 |
| `/settings/workbench-role` | 角色工作台模板 | 仅系统角色 |

用户不能把监控页的进程/程序集表拖进工作台。

## 工作台自上而下

1. **欢迎横幅**（固定 chrome，不在 Catalog）：问候 + 日期；右「自定义工作台」「恢复默认」。
2. **WidgetHost** 12 列栅格，间距 12px。`w`：2=KPI 六宫格，8=Monitor 主图，4=内容半宽，6=Inbox/Profile。
3. 自定义态：每卡操作组（与洞察槽相同：编辑/左移/右移/删除）+ 全局添加。
4. 无自由坐标、无嵌套行容器。

## 管理员默认墙（视觉层次）

```
横幅
[UserCount][TodayLogin][OnlineCount][Log24h][Error24h][CpuRate]   各 w=2
[ Monitor w=8 ][ QuickLink w=4 ]
[ Inbox w=6 ][ SysInfo w=6 ]
[ LoginLog w=6 ][ Profile w=6 ]
```

## 普通用户默认墙

```
横幅
[MyLogins w=3][MyDays w=3][Inbox w=6]
[QuickLink w=6][Profile w=6]
```

无系统 KPI、无 Monitor、无 SysInfo、无 LoginLog。

## 平台部件（添加目录）

与 CubeNC 预定义一一对应，按 Catalog 过滤后展示：

- KPI 组：UserCount / TodayLogin / OnlineCount / Log24h / Error24h / CpuRate / MyLogins / MyDays
- 内容组：QuickLink / Profile / SysInfo / LoginLog / Inbox
- 图表组：Monitor

第二 Tab「实体指标」：metricCard / miniChart / miniKanban（绑定已授权 typePath）。工作台无宿主筛选，不出现「未联动」角标。

## 卡片视觉

- 白底、1px 淡边、圆角 token，无厚阴影。
- KPI：左 IconPark + 标题 + 主值 24–28px + trend 12px 次文案。
- kvList：两列表格，href 用 `a-link` 不是 v-html。
- loginLog：Tabs「最近登录 / 在线」。
- monitorChart：面积折线 CPU/内存 0–100%，5s 追加。
- inbox：未读徽章 + 标题列表；点整卡打开壳 Inbox 抽屉。

## 空与权限

- `widgets:[]`：横幅 + 虚线添加（自定义态）。
- Data 404/403：LockedWidget，不跳登录。
- 未知 kind：UnknownWidget + 可删。
- 内容区 &lt;800px：全部 span 12。

## 明确不做的交互

- 画布拖拽缩放、宣传栏轮播、部门可见范围。
- 快捷入口用户自选 pin（第一期固定六链过滤）。
- 在工作台内嵌 DefaultHome 四张运维大表。
