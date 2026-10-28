<template>
  <div class="home-page">
    <a-spin :loading="loading && !view" style="width: 100%">
      <a-result v-if="error && !view" status="error" title="工作台加载失败" :subtitle="error">
        <template #extra>
          <a-button type="primary" @click="reload">重试</a-button>
        </template>
      </a-result>

      <template v-else>
        <div class="banner">
          <div>
            <h2 class="banner-title">{{ greeting }}，{{ view?.displayName || '-' }}</h2>
            <p class="banner-sub">{{ today }}<span v-if="view?.roleText"> · {{ view.roleText }}</span></p>
          </div>
          <a-space>
            <a-button v-if="!layoutEditing" @click="startLayoutEdit">编辑布局</a-button>
            <template v-else>
              <a-button :loading="savingLayout" @click="saveLayoutChanges">保存</a-button>
              <a-button :loading="savingLayout" @click="resetLayoutChanges">重置</a-button>
              <a-button type="text" @click="cancelLayoutEdit">完成</a-button>
            </template>
            <a-button :loading="loading" @click="reload">刷新</a-button>
          </a-space>
        </div>

        <a-alert v-if="error" class="banner-alert" type="warning">{{ error }}</a-alert>
        <a-alert v-if="layoutTip" class="banner-alert" type="info">{{ layoutTip }}</a-alert>

        <div v-if="layoutEditing && hiddenWidgets.length" class="hidden-widgets">
          <div class="hidden-title">已隐藏的卡片</div>
          <a-space wrap>
            <a-tag v-for="widget in hiddenWidgets" :key="widget.name" class="hidden-tag" @click="restoreWidget(widget.name)">
              {{ widget.title }}（点击恢复）
            </a-tag>
          </a-space>
        </div>

        <a-grid v-if="view?.kpis.length" class="kpis" :cols="{ xs: 1, sm: 2, md: 4, lg: 4 }" :col-gap="16" :row-gap="16">
          <a-grid-item v-for="kpi in view.kpis" :key="kpi.name">
            <a-card :class="['kpi', `kpi-${kpi.color}`]" hoverable @click="openKpi(kpi.url)">
              <div class="kpi-label">{{ kpi.label }}</div>
              <div class="kpi-value">{{ kpi.value }}</div>
              <div v-if="kpi.trend" class="kpi-trend">{{ kpi.trend }}</div>
            </a-card>
          </a-grid-item>
        </a-grid>

        <a-grid v-if="visibleWidgets.length" class="widgets" :cols="12" :col-gap="16" :row-gap="16">
          <a-grid-item
            v-for="widget in visibleWidgets"
            :key="widget.name"
            :span="widget.cols"
            :draggable="layoutEditing"
            @dragstart="onDragStart(widget.name)"
            @dragover.prevent
            @drop="onDropOn(widget.name)"
          >
            <a-card :bordered="true" class="widget" :data-widget="widget.name">
              <template #title>
                <div class="widget-header">
                  <span>{{ widget.title }}</span>
                  <a-button v-if="layoutEditing" type="text" size="mini" @click="hideWidget(widget.name)">隐藏</a-button>
                </div>
              </template>
              <!-- 监控曲线 -->
              <template v-if="widget.kind === 'monitor'">
                <div v-if="monitorPoints.length" class="monitor">
                  <svg :viewBox="`0 0 ${chart.width} ${chart.height}`" class="monitor-chart" preserveAspectRatio="none">
                    <polyline class="line-cpu" :points="chart.cpuPoints" />
                    <polyline class="line-mem" :points="chart.memPoints" />
                  </svg>
                  <div class="monitor-legend">
                    <span class="legend-cpu">CPU {{ latestMonitor?.cpu ?? 0 }}%</span>
                    <span class="legend-mem">内存 {{ latestMonitor?.memory ?? 0 }}%</span>
                    <span v-if="latestMonitor?.memUsed != null && latestMonitor?.memTotal" class="legend-total">
                      {{ latestMonitor.memUsed }} / {{ latestMonitor.memTotal }} MB
                    </span>
                  </div>
                </div>
                <a-empty v-else description="正在采集监控数据" />
              </template>

              <!-- 个人信息 -->
              <template v-else-if="widget.kind === 'profile' && widget.profile">
                <div class="profile-head">
                  <a-avatar :size="44" round>{{ widget.profile.initial }}</a-avatar>
                  <div>
                    <div class="profile-name">{{ widget.profile.displayName }}</div>
                    <div class="profile-tags">
                      <a-tag :color="widget.profile.online ? 'green' : 'gray'">
                        {{ widget.profile.online ? '在线' : '离线' }}
                      </a-tag>
                      <a-tag v-for="role in widget.profile.roles" :key="role">{{ role }}</a-tag>
                    </div>
                  </div>
                </div>
                <a-descriptions :column="2" size="small" :data="profileRows(widget)"></a-descriptions>
              </template>

              <!-- 快捷入口 -->
              <template v-else-if="widget.kind === 'quickLinks'">
                <div v-if="widget.links?.length" class="links">
                  <router-link v-for="link in widget.links" :key="link.url" :to="link.url" class="link">
                    {{ link.name }}
                  </router-link>
                </div>
                <a-empty v-else description="暂无快捷入口" />
              </template>

              <!-- 登录与在线 -->
              <template v-else-if="widget.kind === 'loginLog'">
                <div class="loginlog">
                  <section>
                    <h4>最近登录</h4>
                    <ul v-if="widget.logins?.length">
                      <li v-for="(item, index) in widget.logins" :key="`login-${index}`">
                        <span>{{ item.title }}</span>
                        <span class="muted">{{ item.subtitle }}</span>
                        <time>{{ item.time }}</time>
                      </li>
                    </ul>
                    <a-empty v-else description="暂无登录记录" />
                  </section>
                  <section>
                    <h4>在线用户</h4>
                    <ul v-if="widget.onlines?.length">
                      <li v-for="(item, index) in widget.onlines" :key="`online-${index}`">
                        <span>{{ item.title }}</span>
                        <span class="muted">{{ item.subtitle }}</span>
                        <time>{{ item.time }}</time>
                      </li>
                    </ul>
                    <a-empty v-else description="暂无在线用户" />
                  </section>
                </div>
              </template>

              <!-- 键值卡 -->
              <a-descriptions v-else-if="widget.kind === 'kv' && widget.rows?.length" :column="1" size="small" :data="rowData(widget)" />

              <p v-else-if="widget.kind === 'text'" class="text">{{ widget.text }}</p>

              <a-empty v-else description="暂无数据" />
            </a-card>
          </a-grid-item>
        </a-grid>

        <a-empty v-if="view && !view.hasContent" class="empty" description="暂无工作台数据" />
      </template>
    </a-spin>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { Message } from '@arco-design/web-vue';
