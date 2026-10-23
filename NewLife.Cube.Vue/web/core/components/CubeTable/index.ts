/**
 * `CubeTable` 对外入口
 *
 * 用法（默认列表页一行到位）：
 * ```vue
 * <template><CubeTable /></template>
 * ```
 *
 * 结构对齐 MES ListPage：操作栏 → 搜索条件 → 表格统计 → 表格 → 分页。
 * 定制走插槽；需要自管引擎时 `useCubeEngine({ routePath })` 后 `:context="ctx"` 传入。
 */
export { default as CubeTable } from './CubeTable.vue';
export { default as CubeTableActions } from './CubeTableActions.vue';
export { default as CubeTableSearch } from './CubeTableSearch.vue';
export { default as CubeTableMetrics } from './CubeTableMetrics.vue';
export { default as CubeTableColumnSetting } from './CubeTableColumnSetting.vue';
export { default as CubeTableGrid } from './CubeTableGrid.vue';
export { default as CubeTablePagination } from './CubeTablePagination.vue';
export { default as CubeTableFormDialog } from './CubeTableFormDialog.vue';

export { useCubeTableActions } from './useCubeTableActions';
export type { CubeTableActionOptions } from './useCubeTableActions';
export { useCubeTableColumns } from './useCubeTableColumns';
export type { ColumnStorageLike, UseCubeTableColumnsOptions } from './useCubeTableColumns';
export { useCubeTableLov } from './useCubeTableLov';
export { saveBlob } from './downloadFile';
export {
  registerToolbarRenderer,
  getToolbarRenderer,
  clearToolbarRenderers,
  toolbarDisabled,
} from './toolbarRenderers';
export type { ToolbarRenderer } from './toolbarRenderers';
export {
  registerFieldRenderer,
  getFieldRenderer,
  resolveFieldRenderer,
  clearFieldRenderers,
} from '../../engine/fieldRenderers';
export type { FieldRenderer, FieldRendererProps } from '../../engine/fieldRenderers';
export {
  applyFieldPreset,
  applyPagePreset,
  clearPagePresets,
  getPagePreset,
  normalizePresetPath,
  registerPagePreset,
} from '../../engine/pagePreset';
export type {
  FieldPresetPatch,
  FormPreset,
  ListPreset,
  PagePreset,
  SearchPreset,
} from '../../engine/pagePreset';
export type { BackendToolbarItem } from '../../engine/types';

export type {
  CubeCellSlotProps,
  CubeTableMetric,
  CubeMetricBarFlags,
  CubeColumnView,
  CubeGridFlags,
  CubeRowAction,
  CubeRowActionsSlotProps,
  CubeSearchControl,
  CubeSearchKind,
  CubeSectionSlotProps,
  // 动作集类型改挂 `...Api` 名：`CubeTableActions` 这个导出名已让给同名组件
  CubeTableActions as CubeTableActionsApi,
  CubeTableProps,
  CubeToolbarFlags,
} from './types';

export {
  BOOLEAN_FILTER_OPTIONS,
  FILE_EXISTS_OPTIONS,
  applyLinkTemplate,
  fieldHeader,
  normalizeTextAlign,
  buildColumnViews,
  buildSearchControl,
  buildSearchControls,
  buildToolbarPayload,
  toolbarKeyField,
  toolbarQueryString,
  cellBoolean,
  cellLinkUrl,
  cellParts,
  cellRawValue,
  cellText,
  cellTitle,
  showSearchToggle,
  columnItemsFromFields,
  defaultColumnsStorageKey,
  dialogShapeFor,
  dictOptions,
  isExpandableCell,
  isOverflowTooltipCell,
  linkTemplateHasValue,
  layoutRowActions,
  MAX_VISIBLE_ACTIONS,
  normalizeMulti,
  resolveGridFlags,
  resolveToolbarFlags,
  splitRowActions,
  truncate,
  widthForControl,
} from './cubeTable.logic';
