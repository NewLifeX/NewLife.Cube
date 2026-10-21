# CubeTable

魔方默认列表页的**单一集成组件**：一个组件就是一个列表页，从上到下固定五段
**操作栏 → 搜索条件 → 表格统计 → 表格 → 分页**，结构与视觉对齐 SmartMES 前端的
`src/components/ListPage`（MES 全站 62 个列表页用的就是它）。定制一律走具名插槽。

当前状态：**默认列表页的唯一实现**。`core/views/index.vue` 已只剩这一条渲染路径。

---

## 五段结构（与 MES 同一套尺寸）

| 段 | 组件 | 类名 | 对应 MES |
| --- | --- | --- | --- |
| ⓪ 页头（可选） | 页面经 `#header` 传入 | — | 页面自绘标题 |
| ① 操作栏 | `CubeTableActions` | `.cta` | `.page-actions` |
| ② 搜索条件 | `CubeTableSearch` | `.cts-wrap` | `.search-wrap`（一体式：常驻每行三列栅格） |
| ③ 表格统计 | `CubeTableMetrics` | `.ctm` | `.table-toolbar`（左指标，右 追加控件 / 设置列 / 刷新） |
| ④ 表格 | `CubeTableGrid` | `.ct-table-wrap` | `.table-wrap` + `el-table height=100%` |
| ⑤ 分页 | `CubeTablePagination` | `.ctp` | `ListPage/PaginationBar`（左「共 N 条」，右分页器 + 每页条数下拉） |

### 滚动模型：视口自适应，只有表格内滚动

`.cube-table` 高 100%、页面本身不出滚动条，只有表格数据区滚动（③④⑤ 包在 `.ct-panel` 里，
`flex:1 + min-height:0`；`el-table height=100%`）。搜索区 `flex-shrink:0`，展开时变高、
表格区自动变矮，收起时反之——**筛选展开/收起天然自适应，无需额外代码**。

> **宿主契约（必读，否则分页会被顶出视窗、整页滚动）**：`height:100%` 依赖祖先链给出**确定高度**。
> 宿主内容区必须 `height:100vh; overflow:hidden`（框架自带 `TopMenu` 布局即 `calc(100vh - 页头)`），
> 仅 `min-height:100vh` 不算确定高度，百分比高度不生效→表格按内容撑高。`CubeTable` 自身已加
> `overflow:hidden` 兜底，但撑不满的根因仍在宿主高度链。

---

## 最小用法

```vue
<!-- 默认列表页：按当前路由推导实体，拉 GetPage 元数据，渲染完整 CRUD -->
<template>
  <CubeTable />
</template>

<script setup lang="ts">
import { CubeTable } from '@newlifex/cube-vue/core/components/CubeTable';
</script>
```

```vue
<!-- 嵌入 / 自管引擎：自己建上下文，一个组件挂两张表也互不干扰 -->
<template>
  <CubeTable :context="alarm" />
  <CubeTable :context="history" />
</template>

<script setup lang="ts">
const alarm = useCubeEngine({ apiPrefix: '/api/Andon/Alarm' });
const history = useCubeEngine({ apiPrefix: '/api/Andon/History', pageSize: 10 });
</script>
```

```vue
<!-- 业务自写列（与 MES 同一用法）：写 #default 后元数据生成列整块让位 -->
<template>
  <CubeTable :context="ctx" :metrics="metrics">
    <el-table-column label="编号" prop="Id" width="90" />
    <el-table-column label="状态" prop="Status">
      <template #default="{ row }"><el-tag>{{ row.Status }}</el-tag></template>
    </el-table-column>
  </CubeTable>
</template>
```

```vue
<!-- 配置驱动：后端没配 GetPage 也能用，字段由页面自己给 -->
<template>
  <CubeTable :context="ctx" />
</template>

<script setup lang="ts">
const ctx = useCubeEngine({
  apiPrefix: '/api/Equipments/Equipment',
  fields: {
    listFields: [
      { name: 'Code', displayName: '编号', typeName: 'String' },
      { name: 'Enable', displayName: '启用', typeName: 'Boolean' },
    ],
    addFields: [{ name: 'Code', displayName: '编号', typeName: 'String', nullable: false }],
  },
});
</script>
```

