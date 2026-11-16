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
  const logo = computed(() => site.value.logo || '');
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

  return {
    site,
    siteName,
    logo,
    loginBackground,
    loginTip,
    copyright,
    load
  };
});
