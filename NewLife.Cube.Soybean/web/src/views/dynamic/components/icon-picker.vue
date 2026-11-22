<script setup lang="ts">
import { computed, ref } from 'vue';
import type { Component } from 'vue';
import * as ElementPlusIcons from '@element-plus/icons-vue';
import { resolveIconName } from '@/utils/icon';

defineOptions({ name: 'IconPicker' });

interface Props {
  /** 图标值：Element Plus 图标名（User）、FontAwesome 类名（fa-user）或 iconify 名称（ep:user） */
  modelValue?: string;
}

const props = withDefaults(defineProps<Props>(), { modelValue: '' });

const emit = defineEmits<{
  'update:modelValue': [value: string];
}>();

const icons = ElementPlusIcons as unknown as Record<string, Component>;
const iconNames = Object.keys(icons)
  .filter(name => /^[A-Z]/.test(name))
  .sort((a, b) => a.localeCompare(b));

const visible = ref(false);
const keyword = ref('');

const filtered = computed(() => {
  const kw = keyword.value.trim().toLowerCase();
  return kw ? iconNames.filter(name => name.toLowerCase().includes(kw)) : iconNames;
});

const current = computed(() => (props.modelValue || '').trim());

/** 当前值为 Element Plus 图标名时直接用本地组件渲染，兼容旧数据走 Iconify */
const epComponent = computed<Component | null>(() => {
  const name = current.value;
  return name && /^[A-Z]/.test(name) && icons[name] ? icons[name] : null;
});

const previewIcon = computed(() => resolveIconName(current.value));

function handleSelect(name: string) {
  emit('update:modelValue', name);
  visible.value = false;
}

function handleClear() {
  emit('update:modelValue', '');
  visible.value = false;
}
</script>

<template>
  <ElPopover v-model:visible="visible" placement="bottom-start" trigger="click" :width="392" :show-arrow="false">
    <div class="icon-picker__panel">
      <ElInput v-model="keyword" size="small" clearable placeholder="搜索图标" />
      <div class="icon-picker__grid">
        <button
          v-for="name in filtered"
          :key="name"
          type="button"
          class="icon-picker__item"
          :class="{ 'is-active': current === name }"
          :title="name"
          @click="handleSelect(name)"
        >
          <ElIcon class="text-18px">
            <component :is="icons[name]" />
          </ElIcon>
        </button>
      </div>
      <ElEmpty v-if="!filtered.length" description="未找到图标" :image-size="60" />
      <div class="icon-picker__footer">
        <span class="icon-picker__name">{{ current || '未选择图标' }}</span>
        <ElButton size="small" text @click="handleClear">清除</ElButton>
      </div>
    </div>
    <template #reference>
      <button type="button" class="icon-picker__trigger" :title="current || '选择图标'">
        <component :is="epComponent" v-if="epComponent" />
        <SvgIcon v-else-if="previewIcon" :icon="previewIcon" class="text-16px" />
        <SvgIcon v-else icon="mdi:apps" class="text-16px text-[var(--el-text-color-placeholder)]" />
      </button>
    </template>
  </ElPopover>
</template>

<style scoped>
/* 触发按钮：32px 方块图标预览，点击打开图标面板 */
.icon-picker__trigger {
  display: flex;
  flex: none;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border: 1px solid var(--el-border-color);
  border-radius: 4px;
  color: var(--el-text-color-regular);
  background: var(--el-fill-color-blank);
  cursor: pointer;
  transition: border-color 0.15s;
}

.icon-picker__trigger:hover {
  border-color: var(--el-color-primary);
  color: var(--el-color-primary);
}

.icon-picker__panel {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.icon-picker__grid {
  display: grid;
  gap: 2px;
  grid-template-columns: repeat(9, 1fr);
  max-height: 300px;
  overflow: auto;
}

.icon-picker__item {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 36px;
  border: 1px solid transparent;
  border-radius: 4px;
  color: var(--el-text-color-regular);
  background: transparent;
  cursor: pointer;
}

.icon-picker__item:hover {
  color: var(--el-color-primary);
  background: var(--el-fill-color-light);
}

.icon-picker__item.is-active {
  border-color: var(--el-color-primary);
  color: var(--el-color-primary);
}

.icon-picker__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding-top: 4px;
  border-top: 1px solid var(--el-border-color-lighter);
}

.icon-picker__name {
  overflow: hidden;
  color: var(--el-text-color-secondary);
  font-size: 12px;
  white-space: nowrap;
  text-overflow: ellipsis;
}
</style>
