<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import dayjs from 'dayjs';
import type { WidgetLayoutMap, WorkbenchView, WorkbenchWidgetView } from '@newlifex/page-logic';
import {
  buildWidgetLayout,
  buildWorkbenchView,
  createWidgetLayoutLoader,
  createWorkbenchLoader,
  mergeWidgetsWithLayout,
  moveWidget
} from '@newlifex/page-logic';
import { cubeApi } from '@/api/cube';
import { $t } from '@/locales';
import { localStg } from '@/utils/storage';
import { resolveIconName } from '@/utils/icon';
import KpiCard from './components/kpi-card.vue';
import MonitorChart from './components/monitor-chart.vue';

defineOptions({ name: 'CubeWorkbench' });

const loader = createWorkbenchLoader(cubeApi);
const layoutLoader = createWidgetLayoutLoader(cubeApi);

const view = ref<WorkbenchView | null>(null);
const loading = ref(false);
const errorMessage = ref('');

/** 后端按用户持久化的卡片布局 */
const layout = ref<WidgetLayoutMap>({});
/** 可见卡片，顺序即显示顺序 */
const visibleWidgets = ref<WorkbenchWidgetView[]>([]);
/** 隐藏卡片 */
const hiddenWidgets = ref<WorkbenchWidgetView[]>([]);
const layoutEditing = ref(false);
const layoutDirty = ref(false);
const savingLayout = ref(false);
const draggingName = ref('');

function applyLayout(widgets: WorkbenchWidgetView[]) {
  const merged = mergeWidgetsWithLayout(widgets, layout.value);
  visibleWidgets.value = merged.visible;
  hiddenWidgets.value = merged.hidden;
}

/** 退出编辑模式保留未保存的改动，重新进入后可继续保存或恢复默认 */
function toggleLayoutEditing() {
  layoutEditing.value = !layoutEditing.value;
}

function onDragStart(name: string) {
  if (!layoutEditing.value) return;
  draggingName.value = name;
}

function onDrop(target: string) {
  if (!layoutEditing.value || !draggingName.value) return;
  visibleWidgets.value = moveWidget(visibleWidgets.value, draggingName.value, target);
  draggingName.value = '';
  layoutDirty.value = true;
}

function endDrag() {
  draggingName.value = '';
}

function hideWidget(name: string) {
  const index = visibleWidgets.value.findIndex(item => item.name === name);
  if (index < 0) return;

  const [widget] = visibleWidgets.value.splice(index, 1);
  hiddenWidgets.value.push(widget);
  layoutDirty.value = true;
}

function showWidget(name: string) {
  const index = hiddenWidgets.value.findIndex(item => item.name === name);
  if (index < 0) return;

  const [widget] = hiddenWidgets.value.splice(index, 1);
  visibleWidgets.value.push(widget);
  layoutDirty.value = true;
}

async function saveLayoutChanges() {
  savingLayout.value = true;
  try {
    const map = buildWidgetLayout(visibleWidgets.value, hiddenWidgets.value);
    if (await layoutLoader.save(map)) {
      layout.value = map;
      layoutDirty.value = false;
      ElMessage.success($t('page.workbench.layoutSaved'));
    } else {
      ElMessage.error($t('page.workbench.layoutSaveFailed'));
    }
  } finally {
    savingLayout.value = false;
  }
}

async function resetLayoutChanges() {
  savingLayout.value = true;
  try {
    if (await layoutLoader.reset()) {
      layout.value = {};
      layoutDirty.value = false;
      applyLayout(view.value?.widgets ?? []);
      ElMessage.success($t('page.workbench.layoutReset'));
    } else {
      ElMessage.error($t('page.workbench.layoutSaveFailed'));
    }
  } finally {
    savingLayout.value = false;
  }
}

async function load() {
  loading.value = true;
  errorMessage.value = '';

  try {
    const res = await loader.load();
    if (res.error) {
      errorMessage.value = res.error;
      view.value = null;
      visibleWidgets.value = [];
      hiddenWidgets.value = [];
    } else {
      view.value = buildWorkbenchView(res.data);
      layout.value = await layoutLoader.load();
      applyLayout(view.value.widgets);
    }
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : String(error);
    view.value = null;
    visibleWidgets.value = [];
    hiddenWidgets.value = [];
  } finally {
    loading.value = false;
  }
}

