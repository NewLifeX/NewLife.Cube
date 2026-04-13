import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { api } from '@/api';
import type { MenuItem } from '@cube/api-core';

export const useUserStore = defineStore('user', () => {
  const user = ref<any>(null);
  const menus = ref<MenuItem[]>([]);

  const isLoggedIn = computed(() => !!user.value);
  const displayName = computed(() => user.value?.displayName || user.value?.name || '');

  async function login(username: string, password: string) {
    const res = await api.user.login(username, password);
    if (res?.data) { user.value = res.data; return true; }
    return false;
  }

  async function logout() {
    try { await api.user.logout(); } catch { /* ignore */ }
    user.value = null;
    menus.value = [];
  }

  async function fetchUserInfo() {
    try {
      const res = await api.user.info();
      if (res?.data) user.value = res.data;
    } catch { /* ignore */ }
  }

  async function fetchMenus() {
    try {
      const res = await api.menu.getMenuTree();
      if (res?.data) menus.value = res.data;
    } catch { /* ignore */ }
  }

  return { user, menus, isLoggedIn, displayName, login, logout, fetchUserInfo, fetchMenus };
});
