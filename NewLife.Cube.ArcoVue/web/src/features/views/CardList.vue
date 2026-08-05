<template>
  <!-- key 强制在列数/排版变更时整表重挂，避免 scoped 样式缓存导致无感 -->
  <div
    ref="listRef"
    class="card-list"
    :key="layoutSignature"
    :class="layoutClass"
    :style="{ minHeight: height + 'px' }"
  >
    <RecordCard
      v-for="(row, idx) in records"
      :key="rowKeyOf(row, idx)"
      :record="row"
      :title="titleOf(row)"
      :image-url="resolveImageUrl(row, mapping?.imageField)"
      :body-fields="bodyOf(row)"
      :layout="resolvedLayout"
      :body-columns="resolvedBodyColumns"
      :field-orientation="resolvedFieldOrientation"
      :min-height="cardMinHeight"
      :can-view-detail="canViewDetail"
      :can-edit="canEdit"
      :can-delete="canDelete"
      @detail="$emit('detail', $event)"
      @edit="$emit('edit', $event)"
      @delete="$emit('delete', $event)"
      @toggle-enable="(row, field) => $emit('toggleEnable', row, field)"
    />
    <a-empty v-if="!records.length" description="暂无数据" />
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref } from '@/core/utils/viewProfile';
import type {
  CardBodyColumns,
  CardFieldOrientation,
  CardLayout,
  CardMapping,
} from '@/core/utils/viewMapping';
import { getValueByKey } from '@/core/utils/url';
import RecordCard from './RecordCard.vue';
import { buildCardBodyFields, cardExcludeKeys, resolveImageUrl } from './cardHelpers';

const props = withDefaults(
  defineProps<{
    records: Record<string, unknown>[];
    columns: ColumnPref[];
    fields: FieldMeta[];
    mapping?: CardMapping | null;
    rowKey: string;
    height?: number;
    layout?: CardLayout;
    bodyColumns?: CardBodyColumns;
    fieldOrientation?: CardFieldOrientation;
    canViewDetail: boolean;
    canEdit: boolean;
    canDelete: boolean;
    formatCell?: (field: FieldMeta, record: Record<string, unknown>) => string;
  }>(),
  {
    layout: 'standard',
    bodyColumns: 2,
    fieldOrientation: 'vertical',
  },
);

/** mapping 为配置真源；props 仅作缺省回落（避免 || 把合法值冲掉） */
const resolvedLayout = computed<CardLayout>(() => {
  const m = props.mapping?.layout;
  if (m === 'large' || m === 'row' || m === 'standard') return m;
  return props.layout === 'large' || props.layout === 'row' ? props.layout : 'standard';
});

const resolvedBodyColumns = computed<CardBodyColumns>(() => {
  const fromMap = props.mapping?.bodyColumns;
  if (fromMap === 1 || fromMap === 2 || fromMap === 3) {
    return fromMap === 3 && resolvedLayout.value !== 'row' ? 2 : fromMap;
  }
  const fromProp = props.bodyColumns;
  if (fromProp === 1 || fromProp === 2 || fromProp === 3) {
    return fromProp === 3 && resolvedLayout.value !== 'row' ? 2 : fromProp;
  }
  return 2;
});

const resolvedFieldOrientation = computed<CardFieldOrientation>(() => {
  const fromMap = props.mapping?.fieldOrientation;
  if (fromMap === 'horizontal' || fromMap === 'vertical') return fromMap;
  return props.fieldOrientation === 'horizontal' ? 'horizontal' : 'vertical';
});

const layoutClass = computed(() => `card-list--${resolvedLayout.value}`);
const layoutSignature = computed(
  () =>
    `${resolvedLayout.value}:${resolvedBodyColumns.value}:${resolvedFieldOrientation.value}`,
);

/** 等高：所有卡片高度统一为“所有对象中最高卡片”的高度（后端返回全量对象取最大） */
const listRef = ref<HTMLElement | null>(null);
const cardMinHeight = ref(0);

async function measureTallest() {
  await nextTick();
  const host = listRef.value;
  if (!host) return;
  const cards = host.querySelectorAll('.record-card');
  let max = 0;
  cards.forEach((c) => {
    max = Math.max(max, (c as HTMLElement).offsetHeight);
  });
  if (max > 0) cardMinHeight.value = max;
}

watch(
  () => [props.records, props.columns, layoutSignature.value],
  () => {
    cardMinHeight.value = 0;
    requestAnimationFrame(measureTallest);
  },
  { deep: true },
);

onMounted(measureTallest);

defineEmits<{
  detail: [row: Record<string, unknown>];
  edit: [row: Record<string, unknown>];
  delete: [row: Record<string, unknown>];
  toggleEnable: [row: Record<string, unknown>, field: string];
}>();

const exclude = computed(() =>
  props.mapping ? cardExcludeKeys(props.mapping) : [],
);

function rowKeyOf(row: Record<string, unknown>, idx: number) {
  const v = getValueByKey(row, props.rowKey);
  return v == null || v === '' ? idx : String(v);
}

function titleOf(row: Record<string, unknown>) {
  const key = props.mapping?.titleField;
  if (!key) return '-';
  const field = props.fields.find((f) => f.name === key);
  if (field && props.formatCell) return props.formatCell(field, row);
  const raw = getValueByKey(row, key);
  return raw == null || raw === '' ? '-' : String(raw);
}

function bodyOf(row: Record<string, unknown>) {
  return buildCardBodyFields(
    row,
    props.columns,
    props.fields,
    exclude.value,
    props.formatCell,
  );
}
</script>

<style scoped>
.card-list {
  display: grid;
  gap: 12px;
  padding: 4px 0 12px;
  align-content: start;
  /* 所有卡片高度统一为最高卡片（由 CardList 测量后以 min-height 下发） */
}
.card-list--standard {
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
}
.card-list--large {
  grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
}
.card-list--row {
  grid-template-columns: 1fr;
}
</style>
