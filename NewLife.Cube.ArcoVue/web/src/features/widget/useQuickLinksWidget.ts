import { computed, inject, ref } from 'vue';
import { Message } from '@arco-design/web-vue';
import { useRouter } from 'vue-router';
import { useUserStore } from '@/stores/user';
import {
  menuLeavesForPins,
  readQuickLinkPins,
  readQuickLinkServer,
  resolveQuickLinksDisplay,
  type QuickLinkPin,
} from '@/core/utils/quickLinks';
import { WIDGET_SURFACE_KEY, type WidgetCardProps } from './context';

export type { QuickLinkPin };

export function useQuickLinksWidget(props: WidgetCardProps) {
  const router = useRouter();
  const userStore = useUserStore();
  const surface = inject(WIDGET_SURFACE_KEY, null);
  const editVisible = ref(false);
  const saving = ref(false);

  const serverLinks = computed(() => readQuickLinkServer(props.result));
  const pins = computed(() => readQuickLinkPins(props.widget.query));
  const menuLeaves = computed(() => menuLeavesForPins(userStore.menus));

  const links = computed(() =>
    resolveQuickLinksDisplay(pins.value, serverLinks.value, menuLeaves.value),
  );

  function open(url: string) {
    if (!url) return;
    if (/^https?:/i.test(url)) window.open(url, '_blank');
    else router.push(url.startsWith('/') ? url : `/${url}`);
  }

  function openEdit() {
    if (!userStore.menus?.length) void userStore.fetchMenus();
    editVisible.value = true;
  }

  function closeEdit() {
    editVisible.value = false;
  }

  async function savePins(next: QuickLinkPin[]) {
    if (!surface?.saveDashboard) {
      Message.error('无法保存快捷入口');
      return;
    }
    saving.value = true;
    try {
      const dash = surface.dashboard;
      const widgets = (dash.widgets ?? []).map((w) => {
        if (w.id !== props.widget.id) return w;
        const query = { ...(w.query ?? {}) } as Record<string, unknown>;
        if (next.length) query.pins = next;
        else delete query.pins;
        return { ...w, query };
      });
      await surface.saveDashboard({ ...dash, version: 1, widgets });
      Message.success(next.length ? '快捷入口已更新' : '已恢复默认快捷入口');
      editVisible.value = false;
    } catch (e) {
      Message.error(e instanceof Error ? e.message : '保存失败');
    } finally {
      saving.value = false;
    }
  }

  return {
    links,
    pins,
    menuLeaves,
    editVisible,
    saving,
    open,
    openEdit,
    closeEdit,
    savePins,
  };
}
