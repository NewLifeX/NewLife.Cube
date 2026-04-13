import { writable, derived } from 'svelte/store';
import type { SiteInfo } from '@cube/api-core';

export const collapsed = writable(false);
export const darkMode = writable(false);
export const siteInfo = writable<SiteInfo | null>(null);

export const siteTitle = derived(siteInfo, ($s) => $s?.displayName || '魔方管理平台');

// 深色模式切换
darkMode.subscribe((v) => {
  if (typeof document !== 'undefined') {
    document.documentElement.classList.toggle('dark', v);
  }
});
