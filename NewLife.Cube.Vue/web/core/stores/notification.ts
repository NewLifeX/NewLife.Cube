/**
 * 消息通知 Store
 *
 * 统一维护未读数、最近通知、分页列表与实时连接。实时优先使用 SSE（票据鉴权），
 * 连接不可用时自动降级为 60 秒轮询，保证移动端与代理环境仍能收到通知。
 */
import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import { ElNotification } from 'element-plus';
import request from '../utils/request';
import { getConfig } from '../configure';

/** 通知列表项 */
export interface NotificationItem {
  id: number;
  bizId: string;
  shared: boolean;
  title: string;
  content: string;
  category?: string;
  level?: string;
  link?: string;
  read: boolean;
  readTime?: string;
  readChannel?: string;
  handled?: boolean;
  handledBy?: number;
  handledTime?: string;
  createTime?: string;
}

/** 分页查询参数 */
export interface NotificationQuery {
  pageIndex?: number;
  pageSize?: number;
  category?: string;
  read?: boolean;
  key?: string;
}

interface ApiBody<T> {
  code?: number;
  message?: string;
  data?: T;
}

interface PageBody<T> {
  pageIndex: number;
  pageSize: number;
  totalCount: number;
  items: T[];
}

const POLL_INTERVAL = 60_000;
const RECONNECT_DELAY = 15_000;

