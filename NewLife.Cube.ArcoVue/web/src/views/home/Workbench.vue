<template>
  <div class="workbench">
    <div class="wb-banner">
      <div class="wb-hello">
        <div class="wb-hello-title">{{ hello }}</div>
        <div class="wb-hello-date">{{ todayLabel }}</div>
      </div>
      <a-space :size="4">
        <a-tooltip :content="editing ? '完成' : '自定义工作台'">
          <a-button type="text" class="wb-icon-btn" @click="toggleEdit">
            <icon-park :type="editing ? 'check' : 'setting-config'" :size="16" />
          </a-button>
        </a-tooltip>
        <a-tooltip content="恢复默认">
          <!-- span：disabled 时按钮不接收指针事件，保证 tooltip 仍可显示 -->
          <span class="wb-icon-wrap">
            <a-button
              type="text"
              class="wb-icon-btn"
              :disabled="!canRestore"
              @click="restoreDefault"
            >
              <icon-park type="undo" :size="16" />
            </a-button>
          </span>
        </a-tooltip>
      </a-space>
    </div>
    <a-alert v-if="loadError" type="warning" show-icon class="wb-alert">{{ loadError }}</a-alert>
    <a-spin :loading="loading" class="wb-spin">
      <WidgetHost />
    </a-spin>
  </div>
</template>

<script setup lang="ts">
import WidgetHost from '@/features/widget/WidgetHost.vue';
import { useWorkbench } from './useWorkbench';

defineOptions({ name: 'Workbench' });

const { loading, editing, loadError, hello, todayLabel, canRestore, toggleEdit, restoreDefault } = useWorkbench();
</script>

<style scoped>
.workbench {
  display: flex;
  flex-direction: column;
  gap: 16px;
  min-width: 0;
}
.wb-banner {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 16px;
  flex-wrap: wrap;
}
.wb-hello-title {
  font-size: 22px;
  font-weight: 600;
  color: var(--color-text-1);
  line-height: 1.3;
}
.wb-hello-date {
  margin-top: 4px;
  font-size: 13px;
  color: var(--color-text-3);
}
.wb-icon-wrap {
  display: inline-flex;
}
.wb-icon-btn {
  width: 28px !important;
  height: 28px !important;
  padding: 0 !important;
  color: var(--color-text-2) !important;
}
.wb-icon-btn:hover:not(:disabled) {
  color: rgb(var(--primary-6)) !important;
}
.wb-icon-btn:disabled {
  color: var(--color-text-4) !important;
}
.wb-alert {
  margin: 0;
}
.wb-spin {
  width: 100%;
  min-height: 120px;
}
</style>
