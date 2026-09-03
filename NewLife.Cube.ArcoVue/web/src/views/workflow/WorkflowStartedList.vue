<script setup lang="ts">
/**
 * 我发起的审批页（OSC-26090347f1 T8c）：菜单 URL /Cube/Workflow/Started。薄 .vue，逻辑见 useWorkflowStartedList。
 */
import type { WorkflowInstanceItem } from '@cube/api-core';
import { formatDateTime } from '@/core/utils/datetime';
import { instanceStatusMeta } from './useWorkflowProgress';
import { useWorkflowStartedList } from './useWorkflowStartedList';
import WorkflowProgressPanel from './WorkflowProgressPanel.vue';

const {
  rows,
  loading,
  progressInstanceId,
  progressVisible,
  refresh,
  openProgress,
} = useWorkflowStartedList();

function titleOf(row: WorkflowInstanceItem): string {
  return row.title || `${row.typePath} #${row.id}`;
}
</script>

<template>
  <div class="wf-started-page">
    <div class="wf-started-page__head">
      <h3 class="wf-started-page__title">我发起的</h3>
      <a-button size="small" @click="refresh">刷新</a-button>
    </div>

    <a-spin :loading="loading">
      <a-table :data="rows" :row-key="(r: WorkflowInstanceItem) => r.id" :pagination="false" :bordered="false" size="small">
        <template #columns>
          <a-table-column title="标题" data-index="title" :width="240" ellipsis>
            <template #cell="{ record }">
              <a-typography-text>{{ titleOf(record) }}</a-typography-text>
            </template>
          </a-table-column>
          <a-table-column title="实体" data-index="typePath" :width="160" ellipsis>
            <template #cell="{ record }">
              <a-typography-text type="secondary" style="font-size: 12px">{{ record.typePath }}</a-typography-text>
            </template>
          </a-table-column>
          <a-table-column title="状态" data-index="status" :width="100">
            <template #cell="{ record }">
              <a-tag :color="instanceStatusMeta(record.status).color" size="small">
                {{ instanceStatusMeta(record.status).text }}
              </a-tag>
            </template>
          </a-table-column>
          <a-table-column title="发起时间" data-index="createTime" :width="170">
            <template #cell="{ record }">
              <a-typography-text type="secondary" style="font-size: 12px">
                {{ formatDateTime(record.createTime) }}
              </a-typography-text>
            </template>
          </a-table-column>
          <a-table-column title="完成时间" data-index="finishTime" :width="170">
            <template #cell="{ record }">
              <a-typography-text v-if="record.finishTime" type="secondary" style="font-size: 12px">
                {{ formatDateTime(record.finishTime) }}
              </a-typography-text>
              <a-typography-text v-else type="secondary" style="font-size: 12px">—</a-typography-text>
            </template>
          </a-table-column>
          <a-table-column title="操作" :width="90">
            <template #cell="{ record }">
              <a-button type="text" size="mini" @click="openProgress(record)">进度</a-button>
            </template>
          </a-table-column>
        </template>
        <template #empty>
          <a-empty description="暂无发起的流程" />
        </template>
      </a-table>
    </a-spin>

    <WorkflowProgressPanel v-model="progressVisible" :instance-id="progressInstanceId" />
  </div>
</template>

<style scoped>
.wf-started-page {
  padding: 16px;
}
.wf-started-page__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}
.wf-started-page__title {
  margin: 0;
  font-size: 16px;
}
</style>
