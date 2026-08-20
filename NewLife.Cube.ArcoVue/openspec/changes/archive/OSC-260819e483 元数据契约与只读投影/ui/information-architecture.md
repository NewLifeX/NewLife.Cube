# OSC-260819e483 UI 信息架构

框架：Arco Design Vue；表格 VisActor VTable。业务逻辑在 `useXxx.ts`，`.vue` 保持薄。

## P1

- 表单必填：`isFieldRequired` 认 `nullable===false` 或 `required===true`。布尔列可有星号，开关关断仍可提交。
- 实体 POST/PUT/PATCH（含批量改字段）带头：`X-Cube-Field-Validation: 1`。GetList / GetPage / 评论 GET 不加。

## P2

- 有条件才传 `viewFilter`（`all`/`any`，与 OSC-0015 相同，无嵌套 groups）。JSON 由查询串传递，超长由后端 400。
- 排序：`buildSortPayload` 维持单列 `sort`+`desc`。不做多列排序控件。
- 服务端未下推时保留 `matchesViewFilter`（仅当前页）。分页器 total 在「本页被前端删减且本页已含全部后端行」时才改写（现码逻辑保留）；跨页不完整时不假装滤完。

## P3

- 高级菜单「批量修改」：对话框选**一个**白名单字段 + 新值 + 已选 keys → `POST BatchUpdateFields`。空选中禁用提交。
- 轻量列失焦 → `PATCH PatchFields` `{id, values}`；失败 Message.error，单元格回滚到失焦前值。
- 布尔列仍走工具条/徽标 EnableSelect（GET），不走批量改字段。
- 无 Update 权限：入口禁用（现有权限位），不发请求。

## P4

- 历史 Tab 仍读 `Admin/Log`。Update 行解析 `Field=old -> new` 画表（列：字段显示名 / 旧 / 新）；失败走现有 `historyRemark` 纯文本。
- 空历史（未开 `LogOnChange`）：保持空列表，不额外提示「请开日志」（避免教用户改全局）。
- 评论 `@` 选人（最多 20）：Id 放 `mentionUserIds`，正文纯文本。不做评论表新列。选人失败静默跳过非法 Id。

## P5

- 不增加公式编辑器、不增加 projections 列、不增加多图看板。
- InsightPanel：`showChart` 时若无开发者 `GetChartData`，用 ViewProfile 里的 `insight.chartOption` 灌入当前列表行后 `setOption`。无 option 时给「配置图表」；编辑 JSON、预览、保存走现有 ViewProfile。一张图，清洗掉 data 再存。
- 图表随**已加载列表**变，不另拉 1000 行。

## 明确不做

- Cube.Vue / NaiveUI 改版、字段级权限 UI、双向写回、用户公式编辑器、多列排序 UI、MVC PATCH 页面。
