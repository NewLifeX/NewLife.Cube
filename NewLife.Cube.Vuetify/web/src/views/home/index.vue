<template>
  <div class="home-page">
    <v-skeleton-loader v-if="loading && !view" type="article, card, card" />

    <v-alert v-else-if="error && !view" type="error" variant="tonal" class="mb-4" :title="'工作台加载失败'">
      {{ error }}
      <template #append>
        <v-btn variant="text" @click="reload">重试</v-btn>
      </template>
    </v-alert>

    <template v-else>
      <div class="banner">
        <div>
          <div class="text-h5 font-weight-bold">{{ greeting }}，{{ view?.displayName || '-' }}</div>
          <div class="text-body-2 text-medium-emphasis mt-1">
            {{ today }}<span v-if="view?.roleText"> · {{ view.roleText }}</span>
          </div>
        </div>
        <v-btn :loading="loading" variant="tonal" prepend-icon="mdi-refresh" @click="reload">刷新</v-btn>
      </div>

      <v-alert v-if="error" type="warning" variant="tonal" density="compact" class="mb-4">{{ error }}</v-alert>

      <v-row v-if="view?.kpis.length" class="mb-2">
        <v-col v-for="kpi in view.kpis" :key="kpi.name" cols="12" sm="6" md="3">
          <v-card :class="['kpi', `kpi-${kpi.color}`]" :ripple="Boolean(kpi.url)" @click="openKpi(kpi.url)">
            <v-card-text>
              <div class="text-body-2 text-medium-emphasis">{{ kpi.label }}</div>
              <div class="text-h4 font-weight-bold mt-1">{{ kpi.value }}</div>
              <div v-if="kpi.trend" class="text-caption text-medium-emphasis mt-1">{{ kpi.trend }}</div>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>

      <v-row v-if="view?.widgets.length">
        <v-col v-for="widget in view.widgets" :key="widget.name" cols="12" :md="widget.cols">
          <v-card class="widget" :data-widget="widget.name">
            <v-card-title class="text-subtitle-1">{{ widget.title }}</v-card-title>
            <v-card-text>
              <!-- 监控曲线 -->
              <template v-if="widget.kind === 'monitor'">
                <div v-if="monitorPoints.length">
                  <svg :viewBox="`0 0 ${chart.width} ${chart.height}`" class="monitor-chart" preserveAspectRatio="none">
                    <polyline class="line-cpu" :points="chart.cpuPoints" />
                    <polyline class="line-mem" :points="chart.memPoints" />
                  </svg>
                  <div class="d-flex ga-4 text-caption text-medium-emphasis">
                    <span class="legend-cpu">CPU {{ latestMonitor?.cpu ?? 0 }}%</span>
                    <span class="legend-mem">内存 {{ latestMonitor?.memory ?? 0 }}%</span>
                    <span v-if="latestMonitor?.memUsed != null && latestMonitor?.memTotal">
                      {{ latestMonitor.memUsed }} / {{ latestMonitor.memTotal }} MB
                    </span>
                  </div>
                </div>
                <v-empty-state v-else icon="mdi-chart-line" title="正在采集监控数据" />
              </template>

              <!-- 个人信息 -->
              <template v-else-if="widget.kind === 'profile' && widget.profile">
                <div class="d-flex align-center ga-3 mb-3">
                  <v-avatar color="primary" size="44">{{ widget.profile.initial }}</v-avatar>
                  <div>
                    <div class="font-weight-bold">{{ widget.profile.displayName }}</div>
                    <div class="d-flex flex-wrap ga-2 mt-1">
                      <v-chip size="small" :color="widget.profile.online ? 'success' : 'default'" variant="tonal">
                        {{ widget.profile.online ? '在线' : '离线' }}
                      </v-chip>
                      <v-chip v-for="role in widget.profile.roles" :key="role" size="small" variant="tonal">{{ role }}</v-chip>
                    </div>
                  </div>
                </div>
                <v-row dense>
                  <v-col v-for="(row, index) in widget.profile.rows" :key="row.key" cols="6">
                    <div class="text-caption text-medium-emphasis">{{ profileLabels[index] || row.key }}</div>
                    <div class="text-body-2">{{ row.text }}</div>
                  </v-col>
                </v-row>
              </template>

              <!-- 快捷入口 -->
              <template v-else-if="widget.kind === 'quickLinks'">
                <div v-if="widget.links?.length" class="d-flex flex-wrap ga-2">
                  <v-chip v-for="link in widget.links" :key="link.url" :to="link.url" link variant="outlined">
                    {{ link.name }}
                  </v-chip>
                </div>
                <v-empty-state v-else icon="mdi-link-variant" title="暂无快捷入口" />
              </template>

              <!-- 登录与在线 -->
              <template v-else-if="widget.kind === 'loginLog'">
                <v-row dense>
                  <v-col cols="12" sm="6">
                    <div class="text-caption text-medium-emphasis mb-1">最近登录</div>
                    <v-list v-if="widget.logins?.length" density="compact">
                      <v-list-item v-for="(item, index) in widget.logins" :key="`login-${index}`">
                        <v-list-item-title>{{ item.title }}</v-list-item-title>
                        <v-list-item-subtitle>{{ item.subtitle }} · {{ item.time }}</v-list-item-subtitle>
                      </v-list-item>
                    </v-list>
                    <v-empty-state v-else icon="mdi-login" title="暂无登录记录" />
                  </v-col>
                  <v-col cols="12" sm="6">
                    <div class="text-caption text-medium-emphasis mb-1">在线用户</div>
                    <v-list v-if="widget.onlines?.length" density="compact">
                      <v-list-item v-for="(item, index) in widget.onlines" :key="`online-${index}`">
                        <v-list-item-title>{{ item.title }}</v-list-item-title>
                        <v-list-item-subtitle>{{ item.subtitle }} · {{ item.time }}</v-list-item-subtitle>
                      </v-list-item>
                    </v-list>
                    <v-empty-state v-else icon="mdi-account-group" title="暂无在线用户" />
                  </v-col>
                </v-row>
              </template>

              <!-- 键值卡 -->
              <template v-else-if="widget.kind === 'kv' && widget.rows?.length">
                <v-row v-for="row in widget.rows" :key="row.key" dense>
                  <v-col cols="4" class="text-medium-emphasis">{{ row.key }}</v-col>
                  <v-col cols="8">
                    <a v-if="row.link" :href="row.link.href" :target="row.link.external ? '_blank' : undefined" rel="noopener noreferrer">
                      {{ row.link.text }}
                    </a>
                    <span v-else>{{ row.text }}</span>
                  </v-col>
                </v-row>
              </template>

              <p v-else-if="widget.kind === 'text'" class="text-body-2">{{ widget.text }}</p>

              <v-empty-state v-else icon="mdi-inbox-outline" title="暂无数据" />
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>

      <v-empty-state v-if="view && !view.hasContent" icon="mdi-view-dashboard-outline" title="暂无工作台数据" />
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import {
  appendMonitorPoint,
  buildMonitorChart,
  buildWorkbenchView,
  createWorkbenchLoader,
  MONITOR_POLL_INTERVAL,
  type MonitorPoint,
  type WorkbenchData,
} from '@newlifex/page-logic';
import api from '@/api';

