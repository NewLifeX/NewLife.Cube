import { writable, derived } from 'svelte/store';
import type { SiteConfig } from '@newlifex/api-core';

export const collapsed = writable(false);
export const darkMode = writable(false);
export const siteConfig = writable<SiteConfig | null>(null);

export const siteTitle = derived(siteConfig, ($c) => $c?.name || '魔方管理平台');

// 深色模式切换
darkMode.subscribe((v) => {
  if (typeof document !== 'undefined') {
    document.documentElement.classList.toggle('dark', v);
  }
});
