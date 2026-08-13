import { computed } from 'vue';
import type {
  CardBodyColumns,
  CardFieldOrientation,
  CardLayout,
} from '@/core/utils/viewMapping';
import type { CardBodyField } from './cardHelpers';

/** RecordCard 组件 props 类型（与 RecordCard.vue defineProps 泛型逐字一致） */
interface RecordCardProps {
  record: Record<string, unknown>;
  title: string;
  imageUrl?: string;
  bodyFields: CardBodyField[];
  canViewDetail: boolean;
  canEdit: boolean;
  canDelete: boolean;
  layout?: CardLayout;
  bodyColumns?: CardBodyColumns;
  fieldOrientation?: CardFieldOrientation;
  /** 等高：所有卡片统一最小高度（由 CardList 取最高卡片下发） */
  minHeight?: number;
}

/** RecordCard 组件全部业务 TS：徽标样式与卡片布局 CSS 变量（自 RecordCard.vue script setup 原样搬移） */
export function useRecordCard(props: RecordCardProps) {
  /** 徽标样式：浅底 + 同色文字（与列表徽章一致） */
  function badgeStyle(item: CardBodyField): Record<string, string> | undefined {
    const b = item.badge;
    if (!b) return undefined;
    return {
      backgroundColor: b.buttonColor,
      borderColor: b.buttonBorderColor,
      color: b.textColor,
    };
  }

  const cols = computed(() => {
    const n = props.bodyColumns;
    return n === 1 || n === 3 ? n : 2;
  });

  const layoutClass = computed(() => `record-card--${props.layout || 'standard'}`);
  const orientationClass = computed(
    () => `record-card--orient-${props.fieldOrientation === 'horizontal' ? 'horizontal' : 'vertical'}`,
  );

  /** 用 CSS 变量驱动列数，避免动态 class 未命中时样式不生效；同时下发等高 min-height */
  const cardCssVars = computed(() => ({
    '--record-card-cols': String(cols.value),
    ...(props.minHeight && props.minHeight > 0
      ? { minHeight: `${props.minHeight}px` }
      : {}),
  }));

  return {
    badgeStyle,
    layoutClass,
    orientationClass,
    cardCssVars,
  };
}