import {
  appendMonitorPoint,
  buildMonitorChart,
  buildWorkbenchView,
  buildWidgetLayout,
  createWidgetLayoutLoader,
  createWorkbenchLoader,
  mergeWidgetsWithLayout,
  moveWidget,
  MONITOR_POLL_INTERVAL,
  type MonitorPoint,
  type WidgetLayoutMap,
  type WorkbenchData,
  type WorkbenchWidgetView,
} from '@newlifex/cube';
import api from '@/api';

const router = useRouter();
const loader = createWorkbenchLoader(api);
const layoutLoader = createWidgetLayoutLoader(api);

const raw = ref<WorkbenchData | null>(null);
const loading = ref(false);
const error = ref('');
const monitorPoints = ref<MonitorPoint[]>([]);

// --- 卡片布局：拖动排序 / 隐藏 / 持久化 ---
const layout = ref<WidgetLayoutMap>({});
const visibleWidgets = ref<WorkbenchWidgetView[]>([]);
const hiddenWidgets = ref<WorkbenchWidgetView[]>([]);
const layoutEditing = ref(false);
const savingLayout = ref(false);
const draggingName = ref('');
const layoutTip = ref('');
let timer: ReturnType<typeof setInterval> | null = null;
let polling = false;

const view = computed(() => (raw.value ? buildWorkbenchView(raw.value) : null));
const latestMonitor = computed(() => monitorPoints.value[monitorPoints.value.length - 1] ?? null);
const chart = computed(() => buildMonitorChart(monitorPoints.value, 320, 120));

