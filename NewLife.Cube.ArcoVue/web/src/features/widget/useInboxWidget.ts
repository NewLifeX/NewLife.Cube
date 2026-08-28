import { computed } from 'vue';
import { useAppStore } from '@/stores/app';
import type { WidgetCardProps } from './context';

export function useInboxWidget(props: WidgetCardProps) {
  const appStore = useAppStore();
  const unread = computed(() => {
    const r = (props.result || {}) as Record<string, unknown>;
    return Number(r.unread ?? r.Unread ?? 0) || 0;
  });
  const items = computed(() => {
    const r = (props.result || {}) as Record<string, unknown>;
    const raw = r.items ?? r.Items;
    if (!Array.isArray(raw)) return [];
    return raw.map((it) => {
      const o = (it && typeof it === 'object' ? it : {}) as Record<string, unknown>;
      return {
        id: o.id ?? o.Id,
        title: String(o.title ?? o.Title ?? ''),
        createTime: String(o.createTime ?? o.CreateTime ?? ''),
      };
    });
  });

  function openInbox() {
    appStore.openInboxDrawer();
  }

  return { unread, items, openInbox };
}
