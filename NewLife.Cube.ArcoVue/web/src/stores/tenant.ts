import { defineStore } from 'pinia';
import type { TenantItem, TenantListResult } from '@cube/api-core';
import cubeApi from '@/api';

const CODE_KEY = 'cube.tenant.code';
/** 多租户总开关（与 LoginConfig / Tenants.enableTenant 同步）；关则不发 X-Tenant */
const ENABLE_KEY = 'cube.tenant.enabled';

function readPersistedCode(): string {
  try {
    return sessionStorage.getItem(CODE_KEY) ?? '';
  } catch {
    return '';
  }
}

function readPersistedEnable(): boolean {
  try {
    const v = sessionStorage.getItem(ENABLE_KEY);
    if (v === '0') return false;
    if (v === '1') return true;
  } catch {
    /* ignore */
  }
  // 未写入前默认 true，避免首屏误伤；Tenants/LoginConfig 返回后校正
  return true;
}

export const useTenantStore = defineStore('tenant', {
  state: () => ({
    /** 魔方设置 EnableTenant；关闭后全站隐藏租户 UI 且不带头 */
    enableTenant: readPersistedEnable(),
    currentId: 0 as number,
    currentCode: readPersistedCode(),
    items: [] as TenantItem[],
    loaded: false,
  }),
  getters: {
    /** 用户菜单展示的当前租户名 */
    currentLabel(state): string {
      const hit = state.items.find((i) => i.id === state.currentId);
      return hit?.name || (state.currentCode ? state.currentCode : '平台');
    },
    /** 是否提供租户切换：总开关开启且有可选项 */
    enabled(state): boolean {
      return state.enableTenant && state.items.length > 0;
    },
  },
  actions: {
    persistEnable(on: boolean) {
      this.enableTenant = on;
      try {
        sessionStorage.setItem(ENABLE_KEY, on ? '1' : '0');
      } catch {
        /* ignore */
      }
      if (!on) this.persistCode('');
    },
    persistCode(code: string) {
      this.currentCode = code;
      try {
        if (code) sessionStorage.setItem(CODE_KEY, code);
        else sessionStorage.removeItem(CODE_KEY);
      } catch {
        /* ignore */
      }
    },
    /** 从 LoginConfig 同步总开关（登录页/壳层） */
    applyFeatureFlag(enableTenant: boolean | undefined | null) {
      if (enableTenant == null) return;
      this.persistEnable(!!enableTenant);
      if (!enableTenant) {
        this.currentId = 0;
        this.items = [];
      }
    },
    applyResult(data: TenantListResult | null | undefined) {
      if (!data) return;
      const on = data.enableTenant !== false;
      this.persistEnable(on);
      if (!on) {
        this.currentId = 0;
        this.items = [];
        this.loaded = true;
        return;
      }
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
      this.persistEnable(true);
    },
  },
});
