# OSC-260830a1b2 UI 信息架构

框架：Arco Design Vue（https://arco.design/vue/docs/start）。图标：IconPark，本号查询面只用已注册的 **`search`**（https://iconpark.oceanengine.com/official 已存在，现用于「搜索」按钮与 `QueryComboButton`）。**禁止**为本号再注册新图标；**禁止**查询按钮使用 `filter`。

业务不进 `.vue` script。不新增 VTable 配置。

## 页面角色

实体 DefaultList。无新路由。

## 工具栏：查询簇（紧靠「分组」）

从左到右：

```
[ + 添加记录 ]  ...  [ 关键字 Input ]  8px  [ 查询 ][ ▾ ]  [ 分组 ][ 填色 ][ 分享 ][ 高级 ]
                                        └── a-button-group，组内 -1px 边框重叠（现有 .qcb-*）
```

| 控件 | 行为 |
|------|------|
| **Q** | `a-input`，placeholder「关键字」或沿用「全字段模糊搜索」；`allow-clear`；宽约 180px（收窄）。`enableKey===false` 不渲染。Enter = 与「查询」相同。清空不自动请求，须点「查询」或 Enter。 |
| **查询** | `type=primary`，`icon-park type="search"`，文案「查询」；padding 收窄。立即执行：**已应用**的 `viewFilter` + 当前 Q（不提交自定义弹层里未点「应用」的草稿）。 |
| **▾** | `type=primary`，`icon-park type="down"`；窄图标按钮（`padding:0 8px; min-width:32px`）。只承载菜单（见下）；「自定义查询」为菜单首项。 |

`chrome.showSearch` 控制整簇（无则隐藏 Q 与按钮组）。原 `chrome.showFilter` 不再单独出「筛选」按钮；**自定义查询**不再单独出工具栏按钮，并入 ▾ 菜单首项。

其后顺序：分组 / 填色 / 分享 / 高级。**删除**原「筛选」文本按钮与「搜索」文本按钮。**「+ 添加记录」固定在工具栏最左**，查询簇紧靠「分组」。

## ▾ 弹出菜单

顺序固定：

1. **自定义查询**（`search` 图标）— 打开条件构建器（标题「查询」）；原工具栏「自定义」按钮收编于此（键盘/小屏备援）。
2. **重置查询参数**（`refresh`）— 清空 Q、清空自定义条件、清除 `activeQueryId`，执行。
3. 分隔线。
4. 分组标题「预定义查询」。
5. 空态：「暂无预定义查询」。
6. 列表：每条显示名称；**当前应用且未脏**时左侧 `check`（使用/勾选）。点击行 = 使用该方案（回填 Q + filter 并执行）。行尾删除图标 + Popconfirm。
7. 分隔线。
8. **保存当前查询为预定义…** — 禁用当 `!canSave`（Q 去空白且条件为空）。弹窗输入名称 1–50。保存 **当前 Q + 已应用 viewFilter**。
9. **重命名当前查询** — 仅当有 `activeQueryId` 且未脏；否则禁用。
10. **删除当前查询** — 同重命名启用条件。

列表内删除与「删除当前」都走现有确认。重命名/保存用现有 `a-modal` + 单行输入。

脏（`paramsDirty`）：会话 Q 或 viewFilter 与所勾选方案快照不等 → 勾选隐藏，但 `activeQueryId` 可保留直到重置/另选。

## 自定义查询气泡

原 `FilterBuilderPopover`：

- 标题「查询」（原「筛选」）。
- 「应用」写入会话 viewFilter 并执行（Q 不变）。
- 「保存到此视图」仍只写 NamedView.filter（不含 Q），文案可改为「保存条件到此视图」。
- 操作符增加「开头是 / 不是开头」。
- 不要把 Q 做第一行条件。

## 时间窗提示

响应头 `X-Cube-Filter-Narrowed` 时，工具栏与表格之间 `a-alert` warning，可关闭（仅会话）：

`已自动限定近 {n} 天。可在自定义查询中加入时间条件以更改范围。`

无头不渲染。`<768px` 查询簇换行，Q 仍第一项。

## 空数据

0 行沿用 DefaultList 空态。不出现「请打开高级搜索」。

## 不做的交互

- 不恢复右侧 SearchDrawer。
- 不把预定义放进视图配置抽屉当第二套 UI。
- 不在看板/日历另做一套查询簇。
- 不新增独立查询页。

## Section / apps

`ListSearchBar` 可覆写查询簇布局，不能再挂 SearchDrawer。
