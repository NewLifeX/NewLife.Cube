# 页面呈现配置（preset）参考

纯前端呈现补丁，用于微调 `/meta` 下发的字段、列序、默认显隐和表格开关，不写组件、不发请求。合并顺序：`/meta` → preset → 用户列偏好（「设置列」保存的显隐与列序）。

## 入口

```ts
import { registerPagePreset } from '@newlifex/cube-vue/core/engine/pagePreset';

registerPagePreset('/Admin/Menu', {
  list: {
    patch: {
      ID: { visible: false },
      DisplayName: { label: '菜单名称', width: 260, fixed: 'left' },
    },
    order: ['DisplayName', 'Type', 'Sort'],
  },
  search: { remove: ['ParentID'] },
  toolbar: { remove: ['RefreshName'] },
  grid: { actionWidth: 200, treeExpandAll: false },
});
```

路径注册与查找均归一化为「小写、以 `/` 开头、去尾斜杠」，`/Admin/Menu` 与 `/admin/menu/` 等价。代码位置：`core/engine/pagePreset.ts`。

## `PagePreset`

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `setting` | `Partial<PageSetting>` | 覆盖页面设置（如 `enableAdd`） |
| `grid` | `CubeGridFlags` | 表格开关；由默认列表页透传给 `CubeTable` 的 `grid` 入参 |
| `list` | `ListPreset` | 列表字段与行操作 |
| `form` | `FormPreset` | 新增/编辑/详情表单 |
| `search` | `SearchPreset` | 搜索条件 |
| `toolbar` | `ToolbarPreset` | 后端下发的工具栏按钮 |

### `grid`（`CubeGridFlags`）

| 项 | 说明 |
| --- | --- |
| `actionWidth` | 操作列宽下限（px）；实际更宽时以实测为准 |
| `treeExpandAll` | 树形表是否默认展开全部；不传沿用框架默认（展开），`false` 为默认折叠 |
| `selection` / `indexColumn` / `rowActions` / `doubleClickEdit` | 不传跟随后端 `pageSetting` |

### `ListPreset`

| 项 | 说明 |
| --- | --- |
| `remove` | 彻底移除字段：不在列表渲染，也不在「设置列」候选中出现 |
| `patch` | 字段级补丁（键名大小写不敏感），见下表 |
| `append` | 追加元数据没有的字段（完整 `FieldMeta`） |
| `order` | 列顺序；未列出的字段保持元数据顺序排在后面 |
| `actions` | 追加行操作，与页面 `extraRowActions` 一起参与列宽计算 |

`FormPreset`：`remove` / `patch` / `append` / `columns`（1~3 列）/ `groupBy`。
`SearchPreset`：`remove` / `append`，可用 `q` 或 `keyword` 移除关键字框。
`ToolbarPreset`：`remove: string[]`，按后端按钮的 `name` 或 `action` 移除（大小写不敏感、忽略首尾空格）。后端按钮名通常是动作方法名（如 `RefreshName`），不受界面语言影响；只移除按钮，不影响接口本身。

### `FieldPresetPatch`

| 项 | 说明 |
| --- | --- |
| `label` | 表头文字（写入字段 `header`） |
| `width` | 数字为固定列宽；不设置时文本列走 `auto`（内部按 `min-width: 120` 弹性铺满） |
| `align` | `left` / `center` / `right` |
| `fixed` | `left` / `right` |
| `renderer` | 字段级渲染器名，见 §字段渲染器 |
| `hidden` | 彻底隐藏（连「设置列」候选也不显示） |
| `visible` | **默认是否显示**。`false` 时字段仍保留在 `listFields` 中，会出现在「设置列 → 展示列」里，用户可勾选显示 |
| `overflowTooltip` | `false` 关闭引擎列的溢出提示 |
| `visibleWhen` / `requiredWhen` | 行级/模型级显示与必填条件 |
| `defaultValue` | 新增表单初值，留空提交时回填 |

> `remove` 与 `visible: false` 的取舍：需要保留用户勾选能力时用 `visible: false`；确定永不需要（连候选都不出现）才用 `remove`。`hidden: true` 亦为永久隐藏，且不受用户列显隐覆盖。

## 列偏好与列序缓存

`useCubeTableColumns` 把「设置列」的选择写入 `localStorage`：

| 键 | 内容 |
| --- | --- |
| `cube-table:columns:{apiPrefix}` | 列显隐（字段名 → boolean） |
| `cube-table:columns:{apiPrefix}:order` | 用户列序（优先于 preset 的 `order`） |

因此调整 preset `order` 后，已保存列序的浏览器不会立刻生效。需要一次性迁移时，由应用删除 `:order` 键并记录版本标记（不动显隐键）：

```ts
const version = '2026-11-22.1';
const storageKey = 'cube-table:columns:/api/Admin/Menu';
const versionKey = 'app:menu-column-order-version';
if (localStorage.getItem(versionKey) !== version) {
  localStorage.removeItem(`${storageKey}:order`);
  localStorage.setItem(versionKey, version);
}
```

## 字段渲染器

`registerFieldRenderer(name, { component, props? })` 按名称注册单元格/表单/搜索控件。列表单元格入参：`field` / `value` / `row` / `column` / `ctx`；表单与搜索另有 `model` / `disabled` / `ctx`。未注册的名称会告警一次并回退内置控件。代码位置：`core/engine/fieldRenderers.ts`。

```ts
import { registerFieldRenderer } from '@newlifex/cube-vue/core/engine/fieldRenderers';

registerFieldRenderer('menuIcon', { component: MenuIconCell });
```

preset 中通过 `renderer: 'menuIcon'` 引用。字段的 `renderer` 只挂在列表元数据上时，不影响表单与搜索控件。

## 相关

- 默认列表页：`core/views/index.vue`（读取 preset 的 `grid`）
- 区块覆盖：`docs/guides/customize-page.md`
- 渲染链路：`docs/reference/default-page-engine.md`
