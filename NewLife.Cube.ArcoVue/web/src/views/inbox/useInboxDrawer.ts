import { computed, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import type { InboxMessageItem } from '@newlifex/api-core';
import cubeApi from '@/api';
import { formatDateTime } from '@/core/utils/datetime';
import { parseInboxUnreadCount, resolveInboxTotal } from '@/core/utils/inboxBadge';
import { bucketInboxByDate } from '@/core/utils/inboxBucket';
import { parseInboxTarget } from '@/core/utils/inboxTarget';
import { useAppStore } from '@/stores/app';

export type InboxRow = InboxMessageItem & { timeText: string };

/** 站内通知抽屉：按日期分桶的时间轴列表（今天/本周/本月/更长时间） + 已读 + 可解析 target 跳转 */
export function useInboxDrawer(visible: { value: boolean }) {
  const router = useRouter();
  const appStore = useAppStore();
  const loading = ref(false);
  const marking = ref(false);
  const items = ref<InboxRow[]>([]);
  const unreadCount = ref(0);
  const total = ref(0);

  /** 日期分桶（今天/本周/本月/更长时间，从近到远；空桶不输出） */
  const groups = computed(() => bucketInboxByDate(items.value));

  async function refreshUnread() {
    try {
      const res = await cubeApi.automation.inboxUnreadCount();
      unreadCount.value = parseInboxUnreadCount(res.data);
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
      total.value = resolveInboxTotal(res.page, list.length);
      await refreshUnread();
      const unreadInPage = list.filter((m) => !m.read).length;
      if (!unreadCount.value && unreadInPage) {
        unreadCount.value = unreadInPage;
        appStore.inboxUnreadCount = unreadInPage;
      }
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

  async function onItemClick(m: InboxRow) {
    if (!m.read && m.id) await markRead(m.id);
    const parsed = parseInboxTarget(m.target);
    if (!parsed) return;
    visible.value = false;
    await router.push({ path: parsed.path, query: { id: parsed.id } });
  }

  watch(
    () => visible.value,
    (v) => {
      if (v) void load();
    },
  );

  // 未读数由 ShellToolbar 统一拉取；抽屉仅在打开时 load，避免与顶栏双打 UnreadCount

  return {
    loading,
    marking,
    items,
    groups,
    unreadCount,
    total,
    load,
    markRead,
    markAllRead,
    onItemClick,
    refreshUnread,
  };
}
