import { markRaw } from 'vue';
import { registerWidget } from './registry';
import MetricCardWidget from './MetricCardWidget.vue';
import MiniChartWidget from './MiniChartWidget.vue';
import MiniKanbanWidget from './MiniKanbanWidget.vue';
import LegacyChartWidget from './LegacyChartWidget.vue';

/** 平台三种 + 只读 legacyChart。apps 可再 registerWidget。 */
export function registerPlatformWidgets() {
  registerWidget({
    kind: 'metricCard',
    title: '指标卡',
    providers: ['entity.aggregate', 'named'],
    defaultW: 3,
    component: markRaw(MetricCardWidget),
  });
  registerWidget({
    kind: 'miniChart',
    title: '迷你图表',
    providers: ['entity.aggregate'],
    defaultW: 6,
    component: markRaw(MiniChartWidget),
  });
  registerWidget({
    kind: 'miniKanban',
    title: '迷你看板',
    providers: ['entity.list'],
    defaultW: 12,
    component: markRaw(MiniKanbanWidget),
  });
  registerWidget({
    kind: 'legacyChart',
    title: '旧图表',
    providers: ['entity.aggregate'],
    defaultW: 12,
    component: markRaw(LegacyChartWidget),
  });
}

export { registerWidget, getWidget } from './registry';
export { synthesizeLegacyDashboard, isUnlinkedWidget } from './legacy';
