/**
 * 站点配置 Store
 *
 * 品牌字段已从登录配置拆到匿名接口 `/Cube/Site`，布局与导航需要与登录页使用同一份数据，
 * 避免服务端设置的系统名称/Logo 只在登录页生效。模块内共享请求，多个布局同时挂载时只请求一次。
 */
import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import type { SiteConfig } from '@newlifex/api-core';
import { site } from '../utils/request';

export const useSiteStore = defineStore('site', () => {
  const config = ref<SiteConfig | null>(null);
  const loading = ref(false);
  const loadedTenant = ref<string | undefined>();

  let pending: Promise<SiteConfig | null> | null = null;

  /**
   * 加载站点配置
   * @param tenant 租户标识（编号/编码/名称），为空时由服务端按当前上下文解析
   * @param force 强制重新请求，忽略已有缓存
   * @returns 站点配置，失败时返回已有配置或 null
   */
  async function load(tenant?: string, force = false): Promise<SiteConfig | null> {
    if (!force && config.value && loadedTenant.value === tenant) return config.value;
    if (pending) return pending;

    loading.value = true;
    pending = site
      .getSite(tenant)
      .then((res) => {
        if (res?.code === 0 && res.data) {
          config.value = res.data;
          loadedTenant.value = tenant;
        }
        return config.value;
      })
      .catch(() => config.value)
      .finally(() => {
        pending = null;
        loading.value = false;
      });

    return pending;
  }

  const name = computed(() => config.value?.name || '');
  const logo = computed(() => config.value?.logo || '');

  return { config, loading, loadedTenant, name, logo, load };
});
