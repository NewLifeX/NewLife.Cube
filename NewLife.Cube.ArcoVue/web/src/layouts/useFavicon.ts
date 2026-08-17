import { watchEffect } from 'vue';
import { useAppStore } from '@/stores/app';
import { applyDocumentFavicon } from '@/core/utils/favicon';
import { resolveLoginLogoUrl } from '@/views/login/loginConfig';

/** 魔方设置「登录页 Logo」同步为浏览器页签图标 */
export function useFavicon() {
  const appStore = useAppStore();
  watchEffect(() => {
    applyDocumentFavicon(resolveLoginLogoUrl(appStore.loginConfig));
  });
}
