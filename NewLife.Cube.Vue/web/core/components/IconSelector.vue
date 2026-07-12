<script setup lang="ts">
/**
 * 图标选择器（基于 Element Plus 图标集，支持搜索）
 *
 * v-model 绑定「图标名字符串」（resolveControl 映射的 control = 'icon'）。
 * 列表图标预览由 ListTableContent 直接渲染，本组件用于表单编辑。
 */
import { ref, computed } from 'vue';
import * as ElementPlusIconsVue from '@element-plus/icons-vue';

const props = withDefaults(
  defineProps<{
    modelValue?: string;
    disabled?: boolean;
    placeholder?: string;
  }>(),
  { modelValue: '', disabled: false, placeholder: '选择图标' },
);

const emit = defineEmits<{ (e: 'update:modelValue', value: string): void }>();

const visible = ref(false);
const keyword = ref('');

const iconNames = Object.keys(ElementPlusIconsVue);

const filteredIcons = computed(() => {
  const kw = keyword.value.trim().toLowerCase();
  if (!kw) return iconNames;
  return iconNames.filter((name) => name.toLowerCase().includes(kw));
});

const currentIcon = computed(() =>
  props.modelValue ? (ElementPlusIconsVue as Record<string, unknown>)[props.modelValue] : null,
);

function selectIcon(name: string) {
  emit('update:modelValue', name);
  visible.value = false;
  keyword.value = '';
}
</script>

<template>
  <div class="icon-selector">
    <el-popover
      v-model:visible="visible"
      placement="bottom-start"
      :width="320"
      trigger="click"
      :disabled="disabled"
    >
      <template #reference>
        <el-button :disabled="disabled" class="icon-selector__trigger">
          <el-icon v-if="currentIcon"><component :is="currentIcon" /></el-icon>
          <span class="icon-selector__label">{{ modelValue || placeholder }}</span>
        </el-button>
      </template>

      <div class="icon-selector__panel">
        <el-input
          v-model="keyword"
          placeholder="搜索图标"
          clearable
          size="small"
          class="icon-selector__search"
        />
        <div class="icon-selector__grid">
          <button
            v-for="name in filteredIcons"
            :key="name"
            type="button"
            class="icon-selector__cell"
            :class="{ 'is-active': name === modelValue }"
            :title="name"
            @click="selectIcon(name)"
          >
            <el-icon><component :is="(ElementPlusIconsVue as Record<string, unknown>)[name]" /></el-icon>
          </button>
          <p v-if="filteredIcons.length === 0" class="icon-selector__empty">无匹配图标</p>
        </div>
      </div>
    </el-popover>
  </div>
</template>

<style scoped>
.icon-selector {
  width: 100%;
}

.icon-selector__trigger {
  width: 100%;
  justify-content: flex-start;
}

.icon-selector__label {
  margin-left: 6px;
  font-size: 13px;
}

.icon-selector__panel {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.icon-selector__grid {
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 6px;
  max-height: 240px;
  overflow-y: auto;
}

.icon-selector__cell {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 32px;
  border: 1px solid var(--el-border-color-light);
  border-radius: 4px;
  background: var(--el-bg-color);
  cursor: pointer;
  font-size: 16px;
  color: var(--el-text-color-regular);
  transition: all 0.15s ease;
}

.icon-selector__cell:hover {
  border-color: var(--el-color-primary);
  color: var(--el-color-primary);
}

.icon-selector__cell.is-active {
  border-color: var(--el-color-primary);
  background: var(--el-color-primary-light-9);
  color: var(--el-color-primary);
}

.icon-selector__empty {
  grid-column: 1 / -1;
  margin: 8px 0;
  text-align: center;
  font-size: 12px;
  color: var(--el-text-color-placeholder);
}
</style>