const today = computed(() =>
  new Date().toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric', weekday: 'long' }),
);
const greeting = computed(() => {
  const hour = new Date().getHours();
  if (hour < 6) return '夜深了';
  if (hour < 12) return '早上好';
  if (hour < 14) return '中午好';
  if (hour < 18) return '下午好';
  return '晚上好';
});

const profileLabels = ['登录次数', '最后登录', '登录地址', '注册时间'];
function profileRows(widget: WorkbenchWidgetView) {
  return (widget.profile?.rows ?? []).map((row, index) => ({ label: profileLabels[index] || row.key, value: row.text }));
}

function rowData(widget: WorkbenchWidgetView) {
  return (widget.rows ?? []).map((row) => ({ label: row.key, value: row.link ? row.link.text : row.text }));
}

async function load(): Promise<void> {
  loading.value = true;
  error.value = '';
  try {
    const result = await loader.load();
    if (result.data) {
      raw.value = result.data;
      if (!layoutEditing.value) {
        layout.value = await layoutLoader.load();
        applyLayout(buildWorkbenchView(result.data).widgets);
      }
    } else error.value = result.error || '工作台加载失败';
  } catch (ex) {
    error.value = ex instanceof Error ? ex.message : String(ex);
  } finally {
    loading.value = false;
  }
}

async function pollMonitor(): Promise<void> {
  if (polling) return;
  polling = true;
  try {
    monitorPoints.value = appendMonitorPoint(monitorPoints.value, await loader.pollMonitor());
  } catch {
    // 监控曲线属于增强信息，单次失败不打断工作台主流程
  } finally {
    polling = false;
  }
}

function startPolling(): void {
  if (timer) return;
  void pollMonitor();
  timer = setInterval(() => void pollMonitor(), MONITOR_POLL_INTERVAL);
}

function stopPolling(): void {
  if (!timer) return;
  clearInterval(timer);
  timer = null;
}

async function reload(): Promise<void> {
  await load();
  if (view.value?.hasChartWidget) startPolling();
  else stopPolling();
  if (error.value) Message.warning(error.value);
}

// --- 卡片布局 ---
function applyLayout(widgets: WorkbenchWidgetView[]): void {
  const merged = mergeWidgetsWithLayout(widgets, layout.value);
  visibleWidgets.value = merged.visible;
  hiddenWidgets.value = merged.hidden;
}

function startLayoutEdit(): void {
  layoutEditing.value = true;
  layoutTip.value = '拖动卡片调整顺序，点击隐藏图标可隐藏卡片';
}

function cancelLayoutEdit(): void {
  layoutEditing.value = false;
  layoutTip.value = '';
  if (view.value) applyLayout(view.value.widgets);
}

function onDragStart(name: string): void {
  draggingName.value = name;
}

function onDropOn(target: string): void {
  if (!layoutEditing.value || !draggingName.value) return;
  visibleWidgets.value = moveWidget(visibleWidgets.value, draggingName.value, target);
  draggingName.value = '';
}

function hideWidget(name: string): void {
  const index = visibleWidgets.value.findIndex((item) => item.name === name);
  if (index < 0) return;
  const [widget] = visibleWidgets.value.splice(index, 1);
  hiddenWidgets.value.push(widget);
}

function restoreWidget(name: string): void {
  const index = hiddenWidgets.value.findIndex((item) => item.name === name);
  if (index < 0) return;
  const [widget] = hiddenWidgets.value.splice(index, 1);
  visibleWidgets.value.push(widget);
}

