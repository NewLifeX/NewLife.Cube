<script setup lang="ts" generic="T extends { id: string; label: string; icon: string }">
import { ref, onMounted, onUnmounted } from 'vue';

/**
 * SwitcherDropdown — 通用图标+下拉切换组件
 *
 * 用途：主题切换、布局切换，或任何需要"小图标 → 下拉选单"形式的场景。
 *
 * Props:
 *   options     — 选项列表，每项需要 { id, label, icon }
 *   modelValue  — 当前选中项的 id（支持 v-model）
 *   title       — 按钮 tooltip 文字（可选）
 *
 * Slots:
 *   #icon          — 自定义触发按钮内的图标（不覆盖则使用默认 SVG）
 *   #option        — 自定义下拉选项内容（slot props: { option, active }）
 *
 * Emits:
 *   update:modelValue — 切换时触发，携带新 id
 */

const props = withDefaults(
  defineProps<{
    options: T[];
    modelValue: string;
    title?: string;
  }>(),
  { title: '' },
);

const emit = defineEmits<{
  'update:modelValue': [id: string];
}>();

const dropRef = ref<HTMLElement | null>(null);
const open = ref(false);

function select(id: string) {
  emit('update:modelValue', id);
  open.value = false;
}

function toggle() {
  open.value = !open.value;
}

function close() {
  open.value = false;
}

// 点击外部关闭
function handleClickOutside(e: MouseEvent) {
  if (dropRef.value && !dropRef.value.contains(e.target as Node)) {
    open.value = false;
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside);
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
});

// 暴露给父组件
defineExpose({ close });
</script>

<template>
  <!-- ref 用于检测外部点击 -->
  <div ref="dropRef" class="sw-drop">
    <!-- 触发按钮 -->
    <button class="tn-btn sw-trigger" :title="title" @click="toggle">
      <span class="sw-icon">
        <slot name="icon">
          <!-- 默认：调色板图标 -->
          <svg
            width="15"
            height="15"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
          >
            <circle cx="12" cy="12" r="10" />
            <circle cx="12" cy="12" r="4" />
          </svg>
        </slot>
      </span>
    </button>

    <!-- 下拉菜单 -->
    <Transition name="sw-fade">
      <div v-if="open" class="sw-menu">
        <div
          v-for="opt in options"
          :key="opt.id"
          class="sw-item"
          :class="{ active: modelValue === opt.id }"
          @click="select(opt.id)"
        >
          <!-- 支持通过 #option 插槽自定义每行内容 -->
          <slot name="option" :option="opt" :active="modelValue === opt.id">
            <span class="sw-i-icon">{{ opt.icon }}</span>
            <span class="sw-i-label">{{ opt.label }}</span>
            <svg
              v-if="modelValue === opt.id"
              class="sw-i-check"
              width="13"
              height="13"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2.5"
            >
              <polyline points="20 6 9 17 4 12" />
            </svg>
          </slot>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style lang="scss" scoped>
.sw-drop {
  position: relative;
}

/* 触发按钮 - 默认透明无边框，悬浮时显示背景 */
.sw-trigger {
  min-width: 34px;
  height: 34px;
  padding: 0 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: transparent;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  color: var(--navbar-text, var(--cube-layout-menu-item-color));
  transition:
    background 0.15s,
    color 0.15s;
  font-family: inherit;
  flex-shrink: 0;
  white-space: nowrap;

  &:hover {
    background: var(--navbar-hover-bg, var(--el-fill-color-light));
    color: var(--navbar-text-hover, var(--el-text-color-primary));
  }
}

.sw-icon {
  display: flex;
  align-items: center;
  justify-content: center;

  :deep(svg) {
    width: 14px;
    height: 14px;
  }
}

/* 下拉菜单面板 */
.sw-menu {
  position: absolute;
  right: 0;
  top: calc(100% + 8px);
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--el-border-radius-base);
  box-shadow: var(--el-box-shadow);
  min-width: 180px;
  overflow: hidden;
  z-index: 300;
  padding: 4px;
}

/* 选项行 */
.sw-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  border-radius: 7px;
  cursor: pointer;
  color: var(--cube-layout-menu-item-color);
  font-size: 13px;
  font-weight: 500;
  transition:
    background 0.12s,
    color 0.12s;

  &:hover {
    background: var(--el-color-primary-light-9);
    color: var(--el-text-color-primary);
  }

  &.active {
    color: var(--el-color-primary);
    font-weight: 600;
  }
}

.sw-i-icon {
  font-size: 14px;
  flex-shrink: 0;
}

.sw-i-label {
  flex: 1;
}

.sw-i-check {
  color: var(--el-color-primary);
  flex-shrink: 0;
}

/* 下拉动画 */
.sw-fade-enter-active,
.sw-fade-leave-active {
  transition:
    opacity 0.12s,
    transform 0.12s;
}

.sw-fade-enter-from,
.sw-fade-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>