const router = useRouter();
const loader = createWorkbenchLoader(api);

const raw = ref<WorkbenchData | null>(null);
const loading = ref(false);
const error = ref('');
const monitorPoints = ref<MonitorPoint[]>([]);
let timer: ReturnType<typeof setInterval> | null = null;
let polling = false;

const profileLabels = ['登录次数', '最后登录', '登录地址', '注册时间'];

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

async function load(): Promise<void> {
  loading.value = true;
  error.value = '';
  try {
    const result = await loader.load();
    if (result.data) raw.value = result.data;
    else error.value = result.error || '工作台加载失败';
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
  padding: 8px;
}

.banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

.kpi {
  cursor: default;
  border-left: 3px solid #1867c0;
  height: 100%;
}

.kpi-green {
  border-left-color: #2e7d32;
}

.kpi-cyan {
  border-left-color: #00838f;
}

.kpi-orange {
  border-left-color: #ef6c00;
}

.kpi-red {
  border-left-color: #c62828;
}

.kpi-purple {
  border-left-color: #6a1b9a;
}

.kpi-grey {
  border-left-color: #616161;
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
  stroke: #1867c0;
}

.line-mem {
  stroke: #2e7d32;
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
  background: #1867c0;
}

.legend-mem::before {
  background: #2e7d32;
}
</style>
