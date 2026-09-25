<script setup lang="ts">
/**
 * 效率页（OSC-260922201a T6）：菜单 URL /Cube/Workflow/Efficiency。薄 .vue，逻辑见 useWorkflowEfficiency。
 * 单层不换页：行点击下钻筛选、月/节点行就地展开最慢 10 条、点最慢记录标题开进度抽屉。
 */
import type { WorkflowEfficiencyRow } from '@newlifex/api-core';
import {
  formatDuration,
  formatRate,
  useWorkflowEfficiency,
  type EffGroupBy,
  type EffTagKey,
  type EffWindow,
} from './useWorkflowEfficiency';
import WorkflowProgressPanel from './WorkflowProgressPanel.vue';
import './workflowChrome.css';

const {
  window,
  yearValue,
  yearChoices,
  groupBy,
  canPickNode,
  tags,
  rows,
  summary,
  isEmpty,
  canExpandRows,
  expandedKeys,
  slowOf,
  slowLoading,
  loading,
  error,
  pickRow,
  onExpand,
  pickGroupBy,
  removeTag,
  openProgress,
  progressVisible,
  progressInstanceId,
  reload,
} = useWorkflowEfficiency();

const onWindowChange = (v: string | number | boolean | undefined) => {
  window.value = (v ?? 'days30') as EffWindow;
};
const onYearChange = (v: string | number | boolean | undefined) => {
  yearValue.value = String(v ?? '');
};
const onGroupByChange = (v: string | number | boolean | undefined) => pickGroupBy(v as EffGroupBy);
const onRemoveTag = (key: string) => removeTag(key as EffTagKey);

/** 展开行标题：月/节点行的名称 + 最慢 10 条 */
function slowTitleOf(record: WorkflowEfficiencyRow): string {
  return `${record.title} · 最慢 10 条`;
}
</script>

<template>
  <div class="wf-eff-page list-surface">
    <div class="list-panel list-panel--table">
      <!-- 工具栏与表格同处一个面板（多维视图） -->
      <div class="list-topbar">
        <h3 class="wf-eff-page__title">效率</h3>
        <a-space>
          <a-button size="small" @click="reload">刷新</a-button>
        </a-space>
      </div>

      <div class="wf-eff__bar">
        <a-radio-group :model-value="window" type="button" size="small" @update:model-value="onWindowChange">
          <a-radio value="days7">近 7 天</a-radio>
          <a-radio value="days30">近 30 天</a-radio>
          <a-radio value="days90">近 90 天</a-radio>
          <a-radio value="year">年份</a-radio>
        </a-radio-group>
        <a-select
          v-if="window === 'year'"
          :model-value="yearValue"
          size="small"
          style="width: 110px"
          @update:model-value="onYearChange"
        >
          <a-option v-for="y in yearChoices" :key="y" :value="y">{{ y }} 年</a-option>
        </a-select>

        <span class="wf-eff__by">按：</span>
        <a-radio-group :model-value="groupBy" type="button" size="small" @update:model-value="onGroupByChange">
          <a-radio value="process">流程</a-radio>
          <a-radio value="department">部门</a-radio>
          <a-radio value="user">用户</a-radio>
          <a-radio value="node" :disabled="!canPickNode">节点</a-radio>
          <a-radio value="year">年</a-radio>
          <a-radio value="month">月</a-radio>
        </a-radio-group>
      </div>

      <div v-if="tags.length" class="wf-eff__tags">
        <a-tag
          v-for="t in tags"
          :key="t.key"
          size="small"
          closable
          @close="onRemoveTag(t.key)"
        >
          {{ t.text }}
        </a-tag>
      </div>

      <a-alert v-if="summary.truncated" type="warning" size="small" class="wf-eff__truncated">
        只统计最近 2000 条
      </a-alert>

      <!-- 指标卡：对齐首页工作台部件卡（icon + 标题行 / 居中彩色大数值） -->
      <div class="wf-eff__metrics">
        <div class="wf-eff__metric wf-eff__metric--primary">
          <div class="wf-eff__metric-head">
            <icon-park type="timer" class="wf-eff__metric-ico" :size="14" />
            <span class="wf-eff__metric-label">平均耗时</span>
          </div>
          <div class="wf-eff__metric-value">{{ summary.avgText }}</div>
        </div>
        <div class="wf-eff__metric wf-eff__metric--success">
          <div class="wf-eff__metric-head">
            <icon-park type="check" class="wf-eff__metric-ico" :size="14" />
            <span class="wf-eff__metric-label">完成率</span>
          </div>
          <div class="wf-eff__metric-value">{{ summary.rateText }}</div>
        </div>
        <div class="wf-eff__metric wf-eff__metric--warning">
          <div class="wf-eff__metric-head">
            <icon-park type="remind" class="wf-eff__metric-ico" :size="14" />
            <span class="wf-eff__metric-label">超 48 小时仍未办完</span>
          </div>
          <div class="wf-eff__metric-value">{{ summary.overdueOpen }} 单</div>
        </div>
      </div>

      <div v-if="error" class="wf-eff__error">{{ error }}</div>

      <a-spin :loading="loading" style="width: 100%; display: block">
        <a-empty v-if="isEmpty" description="这段时间没有计入统计的审批" />
        <a-table
          v-else
          v-model:expanded-keys="expandedKeys"
          :data="rows"
          :pagination="false"
          :bordered="false"
          size="small"
          row-key="key"
          :scroll="{ x: 760 }"
          @row-click="pickRow"
          @expand="onExpand"
        >
          <template #columns>
            <a-table-column title="名称" data-index="title" :width="220" ellipsis>
              <template #cell="{ record }">
                <a-link @click.stop="pickRow(record as WorkflowEfficiencyRow)">
                  {{ record.title }}
                </a-link>
              </template>
            </a-table-column>
            <a-table-column title="样本数" data-index="count" :width="90" />
            <a-table-column title="平均耗时" :width="110">
              <template #cell="{ record }">
                {{ formatDuration(record.avgHours as number | null) }}
              </template>
            </a-table-column>
            <a-table-column title="中位耗时" :width="110">
              <template #cell="{ record }">
                {{ formatDuration(record.medianHours as number | null) }}
              </template>
            </a-table-column>
            <a-table-column title="超 48 小时占比" :width="130">
              <template #cell="{ record }">
                {{ formatRate(record.over48Rate as number | null) }}
              </template>
            </a-table-column>
          </template>

          <template #expand-icon="{ expanded }">
            <span v-if="canExpandRows">{{ expanded ? '▾' : '▸' }}</span>
          </template>

          <template #expand-row="{ record }">
            <div class="wf-eff__slow">
              <div class="wf-eff__slow-title">{{ slowTitleOf(record as WorkflowEfficiencyRow) }}</div>
              <a-spin :loading="slowLoading === String(record.key)" style="width: 100%; display: block">
                <div v-if="!slowOf(String(record.key)).length" class="wf-eff__slow-empty">
                  没有最慢记录
                </div>
                <div
                  v-for="(s, i) in slowOf(String(record.key))"
                  :key="`${s.instanceId}-${s.nodeId}-${i}`"
                  class="wf-eff__slow-item"
                >
                  <a-link @click="openProgress(s.instanceId)">{{ s.title }}</a-link>
                  <span class="wf-eff__slow-assignee">{{ s.assignee || '—' }}</span>
                  <span class="wf-eff__slow-hours">{{ formatDuration(s.hours) }}</span>
                  <a-tag v-if="s.waiting" color="orange" size="small">等待中</a-tag>
                </div>
              </a-spin>
            </div>
          </template>
        </a-table>
      </a-spin>
    </div>

    <WorkflowProgressPanel v-model="progressVisible" :instance-id="progressInstanceId" />
  </div>
