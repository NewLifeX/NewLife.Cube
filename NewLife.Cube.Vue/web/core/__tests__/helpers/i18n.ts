import { createI18n } from 'vue-i18n';
import zhCN from '../../i18n/locales/zh-CN';
import enUS from '../../i18n/locales/en-US';

/**
 * 组件测试用 i18n 插件。
 *
 * 组件里用 `useI18n()` 取文案，测试挂载时必须提供 i18n 插件，否则 setup 会抛
 * "Need to install with app.use()" 。这里固定 zh-CN 为主语言，断言可直接用中文文案；
 * 需要验证切换语言时把 `locale` 改成 `en-US` 即可。
 */
export const testI18n = createI18n({
  legacy: false,
  locale: 'zh-CN',
  fallbackLocale: 'en-US',
  messages: { 'zh-CN': zhCN, 'en-US': enUS },
});

/** 便于在 mount 的 global.plugins 里直接展开 */
export const testI18nPlugins = [testI18n];
