# OSC-2608178bdb Tasks — 列表自定义链接分流放置

> 依赖：T1 类型归一 → T2 分流纯函数 → T3 ops 拼装 → T4 列表接线与单元格 → T5 多视图 → T6 文档门禁。  
> 触及前端代码的任务必须勾选测试/构建。

## 元数据与分流

- [x] **T1 FieldMeta / toFieldMeta 保留 dataAction 与 hasTypeName**
  - `web/src/core/types/field.ts`：`dataAction?: string`；`hasTypeName?: boolean`
  - `fieldNormalize.ts`：拷贝 `dataAction`/`DataAction`；`hasTypeName = !!(field.typeName \|\| TypeName)`（在 String 回落前）
  - 补/改 `fieldNormalize.link.spec.ts`
  - [x] 测试通过

- [x] **T2 listLinkFields 分流**
  - 新增 `web/src/core/utils/listLinkFields.ts` + `listLinkFields.spec.ts`
  - 实现 `classifyListLink` / `partitionListFields` / `OPS_LINK_INLINE_MAX=2`
  - spec 覆盖 design §3 真值表全行 + 多链接 partition
  - [x] 测试通过

## 操作列

- [x] **T3 opsAction 扩展自定义链接**
  - `opsAction.ts`：`OpsCustomLink`、`buildOpsPartsWithLinks`、溢出列表
  - 顺序：detail → edit → delete → 自定义直出≤2 → auto≤3；overflow = 其余自定义
  - `opsAction.spec.ts` 覆盖顺序与截断
  - [x] 测试通过

- [x] **T4 useListOpsLinks + 点击行为**
  - 新增 `web/src/views/crud/useListOpsLinks.ts`：`resolveUrl` + 导航 / action 请求 + 刷新列表
  - action：`cubeApi.client` GET（`/api` 前缀）；导航：router.push / `_blank`
  - 冒烟补强：`@cube/page-utils` `lookupRowField`（`{ID}`↔`id` 大小写容错），避免链接 `?parentId=` / `?userId=` 空参
  - [x] 测试通过（`useListOpsLinks.spec.ts`）

## 列表与表格

- [x] **T5 useListQuery / listContext 接线**
  - `selectListColumns` 过滤 ops 列；`opsCustomLinks` computed；`tableColumns.cellLink`
  - [x] 测试通过

- [x] **T6 VTable __ops 直出 + 更多**
  - `useListTable.ts` / `ListTable.vue`：自定义直出、「更多」外挂菜单、单元格可点
  - [x] 测试通过 [x] 构建通过

## 多视图

- [x] **T7 card / kanban 同源操作**
  - `RecordCard` / `CardList` / `KanbanBoard`：底栏直出+更多
  - 冒烟补强：操作按钮 `white-space:nowrap` + `flex-shrink:0` 防竖排；`ResizeObserver` 按 ops 区宽计算自定义链接直出数，溢出进「更多」、禁止折行
  - [x] 测试通过（`useRecordCard.spec.ts`）

- [x] **T8 calendar / gantt 详情次级入口**
  - `RecordDrawer` 详情标题区展示 `opsCustomLinks`
  - [x] 代码完成（手工冒烟见 T10）

## 收尾

- [x] **T9 文档同步**
  - `web/README.md`、`Doc/功能清单.md`、迁移方案能力矩阵一行
  - [x] 文档完成

- [x] **T10 全量门禁与冒烟**
  - `pnpm --filter @cube/arco-vue run test`：验收门禁 **50 文件 472 通过**
  - `pnpm --filter @cube/arco-vue run build`：vue-tsc + vite **通过**
  - 手工：User「链接」、CronJob「日志/马上执行」——代码路径已具备；完整浏览器点验记为残余（🟢）
  - [x] 测试通过 [x] 构建通过 [x] 冒烟（代码路径 + 已知限制）