/** Cube 的 `~/xxx` 表示站点根路径，API 控制器路径不是 SPA 页面 */
function toRouterPath(url: string) {
  if (!url) return '/';
  const path = url.startsWith('~/') ? url.slice(1) : url;
  return path.startsWith('/') ? path : `/${path}`;
}

const API_PATH_PREFIXES = ['/newlife.cube/', '/api/', '/auth/', '/sso', '/oauth'];

function isPageLink(url: string) {
  const path = toRouterPath(url).toLowerCase();
  return Boolean(path) && !API_PATH_PREFIXES.some(prefix => path.startsWith(prefix));
}

function pageLinks<T extends { url: string }>(links: T[]) {
  return links.filter(link => isPageLink(link.url));
}

const kpiCards = computed(() => view.value?.kpis ?? []);
const widgets = computed(() => view.value?.widgets ?? []);
const quickLinks = computed(() => pageLinks(view.value?.quickLinks ?? []));
const sysInfoRows = computed(() => view.value?.sysInfoRows ?? []);

/** 部件已含快捷入口/系统信息时不再重复渲染独立卡片 */
const showQuickLinksCard = computed(
  () => quickLinks.value.length > 0 && !widgets.value.some(widget => widget.kind === 'quickLinks')
);
const showSysInfoCard = computed(
  () => sysInfoRows.value.length > 0 && !widgets.value.some(widget => widget.kind === 'kv')
);

const PROFILE_LABELS: Record<string, string> = {
  logins: 'page.workbench.logins',
  lastLogin: 'page.workbench.lastLogin',
  lastLoginIP: 'page.workbench.lastLoginIp',
  registerTime: 'page.workbench.registerTime'
};

function profileLabel(key: string) {
  return PROFILE_LABELS[key] ? $t(PROFILE_LABELS[key] as 'page.workbench.logins') : key;
}

/** 后端 cols 为 12 栅格，Element 栅格为 24 */
function widgetSpan(widget: WorkbenchWidgetView) {
  return Math.min(24, widget.cols * 2);
}

const dateText = dayjs().format(localStg.get('lang') === 'en-US' ? 'dddd, MMMM D, YYYY' : 'YYYY年M月D日dddd');

onMounted(load);
</script>

