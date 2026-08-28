import { computed } from 'vue';
import { useRouter } from 'vue-router';
import type { WidgetCardProps } from './context';

export function useKvListWidget(props: WidgetCardProps) {
  const router = useRouter();
  const items = computed(() => {
    const r = (props.result || {}) as Record<string, unknown>;
    const raw = r.items ?? r.Items;
    if (!Array.isArray(raw)) return [];
    return raw.map((it) => {
      const o = (it && typeof it === 'object' ? it : {}) as Record<string, unknown>;
      return {
        label: String(o.label ?? o.Label ?? o.key ?? o.Key ?? ''),
        value: String(o.value ?? o.Value ?? ''),
        href: String(o.href ?? o.Href ?? ''),
      };
    });
  });

  function open(href: string) {
    if (!href) return;
    if (/^https?:/i.test(href)) window.open(href, '_blank');
    else router.push(href.startsWith('/') ? href : `/${href}`);
  }

  return { items, open };
}
