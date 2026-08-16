import { onMounted, ref, watch } from 'vue';
import type { InboxMessageItem } from '@cube/api-core';
import cubeApi from '@/api';
import { formatDateTime } from '@/core/utils/datetime';
import { useAppStore } from '@/stores/app';

export type InboxRow = InboxMessageItem & { timeText: string };

/** 站内通知抽屉：时间轴列表 + 已读 */
export function useInboxDrawer(visible: { value: boolean }) {
  const appStore = useAppStore();
  const loading = ref(false);
  const marking = ref(false);
  const items = ref<InboxRow[]>([]);
  const unreadCount = ref(0);
  const total = ref(0);

  async function refreshUnread() {
    try {
      const res = await cubeApi.automation.inboxUnreadCount();
      unreadCount.value = Number(res.data?.count ?? 0);
      appStore.inboxUnreadCount = unreadCount.value;
    } catch {
      unreadCount.value = 0;
      appStore.inboxUnreadCount = 0;
    }
  }

  async function load() {
    loading.value = true;
    try {
      const res = await cubeApi.automation.inbox({ pageIndex: 1, pageSize: 40 });
      const list = res.data ?? [];
      items.value = list.map((m) => ({
        ...m,
        timeText: formatDateTime(m.createTime) || '',
      }));
      total.value = Number(res.page?.totalCount ?? list.length);
      await refreshUnread();
    } catch {
      items.value = [];
    } finally {
      loading.value = false;
    }
  }

  async function markRead(id: number) {
    try {
      await cubeApi.automation.markInboxRead({ id });
      const row = items.value.find((x) => x.id === id);
      if (row) row.read = true;
      await refreshUnread();
    } catch {
      /* ignore */
    }
  }

  async function markAllRead() {
    marking.value = true;
    try {
      await cubeApi.automation.markInboxRead({ all: true });
      items.value.forEach((x) => {
        x.read = true;
      });
      unreadCount.value = 0;
      appStore.inboxUnreadCount = 0;
    } finally {
      marking.value = false;
    }
  }

  watch(
    () => visible.value,
    (v) => {
      if (v) void load();
    },
  );

  onMounted(() => {
    void refreshUnread();
  });

  return {
    loading,
    marking,
    items,
    unreadCount,
    total,
    load,
    markRead,
    markAllRead,
    refreshUnread,
  };
}
