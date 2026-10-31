<script setup lang="ts">
/**
 * SearchBar - 菜单搜索组件
 *
 * icon 模式 — 顶栏图标按钮，点击弹出菜单搜索面板（TopMenu 默认布局）
 * box  模式 — 侧边栏搜索框 + 内联结果下拉（Cyber 布局）
 *
 * 搜索数据来自已加载的菜单树（/Cube/MenuTree，已按角色过滤），纯前端内存过滤。
 * 是否显示由 CubeSetting.EnableMenuSearch 经 /Cube/Site 的 features.menuSearch 下发控制。
 */
import { ref, computed, nextTick, onMounted, onUnmounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { storeToRefs } from 'pinia';
import { useMenuStore } from '@newlifex/cube-vue/core/stores/menu';
import { useSiteStore } from '@newlifex/cube-vue/core/stores/site';
import { openMenuTab } from '@newlifex/cube-vue/core/utils/menuTab';
import {
  getRecentMenus,
  highlightSegments,
  searchMenus,
  type MenuSearchResult,
} from '@newlifex/cube-vue/core/utils/menuSearch';
import ActionButton from './ActionButton.vue';

interface Props {
  mode?: 'icon' | 'box';
  placeholder?: string;
}

const { mode = 'icon', placeholder } = defineProps<Props>();

const { t } = useI18n();
const emit = defineEmits<{ search: [value: string] }>();

const menuStore = useMenuStore();
const siteStore = useSiteStore();
const { flatMenus } = storeToRefs(menuStore);

void siteStore.load();

/** 菜单搜索开关。站点配置未返回时默认显示，避免旧后端缺失该字段时功能消失 */
const menuSearchEnabled = computed(() => siteStore.config?.features?.menuSearch !== false);
const placeholderText = computed(() => placeholder ?? t('topBar.searchMenu'));

const searchText = ref('');
const showResults = ref(false);
const containerRef = ref<HTMLElement | null>(null);

const dialogVisible = ref(false);
const dialogText = ref('');
const activeIndex = ref(0);
const dialogInputRef = ref<HTMLInputElement | null>(null);
const dialogListRef = ref<HTMLElement | null>(null);

const RECENT_KEY = 'cube-menu-search-recent';
const MAX_RECENT = 12;

/** 读取最近使用的菜单 URL，最近的在前 */
function loadRecent(): string[] {
  try {
    const list = JSON.parse(localStorage.getItem(RECENT_KEY) || '[]');
    return Array.isArray(list) ? list.filter((e): e is string => typeof e === 'string' && !!e) : [];
  } catch {
    return [];
  }
}

/** 记录最近使用的菜单 URL */
function addRecent(url: string): string[] {
  try {
    const list = loadRecent().filter((e) => e !== url);
    list.unshift(url);
    if (list.length > MAX_RECENT) list.length = MAX_RECENT;
    localStorage.setItem(RECENT_KEY, JSON.stringify(list));
    return list;
  } catch {
    return loadRecent();
  }
}

const recentUrls = ref<string[]>(loadRecent());

/** 内联搜索（box 模式）。关键字为空时展示最近使用 */
const searchResults = computed(() =>
  searchText.value.trim()
    ? searchMenus(flatMenus.value, searchText.value, 10, recentUrls.value)
    : getRecentMenus(flatMenus.value, recentUrls.value, 8),
);

/** 弹窗搜索（icon 模式）。关键字为空时展示最近使用 */
const dialogResults = computed(() =>
  dialogText.value.trim()
    ? searchMenus(flatMenus.value, dialogText.value, 20, recentUrls.value)
    : getRecentMenus(flatMenus.value, recentUrls.value, 8),
);

/** 取首个关键字用于结果高亮 */
function firstToken(keyword: string): string {
  return (keyword ?? '').trim().split(/\s+/)[0] ?? '';
}

function displayName(result: MenuSearchResult): string {
  return result.menu.title || result.menu.name;
}

function handleInput(e: Event) {
  const value = (e.target as HTMLInputElement).value;
  searchText.value = value;
  showResults.value = value.trim().length > 0 || searchResults.value.length > 0;
  emit('search', value);
}

function clearSearch() {
  searchText.value = '';
  showResults.value = false;
}

function selectResult(result: MenuSearchResult) {
  const { menu } = result;
  if (menu.path) {
    recentUrls.value = addRecent(menu.path);
    openMenuTab({ url: menu.path, title: menu.name });
    menuStore.setActiveMenu(menu);
  }
  clearSearch();
  dialogVisible.value = false;
  dialogText.value = '';
}

function openDialog() {
  dialogText.value = '';
  activeIndex.value = 0;
  dialogVisible.value = true;
  nextTick(() => dialogInputRef.value?.focus());
}

function onDialogInput(e: Event) {
  dialogText.value = (e.target as HTMLInputElement).value;
  activeIndex.value = 0;
}

function setActive(index: number) {
  if (index < 0 || index >= dialogResults.value.length) return;
  activeIndex.value = index;
  nextTick(() => {
    const active = dialogListRef.value?.querySelector<HTMLElement>('.ms-item.is-active');
    active?.scrollIntoView({ block: 'nearest' });
  });
}

function onDialogKeydown(e: KeyboardEvent) {
  if (e.key === 'ArrowDown') {
    e.preventDefault();
    setActive(Math.min(activeIndex.value + 1, dialogResults.value.length - 1));
  } else if (e.key === 'ArrowUp') {
    e.preventDefault();
    setActive(Math.max(activeIndex.value - 1, 0));
  } else if (e.key === 'Enter') {
    const result = dialogResults.value[activeIndex.value] ?? dialogResults.value[0];
    if (!result) return;
    e.preventDefault();
    selectResult(result);
  } else if (e.key === 'Escape') {
    dialogVisible.value = false;
  }
}

function onClickOutside(e: MouseEvent) {
  if (containerRef.value && !containerRef.value.contains(e.target as Node)) {
    showResults.value = false;
  }
}

onMounted(() => document.addEventListener('mousedown', onClickOutside, true));
onUnmounted(() => document.removeEventListener('mousedown', onClickOutside, true));
</script>

<template>
  <template v-if="menuSearchEnabled">
    <!-- 图标模式：点击打开菜单搜索面板 -->
    <template v-if="mode === 'icon'">
      <ActionButton :title="t('topBar.globalSearch')" @click="openDialog">
        <svg
          width="15"
          height="15"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
        >
          <circle cx="11" cy="11" r="8" />
          <path d="M21 21l-4.35-4.35" />
        </svg>
      </ActionButton>

      <el-dialog
        v-model="dialogVisible"
        class="menu-search-dialog"
        width="560px"
        :show-close="false"
        append-to-body
        @opened="dialogInputRef?.focus()"
      >
        <div class="ms-dialog">
          <div class="ms-input-box">
            <svg
              class="ms-icon"
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
            >
              <circle cx="11" cy="11" r="8" />
              <path d="M21 21l-4.35-4.35" />
            </svg>
            <input
              ref="dialogInputRef"
              class="ms-input"
              type="text"
              :value="dialogText"
              :placeholder="placeholderText"
              autocomplete="off"
              @input="onDialogInput"
              @keydown="onDialogKeydown"
            />
            <button v-if="dialogText" class="ms-clear" @mousedown.prevent="dialogText = ''">
              <svg
                width="12"
                height="12"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2.5"
              >
                <path d="M18 6L6 18M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div ref="dialogListRef" class="ms-list">
            <div v-if="dialogText.trim() && !dialogResults.length" class="ms-empty">
              {{ t('topBar.noMatchingMenu') }}
            </div>
            <button
              v-for="(result, index) in dialogResults"
              :key="result.menu.id"
              class="ms-item"
              :class="{ 'is-active': index === activeIndex }"
              @mousemove="setActive(index)"
              @mousedown.prevent="selectResult(result)"
            >
              <span class="ms-item-name">
                <template
                  v-for="(segment, segmentIndex) in highlightSegments(
                    displayName(result),
                    firstToken(dialogText),
                  )"
                  :key="segmentIndex"
                >
                  <mark v-if="segment.match">{{ segment.text }}</mark>
                  <template v-else>{{ segment.text }}</template>
                </template>
              </span>
              <span class="ms-item-path">{{ result.path }}</span>
            </button>
          </div>
        </div>
      </el-dialog>
    </template>

    <!-- 搜索框模式 -->
    <div v-else ref="containerRef" class="sb-wrap">
    <!-- 输入框 -->
    <div class="sb-box" :class="{ focused: showResults }">
      <svg
        class="sb-icon"
        width="14"
        height="14"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="2"
      >
        <circle cx="11" cy="11" r="8" />
        <path d="M21 21l-4.35-4.35" />
      </svg>
      <input
        type="text"
        class="sb-input"
        :placeholder="placeholderText"
        :value="searchText"
        @input="handleInput"
        @focus="showResults = searchResults.length > 0"
      />
      <button v-if="searchText" class="sb-clear" @mousedown.prevent="clearSearch">
        <svg
          width="11"
          height="11"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2.5"
        >
          <path d="M18 6L6 18M6 6l12 12" />
        </svg>
      </button>
    </div>

    <!-- 结果下拉 -->
    <Transition name="sb-drop">
      <div v-if="showResults" class="sb-results">
        <!-- 空状态 -->
        <div v-if="searchResults.length === 0" class="sb-empty">
          <svg
            width="22"
            height="22"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="1.5"
          >
            <circle cx="11" cy="11" r="8" />
            <path d="M21 21l-4.35-4.35" />
          </svg>
          <span>{{ t('topBar.noMatchingMenu') }}</span>
        </div>

        <!-- 结果列表 -->
        <button
          v-for="result in searchResults"
          :key="result.menu.id"
          class="sb-item"
          @mousedown.prevent="selectResult(result)"
        >
          <span class="sb-item-dot" />
          <span class="sb-item-body">
            <span class="sb-item-name">
              <template
                v-for="(segment, segmentIndex) in highlightSegments(
                  displayName(result),
                  firstToken(searchText),
                )"
                :key="segmentIndex"
              >
                <mark v-if="segment.match">{{ segment.text }}</mark>
                <template v-else>{{ segment.text }}</template>
              </template>
            </span>
            <span class="sb-item-path">{{ result.path }}</span>
          </span>
          <svg
            class="sb-item-arrow"
            width="12"
            height="12"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
          >
            <path d="M9 18l6-6-6-6" />
          </svg>
        </button>
      </div>
      </Transition>
    </div>
  </template>