</template>

<style scoped>
.wf-eff__bar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
  padding: 0 16px 12px;
}

.wf-eff__by {
  color: var(--color-text-3);
  font-size: 13px;
}

.wf-eff__tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  padding: 0 16px 12px;
}

.wf-eff__truncated {
  margin: 0 16px 12px;
}

.wf-eff__metrics {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
  padding: 0 16px 16px;
}

/* 指标卡：与首页工作台部件卡同规范（bg-2 + border-2 + radius-8 + padding 10/12） */
.wf-eff__metric {
  --eff-accent: rgb(var(--primary-6));
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 10px 12px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: var(--cube-radius-md, 8px);
}

/* 语义色：完成率=成功绿、超时=警告橙（图标与数字同色） */
.wf-eff__metric--success {
  --eff-accent: rgb(var(--success-6));
}
.wf-eff__metric--warning {
  --eff-accent: rgb(var(--warning-6));
}

/* 卡片头：图标（语义色）+ 标题（对齐工作台部件卡 style） */
.wf-eff__metric-head {
  display: flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
}

.wf-eff__metric-ico {
  color: var(--eff-accent);
  flex: none;
}

.wf-eff__metric-label {
  font-size: var(--font-size-body-3, 14px);
  font-weight: 500;
  color: var(--color-text-2);
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* 数值：居中显示 + 语义彩色（对齐工作台部件卡） */
.wf-eff__metric-value {
  font-size: 32px;
  font-weight: 700;
  line-height: 1.1;
  text-align: center;
  color: var(--eff-accent);
}

.wf-eff__error {
  padding: 0 16px 12px;
  color: rgb(var(--danger-6));
}

.wf-eff__slow {
  padding: 8px 16px;
}

.wf-eff__slow-title {
  color: var(--color-text-3);
  font-size: 12px;
  margin-bottom: 6px;
}

.wf-eff__slow-empty {
  color: var(--color-text-3);
  font-size: 12px;
}

.wf-eff__slow-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 4px 0;
}

.wf-eff__slow-assignee {
  color: var(--color-text-2);
  font-size: 12px;
}

.wf-eff__slow-hours {
  color: var(--color-text-3);
  font-size: 12px;
}

@media (max-width: 768px) {
  .wf-eff__metrics {
    grid-template-columns: 1fr;
  }
}
</style>
