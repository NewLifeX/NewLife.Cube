<script setup lang="ts">
import { computed } from 'vue';
import { RouterLink } from 'vue-router';
import type { WorkbenchKpiView } from '@newlifex/page-logic';
import { isExternalLink } from '@newlifex/page-logic';

defineOptions({ name: 'KpiCard' });

const props = defineProps<{ kpi: WorkbenchKpiView }>();

/** KPI 名称 → 图标，未知名称回退通用折线图标 */
const KPI_ICONS: Record<string, string> = {
  UserCount: 'ep:user',
  TodayLogin: 'ep:switch-button',
  OnlineCount: 'ep:user-filled',
  MaxOnline: 'ep:trend-charts',
  Error24h: 'ep:warning',
  OnlineTime: 'ep:timer'
};

const iconName = computed(() => KPI_ICONS[props.kpi.name] || 'ep:data-line');

const isExternal = computed(() => Boolean(props.kpi.url && isExternalLink(props.kpi.url)));
const isInternal = computed(() => Boolean(props.kpi.url && !isExternalLink(props.kpi.url)));

const tag = computed(() => {
  if (isExternal.value) return 'a';
  if (isInternal.value) return RouterLink;
  return 'div';
});

const linkAttrs = computed(() => {
  if (isExternal.value) return { href: props.kpi.url, target: '_blank', rel: 'noopener' };
  if (isInternal.value) {
    const path = props.kpi.url!.startsWith('~/') ? props.kpi.url!.slice(1) : props.kpi.url!;
    return { to: path.startsWith('/') ? path : `/${path}` };
  }
  return {};
});
</script>

<template>
  <component :is="tag" v-bind="linkAttrs" class="kpi-card" :class="`kpi-card--${kpi.color}`">
    <span class="kpi-card__icon">
      <SvgIcon :icon="iconName" />
    </span>
    <span class="kpi-card__text">
      <span class="kpi-card__label">{{ kpi.label }}</span>
      <span class="kpi-card__value">{{ kpi.value }}</span>
      <span v-if="kpi.trend" class="kpi-card__trend">{{ kpi.trend }}</span>
    </span>
  </component>
</template>

<style scoped>
.kpi-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px;
  border-radius: 8px;
  background-color: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  text-decoration: none;
  transition: box-shadow 0.2s;
}

.kpi-card:hover {
  box-shadow: 0 4px 16px rgb(0 0 0 / 8%);
}

.kpi-card__icon {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  border-radius: 8px;
  background: color-mix(in srgb, var(--kpi-color) 12%, transparent);
  color: var(--kpi-color);
  font-size: 22px;
}

.kpi-card__text {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.kpi-card__label {
  color: var(--el-text-color-secondary);
  font-size: 12px;
}

.kpi-card__value {
  color: var(--el-text-color-primary);
  font-size: 20px;
  font-weight: 600;
  line-height: 1.2;
  white-space: nowrap;
}

.kpi-card__trend {
  color: var(--el-text-color-secondary);
  font-size: 12px;
  white-space: nowrap;
}

.kpi-card--blue {
  --kpi-color: #409eff;
}

.kpi-card--green {
  --kpi-color: #67c23a;
}

.kpi-card--cyan {
  --kpi-color: #17c0d3;
}

.kpi-card--orange {
  --kpi-color: #e6a23c;
}

.kpi-card--red {
  --kpi-color: #f56c6c;
}

.kpi-card--purple {
  --kpi-color: #8e6ef2;
}

.kpi-card--grey {
  --kpi-color: #909399;
}
</style>