</template>

<style lang="scss" scoped>
/* ── 弹窗搜索（icon 模式） ── */
.ms-dialog {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.ms-input-box {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 9px 12px;
  border: 1px solid var(--el-border-color);
  border-radius: var(--radius-sm, 8px);
  background: var(--el-fill-color-light);
  transition: border-color 0.2s;

  &:focus-within {
    border-color: var(--el-color-primary);
  }
}

.ms-icon {
  color: var(--el-text-color-secondary);
  flex-shrink: 0;
}

.ms-input {
  flex: 1;
  min-width: 0;
  border: 0;
  outline: 0;
  background: transparent;
  color: var(--el-text-color-primary);
  font-family: inherit;
  font-size: 14px;

  &::placeholder {
    color: var(--el-text-color-secondary);
  }
}

.ms-clear {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;
  padding: 0;
  border: 0;
  border-radius: 50%;
  background: transparent;
  color: var(--el-text-color-secondary);
  cursor: pointer;

  &:hover {
    background: var(--el-fill-color);
    color: var(--el-text-color-primary);
  }
}

.ms-list {
  max-height: 380px;
  overflow-y: auto;
  scrollbar-width: thin;
  scrollbar-color: var(--el-border-color) transparent;
}

.ms-empty {
  padding: 28px 12px;
  text-align: center;
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.ms-item {
  display: flex;
  flex-direction: column;
  gap: 2px;
  width: 100%;
  padding: 9px 12px;
  border: 0;
  border-radius: var(--radius-sm, 8px);
  background: transparent;
  text-align: left;
  cursor: pointer;
  transition: background 0.12s;

  &.is-active {
    background: var(--el-color-primary-light-9);
  }
}

.ms-item-name {
  font-size: 13px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;

  mark {
    padding: 0;
    background: var(--el-color-warning-light-7);
    color: inherit;
  }
}

.ms-item-path {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* ── 外层容器 ── */
.sb-wrap {
  position: relative;
  width: 100%;
  min-width: 0;
}

/* ── 输入框 ── */
.sb-box {
  display: flex;
  align-items: center;
  gap: 8px;
  background: var(--el-fill-color-light);
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--radius-sm, 8px);
  padding: 7px 10px;
  width: 100%;
  box-sizing: border-box;
  transition:
    border-color 0.2s,
    background 0.2s;

  &.focused,
  &:focus-within {
    border-color: var(--el-color-primary);
  }
}

.sb-icon {
  color: var(--el-text-color-secondary);
  flex-shrink: 0;
}

.sb-input {
  background: none;
  border: none;
  outline: none;
  color: var(--el-text-color-primary);
  font-family: inherit;
  font-size: 13px;
  width: 100%;
  min-width: 0;

  &::placeholder {
    color: var(--el-text-color-secondary);
  }
}

.sb-clear {
  background: none;
  border: none;
  padding: 2px;
  cursor: pointer;
  color: var(--el-text-color-secondary);
  display: flex;
  align-items: center;
  flex-shrink: 0;
  border-radius: 4px;
  transition:
    color 0.15s,
    background 0.15s;

  &:hover {
    color: var(--el-text-color-primary);
    background: var(--el-fill-color-light);
  }
}

/* ── 结果面板 ── */
.sb-results {
  position: absolute;
  top: calc(100% + 6px);
  left: 0;
  right: 0;
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--radius-md, 10px);
  box-shadow: var(--el-box-shadow);
  z-index: 600;
  overflow: hidden;
  max-height: 300px;
  overflow-y: auto;
  scrollbar-width: thin;
  scrollbar-color: var(--el-border-color-light) transparent;
}

/* 空状态 */
.sb-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 28px 16px;
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

/* 每条结果 */
.sb-item {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 9px 14px;
  background: none;
  border: none;
  border-bottom: 1px solid var(--el-border-color-light);
  cursor: pointer;
  text-align: left;
  transition: background 0.1s;

  &:last-child {
    border-bottom: none;
  }

  &:hover {
    background: var(--el-color-primary-light-9);

    .sb-item-dot {
      background: var(--el-color-primary);
      transform: scale(1.2);
    }

    .sb-item-name {
      color: var(--el-text-color-primary);
    }

    .sb-item-arrow {
      color: var(--el-color-primary);
      transform: translateX(2px);
    }
  }
}

.sb-item-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--el-border-color);
  flex-shrink: 0;
  transition:
    background 0.15s,
    transform 0.15s;
}

.sb-item-body {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 1px;
  min-width: 0;
}

.sb-item-name {
  font-size: 13px;
  font-weight: 500;
  color: var(--cube-layout-menu-item-color);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  transition: color 0.1s;

  :deep(mark) {
    background: transparent;
    color: var(--el-color-primary);
    font-weight: 700;
  }
}

.sb-item-path {
  font-size: 11px;
  color: var(--el-text-color-secondary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.sb-item-arrow {
  flex-shrink: 0;
  color: var(--el-text-color-secondary);
  transition:
    color 0.15s,
    transform 0.15s;
}

/* ── 动画 ── */
.sb-drop-enter-active,
.sb-drop-leave-active {
  transition:
    opacity 0.15s,
    transform 0.15s;
}

.sb-drop-enter-from,
.sb-drop-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}
</style>

<style>
/* 弹窗无标题，隐藏 Element Plus 的空表头，避免顶部多出一段空白 */
.menu-search-dialog .el-dialog__header {
  display: none;
}
</style>