上下文三级解析（`CubeTable` 的 `context` prop → 父级 `inject(CubeEngineKey)` → 按当前路由自动创建）
与方案文档一致。若父级已 `useCubeEngine()` 而 `CubeTable` 没传 `context`，它会复用父级引擎，不会重复创建。

---

## 插槽

命名沿用 MES ListPage：`#actions` / `#actions-extra` / `#search-bar` / `#search-advanced`
/ `#metrics` / `#toolbar-extra` / `#table` / `#footer` / `#footer-extra` 是**加内容或换内容**，
不是换掉整段骨架。

| 插槽 | 作用域 | 默认渲染 | 用途 |
| --- | --- | --- | --- |
| `header` | `{ ctx }` | 无 | 页头（标题、面包屑、提示条） |
| `actions` | `{ ctx }` | 无（内置按钮仍在） | 操作栏左侧业务按钮，排在「添加」之前 |
| `actions-extra` | `{ ctx }` | 无 | 操作栏右侧 |
| `search` | `{ ctx }` | `CubeTableSearch` | **Cube 扩展**：整块换掉搜索区 |
| `search-bar` | `{ advanced, toggle, ctx }` | 搜索条（常驻三列栅格） | 换搜索栏那一行（自己管展开态时用） |
| `search-advanced` | `{ ctx, controls }` | 无（默认条件已内联到搜索条） | 业务自带条件面板；有它时即使没有元数据条件也给展开入口 |
| `metrics` | `{ ctx, metrics }` | `metrics` 数组渲染 | 换统计区 |
| `toolbar-extra` | `{ ctx }` | 无 | 统计行右侧追加控件，排在齿轮之前 |
| `table` | `{ ctx, data, loading }` | `CubeTableGrid` | 整块换掉表格（卡片视图等） |
| `default` | `{ visibleColumns, resetColumnVisibility }` | 按元数据生成列 | **业务自写列**，写了它生成列即让位 |
| `col-<字段名>` | `{ row, column, value, index }` | 内置按控件渲染 | 单列单元格替换，如 `#col-Status` |
| `row-actions` | `{ row, index, ctx }` | 查看 / 编辑 / 删除 | 操作列按钮整列替换（不参与自适应；要加动作用 `extraRowActions`） |
| `empty` | — | 「暂无数据」/失败原因 | 空态替换 |
| `footer` | `{ ctx, total, page, pageSize }` | `CubeTablePagination` | 整块换掉分页栏 |
| `footer-extra` | `{ ctx }` | 无 | 在「共 N 条」旁追加（如已选条数） |
| `form` | `{ ctx }` | `CubeTableFormDialog` | 弹窗替换（要用 `el-drawer` 就在这里换） |

子组件也可单独取用：`CubeTableActions` / `CubeTableSearch` / `CubeTableMetrics` /
`CubeTableColumnSetting` / `CubeTableGrid` / `CubeTablePagination` / `CubeTableFormDialog`，
全部以 `ctx` 为 prop（受控、可单测）。

---

## 搜索条：常驻每行三列，收起态也占满第一行

搜索区常驻**每行固定三列**的栅格（`.cts-grid`，`repeat(3, minmax(0,1fr))`，
窄屏 ≤900px 降为两列），不再弹独立条件面板：

- 格序：第一格关键字框，第三格（第一行行尾）固定放「展开/收起胶囊 + 重置 + 搜索」，
  其余每格一个搜索条件从第二格起自动铺格；**动作格用 CSS 钉死在第一行最右列**
  （`grid-row:1; grid-column:-2/-1` + `dense` 补位），展开后不随条件流到最后一行；
- **收起态就露第一行三格**：关键字 + 第一个条件 + 动作钮（展开/重置/搜索），其余条件不渲染；
- 展开把剩余条件铺进后续行，每行仍是三列（第一行放 关键字+条件1，条件从第二行起每行三个）；
- 格内标签固定、控件吃满剩余宽度，同列宽度天然对齐；范围类（数值/时间/日期）两个子控件格内等分；
- `#search-advanced` 仍渲染在 `.cts-panel` 面板区（业务自带表单时用），默认条件不再进面板。

