import { computed, onBeforeUnmount, onMounted, ref, toRef } from 'vue';
import type { WidgetInstance } from '@cube/api-core';

/** 相对旧版 88/160/240/320 减半；h=3 给监控图 / 快捷入口足够绘图高度 */
const HEIGHTS: Record<number, number> = { 1: 72, 2: 100, 3: 180, 4: 260 };

export function minHeightOf(h?: number): number {
  return HEIGHTS[h ?? 2] ?? 100;
}

export function spanOf(w: number, narrow: boolean): number {
  if (narrow) return 12;
  if (w === 2 || w === 3 || w === 4 || w === 6 || w === 8 || w === 12) return w;
  return 3;
}

/** 未写 layout.h 时的默认行高 */
export function fallbackHeightOf(kind: string): number {
  if (kind === 'monitorChart' || kind === 'quickLinks') return 3;
  // 数据列表默认 7 行视口，需要更高卡片
  if (kind === 'dataList') return 4;
  if (kind === 'miniChart' || kind === 'miniKanban' || kind === 'dataCard') return 3;
  if (kind === 'metricCard') return 1;
  return 2;
}

export function useWidgetGrid(props: { widgets: WidgetInstance[] }) {
  const widgets = toRef(props, 'widgets');
  const rootRef = ref<HTMLElement | null>(null);
  const narrow = ref(false);
  let ro: ResizeObserver | null = null;

  function measure() {
    const el = rootRef.value;
    if (!el) return;
    narrow.value = el.clientWidth > 0 && el.clientWidth < 800;
  }

  onMounted(() => {
    measure();
    if (typeof ResizeObserver === 'undefined') return;
    ro = new ResizeObserver(() => measure());
    if (rootRef.value) ro.observe(rootRef.value);
  });

  onBeforeUnmount(() => {
    ro?.disconnect();
    ro = null;
  });

  const items = computed(() =>
    [...(widgets.value ?? [])]
      .sort((a, b) => a.layout.order - b.layout.order)
      .map((w) => {
        const h = w.layout.h ?? fallbackHeightOf(w.kind);
        return {
          widget: w,
          span: spanOf(Number(w.layout.w) || 3, narrow.value),
          minHeight: minHeightOf(h),
        };
      }),
  );

  return { rootRef, items, narrow };
}
