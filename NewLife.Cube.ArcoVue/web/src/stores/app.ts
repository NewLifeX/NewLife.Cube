import { defineStore } from 'pinia';
import type { LoginConfig } from '@cube/api-core';
import cubeApi from '@/api';

/** 应用级状态（登录配置等）。布局/主题请用 userProfileStore。 */
export const useAppStore = defineStore('app', {
  state: () => ({
    loginConfig: null as LoginConfig | null,
  }),
  actions: {
    async fetchLoginConfig() {
      const res = await cubeApi.user.getLoginConfig();
      if (res.data) this.loginConfig = res.data;
    },
  },
});