---

## 操作列：宽度自适应与「更多」收起

操作列宽度按单行动作数自适应（`layoutRowActions`），旧固定 168px 会把「删除」裁成只剩图标：

- ≤4 个动作全部直接显示，列宽逐个增长（3 个即默认列表的 查看/编辑/删除 → 208px）；
- **超过 4 个**只露前 3 个，第 4 个位置是「更多」下拉，后加的动作一律收进下拉、不再撑宽列（按 4 单元封顶）；
- 列宽取当前页各行动作数的最大值；行级 `visible(row)` 藏了动作的那一行不足 4 个时直接全显，不硬塞空的「更多」；
- 页面显式传 `grid.actionWidth` 则覆盖自适应。

业务页加行操作用数据驱动的 `extraRowActions`（而不是整列替换），后续新加的动作自动参与上述布局：

```vue
<CubeTable :extra-row-actions="[
  { key: 'dispatch', label: '派工', icon: Promotion, onClick: (row) => dispatch(row) },
  { key: 'suspend', label: '停用', type: 'warning', onClick: (row) => suspend(row),
    visible: (row) => row.Enable === true },
]" />
```

---

## 宿主 props

| prop | 类型 | 默认 | 说明 |
| --- | --- | --- | --- |
| `context` | `CubeEngineContext` | `null` | 外部引擎；传入则不再自动创建 |
| `autoEngine` | `boolean` | `true` | 无 context 且无注入时是否按路由建引擎 |
| `apiPrefix` / `routePath` | `string` | — | 自动创建时指定实体 |
| `pageSize` | `number` | `20` | 自动创建时的每页条数 |
| `queryParams` | `() => Record<string, unknown>` | — | 惰性附加查询条件（MES `queryParams` 同名），每次取数/导出时合并 |
| `searchPlaceholder` | `string` | `'搜索关键字'` | 关键字框占位 |
| `showSearch` | `boolean` | `true` | 是否显示搜索区 |
| `metrics` | `CubeTableMetric[]` | `[]` | 统计指标（图标 / 名称 / 值 / 环比） |
| `showRefresh` | `boolean` | `true` | 统计行右侧刷新按钮 |
| `pageSizes` | `number[]` | `[10,20,50]` | 每页条数档位（与 MES 同档） |
| `totalUnit` / `sizeUnit` | `string` | `'条'` | 「共 N 条」/「N 条/页」单位 |
| `columnsSetting` | `boolean` | `true` | 统计行「设置列」（localStorage 记忆） |
| `columnsStorageKey` | `string` | `cube-table:columns:{apiPrefix}` | 迁移旧配置时可显式指定 |
| `showAdd` / `showBatchDelete` | `boolean` | 跟随后端 | 添加 / 批量删除按钮 |
| `showImport` / `showExport` | `boolean` | `true` | 导入 / 导出按钮 |
| `showDownloadTemplate` | `boolean` | `false` | 下载模板（后端要有 `DownloadTemplate` Action） |
| `exportFormat` | `string` | `'xlsx'` | 导出格式 |
| `importAccept` | `string` | — | 导入文件类型限制 |
| `confirmDelete` | `boolean` | — | 删除二次确认（不传跟随 `pageSetting.doubleDelete`） |
| `extraRowActions` | `CubeRowAction[]` | `[]` | 操作列追加动作：排在内置 查看/编辑/删除 之后，参与列宽自适应与「更多」收起 |
| `grid` | `CubeGridFlags` | — | 勾选列 / 索引列 / 操作列 / 双击编辑；`grid.actionWidth` 显式指定时不再自适应 |

与 MES 的**有意差异**（其余口径一致）：

1. 列、条件、表单默认由 `GetPage` 元数据生成（MES 是页面各写一遍 `columns` / `formFields`）；
   要自己写列就用 `#default`。
