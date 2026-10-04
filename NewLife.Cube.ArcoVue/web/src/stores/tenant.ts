import { defineStore } from 'pinia';
import type { TenantItem, TenantListResult } from '@newlifex/api-core';
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
    _loadRequest: null as Promise<void> | null,
    _sessionGeneration: 0,
    /** force 世代：作废本资源 in-flight（如快速 switchTo） */
    _loadGen: 0,
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
    async load(force = false) {
      if (!force && this.loaded) return;
      if (!force && this._loadRequest) return this._loadRequest;
      if (force) {
        this._loadGen++;
        this._loadRequest = null;
        this.loaded = false;
      }
      const sessionGen = this._sessionGeneration;
      const loadGen = this._loadGen;
      const request = cubeApi.user
        .listTenants()
        .then((res) => {
          if (sessionGen !== this._sessionGeneration || loadGen !== this._loadGen) return;
          this.applyResult(res.data);
          this.loaded = true;
        })
        .catch(() => {
          if (sessionGen !== this._sessionGeneration || loadGen !== this._loadGen) return;
          this.loaded = false;
        })
        .finally(() => {
          if (this._loadRequest === request) this._loadRequest = null;
        });
      this._loadRequest = request;
      return request;
    },
    async switchTo(tenantId: number) {
      // 后端 SwitchTenant 仅返回布尔结果，切换成功后重新拉取列表，刷新 currentId/currentCode/items
      await cubeApi.user.switchTenant(tenantId);
      await this.load(true);
    },
    clear() {
      this._sessionGeneration++;
      this._loadGen++;
      this.currentId = 0;
      this.items = [];
      this.loaded = false;
      this._loadRequest = null;
      this.persistCode('');
      this.persistEnable(true);
    },
  },
});