<template>
  <div class="workbench">
    <ElSkeleton v-if="loading && !view" :rows="6" animated />

    <ElResult v-else-if="errorMessage" icon="error" :title="$t('page.list.loadFailed')" :sub-title="errorMessage">
      <template #extra>
        <ElButton type="primary" @click="load">{{ $t('page.list.retry') }}</ElButton>
      </template>
    </ElResult>

    <template v-else-if="view">
      <div class="workbench__banner">
        <div class="min-w-0">
          <div class="workbench__greeting">{{ $t('page.workbench.greeting', { name: view.displayName }) }}</div>
          <div class="workbench__date">
            <SvgIcon icon="ep:calendar" class="mr-6px" />
            {{ dateText }}
          </div>
        </div>
        <div class="flex-y-center flex-shrink-0 flex-wrap justify-end gap-8px">
          <span v-if="view.roleText" class="workbench__role">{{ view.roleText }}</span>
          <ElButton class="workbench__ghost" @click="toggleLayoutEditing">
            {{ $t(layoutEditing ? 'page.workbench.finishLayout' : 'page.workbench.editLayout') }}
          </ElButton>
          <template v-if="layoutEditing">
            <ElButton
              class="workbench__refresh"
              type="primary"
              :loading="savingLayout"
              :disabled="!layoutDirty"
              @click="saveLayoutChanges"
            >
              {{ $t('page.workbench.saveLayout') }}
            </ElButton>
            <ElButton class="workbench__ghost" :loading="savingLayout" @click="resetLayoutChanges">
              {{ $t('page.workbench.resetLayout') }}
            </ElButton>
          </template>
          <ElButton class="workbench__refresh" :loading="loading" @click="load">{{ $t('common.refresh') }}</ElButton>
        </div>
      </div>

      <ElAlert
        v-if="layoutEditing"
        class="mb-16px"
        type="info"
        show-icon
        :closable="false"
        :title="$t('page.workbench.dragHint')"
      />

      <div v-if="layoutEditing" class="workbench__hidden">
        <div class="workbench__hidden-title">{{ $t('page.workbench.hiddenCards') }}</div>
        <div v-if="hiddenWidgets.length" class="flex flex-wrap gap-8px">
          <ElTag
            v-for="widget in hiddenWidgets"
            :key="widget.name"
            closable
            type="info"
            @close="showWidget(widget.name)"
          >
            {{ widget.title }}
          </ElTag>
        </div>
        <div v-else class="text-13px text-#999">{{ $t('page.workbench.noHiddenCards') }}</div>
      </div>

      <div v-if="kpiCards.length" class="mb-16px grid gap-16px lt-sm:grid-cols-2 sm:grid-cols-3 lg:grid-cols-6">
        <KpiCard v-for="kpi in kpiCards" :key="kpi.name" :kpi="kpi" />
      </div>

      <ElRow :gutter="16">
        <ElCol
          v-for="widget in visibleWidgets"
          :key="widget.name"
          class="mb-16px"
          :xs="24"
          :sm="24"
          :md="widgetSpan(widget)"
          :lg="widgetSpan(widget)"
        >
          <ElCard
            :body-class="widget.kind === 'profile' ? 'pb-0' : ''"
            :draggable="layoutEditing"
            class="workbench__card"
            :class="{
              'workbench__card--editing': layoutEditing,
              'workbench__card--dragging': draggingName === widget.name
            }"
            @dragstart="onDragStart(widget.name)"
            @dragend="endDrag"
            @dragover.prevent
            @drop.prevent="onDrop(widget.name)"
          >
            <template #header>
              <div class="flex-y-center justify-between gap-8px">
                <div class="min-w-0 flex-y-center gap-8px">
                  <SvgIcon v-if="widget.icon" :icon="resolveIconName(widget.icon)" class="text-16px" />
                  <span class="truncate text-14px font-medium">{{ widget.title }}</span>
                </div>
                <div class="flex-y-center flex-shrink-0 gap-8px">
                  <span v-if="widget.category" class="text-12px text-#999">{{ widget.category }}</span>
                  <ElButton
                    v-if="layoutEditing"
                    link
                    type="danger"
                    size="small"
                    @click.stop="hideWidget(widget.name)"
                  >
                    {{ $t('page.workbench.hideCard') }}
                  </ElButton>
                </div>
              </div>
            </template>

            <div v-if="widget.kind === 'profile' && widget.profile" class="profile-card">
              <div class="mb-12px flex-center gap-12px">
                <div class="profile-card__avatar">{{ widget.profile.initial }}</div>
                <div class="min-w-0">
                  <div class="truncate text-15px font-medium">
                    {{ widget.profile.displayName || widget.profile.name }}
                  </div>
                  <div class="mt-4px flex flex-wrap gap-4px">
                    <ElTag v-for="role in widget.profile.roles" :key="role" size="small" type="info">{{ role }}</ElTag>
                  </div>
                </div>
              </div>
              <div v-for="row in widget.profile.rows" :key="row.key" class="kv-row">
                <span class="kv-row__key">{{ profileLabel(row.key) }}</span>
                <span class="truncate">
                  <a v-if="row.link" :href="toRouterPath(row.link.href)" class="text-primary">{{ row.link.text }}</a>
                  <span v-else>{{ row.text }}</span>
                </span>
              </div>
            </div>

            <div v-else-if="widget.kind === 'quickLinks' && widget.links?.length" class="workbench__links">
              <RouterLink v-for="link in pageLinks(widget.links)" :key="link.url" :to="toRouterPath(link.url)">
                <ElButton plain class="w-full">
                  <SvgIcon icon="mdi:link-variant" class="mr-6px text-12px text-#999" />
                  <span class="truncate">{{ link.name }}</span>
                </ElButton>
              </RouterLink>
            </div>

            <div v-else-if="widget.kind === 'loginLog'">
              <div v-for="item in widget.logins" :key="`login-${item.time}-${item.title}`" class="kv-row">
                <span class="truncate">{{ item.title }}</span>
                <span class="text-12px text-#888">{{ item.time }}</span>
              </div>
              <div v-for="item in widget.onlines" :key="`online-${item.time}-${item.title}`" class="kv-row">
                <span class="truncate">{{ item.title }}</span>
                <span class="text-12px text-#888">{{ item.time }}</span>
              </div>
            </div>

            <div v-else-if="widget.kind === 'kv' && widget.rows?.length">
              <div v-for="row in widget.rows" :key="row.key" class="kv-row">
                <span class="kv-row__key">{{ row.key }}</span>
                <span class="truncate">
                  <a v-if="row.link" :href="toRouterPath(row.link.href)" class="text-primary">{{ row.link.text }}</a>
                  <span v-else>{{ row.text }}</span>
                </span>
              </div>
            </div>

            <div v-else-if="widget.kind === 'text' && widget.text" class="whitespace-pre-wrap text-13px leading-relaxed">
              {{ widget.text }}
            </div>

            <MonitorChart v-else-if="widget.kind === 'monitor'" />

            <ElEmpty v-else :description="$t('page.workbench.empty')" :image-size="72" />
          </ElCard>
        </ElCol>
      </ElRow>

      <ElRow :gutter="16">
        <ElCol v-if="showQuickLinksCard" class="mb-16px" :xs="24" :md="12">
          <ElCard>
            <template #header>
              <span class="text-14px font-medium">{{ $t('page.workbench.quickLinks') }}</span>
            </template>
            <div class="workbench__links">
              <RouterLink v-for="link in quickLinks" :key="link.url" :to="toRouterPath(link.url)">
                <ElButton plain class="w-full">
                  <SvgIcon icon="mdi:link-variant" class="mr-6px text-12px text-#999" />
                  <span class="truncate">{{ link.name }}</span>
                </ElButton>
              </RouterLink>
            </div>
          </ElCard>
        </ElCol>
        <ElCol v-if="showSysInfoCard" class="mb-16px" :xs="24" :md="12">
          <ElCard>
            <template #header>
              <span class="text-14px font-medium">{{ $t('page.workbench.systemInfo') }}</span>
            </template>
            <div v-for="row in sysInfoRows" :key="row.key" class="kv-row">
              <span class="kv-row__key">{{ row.key }}</span>
              <span class="truncate">{{ row.text }}</span>
            </div>
          </ElCard>
        </ElCol>
      </ElRow>
    </template>
  </div>