async function saveLayoutChanges(): Promise<void> {
  savingLayout.value = true;
  try {
    const map = buildWidgetLayout(visibleWidgets.value, hiddenWidgets.value);
    if (await layoutLoader.save(map)) {
      layout.value = map;
      layoutTip.value = '布局已保存';
    } else {
      layoutTip.value = '布局保存失败';
    }
  } finally {
    savingLayout.value = false;
  }
}

async function resetLayoutChanges(): Promise<void> {
  savingLayout.value = true;
  try {
    if (await layoutLoader.reset()) {
      layout.value = {};
      layoutTip.value = '布局已重置';
      if (view.value) applyLayout(view.value.widgets);
    } else {
      layoutTip.value = '布局重置失败';
    }
  } finally {
    savingLayout.value = false;
  }
}

function openKpi(url?: string): void {
  if (url) void router.push(url);
}

onMounted(async () => {
  await load();
  if (view.value?.hasChartWidget) startPolling();
});

onBeforeUnmount(stopPolling);
</script>

<style scoped>
.home-page {
  padding: 0 16px 16px;
}

.banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

.banner-title {
  margin: 0;
  font-size: 22px;
}

.banner-sub {
  margin: 4px 0 0;
  color: var(--color-text-3);
  font-size: 13px;
}

.banner-alert {
  margin-bottom: 16px;
}

.kpis,
.widgets {
  margin-bottom: 16px;
}

.kpi {
  cursor: default;
  border-left: 3px solid #165dff;
}

.kpi-label {
  color: var(--color-text-3);
  font-size: 13px;
}

.kpi-value {
  margin-top: 6px;
  font-size: 26px;
  font-weight: 600;
}

.kpi-trend {
  margin-top: 4px;
  font-size: 12px;
  color: var(--color-text-3);
}

.kpi-green {
  border-left-color: #00b42a;
}

.kpi-cyan {
  border-left-color: #0fc6c2;
}

.kpi-orange {
  border-left-color: #ff7d00;
}

.kpi-red {
  border-left-color: #f53f3f;
}

.kpi-purple {
  border-left-color: #722ed1;
}

.kpi-grey {
  border-left-color: #86909c;
}

.monitor-chart {
  width: 100%;
  height: 120px;
}

.line-cpu,
.line-mem {
  fill: none;
  stroke-width: 2;
  vector-effect: non-scaling-stroke;
}

.line-cpu {
  stroke: #165dff;
}

.line-mem {
  stroke: #00b42a;
}

.monitor-legend {
  display: flex;
  gap: 16px;
  font-size: 12px;
  color: var(--color-text-3);
}

.legend-cpu::before,
.legend-mem::before {
  content: '';
  display: inline-block;
  width: 8px;
  height: 8px;
  border-radius: 2px;
  margin-right: 4px;
}

.legend-cpu::before {
  background: #165dff;
}

.legend-mem::before {
  background: #00b42a;
}

.profile-head {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}

.profile-name {
  font-weight: 600;
}

.profile-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 4px;
}

.links {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.link {
  padding: 4px 10px;
  border: 1px solid var(--color-border);
  border-radius: 4px;
  font-size: 13px;
  text-decoration: none;
  color: rgb(var(--primary-6));
}

.loginlog {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 16px;
}

.loginlog h4 {
  margin: 0 0 8px;
  font-size: 13px;
  color: var(--color-text-3);
}

.loginlog ul {
  margin: 0;
  padding: 0;
  list-style: none;
}

.loginlog li {
  display: flex;
  justify-content: space-between;
  gap: 8px;
  padding: 4px 0;
  font-size: 13px;
  border-bottom: 1px dashed var(--color-border);
}

.loginlog .muted {
  color: var(--color-text-3);
}

.text {
  margin: 0;
  font-size: 13px;
}

.empty {
  padding: 32px 0;
}

.hidden-widgets {
  margin-bottom: 16px;
}
.hidden-title {
  margin-bottom: 8px;
  color: var(--text-secondary, #666);
  font-size: 13px;
}
.hidden-tag {
  cursor: pointer;
}
.widget-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}
</style>
