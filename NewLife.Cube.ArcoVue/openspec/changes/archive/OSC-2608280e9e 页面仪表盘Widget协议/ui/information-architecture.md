# OSC-2608280e9e UI 信息架构

框架：Arco Design Vue（https://arco.design/vue/docs/start）。图表：已有 ECharts 初始化（`echartsTheme.ts`），平台模板 option，禁止配置器粘贴任意 option。图标：IconPark，新图标先查官方库再注册 `iconRegistry.ts`。业务在 `useXxx.ts`，`.vue` 保持薄。

## 洞察槽在 DefaultList 中的位置

自上而下固定，用户不能增删页面区块：

1. 页头 Section（可选）
2. **洞察槽（本号）** — `InsightPanel` = `WidgetHost` 的 insight 表面
3. 命名视图 Tab + 工具栏（筛选构建器仍在此）
4. 六视图舞台
5. 分页
6. 右侧 RecordDrawer

## 空态

- `dashboard` 未配置且无旧 insight 合成：槽高度 **0**，不留空白条。
- `canEdit`：槽区域 hover 或工具栏「仪表盘」出现「添加部件」。
- 显式 `widgets:[]`：高度 0 + 同一添加入口（与「继承模板有卡」区分：后者直接渲染模板卡）。

## 有部件

- CSS grid：`grid-template-columns: repeat(12, 1fr)`，列间距 12px，行间距 12px。
- `layout.w`：3 / 4 / 6 / 12 对应 `span`。`h` 本号仅作 min-height 档（1=88px，2=160px，3=240px，4=320px），不跨行拖高。
- 密度：`comfortable` 卡片 padding 16px；`compact` 10px。跟随 UserProfile，Host 读 CSS 变量不读 store 亦可（`--cube-space-*`）。
- 卡片：白底、1px 淡边、圆角 `var(--cube-radius-md)`，无厚阴影。标题 13–14px，主值 24–28px。

## 指标卡

- 左图标（可选）+ 标题 + 主值 + 次文案（named 的 trend 或「较筛选后」）。
- 若 Query 带 `timeField` items：底部 sparkline 高 32px，不可点选数据点。
- 点击：`style.clickUrl` 或 `/{typePath}`；跨实体不把宿主筛选写入 URL（仅同源把可序列化 filter 作为 query 的后续增强，本号可只跳列表）。
- 加载：Arco `a-spin` 罩单卡。错误：红字一行，不 Toast 刷屏。

## 迷你图表

- 标题行 + 图表区（默认高 200px，`h>=3` 时 280px）。
- 模板：sparkline/line（时间桶）、bar/pie（groupBy TopN）。图例最多 6 项，其余归「其它」仅 pie。
- 无「配置 JSON」按钮。编辑走统一 ConfigDrawer。

## 迷你看板

- `KanbanBoard` `compact=true`：列头 + 计数；卡片仅标题（+ 可选封面）；无详情/编辑/删除；列内最多展示 `limit`（默认 30）。
- 横向滚动列，不出现六视图那种行操作。

## 配置抽屉

- `a-drawer` `placement="right"`（遵守记录抽屉方向规则：配置类同样右侧）。
- 宽 480px。步骤：**类型**（三平台 kind 卡片）→ **数据源**（当前实体置顶 + Sources 搜索）→ **字段/样式** → 底部预览。
- 跨实体出现 `linkFilter` 表（宿主字段 / 源字段）；可空，空则预告「不随列表筛选」。
- 保存写入 DashboardJson。取消不写。
- 卡片菜单：编辑、左移、右移、删除。无拖到任意坐标。

## 权限与降级

- LockedWidget：锁图标 + 「无查看权限」+ 源 typePath 文案。不出现跳登录。
- UnknownWidget：「未安装该部件（kind）」+ 删除。
- 未联动角标：`a-tag` size small「未联动」。

## 旧 insight

- 合成卡右上「来自旧图表」tag。菜单：升级 / 删除。无编辑 JSON。
- ViewConfigDrawer 不再提供统计/图表双开关作为主入口。

## 响应式

- 内容区 < 800px：所有卡强制 `span 12`（读 container 宽度，不读浏览器断点亦可）。本号不做独立移动端信息架构。

## 明确不做的交互

- 画布拖拽、缩放、嵌套、部件内再加筛选构建器。
- 点击迷你看板改宿主筛选（本号不反向联动）。
- 第三方市场、上传 Vue 文件。
- 首页工作台布局（后续表面复用 Host，本号无独立首页 UI）。