</template>

<style scoped>
/* 工作台整体与页面边框保持 16px 间距 */
.workbench {
  --el-card-padding: 16px;

  padding: 16px;
}

/* 同一行卡片等高：栅格列拉伸，卡片填满整行高度 */
.workbench :deep(.el-row) {
  align-items: stretch;
}

.workbench :deep(.el-col) {
  display: flex;
}

.workbench :deep(.el-col > .el-card) {
  flex: 1;
  width: 100%;
  min-width: 0;
}

/* 顶部问候横幅 */
.workbench__banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
  padding: 16px;
  border-radius: 8px;
  background: linear-gradient(135deg, #5ca8f5, #86c2fa);
  box-shadow: 0 2px 8px rgb(64 158 255 / 20%);
  color: #fff;
}

.workbench__greeting {
  font-size: 24px;
  font-weight: 600;
  line-height: 1.3;
}

.workbench__date {
  display: flex;
  align-items: center;
  margin-top: 8px;
  font-size: 13px;
  opacity: 0.9;
}

.workbench__role {
  padding: 2px 10px;
  border-radius: 4px;
  background: rgb(255 255 255 / 22%);
  font-size: 12px;
  white-space: nowrap;
}

.workbench__refresh {
  border-color: #fff;
  background: #fff;
  color: var(--el-text-color-primary);
}

.workbench__refresh:hover {
  border-color: #fff;
  background: #f5f7fa;
  color: var(--el-color-primary);
}

.workbench__ghost {
  border-color: rgb(255 255 255 / 60%);
  background: transparent;
  color: #fff;
}

.workbench__ghost:hover {
  border-color: #fff;
  background: rgb(255 255 255 / 18%);
  color: #fff;
}

/* 布局编辑：卡片可拖动并带虚线轮廓 */
.workbench__card--editing {
  cursor: grab;
  outline: 1px dashed var(--el-color-primary-light-5);
  outline-offset: -4px;
}

.workbench__card--dragging {
  opacity: 0.55;
}

/* 编辑模式下隐藏卡片区 */
.workbench__hidden {
  margin-bottom: 16px;
  padding: 12px 16px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  background: var(--el-fill-color-light);
}

.workbench__hidden-title {
  margin-bottom: 8px;
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

/* 快捷入口两列等宽 */
.workbench__links {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px;
}

.profile-card__avatar {
  width: 44px;
  height: 44px;
  border-radius: 50%;
  background-color: var(--el-color-primary);
  color: #fff;
  font-size: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.kv-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 6px 0;
  border-bottom: 1px dashed var(--el-border-color-lighter);
  font-size: 13px;
}

.kv-row:last-child {
  border-bottom: none;
}

.kv-row__key {
  color: var(--el-text-color-secondary);
  flex-shrink: 0;
}
</style>
