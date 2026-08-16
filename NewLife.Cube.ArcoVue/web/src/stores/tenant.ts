import { defineStore } from 'pinia';
import type { TenantItem, TenantListResult } from '@cube/api-core';
import cubeApi from '@/api';

const CODE_KEY = 'cube.tenant.code';

function readPersistedCode(): string {
  try {
    return sessionStorage.getItem(CODE_KEY) ?? '';
  } catch {
    return '';
  }
}

export const useTenantStore = defineStore('tenant', {
  state: () => ({
    currentId: 0 as number,
    currentCode: readPersistedCode(),
    items: [] as TenantItem[],
    loaded: false,
  }),
  getters: {
    /** 顶栏展示名 */
    currentLabel(state): string {
      const hit = state.items.find((i) => i.id === state.currentId);
      return hit?.name || (state.currentCode ? state.currentCode : '平台');
    },
    enabled(state): boolean {
      return state.items.length > 0;
    },
  },
  actions: {
    persistCode(code: string) {
      this.currentCode = code;
      try {
        if (code) sessionStorage.setItem(CODE_KEY, code);
        else sessionStorage.removeItem(CODE_KEY);
      } catch { /* ignore */ }
    },
    applyResult(data: TenantListResult | null | undefined) {
      if (!data) return;
      this.currentId = data.currentId ?? 0;
      this.items = data.items || [];
      this.persistCode(data.currentCode ?? '');
      this.loaded = true;
    },
    async load() {
      try {
        const res = await cubeApi.user.listTenants();
        this.applyResult(res.data);
      } catch {
        this.loaded = true;
      }
    },
    async switchTo(tenantId: number) {
      const res = await cubeApi.user.switchTenant(tenantId);
      this.applyResult(res.data);
    },
    clear() {
      this.currentId = 0;
      this.items = [];
      this.loaded = false;
      this.persistCode('');
    },
  },
});
