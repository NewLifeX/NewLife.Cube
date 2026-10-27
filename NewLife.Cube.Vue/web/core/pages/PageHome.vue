<script lang="ts" setup>
/**
 * 工作台首页
 *
 * 数据全部来自后端 `Admin/Index/Dashboard` 聚合端点（KPI + Widget 内容卡 + 快捷入口 + 个人信息），
 * 性能监控卡按 5 秒轮询 `Admin/Index/MonitorData`。页面只做布局与渲染，
 * 加载/错误/轮询生命周期集中在 `useWorkbench`。
 */
import { computed, markRaw, onMounted, onUnmounted, ref, type Component } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import {
  Calendar,
  Clock,
  DataLine,
  Document,
  Grid,
  Monitor,
  Setting,
  SwitchButton,
  TrendCharts,
  User,
  UserFilled,
  WarningFilled,
} from '@element-plus/icons-vue';
import { useWorkbench } from '../composables/useWorkbench';
import { normalizeCols, normalizeKpiColor, normalizeRoles } from './workbench/widget';
import { buildWidgetLayout, mergeWidgetsWithLayout, moveWidget, type WidgetLayoutMap } from './workbench/layout';

const { t } = useI18n();
import WorkbenchMonitor from './workbench/WorkbenchMonitor.vue';
import WorkbenchWidgetBody from './workbench/WorkbenchWidgetBody.vue';
import type { WorkbenchKpi, WorkbenchWidget } from '../types/workbench';

const router = useRouter();
const {
  data,
  loading,
  error,
  monitorPoints,
  hasChartWidget,
  load,
  startMonitorPolling,
  stopMonitorPolling,
  loadLayout,
  saveLayout,
  resetLayout,
} = useWorkbench();

/** KPI 名称 → 图标。后端只下发名称/配色，图标由皮肤决定 */
const KPI_ICONS: Record<string, Component> = {
  UserCount: markRaw(UserFilled),
  TodayLogin: markRaw(SwitchButton),
  OnlineCount: markRaw(User),
  MaxOnline: markRaw(TrendCharts),
  Error24h: markRaw(WarningFilled),
  OnlineTime: markRaw(Clock),
};

/** 内容卡名称 → 图标 */
const WIDGET_ICONS: Record<string, Component> = {
  Profile: markRaw(User),
  QuickLink: markRaw(Grid),
  Monitor: markRaw(DataLine),
  SysInfo: markRaw(Monitor),
  LoginLog: markRaw(TrendCharts),
};

/** FontAwesome 图标名 → Element Plus 图标。兼容外部业务部件 */
const FA_ICONS: Record<string, Component> = {
  'fa-users': markRaw(UserFilled),
  'fa-sign-in': markRaw(SwitchButton),
  'fa-user-circle-o': markRaw(User),
  'fa-signal': markRaw(TrendCharts),
  'fa-exclamation-triangle': markRaw(WarningFilled),
  'fa-clock-o': markRaw(Clock),
  'fa-server': markRaw(Monitor),
  'fa-th-large': markRaw(Grid),
  'fa-user-circle': markRaw(User),
  'fa-line-chart': markRaw(DataLine),
  'fa-tachometer': markRaw(Monitor),
  'fa-cog': markRaw(Setting),
};

const FALLBACK_ICON = markRaw(Document);

const kpis = computed(() => data.value?.kpis ?? []);
const hasContent = computed(() => kpis.value.length > 0 || (data.value?.widgets?.length ?? 0) > 0);
const displayName = computed(
  () => data.value?.user?.displayName || data.value?.profile?.displayName || data.value?.user?.name || '-',
);
const roleText = computed(() =>
  normalizeRoles(data.value?.user?.roles ?? data.value?.profile?.roleNames).join(' / '),
);
const today = computed(() =>
  new Date().toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    weekday: 'long',
  }),
);
const greeting = computed(() => {
  const hour = new Date().getHours();
  if (hour < 6) return t('home.greetingDawn');
  if (hour < 12) return t('home.greetingMorning');
  if (hour < 14) return t('home.greetingNoon');
  if (hour < 18) return t('home.greetingAfternoon');
  return t('home.greetingEvening');
});

function iconForKpi(kpi: WorkbenchKpi): Component {
  return KPI_ICONS[kpi.name] || FALLBACK_ICON;
}

function iconForWidget(widget: WorkbenchWidget): Component {
  return WIDGET_ICONS[widget.name] || FA_ICONS[widget.icon || ''] || FALLBACK_ICON;
}

function isMonitor(widget: WorkbenchWidget): boolean {
  return widget.widgetType === 'Chart' && widget.name === 'Monitor';
}

