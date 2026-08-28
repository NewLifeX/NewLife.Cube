import { markRaw } from 'vue';
import { registerWidget } from './registry';
import MetricCardWidget from './MetricCardWidget.vue';
import MiniChartWidget from './MiniChartWidget.vue';
import MiniKanbanWidget from './MiniKanbanWidget.vue';
import DataListWidget from './DataListWidget.vue';
import DataCardWidget from './DataCardWidget.vue';
import LegacyChartWidget from './LegacyChartWidget.vue';
import QuickLinksWidget from './QuickLinksWidget.vue';
import ProfileWidget from './ProfileWidget.vue';
import KvListWidget from './KvListWidget.vue';
import LoginLogWidget from './LoginLogWidget.vue';
import MonitorChartWidget from './MonitorChartWidget.vue';
import InboxWidget from './InboxWidget.vue';

/** 平台 kind。apps 可再 registerWidget。 */
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
    title: '数据看板',
    providers: ['entity.list'],
    defaultW: 12,
    component: markRaw(MiniKanbanWidget),
  });
  registerWidget({
    kind: 'dataList',
    title: '数据列表',
    providers: ['entity.list'],
    defaultW: 6,
    component: markRaw(DataListWidget),
  });
  registerWidget({
    kind: 'dataCard',
    title: '数据卡片',
    providers: ['entity.list'],
    defaultW: 6,
    component: markRaw(DataCardWidget),
  });
  registerWidget({
    kind: 'legacyChart',
    title: '旧图表',
    providers: ['entity.aggregate'],
    defaultW: 12,
    component: markRaw(LegacyChartWidget),
  });
  registerWidget({
    kind: 'quickLinks',
    title: '快捷入口',
    providers: ['named'],
    defaultW: 4,
    component: markRaw(QuickLinksWidget),
  });
  registerWidget({
    kind: 'profile',
    title: '个人信息',
    providers: ['named'],
    defaultW: 4,
    component: markRaw(ProfileWidget),
  });
  registerWidget({
    kind: 'kvList',
    title: '键值列表',
    providers: ['named'],
    defaultW: 4,
    component: markRaw(KvListWidget),
  });
  registerWidget({
    kind: 'loginLog',
    title: '登录与在线',
    providers: ['named'],
    defaultW: 4,
    component: markRaw(LoginLogWidget),
  });
  registerWidget({
    kind: 'monitorChart',
    title: '性能监控',
    providers: ['named'],
    defaultW: 8,
    component: markRaw(MonitorChartWidget),
  });
  registerWidget({
    kind: 'inbox',
    title: '站内信',
    providers: ['named'],
    defaultW: 6,
    component: markRaw(InboxWidget),
  });
}

export { registerWidget, getWidget } from './registry';
export { synthesizeLegacyDashboard, isUnlinkedWidget } from './legacy';
