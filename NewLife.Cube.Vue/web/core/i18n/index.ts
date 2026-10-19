import { createI18n } from 'vue-i18n';
import zhCN from './locales/zh-CN';
import zhTW from './locales/zh-TW';
import enUS from './locales/en-US';
import esES from './locales/es';
import deDE from './locales/de';
import jaJP from './locales/ja';
import frFR from './locales/fr';
import ptBR from './locales/pt';
import ruRU from './locales/ru';
import koKR from './locales/ko';
import arSA from './locales/ar';
/* Element Plus 组件（含 ElMessageBox / ElPagination 等）默认英文文案，
   必须显式传入与框架 i18n 一致的 locale 才会显示「确定 / 取消」 */
import epZhCn from 'element-plus/es/locale/lang/zh-cn';
import epZhTw from 'element-plus/es/locale/lang/zh-tw';
import epEnUs from 'element-plus/es/locale/lang/en';
import epEs from 'element-plus/es/locale/lang/es';
import epDe from 'element-plus/es/locale/lang/de';
import epJa from 'element-plus/es/locale/lang/ja';
import epFr from 'element-plus/es/locale/lang/fr';
import epPt from 'element-plus/es/locale/lang/pt';
import epRu from 'element-plus/es/locale/lang/ru';
import epKo from 'element-plus/es/locale/lang/ko';
import epAr from 'element-plus/es/locale/lang/ar';

/** 支持的语言。前三位固定为简体中文、繁體中文、英文，其余与 README 语种顺序一致 */
export const supportedLanguages = [
  { label: '中文', value: 'zh-CN' },
  { label: '繁體中文', value: 'zh-TW' },
  { label: 'English', value: 'en-US' },
  { label: 'Español', value: 'es-ES' },
  { label: 'Deutsch', value: 'de-DE' },
  { label: '日本語', value: 'ja-JP' },
  { label: 'Français', value: 'fr-FR' },
  { label: 'Português', value: 'pt-BR' },
  { label: 'Русский', value: 'ru-RU' },
  { label: '한국어', value: 'ko-KR' },
  { label: 'العربية', value: 'ar-SA' },
] as const;

export type CubeLocale = (typeof supportedLanguages)[number]['value'];

const supportedValues = supportedLanguages.map((item) => item.value) as readonly string[];

// 获取浏览器语言设置，命中支持列表则复用，否则回退英文
function getBrowserLanguage(): CubeLocale {
  const navigatorLanguage = navigator.language;
  const lower = navigatorLanguage.toLowerCase();
  const exact = supportedValues.find((item) => item.toLowerCase() === lower);
  if (exact) return exact as CubeLocale;

  // 中文按书写系统区分：Hant/TW/HK/MO 使用繁體，其余使用简体
  if (lower.startsWith('zh')) {
    const isHant = lower.includes('hant') || lower.endsWith('-tw') || lower.endsWith('-hk') || lower.endsWith('-mo');
    return isHant ? 'zh-TW' : 'zh-CN';
  }

  const prefix = lower.split('-')[0];
  const matched = supportedLanguages.find((item) => item.value.split('-')[0].toLowerCase() === prefix);
  return matched ? matched.value : 'en-US';
}

// 获取本地存储的语言设置或使用浏览器语言
function getLanguage(): CubeLocale {
  const stored = localStorage.getItem('cube-language');
  if (stored && supportedValues.includes(stored)) return stored as CubeLocale;
  return getBrowserLanguage();
}

// 创建 i18n 实例
const i18n = createI18n({
  legacy: false, // 使用组合式 API
  locale: getLanguage(),
  fallbackLocale: 'en-US', // 英文为源文与兜底
  messages: {
    'zh-CN': zhCN,
    'zh-TW': zhTW,
    'en-US': enUS,
    'es-ES': esES,
    'de-DE': deDE,
    'ja-JP': jaJP,
    'fr-FR': frFR,
    'pt-BR': ptBR,
    'ru-RU': ruRU,
    'ko-KR': koKR,
    'ar-SA': arSA,
  },
  silentTranslationWarn: true,
});

// 初始化页面语言属性，保证首屏 <html lang>/<html dir> 与当前语言一致
if (typeof document !== 'undefined') {
  const lang = getLanguage();
  document.querySelector('html')?.setAttribute('lang', lang);
  document.querySelector('html')?.setAttribute('dir', lang === 'ar-SA' ? 'rtl' : 'ltr');
}

// 导出 i18n 实例
export default i18n;

/**
 * 切换语言的工具函数
 * @param lang - 语言代码
 */
export function setLanguage(lang: CubeLocale) {
  i18n.global.locale.value = lang;
  localStorage.setItem('cube-language', lang);
  document.querySelector('html')?.setAttribute('lang', lang);
  document.querySelector('html')?.setAttribute('dir', lang === 'ar-SA' ? 'rtl' : 'ltr');
}

// 获取当前语言
export function getCurrentLanguage(): CubeLocale {
  return i18n.global.locale.value as CubeLocale;
}

/** 框架语言代码 → Element Plus locale 对象 */
export const elementPlusLocales = {
  'zh-CN': epZhCn,
  'zh-TW': epZhTw,
  'en-US': epEnUs,
  'es-ES': epEs,
  'de-DE': epDe,
  'ja-JP': epJa,
  'fr-FR': epFr,
  'pt-BR': epPt,
  'ru-RU': epRu,
  'ko-KR': epKo,
  'ar-SA': epAr,
} as const;

/**
 * 取当前语言对应的 Element Plus locale。
 * 未知语言回退中文（与 i18n 的 fallbackLocale 一致）。
 */
export function getEpLocale() {
  const lang = getCurrentLanguage();
  return elementPlusLocales[lang as keyof typeof elementPlusLocales] ?? epZhCn;
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
