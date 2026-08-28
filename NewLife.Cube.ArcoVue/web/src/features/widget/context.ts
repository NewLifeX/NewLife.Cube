import type { InjectionKey } from 'vue';
import type { DashboardConfig, WidgetInstance } from '@cube/api-core';
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
