import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

const api = vi.hoisted(() => ({ getUserProfile: vi.fn() }));

vi.mock('@/api', () => ({
  default: { profile: { getUserProfile: api.getUserProfile } },
}));

vi.mock('@/core/utils/userProfile', () => ({
  SYSTEM_DEFAULT_PROFILE: { layout: {}, theme: {}, workspace: {} },
  cloneProfile: (value: unknown) => JSON.parse(JSON.stringify(value)),
  clearLocalProfile: vi.fn(),
  loadLocalProfile: () => null,
  mergeProfile: (value: unknown) => value,
  prefsFromWire: (value: { data?: unknown }) => value.data,
  prefsToWirePayload: (value: unknown) => value,
  saveLocalProfile: vi.fn(),
}));

vi.mock('@/theme/applyTheme', () => ({
  applyTheme: vi.fn(),
  watchSystemAppearance: () => () => undefined,
}));

vi.mock('@arco-design/web-vue', () => ({ Message: { error: vi.fn() } }));

import { useUserProfileStore } from './userProfile';

describe('user profile metadata loading', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    api.getUserProfile.mockReset();
  });

  it('deduplicates concurrent loads, caches success, and reloads after reset', async () => {
    api.getUserProfile
      .mockResolvedValueOnce({ data: { layout: {}, theme: {}, workspace: {} } })
      .mockResolvedValueOnce({ data: { layout: { mode: 'top' }, theme: {}, workspace: {} } });
    const store = useUserProfileStore();

    await Promise.all([store.loadFromServer(), store.loadFromServer()]);
    await store.loadFromServer();
    expect(api.getUserProfile).toHaveBeenCalledOnce();

    store.resetSession();
    await store.loadFromServer();
    expect(api.getUserProfile).toHaveBeenCalledTimes(2);
  });
});
