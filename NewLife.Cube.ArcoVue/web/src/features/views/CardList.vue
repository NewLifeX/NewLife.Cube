<template>
  <!-- key 强制在列数/排版变更时整表重挂，避免 scoped 样式缓存导致无感 -->
  <div
    v-if="!records.length"
    class="view-empty-wrap"
    :style="{ minHeight: (height || 240) + 'px' }"
  >
    <a-empty description="暂无数据" />
  </div>
  <div
    v-else
    ref="listRef"
    class="card-list"
    :key="layoutSignature"
    :class="layoutClass"
    :style="{ minHeight: height + 'px' }"
  >
    <RecordCard
      v-for="(row, idx) in visibleRecords"
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
      :enable-table-double-click="enableTableDoubleClick"
      :can-edit="canEdit"
      :can-delete="canDelete && !isIamRowActionDisabled(typePath, row, 'delete')"
      :ops-custom-links="opsCustomLinks"
      :title-format-color="titleFormatColorOf(row)"
      :title-format-bold="titleFormatBoldOf(row)"
      :side-format-color="sideFormatColorOf(row)"
      @detail="$emit('detail', $event)"
      @edit="$emit('edit', $event)"
      @delete="$emit('delete', $event)"
      @ops-link="(link, row) => $emit('opsLink', link, row)"
      @toggle-enable="(row, field) => $emit('toggleEnable', row, field)"
    />
    <!-- 懒加载哨兵：仅当还有未渲染数据时存在；进入视口附近即追加下一批（滚动动态加载，不一次性渲染全部） -->
    <div v-if="visibleRecords.length < records.length" ref="sentinelRef" class="card-list-sentinel" />
  </div>
</template>

<script setup lang="ts">
import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref } from '@/core/utils/viewProfile';
import type {
  CardBodyColumns,
  CardFieldOrientation,
  CardLayout,
  CardMapping,
} from '@/core/utils/viewMapping';
import type { OpsCustomLink } from '@/core/utils/opsAction';
import RecordCard from './RecordCard.vue';
import { resolveImageUrl } from './cardHelpers';
import { useCardList } from './useCardList';
import type { ViewFormatRule } from '@/core/utils/viewProfile';
import { isIamRowActionDisabled } from '@/core/utils/iamGuards';

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
    /** 魔方设置 EnableTableDoubleClick；false 时卡片/看板禁用双击进详情 */
    enableTableDoubleClick?: boolean;
    canEdit: boolean;
    canDelete: boolean;
    typePath?: string;
    opsCustomLinks?: OpsCustomLink[];
    formatCell?: (field: FieldMeta, record: Record<string, unknown>) => string;
    formatRules?: ViewFormatRule[];
  }>(),
  {
    layout: 'standard',
    bodyColumns: 2,
    fieldOrientation: 'vertical',
    enableTableDoubleClick: true,
    opsCustomLinks: () => [],
  },
);

defineEmits<{
  detail: [row: Record<string, unknown>];
  edit: [row: Record<string, unknown>];
  delete: [row: Record<string, unknown>];
  toggleEnable: [row: Record<string, unknown>, field: string];
  opsLink: [link: OpsCustomLink, row: Record<string, unknown>];
}>();

const {
  listRef,
  layoutSignature,
  layoutClass,
  visibleRecords,
  resolvedLayout,
  resolvedBodyColumns,
  resolvedFieldOrientation,
  cardMinHeight,
  sentinelRef,
  rowKeyOf,
  titleOf,
  bodyOf,
  titleFormatColorOf,
  titleFormatBoldOf,
  sideFormatColorOf,
} = useCardList(props);
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