function cardStyle(cols?: number): Record<string, string> {
  return { gridColumn: `span ${normalizeCols(cols)}` };
}

function openKpi(kpi: WorkbenchKpi): void {
  if (kpi.url) void router.push(kpi.url);
}

// ── 卡片布局：拖动排序 / 隐藏 / 保存 / 恢复默认 ──

/** 后端持久化的用户布局 */
const layout = ref<WidgetLayoutMap>({});
/** 可见卡片（顺序即显示顺序） */
const visibleWidgets = ref<WorkbenchWidget[]>([]);
/** 隐藏卡片 */
const hiddenWidgets = ref<WorkbenchWidget[]>([]);
/** 是否处于布局编辑模式 */
const layoutEditing = ref(false);
/** 是否有未保存的布局改动 */
const layoutDirty = ref(false);
/** 保存中 */
const savingLayout = ref(false);
/** 正在拖动的卡片名 */
const draggingName = ref('');

function applyLayout(widgets: WorkbenchWidget[]): void {
  const merged = mergeWidgetsWithLayout(widgets, layout.value);
  visibleWidgets.value = merged.visible;
  hiddenWidgets.value = merged.hidden;
}

function onDragStart(name: string): void {
  draggingName.value = name;
}

function onDrop(target: string): void {
  if (!draggingName.value) return;
  visibleWidgets.value = moveWidget(visibleWidgets.value, draggingName.value, target);
  draggingName.value = '';
  layoutDirty.value = true;
}

function hideWidget(name: string): void {
  const index = visibleWidgets.value.findIndex((item) => item.name === name);
  if (index < 0) return;

  const [widget] = visibleWidgets.value.splice(index, 1);
  hiddenWidgets.value.push(widget);
  layoutDirty.value = true;
}

function showWidget(name: string): void {
  const index = hiddenWidgets.value.findIndex((item) => item.name === name);
  if (index < 0) return;

  const [widget] = hiddenWidgets.value.splice(index, 1);
  visibleWidgets.value.push(widget);
  layoutDirty.value = true;
}

async function saveLayoutChanges(): Promise<void> {
  savingLayout.value = true;
  try {
    const map = buildWidgetLayout(visibleWidgets.value, hiddenWidgets.value);
    if (await saveLayout(map)) {
      layout.value = map;
      layoutDirty.value = false;
      ElMessage.success(t('workbench.layoutSaved'));
    } else {
      ElMessage.error(t('workbench.layoutSaveFailed'));
    }
  } finally {
    savingLayout.value = false;
  }
}

async function resetLayoutChanges(): Promise<void> {
  savingLayout.value = true;
  try {
    if (await resetLayout()) {
      layout.value = {};
      layoutDirty.value = false;
      applyLayout(data.value?.widgets ?? []);
      ElMessage.success(t('workbench.layoutReset'));
    } else {
      ElMessage.error(t('workbench.layoutSaveFailed'));
    }
  } finally {
    savingLayout.value = false;
  }
}

async function loadData(): Promise<void> {
  await load();
  layout.value = await loadLayout();
  applyLayout(data.value?.widgets ?? []);
  if (hasChartWidget.value) startMonitorPolling();
  else stopMonitorPolling();
}

async function reload(): Promise<void> {
  await load();
  applyLayout(data.value?.widgets ?? []);
  if (hasChartWidget.value) startMonitorPolling();
  else stopMonitorPolling();
}

onMounted(loadData);

onUnmounted(stopMonitorPolling);
</script>

