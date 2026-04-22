import { writable, derived } from 'svelte/store';
import type { LoginConfig } from '@cube/api-core';

export const collapsed = writable(false);
export const darkMode = writable(false);
export const loginConfig = writable<LoginConfig | null>(null);

export const siteTitle = derived(loginConfig, ($c) => $c?.name || '魔方管理平台');

// 深色模式切换
darkMode.subscribe((v) => {
  if (typeof document !== 'undefined') {
    document.documentElement.classList.toggle('dark', v);
  }
});
