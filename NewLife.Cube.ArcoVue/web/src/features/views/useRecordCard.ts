import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import type {
  CardBodyColumns,
  CardFieldOrientation,
  CardLayout,
} from '@/core/utils/viewMapping';
import type { OpsCustomLink } from '@/core/utils/opsAction';
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
  opsCustomLinks?: OpsCustomLink[];
  layout?: CardLayout;
  bodyColumns?: CardBodyColumns;
  fieldOrientation?: CardFieldOrientation;
  /** 等高：所有卡片统一最小高度（由 CardList 取最高卡片下发） */
  minHeight?: number;
}

const OPS_GAP = 6;

/** 估算操作按钮宽度（padding 12*2 + 字宽），偏保守以免仍溢出换行 */
export function estimateOpsBtnWidth(label: string): number {
  const text = label || '';
  let chars = 0;
  for (const ch of text) {
    // CJK 按满宽，ASCII 约半宽
    chars += /[\u1100-\uFFFF]/.test(ch) ? 1 : 0.55;
  }
  return Math.ceil(24 + Math.max(chars, 1) * 12 + 4);
}

/** 在可用宽度内最多直出几条自定义链接（其余进「更多」）；CRUD 始终占位 */
export function fitOpsLinkInlineCount(options: {
  availableWidth: number;
  crudLabels: string[];
  links: OpsCustomLink[];
}): number {
  const { availableWidth, crudLabels, links } = options;
  if (availableWidth <= 0 || !links.length) return 0;

  let used = 0;
  for (let i = 0; i < crudLabels.length; i++) {
    used += estimateOpsBtnWidth(crudLabels[i]) + (i > 0 ? OPS_GAP : 0);
  }

  const moreW = estimateOpsBtnWidth('更多');
  for (let n = links.length; n >= 0; n--) {
    let w = used;
    for (let i = 0; i < n; i++) {
      w += OPS_GAP + estimateOpsBtnWidth(links[i].label);
    }
    if (n < links.length) w += OPS_GAP + moreW;
    if (w <= availableWidth) return n;
  }
  return 0;
}

/** RecordCard 组件全部业务 TS：徽标样式、布局变量、操作区按宽分流 */
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

  const opsRef = ref<HTMLElement | null>(null);
  const inlineLinkCount = ref(0);
  let ro: ResizeObserver | null = null;

  const crudLabels = computed(() => {
    const labels: string[] = [];
    if (props.canViewDetail) labels.push('详情');
    if (props.canEdit) labels.push('编辑');
    if (props.canDelete) labels.push('删除');
    return labels;
  });

  const allOpsLinks = computed(() => props.opsCustomLinks ?? []);

  function recomputeInline() {
    const el = opsRef.value;
    const width = el?.clientWidth ?? 0;
    inlineLinkCount.value = fitOpsLinkInlineCount({
      availableWidth: width,
      crudLabels: crudLabels.value,
      links: allOpsLinks.value,
    });
  }

  const inlineOpsLinks = computed(() =>
    allOpsLinks.value.slice(0, inlineLinkCount.value),
  );
  const overflowOpsLinks = computed(() =>
    allOpsLinks.value.slice(inlineLinkCount.value),
  );

  onMounted(() => {
    nextTick(() => {
      recomputeInline();
      if (typeof ResizeObserver !== 'undefined' && opsRef.value) {
        ro = new ResizeObserver(() => recomputeInline());
        ro.observe(opsRef.value);
      }
    });
  });

  onBeforeUnmount(() => {
    ro?.disconnect();
    ro = null;
  });

  watch(
    () => [
      props.canViewDetail,
      props.canEdit,
      props.canDelete,
      props.opsCustomLinks,
      props.layout,
      props.minHeight,
    ],
    () => nextTick(recomputeInline),
    { deep: true },
  );

  return {
    badgeStyle,
    layoutClass,
    orientationClass,
    cardCssVars,
    opsRef,
    inlineOpsLinks,
    overflowOpsLinks,
  };
}
