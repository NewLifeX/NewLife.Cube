<script setup lang="ts">
/**
 * 审批任务中心页（OSC-26090347f1 + 201a T5，IA §3.3）：菜单 URL /Cube/Workflow/Todo（待办）与
 * /Cube/Workflow/Done（已办）共用；kind 由路由末段推断。待办可勾选批量同意/驳回。
 * 行主操作就地完成（宽屏 popover / 窄屏全宽 modal）：审批行同意/驳回，办理行已办理；
 * 更多（转交/加签/回退）用同一行第二个 popover；标题或「进度」才打开进度抽屉。
 */
import { computed, ref } from 'vue';
import { useRoute } from 'vue-router';
import type { FieldMeta } from '@/core/types/field';
import type { WorkflowTaskItem } from '@newlifex/api-core';
import type { ViewGroup } from '@/core/utils/viewProfile';
import {
  dueText,
  rowActionsOf,
  taskStatusMeta,
  taskTitle,
  entityLabel,
  summaryPlain,
} from './useWorkflowTaskList';
import { useWorkflowTaskPage } from './useWorkflowTaskPage';
import { groupWorkflowRows, type WorkflowGroupRow } from './workflowListGroup';
import WorkflowListBar from './WorkflowListBar.vue';
import WorkflowProgressPanel from './WorkflowProgressPanel.vue';
import WorkflowTaskActions from './WorkflowTaskActions.vue';
import './workflowChrome.css';

const route = useRoute();
const kind = computed(() => (String(route.path).toLowerCase().endsWith('/done') ? 'done' : 'todo'));

const TASK_GROUP_FIELDS: FieldMeta[] = [
  { name: 'typeName', displayName: '业务对象', typeName: 'Enum' },
  {
    name: 'status',
    displayName: '状态',
    typeName: 'Enum',
    dataSource: {
      pending: '待处理',
      Pending: '待处理',
      active: '处理中',
      Active: '处理中',
      done: '已完成',
      Done: '已完成',
      rejected: '已驳回',
      Rejected: '已驳回',
      cancelled: '已取消',
      Cancelled: '已取消',
      transferred: '已转办',
      Transferred: '已转办',
    },
  },
  { name: 'createTime', displayName: '到达时间', typeName: 'DateTime' },
];

const {
  rows,
  loading,
  selected,
  batchVisible,
  batchKind,
  batchComment,
  batchSaving,
  hasBatch,
  toggleSelect,
  openBatch,
  confirmBatch,
  formatDateTime,
  phrases,
  progressInstanceId,
  progressVisible,
  openProgress,
  refresh,
  keyword,
  page,
  pageSize,
  total,
  search,
  changePage,
  changePageSize,
} = useWorkflowTaskPage(kind.value);

const group = ref<ViewGroup>([]);
const displayRows = computed(() =>
  groupWorkflowRows(rows.value as unknown as Record<string, unknown>[], group.value, TASK_GROUP_FIELDS),
);
const columnCount = computed(() => (kind.value === 'todo' ? 9 : 6));

function spanMethod(data: { record: WorkflowGroupRow; columnIndex: number }) {
  if (!data.record.__group) return undefined;
  if (data.columnIndex === 0) return { rowspan: 1, colspan: columnCount.value };
  return { rowspan: 0, colspan: 0 };
}

function isGroup(record: WorkflowGroupRow) {
  return record.__group === true;
}

/** 按钮矩阵（审批行：同意/驳回；办理行：已办理；均可看进度） */
const actions = (row: WorkflowTaskItem) => rowActionsOf(row, kind.value);

/** 勾选仅限可处理任务（Pending/Active 且属我） */
const selectable = (row: WorkflowTaskItem) =>
  kind.value === 'todo' &&
  ['pending', 'active'].includes(String(row.status ?? '').toLowerCase());

function dueMeta(record: WorkflowTaskItem) {
  return dueText(record.dueTime);
}

function isSelected(id: string | number) {
  return selected.value.includes(String(id));
}
</script>

