<script setup lang="ts">
import { useTheme, THEME_OPTIONS, type ThemeFamily } from '../composables/useTheme';
import SwitcherDropdown from './SwitcherDropdown.vue';

interface Props {
  /** 自定义选择回调，不传则使用默认切换逻辑 */
  onChange?: (themeFamily: ThemeFamily) => void;
}

const props = withDefaults(defineProps<Props>(), {});

const { currentTheme, switchTheme } = useTheme();

const handleSelect = (id: string) => {
  const family = id as ThemeFamily;
  if (props.onChange) {
    props.onChange(family);
  } else {
    switchTheme(family);
  }
};
</script>

<template>
  <SwitcherDropdown
    :model-value="currentTheme.family"
    :options="THEME_OPTIONS"
    title="切换主题"
    @update:model-value="handleSelect"
  >
    <!-- 调色盘图标 -->
    <template #icon>
      <slot name="icon">
        <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <!-- 调色板主体 - 半月形 -->
          <path d="M12 2C6.5 2 2 6.5 2 12c0 3.5 1.8 6.6 4.5 8.4" />
          <circle cx="12" cy="12" r="10" />
          <!-- 颜色斑点 -->
          <circle cx="8" cy="8" r="1.5" fill="currentColor" stroke="none" />
          <circle cx="14" cy="6" r="1.5" fill="currentColor" stroke="none" />
          <circle cx="17" cy="10" r="1.5" fill="currentColor" stroke="none" />
          <circle cx="15" cy="15" r="1.5" fill="currentColor" stroke="none" />
          <circle cx="10" cy="16" r="1.5" fill="currentColor" stroke="none" />
        </svg>
      </slot>
    </template>
    <!-- 透传 option 插槽 -->
    <template v-if="$slots['option']" #option="slotProps">
      <slot name="option" v-bind="slotProps" />
    </template>
  </SwitcherDropdown>
</template>
