/**
 * `CubeTable` 视图层类型（插槽作用域与内置子组件的显示态描述）
 *
 * 引擎契约见 `core/engine/types.ts`；这里只描述「怎么渲染」，不含业务状态。
 */
import type { Component } from 'vue';
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

/**
 * 表格统计指标项（对齐 MES `ListPage/types.ts` 的 `ListPageMetric`）。
 * 渲染在「指标行」左侧：图标 + 名称 + 值 + 可选环比变化。
 */
export interface CubeTableMetric {
  /** 指标名称（如「项目总数」） */
  label: string;
  /** 指标值 */
  value: string | number;
  /** 圆形图标组件（Element Plus 图标） */
  icon?: Component;
  /** 图标背景色（CSS 值，推荐 --el-* token） */
  bg?: string;
  /** 图标前景色 */
  color?: string;
  /** 环比变化文案（如「↑12%」），不传则不显示 */
  change?: string;
  /** 变化语义：up 绿 / warning 琥珀 / down 红 */
  changeType?: 'up' | 'warning' | 'down';
}

/** 指标行入参 */
export interface CubeMetricBarFlags {
  /** 是否显示刷新按钮 */
  refresh?: boolean;
  /** 是否显示「设置列」入口 */
  columnsSetting?: boolean;
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

/**
 * `CubeTable` 宿主入参（命名对齐 MES `ListPage` 的 props，业务页两边平移不改称呼）。
 *
 * 引擎接线类（`context` / `autoEngine` / `apiPrefix` / `routePath` / `pageSize`）是 Cube 特有的，
 * 其余全部与 MES 一一对应。
 */
export interface CubeTableProps {
  // ── 引擎接线 ──
  /** 外部指定引擎上下文（嵌入 / 自管引擎场景），传入则不再自动创建 */
  context?: CubeEngineContext | null;
  /** 无 context 且无注入时，是否按当前路由自建引擎（默认 true） */
  autoEngine?: boolean;
  /** 自建引擎时使用的实体前缀 */
  apiPrefix?: string;
  /** 自建引擎时使用的路由路径 */
  routePath?: string;
  /** 初始每页条数 */
  pageSize?: number;
  /** 惰性附加查询条件（对应 MES `queryParams`），每次取数/导出时合并 */
  queryParams?: () => Record<string, unknown>;

  // ── 搜索区 ──
  /** 搜索框占位文案 */
  searchPlaceholder?: string;
  /** 是否显示搜索区 */
  showSearch?: boolean;

  // ── 统计行 / 分页 ──
  /** 统计指标（统计行左侧） */
  metrics?: CubeTableMetric[];
  /** 是否显示刷新按钮 */
  showRefresh?: boolean;
  /** 可选每页条数 */
  pageSizes?: number[];
  /** 总数单位（「共 N 条」） */
  totalUnit?: string;
  /** 每页条数单位（「N 条/页」），缺省跟随 `totalUnit` */
  sizeUnit?: string;

  // ── 设置列 ──
  /** 是否启用「设置列」（localStorage 记忆） */
  columnsSetting?: boolean;
  /** 列显隐存储键，默认按实体前缀分桶 */
  columnsStorageKey?: string;

  // ── 操作栏 ──
  /** 新增按钮：不传跟随后端 `enableAdd` */
  showAdd?: boolean;
  /** 批量删除按钮：不传跟随后端 `enableSelect` */
  showBatchDelete?: boolean;
  /** 导入按钮：不传默认开 */
  showImport?: boolean;
  /** 导出按钮：不传默认开 */
  showExport?: boolean;
  /** 下载模板按钮：不传默认关（后端需实现 `DownloadTemplate`） */
  showDownloadTemplate?: boolean;
  /** 导出格式（xlsx / csv / json） */
  exportFormat?: string;
  /** 导入可选文件类型 */
  importAccept?: string;
  /** 删除是否二次确认：不传跟随后端 `doubleDelete` */
  confirmDelete?: boolean;

  // ── 表格区 ──
  /** 表格区开关（勾选列 / 索引列 / 操作列 / 双击编辑） */
  grid?: CubeGridFlags;
}
