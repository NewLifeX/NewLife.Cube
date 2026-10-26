<template>
  <div class="home-page">
    <n-spin :show="loading && !view">
      <n-result v-if="error && !view" status="error" title="工作台加载失败" :description="error">
        <template #footer>
          <n-button type="primary" @click="reload">重试</n-button>
        </template>
      </n-result>

      <template v-else>
        <section class="banner">
          <div>
            <h2 class="banner-title">{{ greeting }}，{{ view?.displayName || '-' }}</h2>
            <p class="banner-sub">{{ today }}<span v-if="view?.roleText"> · {{ view.roleText }}</span></p>
          </div>
          <n-button :loading="loading" secondary @click="reload">刷新</n-button>
        </section>

        <n-alert v-if="error" class="banner-alert" type="warning" :show-icon="true" :bordered="false">
          {{ error }}
        </n-alert>

        <n-grid v-if="view?.kpis.length" class="kpis" :cols="'1 s:2 m:4 l:4'" :x-gap="16" :y-gap="16">
          <n-gi v-for="kpi in view.kpis" :key="kpi.name">
            <n-card :class="['kpi', `kpi-${kpi.color}`]" hoverable @click="openKpi(kpi.url)">
              <div class="kpi-label">{{ kpi.label }}</div>
              <div class="kpi-value">{{ kpi.value }}</div>
              <div v-if="kpi.trend" class="kpi-trend">{{ kpi.trend }}</div>
            </n-card>
          </n-gi>
        </n-grid>

        <n-grid v-if="view?.widgets.length" class="widgets" :cols="12" :x-gap="16" :y-gap="16">
          <n-gi v-for="widget in view.widgets" :key="widget.name" :span="widget.cols">
            <n-card :title="widget.title" size="small" class="widget" :data-widget="widget.name">
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
                <n-empty v-else description="正在采集监控数据" size="small" />
              </template>

              <!-- 个人信息 -->
              <template v-else-if="widget.kind === 'profile' && widget.profile">
                <div class="profile-head">
                  <n-avatar round :size="44">{{ widget.profile.initial }}</n-avatar>
                  <div>
                    <div class="profile-name">{{ widget.profile.displayName }}</div>
                    <div class="profile-tags">
                      <n-tag size="small" :type="widget.profile.online ? 'success' : 'default'">
                        {{ widget.profile.online ? '在线' : '离线' }}
                      </n-tag>
                      <n-tag v-for="role in widget.profile.roles" :key="role" size="small">{{ role }}</n-tag>
                    </div>
                  </div>
                </div>
                <n-descriptions :column="2" size="small" label-placement="left">
                  <n-descriptions-item label="登录次数">{{ widget.profile.rows[0]?.text }}</n-descriptions-item>
                  <n-descriptions-item label="最后登录">{{ widget.profile.rows[1]?.text }}</n-descriptions-item>
                  <n-descriptions-item label="登录地址">{{ widget.profile.rows[2]?.text }}</n-descriptions-item>
                  <n-descriptions-item label="注册时间">{{ widget.profile.rows[3]?.text }}</n-descriptions-item>
                </n-descriptions>
              </template>

              <!-- 快捷入口 -->
              <template v-else-if="widget.kind === 'quickLinks'">
                <div v-if="widget.links?.length" class="links">
                  <router-link v-for="link in widget.links" :key="link.url" :to="link.url" class="link">
                    {{ link.name }}
                  </router-link>
                </div>
                <n-empty v-else description="暂无快捷入口" size="small" />
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
                    <n-empty v-else description="暂无登录记录" size="small" />
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
                    <n-empty v-else description="暂无在线用户" size="small" />
                  </section>
                </div>
              </template>

              <!-- 键值卡 -->
              <n-descriptions v-else-if="widget.kind === 'kv' && widget.rows?.length" :column="1" size="small" label-placement="left">
                <n-descriptions-item v-for="row in widget.rows" :key="row.key" :label="row.key">
                  <a v-if="row.link" :href="row.link.href" :target="row.link.external ? '_blank' : undefined" rel="noopener noreferrer">
                    {{ row.link.text }}
                  </a>
                  <span v-else>{{ row.text }}</span>
                </n-descriptions-item>
              </n-descriptions>

              <p v-else-if="widget.kind === 'text'" class="text">{{ widget.text }}</p>

              <n-empty v-else description="暂无数据" size="small" />
            </n-card>
          </n-gi>
        </n-grid>

        <n-empty v-if="view && !view.hasContent" class="empty" description="暂无工作台数据" />
      </template>
    </n-spin>
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

.banner-title {
  margin: 0;
  font-size: 22px;
}

.banner-sub {
  margin: 4px 0 0;
  color: var(--n-text-color-3);
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
  border-left: 3px solid #2080f0;
}

.kpi-label {
  color: var(--n-text-color-3);
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
  color: var(--n-text-color-3);
}

.kpi-blue {
  border-left-color: #2080f0;
}

.kpi-green {
  border-left-color: #18a058;
}

.kpi-cyan {
  border-left-color: #00b4d8;
}

.kpi-orange {
  border-left-color: #f0a020;
}

.kpi-red {
  border-left-color: #d03050;
}

.kpi-purple {
  border-left-color: #8a2be2;
}

.kpi-grey {
  border-left-color: #909399;
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
  stroke: #2080f0;
}

.line-mem {
  stroke: #18a058;
}

.monitor-legend {
  display: flex;
  gap: 16px;
  font-size: 12px;
  color: var(--n-text-color-3);
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
  background: #2080f0;
}

.legend-mem::before {
  background: #18a058;
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
  border: 1px solid var(--n-border-color);
  border-radius: 4px;
  font-size: 13px;
  text-decoration: none;
}

.loginlog {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 16px;
}

.loginlog h4 {
  margin: 0 0 8px;
  font-size: 13px;
  color: var(--n-text-color-3);
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
  border-bottom: 1px dashed var(--n-border-color);
}

.loginlog .muted {
  color: var(--n-text-color-3);
}

.text {
  margin: 0;
  font-size: 13px;
}

.empty {
  padding: 32px 0;
}
</style>