export const useNotificationStore = defineStore('notification', () => {
  const unread = ref(0);
  const recent = ref<NotificationItem[]>([]);
  const list = ref<NotificationItem[]>([]);
  const total = ref(0);
  const pageIndex = ref(1);
  const pageSize = ref(20);
  const loading = ref(false);
  const realtime = ref(false);
  const started = ref(false);

  let source: EventSource | null = null;
  let pollTimer: number | null = null;
  let reconnectTimer: number | null = null;
  let lastId = 0;
  let reconnectCount = 0;
  let channel: BroadcastChannel | null = null;
  let consumers = 0;

  const hasUnread = computed(() => unread.value > 0);

  function buildStreamUrl(ticket: string): string {
    const base = (getConfig().request.baseUrl || '').replace(/\/$/, '');
    return `${base}/Notify/Stream?ticket=${encodeURIComponent(ticket)}&lastId=${lastId}`;
  }

  async function refreshUnread(): Promise<void> {
    try {
      const res = (await request.get('/Notify/Count')) as ApiBody<{ count: number; }>;
      if (res?.code === 0) unread.value = res.data?.count || 0;
    } catch {
      // 轮询/实时失败不打断页面，下次重试
    }
  }

  async function loadRecent(size = 8): Promise<void> {
    try {
      const res = (await request.get('/Notify/Recent', { params: { size } })) as ApiBody<NotificationItem[]>;
      if (res?.code === 0) recent.value = res.data || [];
    } catch {
      // 静默降级
    }
  }

  async function loadList(query: NotificationQuery = {}): Promise<void> {
    loading.value = true;
    try {
      const params = {
        pageIndex: query.pageIndex ?? pageIndex.value,
        pageSize: query.pageSize ?? pageSize.value,
        category: query.category,
        read: query.read,
        key: query.key,
      };
      const res = (await request.get('/Notify/List', { params })) as ApiBody<PageBody<NotificationItem>>;
      if (res?.code === 0 && res.data) {
        list.value = res.data.items || [];
        total.value = res.data.totalCount || 0;
        pageIndex.value = res.data.pageIndex || params.pageIndex;
        pageSize.value = res.data.pageSize || params.pageSize;
      }
    } finally {
      loading.value = false;
    }
  }

  async function initLastId(): Promise<void> {
    try {
      const res = (await request.get('/Notify/List', { params: { pageIndex: 1, pageSize: 1 } })) as ApiBody<PageBody<NotificationItem>>;
      lastId = res?.data?.items?.[0]?.id || 0;
    } catch {
      lastId = 0;
    }
  }

  function notifyDesktop(item: NotificationItem): void {
    const level = (item.level || 'Info').toLowerCase();
    if (!['warning', 'error', 'urgent'].includes(level)) return;

    // 已授权浏览器通知时走系统级提示；否则用站内 ElNotification 兜底
    if (typeof window !== 'undefined' && 'Notification' in window && window.Notification.permission === 'granted') {
      const notice = new window.Notification(item.title, {
        body: item.content,
        tag: item.bizId || String(item.id),
        requireInteraction: level === 'urgent',
      });
      notice.onclick = () => {
        window.focus();
        if (item.link) window.location.assign(item.link);
      };
      return;
    }

    const type = level === 'urgent' ? 'error' : (level as 'warning' | 'error');
    ElNotification({
      type,
      title: item.title,
      message: item.content,
      duration: 6000,
      onClick: () => {
        if (item.link) window.location.assign(item.link);
      },
    });
  }

  async function requestSystemPermission(): Promise<void> {
    if (typeof window === 'undefined' || !('Notification' in window)) return;
    if (window.Notification.permission !== 'default') return;

    try {
      await window.Notification.requestPermission();
    } catch {
      // 用户拒绝或浏览器不支持，不影响站内提示
    }
  }

  function onEvent(event: MessageEvent): void {
    try {
      const item = JSON.parse(event.data) as NotificationItem;
      if (!item?.id) return;

      lastId = Math.max(lastId, item.id);
      if (!item.read) {
        unread.value += 1;
        recent.value = [item, ...recent.value.filter((e) => e.id !== item.id)].slice(0, 8);
      }
      notifyDesktop(item);
      channel?.postMessage({ type: 'changed' });
    } catch {
      // 忽略格式异常，保持连接
    }
  }

  function onCount(event: MessageEvent): void {
    try {
      const payload = JSON.parse(event.data) as { count?: number; };
      if (typeof payload.count !== 'number' || payload.count === unread.value) return;

      unread.value = payload.count;
      // 其它终端已读或团队消息被处理时，重新拉取最近列表
      loadRecent();
    } catch {
      // 忽略格式异常，保持连接
    }
  }

  function closeSource(): void {
    if (source) {
      source.removeEventListener('notification', onEvent as EventListener);
      source.removeEventListener('count', onCount as EventListener);
      source.close();
      source = null;
    }
    realtime.value = false;
  }

  async function connect(): Promise<void> {
    closeSource();
    try {
      const res = (await request.post('/Notify/StreamTicket')) as ApiBody<{ ticket: string; }>;
      const ticket = res?.data?.ticket;
      if (!ticket) throw new Error('No ticket');

      source = new EventSource(buildStreamUrl(ticket), { withCredentials: true });
      source.addEventListener('notification', onEvent as EventListener);
      source.addEventListener('count', onCount as EventListener);
      source.onopen = () => {
        realtime.value = true;
        reconnectCount = 0;
        stopPolling();
      };
      source.onerror = () => {
        closeSource();
        startPolling();
        scheduleReconnect();
      };
    } catch {
      realtime.value = false;
      startPolling();
      scheduleReconnect();
    }
  }

  function scheduleReconnect(): void {
    if (reconnectTimer != null) return;

    reconnectCount += 1;
    const delay = reconnectCount <= 3 ? RECONNECT_DELAY : POLL_INTERVAL;
    reconnectTimer = window.setTimeout(() => {
      reconnectTimer = null;
      if (started.value && document.visibilityState !== 'hidden') connect();
    }, delay);
  }

  function startPolling(): void {
    if (pollTimer != null) return;

    pollTimer = window.setInterval(() => {
      refreshUnread();
      loadRecent();
    }, POLL_INTERVAL);
  }

  function stopPolling(): void {
    if (pollTimer != null) {
      window.clearInterval(pollTimer);
      pollTimer = null;
    }
  }

  function onVisible(): void {
    if (document.visibilityState !== 'visible') return;

    refreshUnread();
    loadRecent();
    if (!source) connect();
  }

  async function start(): Promise<void> {
    consumers += 1;
    if (started.value) return;
    started.value = true;

    await Promise.all([refreshUnread(), loadRecent()]);
    await initLastId();

    if (typeof BroadcastChannel !== 'undefined') {
      channel = new BroadcastChannel('cube-notify');
      channel.onmessage = () => {
        refreshUnread();
        loadRecent();
      };
    }

    document.addEventListener('visibilitychange', onVisible);
    connect();
  }

  function stop(): void {
    consumers = Math.max(0, consumers - 1);
    if (consumers > 0) return;

    started.value = false;
    closeSource();
    stopPolling();
    if (reconnectTimer != null) {
      window.clearTimeout(reconnectTimer);
      reconnectTimer = null;
    }
    document.removeEventListener('visibilitychange', onVisible);
    channel?.close();
    channel = null;
  }

  async function markRead(item: NotificationItem, readChannel = 'InApp'): Promise<void> {
    if (!item.bizId && !item.id) return;

    await request.post('/Notify/Read', { bizId: item.bizId, id: item.id, channel: readChannel });

    const apply = (target: NotificationItem) => {
      if (target.bizId === item.bizId || target.id === item.id) {
        target.read = true;
        target.readChannel = readChannel;
      }
    };
    recent.value.forEach(apply);
    list.value.forEach(apply);
    refreshUnread();
    channel?.postMessage({ type: 'changed' });
  }

  async function markAllRead(): Promise<number> {
    const res = (await request.post('/Notify/ReadAll')) as ApiBody<{ count: number; }>;
    const count = res?.data?.count || 0;
    recent.value = recent.value.filter((e) => e.shared);
    list.value.forEach((e) => {
      if (!e.shared) e.read = true;
    });
    refreshUnread();

    return count;
  }

  async function handle(item: NotificationItem, result = ''): Promise<void> {
    await request.post('/Notify/Handle', { bizId: item.bizId, result });
    recent.value = recent.value.filter((e) => e.bizId !== item.bizId);
    list.value.forEach((e) => {
      if (e.bizId === item.bizId) {
        e.handled = true;
      }
    });
    refreshUnread();
  }

  async function remove(item: NotificationItem): Promise<void> {
    await request.post('/Notify/Delete', { id: item.id });
    recent.value = recent.value.filter((e) => e.id !== item.id);
    list.value = list.value.filter((e) => e.id !== item.id);
    total.value = Math.max(0, total.value - 1);
    refreshUnread();
  }

  async function loadPreferences(): Promise<Array<{ category: string; channels: string[]; }>> {
    const res = (await request.get('/Notify/Preferences')) as ApiBody<Array<{ category: string; channels: string[]; }>>;

    return res?.data || [];
  }

  async function savePreferences(category: string, channels: string[]): Promise<void> {
    await request.post('/Notify/SavePreferences', { category, channels: channels.join(',') });
  }

  return {
    unread,
    recent,
    list,
    total,
    pageIndex,
    pageSize,
    loading,
    realtime,
    hasUnread,
    start,
    stop,
    refreshUnread,
    loadRecent,
    loadList,
    markRead,
    markAllRead,
    handle,
    remove,
    loadPreferences,
    savePreferences,
    requestSystemPermission,
  };
});