<template>
  <div class="workbench" data-cy="workbench">
    <el-skeleton v-if="loading && !data" class="wb-skeleton" :rows="8" animated />

    <el-result
      v-else-if="error && !data"
      icon="error"
      :title="t('home.loadFailed')"
      :sub-title="error"
    >
      <template #extra>
        <el-button type="primary" @click="reload">{{ t('home.retry') }}</el-button>
      </template>
    </el-result>

    <template v-else-if="data">
      <!-- 欢迎横幅 -->
      <section class="wb-banner" data-cy="workbench-banner">
        <div class="wb-banner-main">
          <h1>{{ greeting }}，{{ displayName }}</h1>
          <p class="wb-date"><el-icon><Calendar /></el-icon>{{ today }}</p>
        </div>
        <div class="wb-banner-actions">
          <el-tag v-if="roleText" type="primary" effect="plain">{{ roleText }}</el-tag>
          <el-button :type="layoutEditing ? 'primary' : 'default'" @click="layoutEditing = !layoutEditing">
            {{ layoutEditing ? t('workbench.finishLayout') : t('workbench.editLayout') }}
          </el-button>
          <template v-if="layoutEditing">
            <el-button :loading="savingLayout" :disabled="!layoutDirty" type="primary" @click="saveLayoutChanges">
              {{ t('workbench.saveLayout') }}
            </el-button>
            <el-button :loading="savingLayout" @click="resetLayoutChanges">{{ t('workbench.resetLayout') }}</el-button>
          </template>
          <el-button data-cy="workbench-refresh" :loading="loading" @click="reload">{{ t('grid.refresh') }}</el-button>
        </div>
      </section>

      <el-alert
        v-if="error"
        class="wb-alert"
        data-cy="workbench-error"
        type="warning"
        :title="error"
        show-icon
        :closable="false"
      />

      <!-- KPI 指标行 -->
      <section v-if="kpis.length" class="wb-kpis" data-cy="workbench-kpis">
        <button
          v-for="kpi in kpis"
          :key="kpi.name"
          type="button"
          class="wb-kpi"
          :class="`wb-kpi-${normalizeKpiColor(kpi.color)}`"
          :disabled="!kpi.url"
          :data-cy="`workbench-kpi-${kpi.name}`"
          @click="openKpi(kpi)"
        >
          <span class="wb-kpi-icon"><el-icon :size="20"><component :is="iconForKpi(kpi)" /></el-icon></span>
          <span class="wb-kpi-value">{{ kpi.value }}</span>
          <span class="wb-kpi-label">{{ kpi.label }}</span>
          <span v-if="kpi.trend" class="wb-kpi-trend">{{ kpi.trend }}</span>
        </button>
      </section>

      <!-- 布局编辑提示与隐藏卡片 -->
      <el-alert
        v-if="layoutEditing"
        class="wb-alert"
        data-cy="workbench-layout-hint"
        type="info"
        :title="t('workbench.dragHint')"
        show-icon
        :closable="false"
      />
      <section v-if="layoutEditing" class="wb-hidden" data-cy="workbench-hidden">
        <h3>{{ t('workbench.hiddenCards') }}</h3>
        <div v-if="hiddenWidgets.length" class="wb-hidden-list">
          <el-tag
            v-for="widget in hiddenWidgets"
            :key="widget.name"
            closable
            @close="showWidget(widget.name)"
          >
            {{ widget.title }} · {{ t('workbench.showCard') }}
          </el-tag>
        </div>
        <p v-else class="wb-hidden-empty">{{ t('workbench.noHiddenCards') }}</p>
      </section>

      <!-- 内容卡 -->
      <section v-if="visibleWidgets.length" class="wb-grid" data-cy="workbench-widgets">
        <el-card
          v-for="widget in visibleWidgets"
          :key="widget.name"
          class="wb-card"
          :class="{ 'wb-card-editing': layoutEditing, 'wb-card-dragging': draggingName === widget.name }"
          :style="cardStyle(widget.cols)"
          :draggable="layoutEditing"
          shadow="never"
          data-cy="workbench-widget"
          @dragstart="onDragStart(widget.name)"
          @dragend="draggingName = ''"
          @dragover.prevent
          @drop.prevent="onDrop(widget.name)"
        >
          <template #header>
            <div class="wb-card-header">
              <el-icon><component :is="iconForWidget(widget)" /></el-icon>
              <span class="wb-card-title">{{ widget.title }}</span>
              <span v-if="widget.category" class="wb-card-category">{{ widget.category }}</span>
              <el-button
                v-if="layoutEditing"
                class="wb-card-hide"
                link
                type="danger"
                :title="t('workbench.hideCard')"
                @click="hideWidget(widget.name)"
              >
                {{ t('workbench.hideCard') }}
              </el-button>
            </div>
          </template>

          <WorkbenchMonitor v-if="isMonitor(widget)" :points="monitorPoints" />
          <WorkbenchWidgetBody v-else :widget="widget" />
        </el-card>
      </section>

      <el-empty v-if="!hasContent" :description="t('home.empty')" />
    </template>
  </div>
</template>

<style lang="scss" scoped>
.workbench {
  box-sizing: border-box;
  min-height: 100%;
  padding: 20px;
  background: var(--el-bg-color-page);
}

.wb-skeleton {
  padding: 8px;
}

/* ── 欢迎横幅 ── */
.wb-banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  padding: 22px 26px;
  margin-bottom: 16px;
  border-radius: 12px;
  background:
    radial-gradient(circle at 92% 18%, rgb(255 255 255 / 20%) 0, transparent 42%),
    linear-gradient(120deg, var(--el-color-primary) 0%, var(--el-color-primary-light-3) 100%);
  color: var(--el-color-white);
  box-shadow: var(--el-box-shadow-light);

  h1 {
    margin: 0 0 6px;
    font-size: 22px;
    font-weight: 600;
  }
}

