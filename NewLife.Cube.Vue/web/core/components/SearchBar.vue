<script setup lang="ts">
/**
 * SearchBar - 通用搜索组件
 *
 * icon 模式 — 仅图标按钮（ActionButton）
 * box  模式 — 搜索框 + 菜单模糊搜索结果下拉
 */
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { storeToRefs } from 'pinia';
import { useMenuStore, type FlatMenuItem } from 'cube-front/core/stores/menu';
import { openMenuTab } from 'cube-front/core/utils/menuTab';
import ActionButton from './ActionButton.vue';

interface Props {
  mode?: 'icon' | 'box';
  placeholder?: string;
}

const { mode = 'icon', placeholder = '搜索菜单...' } = defineProps<Props>();

const emit = defineEmits<{ search: [value: string] }>();

const menuStore = useMenuStore();
const { flatMenus } = storeToRefs(menuStore);

const searchText = ref('');
const showResults = ref(false);
const containerRef = ref<HTMLElement | null>(null);

/** 过滤：按名称/标题模糊匹配，搜索全部菜单（含无路径的父级分类） */
const searchResults = computed(() => {
  const kw = searchText.value.trim().toLowerCase();
  if (!kw || !flatMenus.value?.length) return [];
  return flatMenus.value
    .filter((m) => {
      const name = (m.name || '').toLowerCase();
      const title = (m.title || '').toLowerCase();
      return name.includes(kw) || title.includes(kw);
    })
    .slice(0, 10);
});

/** 菜单显示名：优先 title(displayName)，其次 name */
function getDisplayName(menu: FlatMenuItem): string {
  return menu.title || menu.name;
}

/** 面包屑：父级名 › 当前名 */
function getBreadcrumb(menu: FlatMenuItem): string {
  if (!flatMenus.value || !menu.parentId) return getDisplayName(menu);
  const parent = flatMenus.value.find((m) => m.id === menu.parentId);
  return parent ? `${getDisplayName(parent)} › ${getDisplayName(menu)}` : getDisplayName(menu);
}

/** 高亮匹配字符 */
function highlight(text: string, kw: string): string {
  if (!kw) return text;
  const i = text.toLowerCase().indexOf(kw.toLowerCase());
  if (i === -1) return text;
  return (
    text.slice(0, i) + `<mark>${text.slice(i, i + kw.length)}</mark>` + text.slice(i + kw.length)
  );
}

function handleInput(e: Event) {
  const val = (e.target as HTMLInputElement).value;
  searchText.value = val;
  showResults.value = val.trim().length > 0;
  emit('search', val);
}

function clearSearch() {
  searchText.value = '';
  showResults.value = false;
}

function selectResult(menu: FlatMenuItem) {
  if (menu.path) {
    openMenuTab({ url: menu.path, title: menu.name });
    menuStore.setActiveMenu(menu as any);
  }
  clearSearch();
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
  <!-- 图标模式 -->
  <ActionButton v-if="mode === 'icon'" title="全局搜索">
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
        :placeholder="placeholder"
        :value="searchText"
        @input="handleInput"
        @focus="showResults = searchText.trim().length > 0"
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
          <span>未找到匹配菜单</span>
        </div>

        <!-- 结果列表 -->
        <button
          v-for="menu in searchResults"
          :key="menu.id"
          class="sb-item"
          @mousedown.prevent="selectResult(menu)"
        >
          <span class="sb-item-dot" />
          <span class="sb-item-body">
            <span class="sb-item-name" v-html="highlight(getDisplayName(menu), searchText)" />
            <span class="sb-item-path">{{ getBreadcrumb(menu) }}</span>
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

<style lang="scss" scoped>
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
  background: var(--bg-tertiary, rgba(0, 0, 0, 0.04));
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-sm, 8px);
  padding: 7px 10px;
  width: 100%;
  box-sizing: border-box;
  transition:
    border-color 0.2s,
    background 0.2s;

  &.focused,
  &:focus-within {
    border-color: var(--accent);
  }
}

.sb-icon {
  color: var(--text-muted);
  flex-shrink: 0;
}

.sb-input {
  background: none;
  border: none;
  outline: none;
  color: var(--text-primary);
  font-family: inherit;
  font-size: 13px;
  width: 100%;
  min-width: 0;

  &::placeholder {
    color: var(--text-muted);
  }
}

.sb-clear {
  background: none;
  border: none;
  padding: 2px;
  cursor: pointer;
  color: var(--text-muted);
  display: flex;
  align-items: center;
  flex-shrink: 0;
  border-radius: 4px;
  transition:
    color 0.15s,
    background 0.15s;

  &:hover {
    color: var(--text-primary);
    background: var(--bg-tertiary);
  }
}

/* ── 结果面板 ── */
.sb-results {
  position: absolute;
  top: calc(100% + 6px);
  left: 0;
  right: 0;
  background: var(--bg-elevated);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md, 10px);
  box-shadow: var(--shadow-lg, 0 10px 40px rgba(0, 0, 0, 0.18));
  z-index: 600;
  overflow: hidden;
  max-height: 300px;
  overflow-y: auto;
  scrollbar-width: thin;
  scrollbar-color: var(--border-subtle) transparent;
}

/* 空状态 */
.sb-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 28px 16px;
  color: var(--text-muted);
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
  border-bottom: 1px solid var(--border-subtle);
  cursor: pointer;
  text-align: left;
  transition: background 0.1s;

  &:last-child {
    border-bottom: none;
  }

  &:hover {
    background: var(--accent-muted);

    .sb-item-dot {
      background: var(--accent);
      transform: scale(1.2);
    }

    .sb-item-name {
      color: var(--text-primary);
    }

    .sb-item-arrow {
      color: var(--accent);
      transform: translateX(2px);
    }
  }
}

.sb-item-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--border-default, #ccc);
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
  color: var(--text-secondary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  transition: color 0.1s;

  :deep(mark) {
    background: transparent;
    color: var(--accent);
    font-weight: 700;
  }
}

.sb-item-path {
  font-size: 11px;
  color: var(--text-muted);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.sb-item-arrow {
  flex-shrink: 0;
  color: var(--text-muted);
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
