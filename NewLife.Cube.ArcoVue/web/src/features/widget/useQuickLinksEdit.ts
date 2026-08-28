import { computed, ref, watch } from 'vue';
import type { QuickLinkPin } from '@/core/utils/quickLinks';

export interface QuickLinksEditProps {
  visible: boolean;
  menuLeaves: QuickLinkPin[];
  pins: QuickLinkPin[];
  saving?: boolean;
}

function normUrl(url: string): string {
  return url.trim().replace(/\/+$/, '').toLowerCase() || '/';
}

export function useQuickLinksEdit(
  props: QuickLinksEditProps,
  emit: {
    (e: 'update:visible', v: boolean): void;
    (e: 'save', pins: QuickLinkPin[]): void;
  },
) {
  const selected = ref<string[]>([]);
  const keyword = ref('');

  watch(
    () => [props.visible, props.pins, props.menuLeaves] as const,
    ([vis]) => {
      if (!vis) return;
      const pinUrls = new Set(props.pins.map((p) => normUrl(p.url)));
      if (pinUrls.size) {
        selected.value = props.menuLeaves.filter((m) => pinUrls.has(normUrl(m.url))).map((m) => m.url);
      } else {
        selected.value = [];
      }
      keyword.value = '';
    },
    { immediate: true },
  );

  const filtered = computed(() => {
    const q = keyword.value.trim().toLowerCase();
    if (!q) return props.menuLeaves;
    return props.menuLeaves.filter(
      (m) => m.name.toLowerCase().includes(q) || m.url.toLowerCase().includes(q),
    );
  });

  function toggle(url: string) {
    const i = selected.value.indexOf(url);
    if (i >= 0) selected.value = selected.value.filter((u) => u !== url);
    else selected.value = [...selected.value, url];
  }

  function isChecked(url: string) {
    return selected.value.includes(url);
  }

  function cancel() {
    emit('update:visible', false);
  }

  function confirm() {
    const byUrl = new Map(props.menuLeaves.map((m) => [m.url, m]));
    const pins = selected.value
      .map((url) => byUrl.get(url))
      .filter((x): x is QuickLinkPin => !!x);
    emit('save', pins);
  }

  function clearToDefault() {
    emit('save', []);
  }

  return {
    selected,
    keyword,
    filtered,
    toggle,
    isChecked,
    cancel,
    confirm,
    clearToDefault,
  };
}
