import { createI18n } from 'vue-i18n';
import zhCN from './locales/zh-CN';
import enUS from './locales/en-US';

// 获取浏览器语言设置
function getBrowserLanguage() {
  const navigatorLanguage = navigator.language;
  if (navigatorLanguage.startsWith('zh')) {
    return 'zh-CN';
  }
  return 'en-US';
}

// 获取本地存储的语言设置或使用浏览器语言
function getLanguage() {
  return localStorage.getItem('cube-language') || getBrowserLanguage();
}

// 创建 i18n 实例
const i18n = createI18n({
  legacy: false, // 使用组合式 API
  locale: getLanguage(),
  fallbackLocale: 'zh-CN', // 设置回退语言
  messages: {
    'zh-CN': zhCN,
    'en-US': enUS,
  },
  silentTranslationWarn: true,
});

// 导出 i18n 实例
export default i18n;

/**
 * 切换语言的工具函数
 * @param {string} lang - 语言代码
 */
export function setLanguage(lang: typeof i18n.global.locale.value) {
  i18n.global.locale.value = lang;
  localStorage.setItem('cube-language', lang);
  // 可以在这里添加其他语言切换逻辑，比如更新 HTML 标签的 lang 属性
  document.querySelector('html')?.setAttribute('lang', lang);
}

// 获取当前语言
export function getCurrentLanguage() {
  return i18n.global.locale.value;
}

// 创建一个与 react-intl-universal 兼容的接口
export const intl = {
  get: (key: string) => {
    return {
      d: (defaultValue: string) => {
        const message = i18n.global.t(key);
        return message !== key ? message : defaultValue;
      },
    };
  },
  getHTML: (key: string) => {
    return {
      d: (defaultValue: string) => {
        const message = i18n.global.t(key);
        return message !== key ? message : defaultValue;
      },
    };
  },
  formatMessage: (options: { id: string; defaultMessage?: string }) => {
    const message = i18n.global.t(options.id);
    return message !== options.id ? message : options.defaultMessage || options.id;
  },
  formatHTMLMessage: (options: { id: string; defaultMessage?: string }) => {
    const message = i18n.global.t(options.id);
    return message !== options.id ? message : options.defaultMessage || options.id;
  },
  getLocale: () => i18n.global.locale.value,
  determineLocale: () => i18n.global.locale.value,
};