<template>
  <div class="wf-task-page list-surface">
    <div class="list-panel list-panel--table wf-task-page__list">
      <!-- 工具栏与表格同处一个面板（多维视图） -->
      <WorkflowListBar
        v-model:q="keyword"
        :fields="TASK_GROUP_FIELDS"
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
        <template v-if="kind === 'todo'" #actions>
          <a-button type="primary" :disabled="!hasBatch" @click="openBatch('approve')">批量同意</a-button>
          <a-button status="danger" :disabled="!hasBatch" @click="openBatch('reject')">批量驳回</a-button>
        </template>
      <a-spin :loading="loading" style="width: 100%; display: block">
      <a-table
        :data="displayRows"
        :loading="loading"
        :row-key="(r: WorkflowGroupRow) => String(r.id)"
        :pagination="false"
        :bordered="false"
        :span-method="spanMethod"
        :scroll="{ x: kind === 'todo' ? 1480 : 1200 }"
        size="small"
        :row-class-name="(record: WorkflowTaskItem) => (String(record.instanceId) === progressInstanceId && progressVisible ? 'wf-task-row wf-task-row--on' : 'wf-task-row')"
      >
        <template #columns>
          <a-table-column
            v-if="kind === 'todo'"
            :width="40"
            :body-cell-style="{ cursor: 'pointer' }"
          >
            <template #cell="{ record }">
              <span v-if="isGroup(record)" class="wf-group-label">{{ record.__groupLabel }}</span>
              <a-checkbox
                v-else
                :model-value="isSelected(record.id)"
                :disabled="!selectable(record)"
                @change="(v: unknown) => toggleSelect(record.id, !!v)"
                @click.stop
              />
            </template>
          </a-table-column>
          <a-table-column title="标题" data-index="titleKey" :width="180" ellipsis>
            <template #cell="{ record }">
              <span v-if="isGroup(record)" class="wf-group-label">{{ record.__groupLabel }}</span>
              <a v-else class="wf-task-page__title-link" @click.stop="openProgress(record)">
                {{ taskTitle(record) }}
              </a>
            </template>
          </a-table-column>
          <a-table-column title="摘要" data-index="summary" :width="200" ellipsis>
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
          <a-table-column title="状态" data-index="status" :width="90">
            <template #cell="{ record }">
              <a-tag :color="taskStatusMeta(record.status).color" size="small">
                {{ taskStatusMeta(record.status).text }}
              </a-tag>
            </template>
          </a-table-column>
          <a-table-column title="到达时间" data-index="createTime" :width="160">
            <template #cell="{ record }">
              <a-typography-text type="secondary" style="font-size: 12px">
                {{ formatDateTime(record.createTime) }}
              </a-typography-text>
            </template>
          </a-table-column>
          <a-table-column v-if="kind === 'todo'" title="截止" data-index="dueTime" :width="120">
            <template #cell="{ record }">
              <span v-if="dueMeta(record)" :style="{ color: dueMeta(record)!.overdue ? 'var(--color-danger-6)' : undefined }">
                {{ dueMeta(record)!.text }}
              </span>
              <a-typography-text v-else type="secondary" style="font-size: 12px">—</a-typography-text>
            </template>
          </a-table-column>
          <a-table-column v-if="kind === 'todo'" title="当前审批人" data-index="currentApprover" :width="140" ellipsis>
            <template #cell="{ record }">
              <a-typography-text style="font-size: 12px">
                {{ record.currentApprover }}
              </a-typography-text>
            </template>
          </a-table-column>
          <a-table-column title="操作" :width="kind === 'todo' ? 280 : 88">
            <template #cell="{ record }">
              <a-space v-if="!isGroup(record)" :size="4" class="wf-ops">
                <WorkflowTaskActions
                  v-if="kind === 'todo' && (actions(record).canApprove || actions(record).canHandle)"
                  appearance="text"
                  :variant="actions(record).canHandle ? 'handle' : 'approve'"
                  :show-more="actions(record).canMore"
                  :allow-cc="actions(record).canApprove"
                  :task-id="record.id"
                  :instance-id="record.instanceId"
                  :node-id="record.nodeId"
                  :assignee-id="record.assigneeId"
                  :candidate="record.candidate"
                  :phrases="phrases"
                  @done="refresh()"
                />
                <a-button type="text" size="mini" @click.stop="openProgress(record)">进度</a-button>
              </a-space>
            </template>
          </a-table-column>
        </template>
        <template #empty>
          <a-empty :description="kind === 'todo' ? '暂无待办' : '暂无已办记录'" />
        </template>
      </a-table>
      </a-spin>
      </WorkflowListBar>

    <!-- 批量同意/驳回意见 -->
    <a-modal
      v-model:visible="batchVisible"
      :title="batchKind === 'approve' ? `批量同意（${selected.length}）` : `批量驳回（${selected.length}）`"
      :on-before-ok="confirmBatch"
      :ok-loading="batchSaving"
    >
      <a-textarea v-model="batchComment" :max-length="500" placeholder="意见（可选）" allow-clear />
    </a-modal>

    <WorkflowProgressPanel v-model="progressVisible" :instance-id="progressInstanceId" />
    </div>
  </div>
</template>

<style scoped>
.wf-task-page {
  min-height: 0;
}
.wf-task-page__title {
  margin: 0;
  font-size: 16px;
}
.wf-task-page__title-link {
  cursor: pointer;
}
.wf-ops {
  white-space: nowrap;
}
.wf-group-label {
  font-weight: 600;
}
</style>
