<script setup lang="ts">
const emit = defineEmits<{
  new: [];
  delete: [];
  export: [];
  refresh: [];
}>();
</script>

<template>
  <div class="list-toolbar">
    <div class="lt-left">
      <el-button type="primary" class="lt-btn lt-btn--primary" @click="emit('new')">
        新建
      </el-button>
      <el-button class="lt-btn lt-btn--secondary" @click="emit('delete')"> 批量删除 </el-button>
      <el-button class="lt-btn lt-btn--secondary" @click="emit('export')">导出</el-button>
    </div>
    <div class="lt-right">
      <span class="lt-meta">批量操作与导出保持独立，不进入页头区。</span>
      <el-button class="lt-btn lt-btn--refresh" @click="emit('refresh')">刷新</el-button>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.list-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  background: var(--el-bg-color-overlay);
  padding: 12px 16px;
  border-radius: var(--el-border-radius-base);
  box-shadow: var(--el-box-shadow-light);
  border: 1px solid var(--el-border-color-light);
}

.lt-left {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.lt-right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.lt-meta {
  font-family: 'Fira Sans', system-ui, sans-serif;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  white-space: nowrap;
}

.lt-btn {
  min-height: 34px;
  padding: 0 14px;
  border-radius: 7px;
  font-family: 'Fira Sans', system-ui, sans-serif;
  font-size: 13px;
  font-weight: 500;

  &--primary {
    --el-button-bg-color: var(--el-color-primary);
    --el-button-border-color: var(--el-color-primary);
    --el-button-hover-bg-color: var(--accent-hover);
    --el-button-hover-border-color: var(--accent-hover);
    --el-button-active-bg-color: var(--accent-hover);
    --el-button-active-border-color: var(--accent-hover);
  }

  &--secondary {
    --el-button-bg-color: var(--el-fill-color-blank);
    --el-button-border-color: var(--el-border-color-light);
    --el-button-text-color: var(--el-text-color-regular);
    --el-button-hover-bg-color: var(--el-color-primary-light-9);
    --el-button-hover-border-color: var(--el-color-primary-light-8);
    --el-button-hover-text-color: var(--el-color-primary);
    --el-button-active-bg-color: var(--el-color-primary-light-8);
    --el-button-active-border-color: var(--el-color-primary-light-8);
    --el-button-active-text-color: var(--el-color-primary);
    margin-left: 0;
  }

  &--refresh {
    --el-button-bg-color: var(--el-fill-color-blank);
    --el-button-border-color: var(--el-border-color-light);
    --el-button-text-color: var(--el-text-color-regular);
    --el-button-hover-bg-color: var(--el-color-primary-light-9);
    --el-button-hover-border-color: var(--el-color-primary-light-8);
    --el-button-hover-text-color: var(--el-color-primary);
    --el-button-active-bg-color: var(--el-color-primary-light-8);
    --el-button-active-border-color: var(--el-color-primary-light-8);
    --el-button-active-text-color: var(--el-color-primary);
    margin-left: 0;
  }
}

@media (max-width: 768px) {
  .list-toolbar {
    flex-direction: column;
    align-items: stretch;
  }

  .lt-right {
    justify-content: space-between;
    flex-wrap: wrap;
  }

  .lt-meta {
    white-space: normal;
  }
}
</style>
