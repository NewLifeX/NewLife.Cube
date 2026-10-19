/**
 * `CubeTable` 对外入口
 *
 * 用法（默认列表页一行到位）：
 * ```vue
 * <template><CubeTable /></template>
 * ```
 *
 * 定制走插槽；需要自管引擎时 `useCubeEngine({ routePath })` 后 `:context="ctx"` 传入。
 */
export { default as CubeTable } from './CubeTable.vue';
export { default as CubeTableSearch } from './CubeTableSearch.vue';
export { default as CubeTableToolbar } from './CubeTableToolbar.vue';
export { default as CubeTableGrid } from './CubeTableGrid.vue';
export { default as CubeTablePagination } from './CubeTablePagination.vue';
export { default as CubeTableFormDialog } from './CubeTableFormDialog.vue';

export { useCubeTableActions } from './useCubeTableActions';
export type { CubeTableActionOptions } from './useCubeTableActions';
export { useCubeTableColumns } from './useCubeTableColumns';
export type { ColumnStorageLike, UseCubeTableColumnsOptions } from './useCubeTableColumns';
export { useCubeTableLov } from './useCubeTableLov';
export { saveBlob } from './downloadFile';

export type {
  CubeCellSlotProps,
  CubeColumnView,
  CubeGridFlags,
  CubeRowActionsSlotProps,
  CubeSearchControl,
  CubeSearchKind,
  CubeSectionSlotProps,
  CubeTableActions,
  CubeToolbarFlags,
} from './types';

export {
  BOOLEAN_FILTER_OPTIONS,
  FILE_EXISTS_OPTIONS,
  SEARCH_COLLAPSE_LIMIT,
  buildColumnViews,
  buildSearchControl,
  buildSearchControls,
  cellBoolean,
  cellParts,
  cellText,
  collapseSearchControls,
  columnItemsFromFields,
  defaultColumnsStorageKey,
  dialogShapeFor,
  dictOptions,
  isExpandableCell,
  isOverflowTooltipCell,
  normalizeMulti,
  resolveGridFlags,
  resolveToolbarFlags,
  truncate,
  widthForControl,
} from './cubeTable.logic';
