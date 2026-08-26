import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref } from '@/core/utils/viewProfile';
import type {
  CardBodyColumns,
  CardFieldOrientation,
  CardLayout,
  CardMapping,
} from '@/core/utils/viewMapping';
import { getValueByKey } from '@/core/utils/url';
import { buildCardBodyFields, cardExcludeKeys } from './cardHelpers';
import type { ViewFormatRule } from '@/core/utils/viewProfile';
import { resolveCardTitleFormat, resolveRowSideColor } from '@/core/utils/viewFormat';

/** CardList 组件 props 类型（与 CardList.vue defineProps 泛型逐字一致） */
interface CardListProps {
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
  typePath?: string;
  formatCell?: (field: FieldMeta, record: Record<string, unknown>) => string;
  formatRules?: ViewFormatRule[];
}

/** CardList 组件全部业务 TS：卡片布局/等高测量/滚动懒加载（自 CardList.vue script setup 原样搬移；本组件 emits 仅模板 $emit 使用，composable 无需 emit） */
export function useCardList(props: CardListProps) {
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

  /* ---------------- 滚动懒加载（1000 条及以上分批渲染） ---------------- */
  /** 初始渲染条数与滚动追加步长 */
  const INITIAL_VISIBLE = 100;
  const LOAD_STEP = 100;
  /** 当前已渲染条数（先 100，滚动接近底部动态追加，不一次性渲染全部） */
  const visibleCount = ref(INITIAL_VISIBLE);
  /** 底部哨兵：进入视口附近（提前 400px）触发追加 */
  const sentinelRef = ref<HTMLElement | null>(null);
  let sentinelIo: IntersectionObserver | null = null;
  /** 等高测量延迟计时器：卸载/重复触发时清理 */
  let measureTimer = 0;
  /** 当前应渲染的记录（slice） */
  const visibleRecords = computed(() => props.records.slice(0, visibleCount.value));

  function observeSentinel() {
    sentinelIo?.disconnect();
    sentinelIo = null;
    if (!sentinelRef.value || visibleCount.value >= props.records.length) return;
    sentinelIo = new IntersectionObserver(
      (entries) => {
        if (entries[0]?.isIntersecting && visibleCount.value < props.records.length) {
          visibleCount.value = Math.min(props.records.length, visibleCount.value + LOAD_STEP);
        }
      },
      { rootMargin: '400px 0px' },
    );
    sentinelIo.observe(sentinelRef.value);
  }

  /* scroll 兜底：IntersectionObserver 在部分环境（无头浏览器/嵌入渲染等）可能不触发，
     监听最近滚动容器（含页面）接近底部时追加，保证滚动懒加载始终可用（与 IO 幂等） */
  let scrollParent: HTMLElement | Window | null = null;
  let scrollBound = false;

  function findScrollParent(el: HTMLElement | null): HTMLElement | Window | null {
    let node = el?.parentElement ?? null;
    while (node) {
      const s = getComputedStyle(node);
      if (/(auto|scroll|overlay)/.test(s.overflowY) && node.scrollHeight > node.clientHeight) return node;
      node = node.parentElement;
    }
    return window;
  }

  function onScrollFallback() {
    const sp = scrollParent;
    if (!sp || visibleCount.value >= props.records.length) return;
    const isWin = sp === window;
    const el = sp as HTMLElement;
    const scrollTop = isWin ? window.scrollY : el.scrollTop;
    const clientH = isWin ? window.innerHeight : el.clientHeight;
    const scrollH = isWin ? document.documentElement.scrollHeight : el.scrollHeight;
    if (scrollTop + clientH >= scrollH - 400) {
      visibleCount.value = Math.min(props.records.length, visibleCount.value + LOAD_STEP);
    }
  }

  function bindScrollFallback() {
    if (scrollBound) return;
    scrollParent = findScrollParent(listRef.value);
    if (!scrollParent) return;
    scrollParent.addEventListener('scroll', onScrollFallback, { passive: true });
    scrollBound = true;
  }

  function unbindScrollFallback() {
    if (!scrollBound) return;
    scrollParent?.removeEventListener('scroll', onScrollFallback);
    scrollBound = false;
    scrollParent = null;
  }

  watch(
    () => [props.records, props.columns, layoutSignature.value] as const,
    () => {
      // 数据/排版变化 → 重置懒加载计数并重新观察哨兵；测高延迟到首帧渲染之后（读 offsetHeight 强制布局，避免阻塞首帧）
      visibleCount.value = INITIAL_VISIBLE;
      cardMinHeight.value = 0;
      nextTick(() => {
        observeSentinel();
        // 数据到达后滚动容器才可能变为可滚动，需重查并重新绑定 scroll 兜底
        unbindScrollFallback();
        bindScrollFallback();
        // setTimeout 而非 rAF：受限环境 rAF 可能不触发；50ms 后测高二次布局等高，首帧不被强制同步布局阻塞
        clearTimeout(measureTimer);
        measureTimer = window.setTimeout(() => void measureTallest(), 50);
      });
    },
  );

  onMounted(() => {
    nextTick(() => {
      observeSentinel();
      bindScrollFallback();
      // 初始测高同样延迟，避免首帧强制同步布局
      clearTimeout(measureTimer);
      measureTimer = window.setTimeout(() => void measureTallest(), 50);
    });
  });

  onBeforeUnmount(() => {
    sentinelIo?.disconnect();
    sentinelIo = null;
    clearTimeout(measureTimer);
    unbindScrollFallback();
  });

  async function measureTallest() {
    await nextTick();
    const host = listRef.value;
    if (!host) return;
    const cards = host.querySelectorAll('.record-card');
    // 性能：千条卡片时 offsetHeight 全量读取会强制全量布局，只测前 200 张代表
    // （等高语义下卡片高度由字段/图片决定，前 200 张足够反映最高卡片）
    const maxCards = 200;
    let max = 0;
    const count = Math.min(cards.length, maxCards);
    for (let i = 0; i < count; i++) {
      max = Math.max(max, (cards[i] as HTMLElement).offsetHeight);
    }
    if (max > 0) cardMinHeight.value = max;
  }

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

  function titleFormatColorOf(row: Record<string, unknown>) {
    return resolveCardTitleFormat(row, props.formatRules || [], props.fields)?.color;
  }

  function titleFormatBoldOf(row: Record<string, unknown>) {
    return !!resolveCardTitleFormat(row, props.formatRules || [], props.fields)?.bold;
  }

  function sideFormatColorOf(row: Record<string, unknown>) {
    return resolveRowSideColor(row, props.formatRules || [], props.fields);
  }

  return {
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
  };
}
