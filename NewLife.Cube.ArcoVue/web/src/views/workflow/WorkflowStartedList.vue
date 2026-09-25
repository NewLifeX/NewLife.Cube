<script setup lang="ts">
/**
 * 我发起的审批页（OSC-26090347f1 T8c）：菜单 URL /Cube/Workflow/Started。薄 .vue，逻辑见 useWorkflowStartedList。
 */
import { computed, ref } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { WorkflowInstanceItem } from '@newlifex/api-core';
import type { ViewGroup } from '@/core/utils/viewProfile';
import { formatDateTime } from '@/core/utils/datetime';
import { instanceStatusMeta } from './useWorkflowProgress';
import { entityLabel, summaryPlain } from './useWorkflowTaskList';
import { useWorkflowStartedList } from './useWorkflowStartedList';
import { groupWorkflowRows, type WorkflowGroupRow } from './workflowListGroup';
import WorkflowListBar from './WorkflowListBar.vue';
import WorkflowProgressPanel from './WorkflowProgressPanel.vue';
import './workflowChrome.css';

const STARTED_GROUP_FIELDS: FieldMeta[] = [
  { name: 'typeName', displayName: '业务对象', typeName: 'Enum' },
  {
    name: 'status',
    displayName: '状态',
    typeName: 'Enum',
    dataSource: {
      running: '审批中',
      Running: '审批中',
      approved: '已通过',
      Approved: '已通过',
      rejected: '已驳回',
      Rejected: '已驳回',
      withdrawn: '已撤回',
      Withdrawn: '已撤回',
      cancelled: '已作废',
      Cancelled: '已作废',
    },
  },
  { name: 'createTime', displayName: '发起时间', typeName: 'DateTime' },
];

const {
  rows,
  loading,
  progressInstanceId,
  progressVisible,
  refresh,
  openProgress,
  keyword,
  page,
  pageSize,
  total,
  search,
  changePage,
  changePageSize,
} = useWorkflowStartedList();

const group = ref<ViewGroup>([]);
const displayRows = computed(() =>
  groupWorkflowRows(rows.value as unknown as Record<string, unknown>[], group.value, STARTED_GROUP_FIELDS),
);

function spanMethod(data: { record: WorkflowGroupRow; columnIndex: number }) {
  if (!data.record.__group) return undefined;
  if (data.columnIndex === 0) return { rowspan: 1, colspan: 8 };
  return { rowspan: 0, colspan: 0 };
}

function isGroup(record: WorkflowGroupRow) {
  return record.__group === true;
}

function titleOf(row: WorkflowInstanceItem): string {
  return row.title || `${row.typePath} #${row.id}`;
}
</script>

<template>
  <div class="wf-started-page list-surface">
    <div class="list-panel list-panel--table">
      <!-- 工具栏与表格同处一个面板（多维视图） -->
      <WorkflowListBar
        v-model:q="keyword"
        :fields="STARTED_GROUP_FIELDS"
        :group="group"
        :page="page"
        :page-size="pageSize"
        :total="total"
        @search="search(keyword)"
        @refresh="refresh()"
        @update:group="group = $event"
        @page-change="changePage"
        @page-size-change="changePageSize"
      >
      <a-spin :loading="loading" style="width: 100%; display: block">
        <a-table
          :data="displayRows"
          :loading="loading"
          :row-key="(r: WorkflowGroupRow) => String(r.id)"
          :pagination="false"
          :bordered="false"
          :span-method="spanMethod"
          :scroll="{ x: 1200 }"
          size="small"
        >
        <template #columns>
          <a-table-column title="标题" data-index="title" :width="180" ellipsis>
            <template #cell="{ record }">
              <span v-if="isGroup(record)" class="wf-group-label">{{ record.__groupLabel }}</span>
              <a-typography-text v-else>{{ titleOf(record) }}</a-typography-text>
            </template>
          </a-table-column>
          <a-table-column title="摘要" data-index="summary" :width="220" ellipsis>
            <template #cell="{ record }">
              <a-typography-text type="secondary" style="font-size: 12px">
                {{ summaryPlain(record.summary) }}
              </a-typography-text>
            </template>
          </a-table-column>
          <a-table-column title="业务对象" data-index="typeName" :width="120" ellipsis>
            <template #cell="{ record }">
              <a-typography-text type="secondary" style="font-size: 12px">
                {{ entityLabel(record.typeName, record.typePath) }}
              </a-typography-text>
            </template>
          </a-table-column>
          <a-table-column title="当前审批人" data-index="currentApprover" :width="140" ellipsis>
            <template #cell="{ record }">
              <a-typography-text style="font-size: 12px">
                {{ record.currentApprover }}
              </a-typography-text>
            </template>
          </a-table-column>
          <a-table-column title="状态" data-index="status" :width="90">
            <template #cell="{ record }">
              <a-tag :color="instanceStatusMeta(record.status).color" size="small">
                {{ instanceStatusMeta(record.status).text }}
              </a-tag>
            </template>
          </a-table-column>
          <a-table-column title="发起时间" data-index="createTime" :width="160">
            <template #cell="{ record }">
              <a-typography-text type="secondary" style="font-size: 12px">
                {{ formatDateTime(record.createTime) }}
              </a-typography-text>
            </template>
          </a-table-column>
          <a-table-column title="完成时间" data-index="finishTime" :width="160">
            <template #cell="{ record }">
              <a-typography-text v-if="record.finishTime" type="secondary" style="font-size: 12px">
                {{ formatDateTime(record.finishTime) }}
              </a-typography-text>
              <a-typography-text v-else type="secondary" style="font-size: 12px">—</a-typography-text>
            </template>
          </a-table-column>
          <a-table-column title="操作" :width="88">
            <template #cell="{ record }">
              <a-button v-if="!isGroup(record)" type="text" size="mini" @click="openProgress(record)">进度</a-button>
            </template>
          </a-table-column>
        </template>
        <template #empty>
          <a-empty description="暂无发起的流程" />
        </template>
        </a-table>
      </a-spin>
      </WorkflowListBar>

      <WorkflowProgressPanel v-model="progressVisible" :instance-id="progressInstanceId" />
    </div>
  </div>
</template>

<style scoped>
.wf-started-page {
  /* 内边距/背景由共享 .list-surface/.list-panel 承担 */
  min-height: 0;
}
.wf-started-page__title {
  margin: 0;
  font-size: 16px;
}
</style>
