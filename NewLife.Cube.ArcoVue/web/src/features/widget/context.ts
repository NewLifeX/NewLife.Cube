import type { InjectionKey } from 'vue';
import type { DashboardConfig, WidgetInstance } from '@newlifex/api-core';
import type { FieldMeta } from '@/core/types/field';
import type { ViewFilter } from '@/core/utils/viewProfile';

export interface WidgetSurfaceContext {
  surface: 'insight' | 'workbench';
  hostTypePath?: string;
  hostFilter: ViewFilter | null;
  canEdit: boolean;
  dashboard: DashboardConfig;
  saveDashboard: (next: DashboardConfig) => Promise<void>;
  /** 合成 legacy 时由表面注入，Host 不读列表 store */
  legacyChartData?: unknown[];
  legacyChartLoading?: boolean;
  legacyChartError?: string;
  listFields?: { name: string; displayName?: string; typeName?: string }[];
  /** 宿主页 search∪list 字段候选（实体页洞察槽注入；工作台无，部件查询条件宿主引用据此启用/隐藏，OSC-260903e2a4） */
  hostFilterFields?: FieldMeta[];
  /** 上层（工作台顶部菜单）已提供「添加部件…」入口（OSC-260921）：有则部件不再给行内「+」；无（普通用户无顶部菜单）则退回最后一个部件上的入口 */
  topAddEntry?: boolean;
}

export const WIDGET_SURFACE_KEY: InjectionKey<WidgetSurfaceContext> = Symbol('cubeWidgetSurface');

export interface WidgetCardProps {
  widget: WidgetInstance;
  result?: unknown;
  loading?: boolean;
  error?: string;
  locked?: boolean;
  unlinked?: boolean;
  canEdit?: boolean;
  onTitleCommit?: (title: string) => void;
}
