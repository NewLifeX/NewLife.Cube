import { computed, onBeforeUnmount, onMounted, ref, toRef } from 'vue';
import type { WidgetInstance } from '@cube/api-core';

/** 相对旧版 88/160/240/320 减半：指标卡默认 h=2→80，迷你图默认 h=3→120 */
const HEIGHTS: Record<number, number> = { 1: 44, 2: 80, 3: 120, 4: 160 };

export function minHeightOf(h?: number): number {
  return HEIGHTS[h ?? 2] ?? 80;
}

export function spanOf(w: number, narrow: boolean): number {
  if (narrow) return 12;
  if (w === 4 || w === 6 || w === 12) return w;
  return 3;
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
        const fallbackH =
          w.kind === 'miniChart' || w.kind === 'miniKanban' ? 3 : 2;
        return {
          widget: w,
          span: spanOf(Number(w.layout.w) || 3, narrow.value),
          minHeight: minHeightOf(w.layout.h ?? fallbackH),
        };
      }),
  );

  return { rootRef, items, narrow };
}