2. 取数失败就地说明在表格空态里，不再顶部弹错误条（`ElMessage` 由引擎统一提示）。
3. 「图表」这类后台专属能力不属于组件：由页面经 `#toolbar-extra` 挂到统计行右侧，与「设置列」「刷新」同组
   （共用 `core/views/components/ListChartButton.vue`；操作栏第一位留给业务按钮）。

---

## 默认页只有一条渲染路径

`core/views/index.vue` 现在只是 `CubeTable` 的薄壳（61 行）：页头 + 图表按钮，其余全部交给组件。
旧的「双路径 + 灰度开关」已删除，理由是全仓真正需要兼容的面太小：

| 兼容对象 | 实测数量 | 处置 |
| --- | --- | --- |
| Section 覆盖页（`ListTableContent.vue` 等） | **1**（`/Admin/Parameter`） | 迁成自定义视图 `views/admin/parameter/index.vue`：`CubeTable` + `#col-Kind` 一个插槽，删掉整张手写表格 |
| 受控模式（父级传 `data` / `loading` / `total`） | **0** | 连同一组 props 与 emits 一起删除；要自己供数据请用 `#table` 插槽 |
| `?cubeTable=0/1` 灰度开关 | — | 删除 |
| MES 前端使用 Section 机制 | **0**（未调 `registerPageSections`） | 回流不涉及 |

仍保留的只有 `ListPageHeader` 一处覆盖入口（页头标题），因为它对渲染结构没有任何影响。

副作用：`ListSearchBar` / `ListToolbar` / `ListTableContent` / `ListPagination` / `ListPageFooter`
这 5 个组件自此**在仓库内 0 引用**（此前默认页是唯一使用者；`ListPageHeader` 还有 2 处）。
它们是已发布包的公开导入路径，所以本次没有一并删除，见下表「旧 Section 组件下线」。

单列定制的参考实现：`apps/cube-admin/src/views/admin/parameter/index.vue`。


---

## 契约要点（排查问题时先看这里）

- **分页 1-based**：`pageIndex` / `pageSize` 由引擎发请求时现取当前页码补上，组件不各算一份。
- **查询参数**：关键字走 `q`；数值/时间区间按 `名称_min` / `名称_max` 两个键提交（与旧 Section 组件同口径）；
  `''` / `null` / `undefined` 被丢弃，但 **`0` 与 `false` 保留**（否则「数量=0」「启用=否」永远查不出来）。
- **写操作是集合路由**：`POST {type}`、`PUT {type}`（主键在 body）、`DELETE {type}?id=`。
  后端 `EntityController` 只声明了 `[HttpPut("/api/[area]/[controller]")]` 与
  `[HttpDelete("/api/[area]/[controller]")]`，路径式 `{type}/{id}` 命中不到路由会 **405**，
  所以方案草案 §4.2 里的路径式写法没有照搬。
- **编辑/查看先取详情**：`GET {type}/Detail?id=`。列表行是「显示态投影」（值集已翻译成标签、
  列可能不全），直接铺进表单会把标签当值提交。
- **整行回传前清洗 PascalCase 别名键**：回包里 `enable:true` 与 `Enable:1` 并存时，
  反序列化会取到后出现的旧值 → 实体与库里一致 → 更新被**静默忽略**（接口照样返回成功文案）。
- **详情回包期间的弹窗是骨架屏**：否则用户已经往默认值上敲了字，回包一到就把输入覆盖掉。
- **导入 0 行降级为警示**：后端 `ImportFile` 的 `message` 里「共0行」「成功[0]行」意味着一行都没落库
  （多半是表头和字段名/显示名对不上），此时报「成功」就是在骗人。
- **导出按 `Content-Type` 定扩展名**：xlsx 的 `spreadsheetml` 必须识别，否则会存成打不开的 `.bin`。
- **值集标签只有一个来源**：`LovSelect` 与 `core/components/LovSelect/lovStore.ts` 的会话级缓存。
  引擎**不**预拉 LOV、不另建缓存（`deps.lookup` 保留但默认不启用）。
