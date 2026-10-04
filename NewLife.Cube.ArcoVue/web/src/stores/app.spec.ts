import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

const api = vi.hoisted(() => ({
  getLoginConfig: vi.fn(),
  getAiConfig: vi.fn(),
  getMapConfig: vi.fn(),
  inboxUnreadCount: vi.fn(),
  workflowMeta: vi.fn(),
}));

vi.mock('@/api', () => ({
  default: {
    user: { getLoginConfig: api.getLoginConfig },
    config: { getAiConfig: api.getAiConfig, getMapConfig: api.getMapConfig },
    automation: { inboxUnreadCount: api.inboxUnreadCount },
    workflow: { meta: api.workflowMeta },
  },
}));

vi.mock('@arco-design/web-vue', () => ({ Message: { error: vi.fn() } }));

import { useAppStore } from './app';

describe('app store metadata requests', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    api.getLoginConfig.mockReset();
    api.getAiConfig.mockReset();
    api.getMapConfig.mockReset();
    api.inboxUnreadCount.mockReset();
    api.workflowMeta.mockReset();
  });

  it('deduplicates stable metadata and reuses successful values until forced', async () => {
    api.getLoginConfig.mockResolvedValue({ data: { enableTenant: true } });
    const store = useAppStore();

    await Promise.all([store.fetchLoginConfig(), store.fetchLoginConfig()]);
    await store.fetchLoginConfig();
    expect(api.getLoginConfig).toHaveBeenCalledOnce();

    await store.fetchLoginConfig(true);
    expect(api.getLoginConfig).toHaveBeenCalledTimes(2);
  });

  it('deduplicates dynamic badge refreshes but requests again after completion', async () => {
    api.inboxUnreadCount.mockResolvedValue({ data: { count: 3 } });
    api.workflowMeta.mockResolvedValue({ data: { enabled: true, todoCount: 2 } });
    const store = useAppStore();

    await Promise.all([store.refreshInboxUnread(), store.refreshInboxUnread()]);
    await Promise.all([store.refreshWorkflowMeta(), store.refreshWorkflowMeta()]);
    expect(api.inboxUnreadCount).toHaveBeenCalledOnce();
    expect(api.workflowMeta).toHaveBeenCalledOnce();

    await store.refreshInboxUnread();
    await store.refreshWorkflowMeta();
    expect(api.inboxUnreadCount).toHaveBeenCalledTimes(2);
    expect(api.workflowMeta).toHaveBeenCalledTimes(2);
  });

  it('地图配置缓存并支持会话清理后重取', async () => {
    api.getMapConfig.mockResolvedValue({ data: { provider: 'amap', key: 'k' } });
    const store = useAppStore();

    await Promise.all([store.fetchMapConfig(), store.fetchMapConfig()]);
    await store.fetchMapConfig();
    expect(api.getMapConfig).toHaveBeenCalledOnce();
    expect(store.mapConfig.provider).toBe('amap');

    store.clearSessionMetadata();
    expect(store.mapConfig.provider).toBeNull();

    await store.fetchMapConfig();
    expect(api.getMapConfig).toHaveBeenCalledTimes(2);
  });

  it('force=true abandons in-flight LoginConfig and keeps later response', async () => {
    let resolveFirst!: (value: unknown) => void;
    const first = new Promise((resolve) => {
      resolveFirst = resolve;
    });
    api.getLoginConfig
      .mockImplementationOnce(() => first)
      .mockResolvedValueOnce({ data: { enableTenant: false, name: 'later' } });
    const store = useAppStore();

    const p1 = store.fetchLoginConfig();
    const p2 = store.fetchLoginConfig(true);
    expect(api.getLoginConfig).toHaveBeenCalledTimes(2);

    resolveFirst({ data: { enableTenant: true, name: 'stale' } });
    await Promise.all([p1, p2]);
    expect(store.loginConfig?.name).toBe('later');
    expect(store.loginConfig?.enableTenant).toBe(false);
  });
});
