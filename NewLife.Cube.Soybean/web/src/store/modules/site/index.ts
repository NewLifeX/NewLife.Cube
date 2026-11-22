import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import type { SiteConfig } from '@newlifex/api-core';
import { cubeApi } from '@/api/cube';
import { SetupStoreId } from '@/enum';
import { $t } from '@/locales';

/** 站点品牌配置（/Cube/Site，匿名可访问） */
export const useSiteStore = defineStore(SetupStoreId.Site, () => {
  const site = ref<SiteConfig>({});

  let loading: Promise<void> | null = null;

  const siteName = computed(() => site.value.name || $t('system.title'));
  /** 后台配置的 Logo 地址，协议相对地址补全为当前协议，其余原样使用 */
  const logo = computed(() => {
    const value = site.value.logo || '';
    if (!value) return '';
    if (/^https?:\/\//i.test(value)) return value;
    if (value.startsWith('//')) return `${window.location.protocol}${value}`;
    return value;
  });
  const loginBackground = computed(() => site.value.loginBackground || '');
  const loginTip = computed(() => site.value.loginTip || '');
  const copyright = computed(() => site.value.copyright || '');

  /** 加载站点配置（仅首次请求，失败静默回退默认品牌） */
  async function load() {
    if (loading) return loading;

    loading = cubeApi.site
      .getSite()
      .then(res => {
        site.value = res.data ?? {};
      })
      .catch(() => {
        loading = null;
      });

    return loading;
  }

  /** 重新拉取站点配置（后台保存品牌配置后调用，避免整页刷新） */
  async function refresh() {
    loading = null;
    site.value = (await cubeApi.site.getSite(undefined, true).catch(() => null))?.data ?? site.value;
  }

  return {
    site,
    siteName,
    logo,
    loginBackground,
    loginTip,
    copyright,
    load,
    refresh
  };
});
