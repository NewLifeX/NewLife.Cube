# CubeTable

魔方默认列表页的**单一集成组件**：搜索、工具栏、表格、分页、新增/编辑/查看弹窗一次到位，
定制一律走具名插槽。设计依据是 `docs/architecture/cube-engine.md`（§3–§5）与
`docs/decisions/0005-cube-engine-context.md`，本目录是它俩的**组件层落地**。

当前状态：**全新组件，仓库内零引用**，未接入 `core/views/index.vue`，也不影响默认页面渲染链。

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

| 插槽 | 作用域 | 默认渲染 | 用途 |
| --- | --- | --- | --- |
| `header` | `{ ctx }` | 无 | 顶部横幅（提示、告警） |
| `search` | `{ ctx }` | `CubeTableSearch` | 整块搜索区替换 |
| `toolbar` | `{ ctx }` | `CubeTableToolbar` | 整块工具栏替换 |
| `toolbar-extra` | `{ ctx }` | 无 | 在默认工具栏内追加按钮（排在内置「设置列」之后） |
| `table` | `{ ctx }` | `CubeTableGrid` | 整块表格替换（卡片视图等） |
| `table-row-actions` | `{ row, index }` | 查看 / 编辑 / 删除 | 操作列按钮替换 |
| `col-<字段名>` | `{ row, column, value, index }` | 内置按控件渲染 | 单列单元格替换，如 `#col-Status` |
| `empty` | — | 「暂无数据」/失败原因 | 空态替换 |
| `pagination` | `{ ctx }` | `CubeTablePagination` | 分页替换 |
| `footer` | `{ ctx }` | 无 | 底部区域 |
| `form` | `{ ctx }` | `CubeTableFormDialog` | 弹窗替换（要用 `el-drawer` 就在这里换） |

子组件也可单独取用：`CubeTableSearch` / `CubeTableToolbar` / `CubeTableGrid` /
`CubeTablePagination` / `CubeTableFormDialog`，全部以 `ctx` 为 prop（受控、可单测）。

---

## 宿主 props

| prop | 类型 | 默认 | 说明 |
| --- | --- | --- | --- |
| `context` | `CubeEngineContext` | `null` | 外部引擎；传入则不再自动创建 |
| `autoEngine` | `boolean` | `true` | 无 context 且无注入时是否按路由建引擎 |
| `apiPrefix` / `routePath` | `string` | — | 自动创建时指定实体 |
| `pageSize` | `number` | `20` | 自动创建时的每页条数 |
| `columnsSetting` | `boolean` | `true` | 工具栏「设置列」（localStorage 记忆） |
| `columnsStorageKey` | `string` | `cube-table:columns:{apiPrefix}` | 迁移旧配置时可显式指定 |
| `toolbar` | `CubeToolbarFlags` | — | 按钮开关；不传的跟随后端 `pageSetting` |
| `grid` | `CubeGridFlags` | — | 勾选列 / 索引列 / 操作列 / 双击编辑 |
| `exportFormat` | `string` | `'xlsx'` | 导出格式 |
| `importAccept` | `string` | — | 导入文件类型限制 |
| `confirmDelete` | `boolean` | — | 删除二次确认（不传跟随 `pageSetting.doubleDelete`） |
| `pageSizes` | `number[]` | `[10,20,50,100]` | 每页条数档位 |

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

## 相对旧 Section 组件的两处口径改进

只影响本组件，不改动 `ListSearchBar.vue` / `ListTableContent.vue`：

1. 日期类单元格有格式化（`2026-10-19T08:30:00` → `2026-10-19 08:30:00`）。旧组件对 `date` / `time`
   两类没有分支，直接把后端原始串透出来。
2. 带内联字典 `dataSource` 的枚举字段在搜索区出下拉（旧组件只认 `lovCode`，这类字段退化成文本框）；
   反向也补了一处降级：`typeName === 'Enum'` 但没有 `lovCode` 时不再渲染空的 LOV。

