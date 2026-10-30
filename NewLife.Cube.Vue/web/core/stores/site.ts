/**
 * 站点配置 Store
 *
 * 品牌字段已从登录配置拆到匿名接口 `/Cube/Site`，布局与导航需要与登录页使用同一份数据，
 * 避免服务端设置的系统名称/Logo 只在登录页生效。模块内共享请求，多个布局同时挂载时只请求一次。
 */
import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import type { SiteConfig } from '@newlifex/cube';
import { applySiteLanguage } from '../i18n';
import { site } from '../utils/request';

export const useSiteStore = defineStore('site', () => {
  const config = ref<SiteConfig | null>(null);
  const loading = ref(false);
  const loadedTenant = ref<string | undefined>();

  let pending: Promise<SiteConfig | null> | null = null;

  /**
   * 将站点名称写入浏览器标题
   * 站点名称为服务端配置的品牌名，标题随站点配置与服务端设置保持一致，避免各皮肤自行硬编码
   * @param title 站点名称，为空时保持原样
   */
  function applyTitle(title?: string) {
    if (typeof document === 'undefined') return;
    if (title) document.title = title;
  }

  /**
   * 将站点 Logo 写入浏览器图标
   * 站点未配置 Logo 时保持默认图标，避免白屏或空图标
   * @param logo 站点 Logo 地址，为空时保持原样
   */
  function applyFavicon(logo?: string) {
    if (typeof document === 'undefined' || !logo) return;

    let link = document.head.querySelector<HTMLLinkElement>('link[rel="icon"]');
    if (!link) {
      link = document.createElement('link');
      link.rel = 'icon';
      document.head.appendChild(link);
    }
    link.href = logo;
  }

  /**
   * 应用站点品牌与语言
   * @param value 站点配置
   */
  function applySite(value: SiteConfig) {
    applyTitle(value.name);
    applyFavicon(value.logo);
    applySiteLanguage(value.language);
  }

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
          applySite(res.data);
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
