<script setup lang="ts">
/**
 * 审批任务中心页（OSC-26090347f1 T8c，IA §3.3）：菜单 URL /Cube/Workflow/Todo（待办）与
 * /Cube/Workflow/Done（已办）共用；kind 由路由末段推断。待办可勾选批量同意/驳回，
 * 行点击打开进度抽屉（含打开记录由实例详情提供）。
 */
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import type { WorkflowTaskItem } from '@cube/api-core';
import { dueText, modeLabel, taskStatusMeta, taskTitle } from './useWorkflowTaskList';
import { useWorkflowTaskPage } from './useWorkflowTaskPage';
import WorkflowProgressPanel from './WorkflowProgressPanel.vue';

const route = useRoute();
const kind = computed(() => (String(route.path).toLowerCase().endsWith('/done') ? 'done' : 'todo'));
const pageTitle = computed(() => (kind.value === 'todo' ? '我的待办' : '已办'));

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
  progressInstanceId,
  progressVisible,
  openProgress,
  openRecord,
  refresh,
} = useWorkflowTaskPage(kind.value);

/** 勾选仅限可处理任务（Pending/Active 且属我） */
const selectable = (row: WorkflowTaskItem) =>
  kind.value === 'todo' &&
  ['pending', 'active'].includes(String(row.status ?? '').toLowerCase());

function modeTagColor(mode?: string): string {
  return mode === 'and' ? 'blue' : mode === 'sequence' ? 'purple' : 'arcoblue';
}

function dueMeta(record: WorkflowTaskItem) {
  return dueText(record.dueTime);
}
</script>

<template>
  <div class="wf-task-page">
    <div class="wf-task-page__head">
      <h3 class="wf-task-page__title">{{ pageTitle }}</h3>
      <a-space v-if="kind === 'todo'">
        <a-button type="primary" size="small" :disabled="!hasBatch" @click="openBatch('approve')">
          批量同意
        </a-button>
        <a-button status="danger" size="small" :disabled="!hasBatch" @click="openBatch('reject')">
          批量驳回
        </a-button>
        <a-button size="small" @click="refresh()">刷新</a-button>
      </a-space>
      <a-button v-else size="small" @click="refresh()">刷新</a-button>
    </div>

    <a-spin :loading="loading">
      <a-table
        :data="rows"
        :row-key="(r: WorkflowTaskItem) => r.id"
        :pagination="false"
        :bordered="false"
        size="small"
        :row-class-name="() => (kind === 'todo' ? 'wf-task-row' : '')"
        @row-click="openProgress"
      >
        <template #columns>
          <a-table-column
            v-if="kind === 'todo'"
            :width="40"
            :body-cell-style="{ cursor: 'pointer' }"
          >
            <template #cell="{ record }">
              <a-checkbox
                :model-value="selected.includes(record.id)"
                :disabled="!selectable(record)"
                @change="(v: unknown) => toggleSelect(record.id, !!v)"
                @click.stop
              />
            </template>
          </a-table-column>
          <a-table-column title="标题" data-index="titleKey" :width="220" ellipsis>
            <template #cell="{ record }">
              <a class="wf-task-page__title-link" @click.stop="openRecord(record)">
                {{ taskTitle(record) }}
              </a>
            </template>
          </a-table-column>
          <a-table-column title="实体" data-index="typePath" :width="160" ellipsis>
            <template #cell="{ record }">
              <a-typography-text type="secondary" style="font-size: 12px">
                {{ record.typePath || '—' }}
              </a-typography-text>
            </template>
          </a-table-column>
          <a-table-column title="节点" data-index="mode" :width="110">
            <template #cell="{ record }">
              <a-tag size="small" :color="modeTagColor(record.mode)">
                {{ modeLabel(record.mode) || record.nodeId || '—' }}
              </a-tag>
            </template>
          </a-table-column>
          <a-table-column title="到达时间" data-index="createTime" :width="170">
            <template #cell="{ record }">
              <a-typography-text type="secondary" style="font-size: 12px">
                {{ formatDateTime(record.createTime) }}
              </a-typography-text>
            </template>
          </a-table-column>
          <a-table-column v-if="kind === 'todo'" title="截止" data-index="dueTime" :width="140">
            <template #cell="{ record }">
              <span v-if="dueMeta(record)" :style="{ color: dueMeta(record)!.overdue ? 'var(--color-danger-6)' : undefined }">
                {{ dueMeta(record)!.text }}
              </span>
              <a-typography-text v-else type="secondary" style="font-size: 12px">—</a-typography-text>
            </template>
          </a-table-column>
          <a-table-column title="状态" data-index="status" :width="100">
            <template #cell="{ record }">
              <a-tag :color="taskStatusMeta(record.status).color" size="small">
                {{ taskStatusMeta(record.status).text }}
              </a-tag>
            </template>
          </a-table-column>
          <a-table-column title="操作" :width="80">
            <template #cell="{ record }">
              <a-button type="text" size="mini" @click.stop="openProgress(record)">进度</a-button>
            </template>
          </a-table-column>
        </template>
        <template #empty>
          <a-empty :description="kind === 'todo' ? '暂无待办' : '暂无已办记录'" />
        </template>
      </a-table>
    </a-spin>

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
</template>

<style scoped>
.wf-task-page {
  padding: 16px;
}
.wf-task-page__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}
.wf-task-page__title {
  margin: 0;
  font-size: 16px;
}
.wf-task-page__title-link {
  cursor: pointer;
}
</style>
