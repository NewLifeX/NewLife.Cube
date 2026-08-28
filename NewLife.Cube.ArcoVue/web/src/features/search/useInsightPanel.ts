import { computed, provide, reactive, watch } from 'vue';
import { emptyDashboard, type DashboardConfig, type WidgetInstance } from '@cube/api-core';
import { useUserStore } from '@/stores/user';
import { useViewProfileStore } from '@/stores/viewProfile';
import { isEmbedMode } from '@/core/utils/embedMode';
import type { ViewFilter, ViewInsight } from '@/core/utils/viewProfile';
import { WIDGET_SURFACE_KEY, type WidgetSurfaceContext } from '@/features/widget/context';
import { synthesizeLegacyDashboard } from '@/features/widget/legacy';

export interface InsightPanelProps {
  typePath: string;
  showStat: boolean;
  showChart: boolean;
  statData: Record<string, unknown> | null;
  chartData: unknown[];
  chartLoading: boolean;
  chartError: string;
  chartOption?: unknown;
  hostFilter: ViewFilter | null;
  listFields?: { name: string; displayName?: string; typeName?: string }[];
}

function persistableDashboard(cfg: DashboardConfig, hadStored: boolean): DashboardConfig {
  const widgets: WidgetInstance[] = cfg.widgets
    .filter((w) => w.kind !== 'legacyChart' && (hadStored || !String(w.id).startsWith('legacy-')))
    .map((w) => {
      const next = { ...w };
      delete next.syntheticValue;
      delete next.chartOption;
      return next;
    });
  return { version: 1, widgets };
}

export function useInsightPanel(props: InsightPanelProps) {
  const userStore = useUserStore();
  const evpStore = useViewProfileStore();
  const canEdit = computed(() => userStore.isLoggedIn && !isEmbedMode());
  const stored = computed(() => evpStore.getDashboard(props.typePath));
  const insight = computed<ViewInsight>(() => ({
    showStat: props.showStat,
    showChart: props.showChart,
    chartOption: props.chartOption,
  }));
  const synthesized = computed(() => {
    if (stored.value != null) return null;
    return synthesizeLegacyDashboard(
      insight.value,
      props.statData,
      Array.isArray(props.chartData) && props.chartData.length > 0,
      props.typePath,
    );
  });
  const dashboard = computed<DashboardConfig>(
    () => stored.value ?? synthesized.value ?? emptyDashboard(),
  );

  const surface = reactive<WidgetSurfaceContext>({
    surface: 'insight',
    hostTypePath: props.typePath,
    hostFilter: props.hostFilter,
    canEdit: canEdit.value,
    dashboard: dashboard.value,
    saveDashboard: async (next: DashboardConfig) => {
      await evpStore.updateDashboard(
        props.typePath,
        persistableDashboard(next, stored.value != null),
        true,
      );
    },
    legacyChartData: props.chartData,
    legacyChartLoading: props.chartLoading,
    legacyChartError: props.chartError,
    listFields: props.listFields,
  });

  watch(
    () => [
      props.typePath,
      props.hostFilter,
      canEdit.value,
      dashboard.value,
      props.chartData,
      props.chartLoading,
      props.chartError,
      props.listFields,
    ],
    () => {
      surface.hostTypePath = props.typePath;
      surface.hostFilter = props.hostFilter;
      surface.canEdit = canEdit.value;
      surface.dashboard = dashboard.value;
      surface.legacyChartData = props.chartData;
      surface.legacyChartLoading = props.chartLoading;
      surface.legacyChartError = props.chartError;
      surface.listFields = props.listFields;
    },
    { deep: true },
  );

  provide(WIDGET_SURFACE_KEY, surface);

  return { canEdit, dashboard };
}