- **枚举列不会停在码值**：`resolveColumnLabels()` 的 ENUM 快路径只读 `getCachedMeta()`、自己不拉 meta，
  而 `labelCache` 是普通 `Map`（非响应式）。所以 `useCubeTableLov` 先 `getMeta()` 播种、
  再把结果抄进本地响应式副本；共享缓存已命中时也必须抄，否则单元格会一直显示 `1` 而不是 `启用`。
  （`ListTableContent.vue` 这两个坑都在，本组件修掉了，没有回改旧组件。）
- **必填口径只有一个来源**：`core/utils/fieldControl.ts` 的 `isRequiredField()`，
  没有往 `FieldMeta` 里加草案设想的 `required` 同义字段。

---

## 相对旧 Section 组件的口径改进

只影响本组件，不改动 `ListSearchBar.vue` / `ListTableContent.vue`：

1. 日期类单元格有格式化（`2026-10-19T08:30:00` → `2026-10-19 08:30:00`）。旧组件对 `date` / `time`
   两类没有分支，直接把后端原始串透出来。
2. 带内联字典 `dataSource` 的枚举字段在搜索区出下拉（旧组件只认 `lovCode`，这类字段退化成文本框）；
   反向也补了一处降级：`typeName === 'Enum'` 但没有 `lovCode` 时不再渲染空的 LOV。
3. 枚举值集列不再停在码值：先 `getMeta()` 播种再抄进本地响应式副本（见上面「契约要点」）。

---

## 还没做的（有意留给后续阶段）

| 项 | 现状 | 需要动的东西 |
| --- | --- | --- |
| 旧 Section 组件下线 | `ListSearchBar` / `ListToolbar` / `ListTableContent` / `ListPagination` / `ListPageFooter` 已 0 引用（实测：全仓含 MES 前端 `import` 命中数为 0），但文件与 `useSections` 的对应键仍在 | 方案 §7 P6：确认外部项目没有直接 import 这些路径后删除；`ListPageHeader` 保留（默认页与参数页在用） |
| `@newlifex/page-logic` | 未接入。该包已有 `loadFields/loadData/add/remove/deleteSelect` 全家桶，但 `cube-vue` 目前零引用 | 引擎数据层收敛到一处，属架构决策，不该由组件层单方向定 |
| 组件目录登记 | `docs/reference/component-catalog.md` 未加条目 | `add-component.md` 第 6 步要求登记；纯文档，留待评审后一并加 |
| 回流 SmartMES | MES 侧 `ListPage` 未改，仍用自身组件 | 待 Cube 侧单独验证通过后再定接入方式 |

---

## CT（组件视觉回归）

`CubeTable.story.ts` 定义 8 个变体，`CubeTable.ct.spec.ts` 共 16 条用例（9 条带像素基线 + 7 条纯行为）：
`Default`（五段结构 + 全控件形态一屏）、`ReadOnly`、`Empty`、`LoadFail`、`Metrics`（指标行）、
`OneFilter`（只有一个条件时的搜索栏）、`FormDialog`、`RowActions`（操作列 5 个动作：前 3 + 「更多」，
含展开下拉点选真实触发回调的行为断言）。
story 用内存版 `deps` 构造上下文，**不依赖后端**；基线目录 `*-snapshots/` 已在 `.gitignore` 内（各机自产）。

首次生成基线用 `--update-snapshots=missing`（`pnpm test:ct:update <过滤词>` 会把过滤词当成 mode 参数报错）。

为了能在 gallery 里跑起来，动过两个既有文件，都不改任何组件行为：

| 文件 | 改动 | 为什么必须 |
| --- | --- | --- |
| `ct/stories.ts` | 加 1 行 import + 展开进 `stories` | 该文件自己的注释就规定「新增组件 story 只需在此处追加一行 import」；不注册则 `mountStory` 报「未知 story」 |
| `ct/vite.config.ts` | 配置虚拟模块的桩补上具名导出 `configData` / `currentEnv` | 原桩只给 `export default {}`，而 `core/configure/index.ts` 用的是具名导入 → 任何 import `core/utils/url` 的组件在 gallery 里整页白屏（报 `does not provide an export named 'configData'`）。形状对齐 `core/client.d.ts` 与 `vitest.config.unit.ts` 的同名桩 |

## 本地校验