---

## 还没做的（有意留给后续阶段）

| 项 | 现状 | 需要动的东西 |
| --- | --- | --- |
| 接入默认页 | `core/views/index.vue` 未改，默认页仍走裸 `request` + Section 组合 | 方案 §7 P3 后半段（含兼容桥与 e2e 回归） |
| `@newlifex/page-logic` | 未接入。该包已有 `loadFields/loadData/add/remove/deleteSelect` 全家桶，但 `cube-vue` 目前零引用 | 引擎数据层收敛到一处，属架构决策，不该由组件层单方向定 |
| 组件目录登记 | `docs/reference/component-catalog.md` 未加条目 | `add-component.md` 第 6 步要求登记；纯文档，留待评审后一并加 |
| 删除 `useSections` / Section 组件 | 全部保留原样 | 方案 §7 P6，必须在默认页切换并回归之后 |

---

## CT（组件视觉回归）

`CubeTable.story.ts` 定义 6 个变体，`CubeTable.ct.spec.ts` 对其中 5 个做像素基线 + 5 个做行为断言：
`Default`（全控件形态的一屏）、`ReadOnly`、`Empty`、`LoadFail`、`ManyFilters`（折叠/展开两态）、`FormDialog`。
story 用内存版 `deps` 构造上下文，**不依赖后端**；基线目录 `*-snapshots/` 已在 `.gitignore` 内（各机自产）。

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
pnpm run test:ct:update   # 首次生成基线
pnpm run test:ct          # 之后做像素比对
```

测试文件与覆盖范围：

| 文件 | 覆盖 |
| --- | --- |
| `core/engine/__tests__/logic.spec.ts` | 参数过滤、导出扩展名、导入行数解析、列显隐合并、主键容错、整行清洗、日期格式、必填判定 |
| `core/engine/__tests__/createCubeEngine.spec.ts` | 首查/翻页/关键字/条件、删除端点、批量删除与降级、提交校验与清洗、详情回显、导入 0 行、导出文件名、失败分支、换实体 |
| `core/engine/__tests__/useCubeEngine.spec.ts` | 路由推导、provide/inject、找不到引擎时报错、默认 deps 的解包与通知接线 |
| `__tests__/cubeTable.logic.spec.ts` | 搜索控件显示态、条件折叠、列视图与主键列、工具栏/表格开关优先级、弹窗分档、单元格渲染 |
| `__tests__/useCubeTableActions.spec.ts` | 确认框取消不请求、`doubleDelete` 跟随后端、条数文案、导入复位、blob 落地后清空 |
| `__tests__/useCubeTableColumns.spec.ts` | 历史配置读取、按前缀分桶、迁移旧键、新增列默认显示、存储不可用不炸页 |
| `__tests__/CubeTableGrid.spec.ts` | 列头/行值/空值占位/0 与 false、值集翻译、图片附件、勾选列与操作列、插槽覆盖、双击编辑、空态与失败态 |
| `__tests__/CubeTableSearch.spec.ts` | 关键字回车即查、各类型条件出的控件、字典下拉、是/否三档、LOV 交 LovSelect、折叠与展开 |
| `__tests__/CubeTableToolbar.spec.ts` | 按钮默认集、后端开关与页面意图优先级、只读收写入口、批量删除与勾选联动、导入文件框、插槽 |
| `__tests__/CubeTablePagination.spec.ts` | 1-based 页码直传、条数档位、动作交给引擎 |
| `__tests__/CubeTable.spec.ts` | 上下文三级解析优先级、每个插槽的替换边界、设置列持久化生效、失败重试、导出下载落地 |
| `CubeTable.ct.spec.ts` + `CubeTable.story.ts` | 六个变体的像素基线与行为：勾选→批量删除带条数并弹确认、关键字走 `q`、设置列隐藏并落 localStorage、行内删除取消不动数据、编辑先取详情 |
