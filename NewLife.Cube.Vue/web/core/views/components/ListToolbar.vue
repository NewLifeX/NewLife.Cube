<script setup lang="ts">
import { ref } from 'vue';
import { Grid, List, Upload, DataLine } from '@element-plus/icons-vue';

const emit = defineEmits<{
  new: [];
  delete: [];
  export: [];
  import: [];
  chart: [];
  refresh: [];
  'view-change': [view: 'table' | 'card'];
}>();

const viewMode = ref<'table' | 'card'>('table');

function toggleView() {
  viewMode.value = viewMode.value === 'table' ? 'card' : 'table';
  emit('view-change', viewMode.value);
}
</script>

<template>
  <div class="list-toolbar">
    <div class="lt-left">
      <el-button type="primary" class="lt-btn lt-btn--primary" @click="emit('new')">
        <template #icon>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="16" height="16">
            <line x1="12" y1="5" x2="12" y2="19"></line>
            <line x1="5" y1="12" x2="19" y2="12"></line>
          </svg>
        </template>
        新建
      </el-button>
      <el-button class="lt-btn lt-btn--secondary" @click="emit('delete')">
        <template #icon>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="16" height="16">
            <polyline points="3 6 5 6 21 6"></polyline>
            <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
          </svg>
        </template>
        批量删除
      </el-button>
      <el-button class="lt-btn lt-btn--secondary" @click="emit('export')">
        <template #icon>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="16" height="16">
            <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
            <polyline points="7 10 12 15 17 10"></polyline>
            <line x1="12" y1="15" x2="12" y2="3"></line>
          </svg>
        </template>
        导出
      </el-button>
      <el-button class="lt-btn lt-btn--secondary" @click="emit('import')">
        <template #icon>
          <Upload />
        </template>
        导入
      </el-button>
      <el-button class="lt-btn lt-btn--secondary" @click="emit('chart')">
        <template #icon>
          <DataLine />
        </template>
        图表
      </el-button>
    </div>
    <div class="lt-right">
      <div class="lt-view-switch">
        <button
          class="lt-view-btn"
          :class="{ active: viewMode === 'table' }"
          @click="toggleView"
          title="表格视图"
        >
          <Grid />
        </button>
        <button
          class="lt-view-btn"
          :class="{ active: viewMode === 'card' }"
          @click="toggleView"
          title="卡片视图"
        >
          <List />
        </button>
      </div>
      <el-button class="lt-btn lt-btn--refresh" @click="emit('refresh')">
        <template #icon>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="16" height="16">
            <polyline points="23 4 23 10 17 10"></polyline>
            <polyline points="1 20 1 14 7 14"></polyline>
            <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
          </svg>
        </template>
        刷新
      </el-button>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.list-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.lt-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.lt-right {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-shrink: 0;
}

.lt-view-switch {
  display: flex;
  align-items: center;
  background: var(--el-fill-color-light);
  border-radius: var(--el-border-radius-base);
  border: 1px solid var(--el-border-color-light);
  overflow: hidden;
}

.lt-view-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 32px;
  background: transparent;
  border: none;
  color: var(--el-text-color-secondary);
  cursor: pointer;
  transition: all 0.15s ease;

  &:hover {
    background: var(--el-color-primary-light-9);
    color: var(--el-color-primary);
  }

  &.active {
    background: var(--el-color-primary);
    color: var(--el-color-white);
  }

  svg {
    width: 16px;
    height: 16px;
  }
}

.lt-btn {
  min-height: 36px;
  padding: 0 16px;
  border-radius: var(--el-border-radius-base);
  font-family: var(--el-font-family);
  font-size: 13px;
  font-weight: 500;
  transition: all 0.15s ease;

  &--primary {
    --el-button-bg-color: var(--el-color-primary);
    --el-button-border-color: var(--el-color-primary);
    --el-button-hover-bg-color: var(--el-color-primary-dark-2);
    --el-button-hover-border-color: var(--el-color-primary-dark-2);
    --el-button-active-bg-color: var(--el-color-primary-dark-2);
    --el-button-active-border-color: var(--el-color-primary-dark-2);
  }

  &--secondary {
    --el-button-bg-color: transparent;
    --el-button-border-color: var(--el-border-color-light);
    --el-button-text-color: var(--el-text-color-regular);
    --el-button-hover-bg-color: var(--el-color-primary-light-9);
    --el-button-hover-border-color: var(--el-color-primary);
    --el-button-hover-text-color: var(--el-color-primary);
    --el-button-active-bg-color: var(--el-color-primary-light-8);
    --el-button-active-border-color: var(--el-color-primary);
    --el-button-active-text-color: var(--el-color-primary);
    margin-left: 0;
  }

  &--refresh {
    --el-button-bg-color: transparent;
    --el-button-border-color: var(--el-border-color-lighter);
    --el-button-text-color: var(--el-text-color-secondary);
    --el-button-hover-bg-color: var(--el-bg-color);
    --el-button-hover-border-color: var(--el-border-color);
    --el-button-hover-text-color: var(--el-text-color-regular);
    --el-button-active-bg-color: var(--el-fill-color);
    --el-button-active-border-color: var(--el-border-color);
    --el-button-active-text-color: var(--el-text-color-regular);
    margin-left: 0;
  }
}

@media (max-width: 768px) {
  .list-toolbar {
    flex-direction: column;
    align-items: stretch;
  }

  .lt-right {
    justify-content: flex-end;
    flex-wrap: wrap;
  }

  .lt-view-switch {
    order: 1;
  }

  .lt-btn--refresh {
    order: 2;
  }
}
</style>