```powershell
cd Cube/NewLife.Cube.Vue/web
pnpm exec vitest run --config vitest.config.unit.ts core/components/CubeTable core/engine
pnpm exec vue-tsc --build
pnpm exec eslint core/components/CubeTable core/engine core/utils/fieldMeta.ts
pnpm run check:theme
npx playwright test --config playwright-ct.config.ts --update-snapshots=missing CubeTable   # 首次生成基线
pnpm run test:ct          # 之后做像素比对
```

`vue-tsc --build` 在本机对 `apps/*` 与 `core/components/CbTable.vue` 有一批**既有**报错
（`DefaultRow` 与实体类型不匹配等），与本组件无关；判断口径是 `core/components/CubeTable`、
`core/engine`、`core/views/index.vue` 三处零报错。

测试文件与覆盖范围：

| 文件 | 覆盖 |
| --- | --- |
| `core/engine/__tests__/logic.spec.ts` | 参数过滤、导出扩展名、导入行数解析、列显隐合并、主键容错、整行清洗、日期格式、必填判定 |
| `core/engine/__tests__/createCubeEngine.spec.ts` | 首查/翻页/关键字/条件、附加条件、删除端点、批量删除与降级、提交校验与清洗、详情回显、导入 0 行、导出文件名、失败分支、换实体 |
| `core/engine/__tests__/useCubeEngine.spec.ts` | 路由推导、provide/inject、找不到引擎时报错、默认 deps 的解包与通知接线 |
| `__tests__/cubeTable.logic.spec.ts` | 搜索控件显示态、展开入口判定、列视图与主键列、工具栏/表格开关优先级、操作列自适应与收起、弹窗分档、单元格渲染 |
| `__tests__/useCubeTableActions.spec.ts` | 确认框取消不请求、`doubleDelete` 跟随后端、条数文案、导入复位、blob 落地后清空 |
| `__tests__/useCubeTableColumns.spec.ts` | 历史配置读取、按前缀分桶、迁移旧键、新增列默认显示、存储不可用不炸页 |
| `__tests__/CubeTableGrid.spec.ts` | 列头/行值/空值占位/0 与 false、值集翻译、图片附件、勾选列与操作列、操作列宽自适应与「更多」收起、`extraRowActions` 行级可见/禁用、插槽覆盖、双击编辑、业务自写列接管、`height=100%` 滚动模型、空态与失败态 |
| `__tests__/CubeTableSearch.spec.ts` | 一体式结构（收起态第一行三格 / 展开铺入全部条件、格序、无条件不给入口）、各类型条件出的控件、字典下拉、是/否三档、LOV 交 LovSelect、`#bar` 与 `#advanced` 接管 |
| `__tests__/CubeTableActions.spec.ts` | 按钮默认集、后端开关与页面意图优先级、只读收写入口、批量删除与勾选联动、导入文件框、业务按钮排在内置之前、空行不渲染 |
| `__tests__/CubeTableMetrics.spec.ts` | 指标/分隔线/图标/环比、`#metrics` 接管、右侧「追加→齿轮→刷新」顺序、空行不渲染 |
| `__tests__/CubeTableColumnSetting.spec.ts` | 列清单、未记录视为显示、按记录取消勾选、set/reset 事件 |
| `__tests__/CubeTablePagination.spec.ts` | 「共 N 条」与单位、`prev,pager,next,jumper` 布局、每页条数独立下拉、档位默认与覆盖 |
| `__tests__/CubeTable.spec.ts` | 五段自上而下顺序、上下文三级解析优先级、区块开关、每个插槽的替换/追加边界、自写列转发、设置列持久化、失败就地说明、导出下载落地 |
| `CubeTable.ct.spec.ts` + `CubeTable.story.ts` | 八个变体的像素基线与行为：勾选→批量删除带条数并弹确认、关键字回车走 `q`、重置回全量、设置列隐藏并落 localStorage、换每页条数回第 1 页、行内删除取消不动数据、编辑先取详情、操作列超 4 个收「更多」并点选触发、滚动模型（矮视口下验证页面不滚、分页在视口内、只有表体内滚、展开筛选面板变矮） |

