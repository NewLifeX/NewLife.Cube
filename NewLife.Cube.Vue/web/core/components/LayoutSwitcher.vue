<script setup lang="ts">
import { useLayout } from '../composables/useLayout';
import SwitcherDropdown from './SwitcherDropdown.vue';

interface Props {
  /** 自定义选择回调，不传则使用默认切换逻辑 */
  onChange?: (layoutId: string) => void;
}

const props = withDefaults(defineProps<Props>(), {});

const { layouts, currentLayoutId, setLayout } = useLayout();

const handleSelect = (layoutId: string) => {
  if (props.onChange) {
    props.onChange(layoutId);
  } else {
    setLayout(layoutId);
  }
};
</script>

<template>
  <!-- 只有注册了多个布局时才显示 -->
  <SwitcherDropdown
    v-if="layouts.length > 1"
    v-model="currentLayoutId"
    :options="layouts"
    title="切换布局"
    @update:model-value="handleSelect"
  >
    <!-- 默认图标：三栏布局 -->
    <template #icon>
      <slot name="icon">
        <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2">
          <rect x="3" y="3" width="18" height="4" rx="1" />
          <rect x="3" y="10" width="7" height="11" rx="1" />
          <rect x="13" y="10" width="8" height="11" rx="1" />
        </svg>
      </slot>
    </template>
    <!-- 透传 option 插槽 -->
    <template v-if="$slots['option']" #option="slotProps">
      <slot name="option" v-bind="slotProps" />
    </template>
  </SwitcherDropdown>
</template>