.wb-date {
  display: flex;
  align-items: center;
  gap: 6px;
  margin: 0;
  font-size: 13px;
  opacity: 0.9;
}

.wb-banner-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
}

.wb-alert {
  margin-bottom: 16px;
}

/* ── KPI 指标行 ── */
.wb-kpis {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(168px, 1fr));
  gap: 12px;
  margin-bottom: 16px;
}

.wb-kpi {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr);
  grid-template-areas:
    'icon label'
    'icon value'
    'icon trend';
  align-items: center;
  column-gap: 12px;
  padding: 14px 16px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 10px;
  background: var(--el-bg-color);
  color: var(--el-text-color-primary);
  text-align: left;
  cursor: pointer;
  transition:
    transform 0.2s,
    box-shadow 0.2s,
    border-color 0.2s;

  &:hover:not(:disabled) {
    border-color: var(--wb-kpi-color);
    box-shadow: var(--el-box-shadow-light);
    transform: translateY(-1px);
  }

  &:disabled {
    cursor: default;
  }
}

.wb-kpi-icon {
  grid-area: icon;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  border-radius: 10px;
  background: var(--el-fill-color-light);
  color: var(--el-color-primary);
}

.wb-kpi-value {
  grid-area: value;
  font-size: 22px;
  font-weight: 700;
  line-height: 1.15;
  font-variant-numeric: tabular-nums;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.wb-kpi-label {
  grid-area: label;
  color: var(--el-text-color-secondary);
  font-size: 12px;
}

.wb-kpi-trend {
  grid-area: trend;
  color: var(--el-text-color-secondary);
  font-size: 12px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* KPI 强调色：直接映射 Element 主题变量；青/紫由主题色 color-mix 合成，避免引入硬编码色值 */
.wb-kpi-blue {
  .wb-kpi-icon {
    color: var(--el-color-primary);
  }

  &:hover:not(:disabled) {
    border-color: var(--el-color-primary);
  }
}

.wb-kpi-green {
  .wb-kpi-icon {
    color: var(--el-color-success);
  }

  &:hover:not(:disabled) {
    border-color: var(--el-color-success);
  }
}

.wb-kpi-cyan {
  --wb-kpi-color: color-mix(in srgb, var(--el-color-success) 55%, var(--el-color-primary));

  .wb-kpi-icon {
    color: var(--wb-kpi-color);
  }
}

.wb-kpi-orange {
  .wb-kpi-icon {
    color: var(--el-color-warning);
  }

  &:hover:not(:disabled) {
    border-color: var(--el-color-warning);
  }
}

.wb-kpi-red {
  .wb-kpi-icon {
    color: var(--el-color-danger);
  }

  &:hover:not(:disabled) {
    border-color: var(--el-color-danger);
  }
}

.wb-kpi-purple {
  --wb-kpi-color: color-mix(in srgb, var(--el-color-primary) 55%, var(--el-color-danger));

  .wb-kpi-icon {
    color: var(--wb-kpi-color);
  }
}

.wb-kpi-grey {
  .wb-kpi-icon {
    color: var(--el-color-info);
  }

  &:hover:not(:disabled) {
    border-color: var(--el-color-info);
  }
}

/* ── 内容卡栅格 ── */
.wb-grid {
  display: grid;
  grid-template-columns: repeat(12, minmax(0, 1fr));
  gap: 16px;
}

.wb-card {
  min-width: 0;
  border-radius: 10px;
}

.wb-card-header {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
  color: var(--el-text-color-primary);
}

/* 布局编辑：卡片可拖动，隐藏按钮贴右 */
.wb-card-editing {
  cursor: grab;
  outline: 1px dashed var(--el-color-primary-light-5);
  outline-offset: -4px;
}

.wb-card-dragging {
  opacity: 0.55;
}

.wb-card-hide {
  margin-left: auto;
  font-size: 12px;
}

.wb-hidden {
  padding: 12px 16px;
  margin-bottom: 16px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  background: var(--el-fill-color-light);

  h3 {
    margin: 0 0 8px;
    font-size: 13px;
    color: var(--el-text-color-secondary);
  }
}

.wb-hidden-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.wb-hidden-empty {
  margin: 0;
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.wb-card-title {
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.wb-card-category {
  margin-left: auto;
  color: var(--el-text-color-secondary);
  font-size: 12px;
  font-weight: 400;
}

@media (max-width: 1200px) {
  .wb-card {
    grid-column: span 6;
  }
}

@media (max-width: 768px) {
  .workbench {
    padding: 12px;
  }

  .wb-banner {
    flex-direction: column;
    align-items: flex-start;
    padding: 18px;
  }

  .wb-card {
    grid-column: span 12;
  }
}
</style>
