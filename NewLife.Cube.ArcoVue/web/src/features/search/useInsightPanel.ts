import { computed, provide, reactive, watch } from 'vue';
import { emptyDashboard, type DashboardConfig, type WidgetInstance } from '@newlifex/api-core';
import { useUserStore } from '@/stores/user';
import { useViewProfileStore } from '@/stores/viewProfile';
import type { FieldMeta } from '@/core/types/field';
import { isEmbedMode } from '@/core/utils/embedMode';
import { mergeDeveloperCharts, isFixedDeveloperChart } from '@/core/utils/chartOptions';
import type { ViewFilter, ViewInsight } from '@/core/utils/viewProfile';
import { WIDGET_SURFACE_KEY, type WidgetSurfaceContext } from '@/features/widget/context';
import { synthesizeLegacyDashboard } from '@/features/widget/legacy';

/** 洞察槽是否应占列表顶栏布局（有部件墙或开发者图失败告警） */
export function insightPanelShouldOccupyLayout(
  widgetCount: number,
  developerChartError: string,
  hasDeveloperCharts: boolean,
): boolean {
  if (widgetCount > 0) return true;
  if (developerChartError && !hasDeveloperCharts) return true;
  return false;
}

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
  /** 宿主页 search∪list 字段候选（OSC-260903e2a4） */
  filterFields?: FieldMeta[];
  /** 开发者 GetChartData：经 WidgetHost 按 legacyChart 部件样式挂洞察区（最多前 2 张） */
  developerCharts?: unknown[];
  developerChartError?: string;
}

function persistableDashboard(cfg: DashboardConfig, hadStored: boolean): DashboardConfig {
  const widgets: WidgetInstance[] = cfg.widgets
    .filter(
      (w) =>
        w.kind !== 'legacyChart' &&
        !isFixedDeveloperChart(w.id) &&
        (hadStored || !String(w.id).startsWith('legacy-')),
    )
    .map((w) => {
      const next = { ...w };
      delete next.syntheticValue;
      delete next.chartOption;
      delete next.chartIndex;
      delete next.fixed;
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
  const developerCharts = computed(() =>
    Array.isArray(props.developerCharts) ? props.developerCharts : [],
  );
  const developerChartError = computed(() => String(props.developerChartError ?? ''));
  const hasDeveloperCharts = computed(() => developerCharts.value.length > 0);

  const synthesized = computed(() => {
    if (stored.value != null) return null;
    // 开发者图已由 mergeDeveloperCharts 注入，避免再合成一张全宽 legacyChart
    if (hasDeveloperCharts.value) {
      return synthesizeLegacyDashboard(insight.value, props.statData, false, props.typePath);
    }
    return synthesizeLegacyDashboard(
      insight.value,
      props.statData,
      Array.isArray(props.chartData) && props.chartData.length > 0,
      props.typePath,
    );
  });

  const baseDashboard = computed<DashboardConfig>(
    () => stored.value ?? synthesized.value ?? emptyDashboard(),
  );

  const dashboard = computed<DashboardConfig>(() => {
    if (!hasDeveloperCharts.value) return baseDashboard.value;
    return mergeDeveloperCharts(baseDashboard.value, developerCharts.value, props.typePath);
  });

  /** 有开发者图时用其数组供各 legacyChart 按 chartIndex 取 option；否则沿用列表洞察 chartData */
  const legacyChartData = computed(() =>
    hasDeveloperCharts.value ? developerCharts.value : props.chartData,
  );
  const legacyChartError = computed(() =>
    hasDeveloperCharts.value ? developerChartError.value : props.chartError,
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
    legacyChartData: legacyChartData.value,
    legacyChartLoading: props.chartLoading,
    legacyChartError: legacyChartError.value,
    listFields: props.listFields,
    hostFilterFields: props.filterFields,
  });

  watch(
    () => [
      props.typePath,
      props.hostFilter,
      canEdit.value,
      dashboard.value,
      legacyChartData.value,
      props.chartLoading,
      legacyChartError.value,
      props.listFields,
      props.filterFields,
    ],
    () => {
      surface.hostTypePath = props.typePath;
      surface.hostFilter = props.hostFilter;
      surface.canEdit = canEdit.value;
      surface.dashboard = dashboard.value;
      surface.legacyChartData = legacyChartData.value;
      surface.legacyChartLoading = props.chartLoading;
      surface.legacyChartError = legacyChartError.value;
      surface.listFields = props.listFields;
      surface.hostFilterFields = props.filterFields;
    },
    { deep: true },
  );

  provide(WIDGET_SURFACE_KEY, surface);

  const hasVisibleContent = computed(() =>
    insightPanelShouldOccupyLayout(
      (dashboard.value.widgets ?? []).length,
      developerChartError.value,
      hasDeveloperCharts.value,
    ),
  );

  return {
    canEdit,
    dashboard,
    developerChartError,
    hasVisibleContent,
  };
}
