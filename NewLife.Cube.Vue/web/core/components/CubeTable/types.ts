/**
 * `CubeTable` 视图层类型（插槽作用域与内置子组件的显示态描述）
 *
 * 引擎契约见 `core/engine/types.ts`；这里只描述「怎么渲染」，不含业务状态。
 */
import type { FieldMeta, ListControlType, SearchControlType } from '../../types/field';
import type { CubeEngineContext } from '../../engine/types';

/**
 * 搜索控件类型扩展：在引擎的 `SearchControlType` 之外补一个 `dict`。
 *
 * `resolveSearchControl()` 不读 `field.dataSource`（只认 `lovCode`），所以后端下发内联字典
 * 的枚举字段会退化成文本框（`core/utils/fieldControl.ts:111-139`）。表单侧由 `FormContent`
 * 的 `select` 分支消费 dataSource，搜索侧此前没有对应处理。这里按同一份 `dataSource`
 * 渲染下拉，值仍提交字典键（数字键转 number），与表单口径一致。
 */
export type CubeSearchKind = SearchControlType | 'dict';

/** 搜索控件显示态 */
export interface CubeSearchControl {
  /** 字段元数据 */
  field: FieldMeta;
  /** 控件类型 */
  kind: CubeSearchKind;
  /** 主绑定键（范围类为字段名本身或 `_min` 前缀键） */
  key: string;
  /** 范围类的第二个键（`_max`），非范围类为 null */
  maxKey: string | null;
  /** `dict` / 本地选项 */
  options: Array<{ label: string; value: string | number }>;
  /** 栅格跨度（大控件占整行） */
  span: number;
}

/** 列表列显示态 */
export interface CubeColumnView {
  /** 取值键（元数据字段名，实际取值由 `getValueByKey` 容错） */
  key: string;
  /** 表头文本 */
  label: string;
  /** 渲染类型 */
  control: ListControlType;
  /** 字段元数据（插槽作用域透传） */
  field: FieldMeta;
  /** 列宽 */
  width: number | string;
  /** 对齐方式 */
  align: 'left' | 'center' | 'right';
  /** 是否主键列（受 `pageSetting.enableKey` 控制显隐） */
  isKey: boolean;
}

/** `#table-row-actions` 插槽作用域 */
export interface CubeRowActionsSlotProps {
  row: Record<string, unknown>;
  index: number;
  ctx: CubeEngineContext;
}

/** `#col-[prop]` 插槽作用域 */
export interface CubeCellSlotProps {
  row: Record<string, unknown>;
  column: CubeColumnView;
  value: unknown;
  ctx: CubeEngineContext;
}

/** 区域插槽的通用作用域 */
export interface CubeSectionSlotProps {
  ctx: CubeEngineContext;
}

/**
 * 工具栏 / 表格共用的动作集（由 `useCubeTableActions` 提供）。
 *
 * 组件按契约依赖而不是按实现类型依赖，便于单测直接注入假动作。
 */
export interface CubeTableActions {
  removeRow: (row: Record<string, unknown>) => Promise<boolean>;
  removeSelectedRows: () => Promise<boolean>;
  exportNow: () => Promise<string | null>;
  onImportFileChange: (event: Event) => Promise<void>;
  openFilePicker: (input: HTMLInputElement | null) => void;
  bindBlobDownload: () => void;
  needsConfirm: () => boolean;
}

/**
 * 宿主转发给工具栏的开关（键名与 `resolveToolbarFlags` 入参一致）。
 * 全部可选：不传就跟随后端 `pageSetting`，传了就以页面意图为准。
 */
export interface CubeToolbarFlags {
  add?: boolean;
  batchDelete?: boolean;
  importFile?: boolean;
  exportFile?: boolean;
  downloadTemplate?: boolean;
  refresh?: boolean;
}

/** 宿主转发给表格区的开关 */
export interface CubeGridFlags {
  selection?: boolean;
  indexColumn?: boolean;
  rowActions?: boolean;
  doubleClickEdit?: boolean;
  actionWidth?: number;
}
