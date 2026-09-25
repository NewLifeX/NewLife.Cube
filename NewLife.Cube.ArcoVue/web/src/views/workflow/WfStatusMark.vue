<script setup lang="ts">
/**
 * 与实体列表审批列相同的图标徽标：只画图标，文案放 Tooltip。
 */
import { computed } from 'vue';
import { wfMarkKind } from '@/features/vtable/wfStatusMark';
import { wfStatusBadge } from '@/views/crud/useWorkflowList';

const props = defineProps<{ status?: string }>();

const badge = computed(() => wfStatusBadge((props.status ?? '').toLowerCase()));
const kind = computed(() => wfMarkKind((props.status ?? '').toLowerCase()));
</script>

<template>
  <a-tooltip :content="badge.tooltip || badge.text" position="top">
    <span
      class="wf-mark"
      :style="{ background: badge.bgColor, color: badge.textColor }"
      role="img"
      :aria-label="badge.text"
    >
      <svg viewBox="0 0 22 22" width="22" height="22" aria-hidden="true">
        <circle
          v-if="kind === 'ring' || kind === 'ringDot'"
          cx="11"
          cy="11"
          r="4.6"
          fill="none"
          stroke="currentColor"
          stroke-width="1.5"
        />
        <circle v-if="kind === 'ringDot'" cx="11" cy="11" r="1.7" fill="currentColor" />
        <polyline
          v-if="kind === 'check'"
          points="7.5,11.2 9.9,13.8 14.5,8.2"
          fill="none"
          stroke="currentColor"
          stroke-width="1.6"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
        <g v-if="kind === 'cross'" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round">
          <line x1="8.1" y1="8.1" x2="13.9" y2="13.9" />
          <line x1="13.9" y1="8.1" x2="8.1" y2="13.9" />
        </g>
        <g v-if="kind === 'undo'" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
          <path d="M15.4 10.3 A4.6 4.6 0 1 0 11 15.6" />
          <polyline points="13.2,14.2 11,15.6 12.6,17.6" />
        </g>
      </svg>
    </span>
  </a-tooltip>
</template>

<style scoped>
.wf-mark {
  display: inline-flex;
  width: 22px;
  height: 22px;
  border-radius: 4px;
  overflow: hidden;
  flex: none;
}
</style>
