<template>
  <div class="home-page">
    <t-loading :loading="loading && !view" size="small" style="width: 100%">
      <t-alert v-if="error && !view" theme="error" :message="`工作台加载失败：${error}`">
        <template #operation>
          <t-button size="small" variant="outline" @click="reload">重试</t-button>
        </template>
      </t-alert>

      <template v-else>
        <div class="banner">
          <div>
            <h2 class="banner-title">{{ greeting }}，{{ view?.displayName || '-' }}</h2>
            <p class="banner-sub">{{ today }}<span v-if="view?.roleText"> · {{ view.roleText }}</span></p>
          </div>
          <t-button :loading="loading" variant="outline" @click="reload">刷新</t-button>
        </div>

        <t-alert v-if="error" class="banner-alert" theme="warning" :message="error" />

        <t-row v-if="view?.kpis.length" :gutter="[16, 16]" class="kpis">
          <t-col v-for="kpi in view.kpis" :key="kpi.name" :xs="12" :sm="6" :md="3">
            <t-card :class="['kpi', `kpi-${kpi.color}`]" :bordered="true" hover-shadow @click="openKpi(kpi.url)">
              <div class="kpi-label">{{ kpi.label }}</div>
              <div class="kpi-value">{{ kpi.value }}</div>
              <div v-if="kpi.trend" class="kpi-trend">{{ kpi.trend }}</div>
            </t-card>
          </t-col>
        </t-row>

        <t-row v-if="view?.widgets.length" :gutter="[16, 16]" class="widgets">
          <t-col v-for="widget in view.widgets" :key="widget.name" :xs="12" :md="widget.cols">
            <t-card :title="widget.title" :bordered="true" class="widget" :data-widget="widget.name">
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
                    <span v-if="latestMonitor?.memUsed != null && latestMonitor?.memTotal">
                      {{ latestMonitor.memUsed }} / {{ latestMonitor.memTotal }} MB
                    </span>
                  </div>
                </div>
                <t-empty v-else description="正在采集监控数据" />
              </template>

              <!-- 个人信息 -->
              <template v-else-if="widget.kind === 'profile' && widget.profile">
                <div class="profile-head">
                  <t-avatar size="44px">{{ widget.profile.initial }}</t-avatar>
                  <div>
                    <div class="profile-name">{{ widget.profile.displayName }}</div>
                    <div class="profile-tags">
                      <t-tag size="small" :theme="widget.profile.online ? 'success' : 'default'" variant="light">
                        {{ widget.profile.online ? '在线' : '离线' }}
                      </t-tag>
                      <t-tag v-for="role in widget.profile.roles" :key="role" size="small" variant="light">{{ role }}</t-tag>
                    </div>
                  </div>
                </div>
                <t-descriptions :column="2" size="small" bordered>
                  <t-descriptions-item v-for="(row, index) in widget.profile.rows" :key="row.key" :label="profileLabels[index] || row.key">
                    {{ row.text }}
                  </t-descriptions-item>
                </t-descriptions>
              </template>

              <!-- 快捷入口 -->
              <template v-else-if="widget.kind === 'quickLinks'">
                <div v-if="widget.links?.length" class="links">
                  <router-link v-for="link in widget.links" :key="link.url" :to="link.url" class="link">
                    {{ link.name }}
                  </router-link>
                </div>
                <t-empty v-else description="暂无快捷入口" />
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
                    <t-empty v-else description="暂无登录记录" />
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
                    <t-empty v-else description="暂无在线用户" />
                  </section>
                </div>
              </template>

              <!-- 键值卡 -->
              <t-descriptions v-else-if="widget.kind === 'kv' && widget.rows?.length" :column="1" size="small" bordered>
                <t-descriptions-item v-for="row in widget.rows" :key="row.key" :label="row.key">
                  <a v-if="row.link" :href="row.link.href" :target="row.link.external ? '_blank' : undefined" rel="noopener noreferrer">
                    {{ row.link.text }}
                  </a>
                  <span v-else>{{ row.text }}</span>
                </t-descriptions-item>
              </t-descriptions>

              <p v-else-if="widget.kind === 'text'" class="text">{{ widget.text }}</p>

              <t-empty v-else description="暂无数据" />
            </t-card>
          </t-col>
        </t-row>

        <t-empty v-if="view && !view.hasContent" class="empty" description="暂无工作台数据" />
      </template>
    </t-loading>
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
import { api } from '@/api';

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

.banner-title {
  margin: 0;
  font-size: 22px;
}

.banner-sub {
  margin: 4px 0 0;
  color: var(--td-text-color-secondary);
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
  border-left: 3px solid var(--td-brand-color);
  height: 100%;
}

.kpi-label {
  color: var(--td-text-color-secondary);
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
  color: var(--td-text-color-secondary);
}

.kpi-green {
  border-left-color: var(--td-success-color);
}

.kpi-cyan {
  border-left-color: var(--td-brand-color-light-active, #0052d9);
}

.kpi-orange {
  border-left-color: var(--td-warning-color);
}

.kpi-red {
  border-left-color: var(--td-error-color);
}

.kpi-purple {
  border-left-color: #834ec2;
}

.kpi-grey {
  border-left-color: var(--td-gray-color-7, #8d8d8d);
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
  stroke: var(--td-brand-color, #0052d9);
}

.line-mem {
  stroke: var(--td-success-color, #2ba471);
}

.monitor-legend {
  display: flex;
  gap: 16px;
  font-size: 12px;
  color: var(--td-text-color-secondary);
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
  background: var(--td-brand-color, #0052d9);
}

.legend-mem::before {
  background: var(--td-success-color, #2ba471);
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
  border: 1px solid var(--td-component-border);
  border-radius: 4px;
  font-size: 13px;
  text-decoration: none;
  color: var(--td-brand-color);
}

.loginlog {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 16px;
}

.loginlog h4 {
  margin: 0 0 8px;
  font-size: 13px;
  color: var(--td-text-color-secondary);
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
  border-bottom: 1px dashed var(--td-component-stroke);
}

.loginlog .muted {
  color: var(--td-text-color-secondary);
}

.text {
  margin: 0;
  font-size: 13px;
}

.empty {
  padding: 32px 0;
}
</style>
