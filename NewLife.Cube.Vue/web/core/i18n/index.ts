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

/** 站点下发的语言设置（`/Cube/Site` 的 language 字段） */
export interface SiteLanguageInput {
  /** 当前请求解析出的语言 */
  current?: string | null;
  /** 站点默认语言 */
  default?: string | null;
  /** 是否锁定语言 */
  locked?: boolean;
  /** 站点支持的语言 */
  options?: ReadonlyArray<{ name?: string | null }> | null;
}

/**
 * 将语言代码归一化到前端已实现的区域语言
 *
 * 裸语言码（en）与未收录区域（de-AT）按前缀映射，中文按书写系统区分繁简；
 * 完全不支持的语言返回 null，由调用方决定回退。
 * @param value 原始语言代码
 * @returns 前端支持的语言代码，不支持时返回 null
 */
export function tryNormalizeLanguage(value?: string | null): CubeLocale | null {
  if (!value) return null;
  const lower = value.trim().toLowerCase().replace(/_/g, '-');
  if (!lower) return null;

  const exact = supportedValues.find((item) => item.toLowerCase() === lower);
  if (exact) return exact as CubeLocale;

  // 中文按书写系统区分：Hant/TW/HK/MO 使用繁體，其余使用简体
  if (lower.startsWith('zh')) {
    const isHant = lower.includes('hant') || lower.endsWith('-tw') || lower.endsWith('-hk') || lower.endsWith('-mo');
    return isHant ? 'zh-TW' : 'zh-CN';
  }

  const prefix = lower.split('-')[0];
  const matched = supportedLanguages.find((item) => item.value.split('-')[0].toLowerCase() === prefix);
  return matched ? (matched.value as CubeLocale) : null;
}

/**
 * 归一化语言代码，不支持时回退英文
 * @param value 原始语言代码
 * @returns 前端支持的语言代码
 */
export function normalizeLanguage(value?: string | null): CubeLocale {
  return tryNormalizeLanguage(value) ?? 'en-US';
}

// 获取浏览器语言设置，命中支持列表则复用，否则回退英文
function getBrowserLanguage(): CubeLocale {
  return tryNormalizeLanguage(navigator.language) ?? 'en-US';
}

/**
 * 读取本地显式选择的语言
 * @returns 本地语言；未选择或不在前端词表内时返回 null
 */
export function getStoredLanguage(): CubeLocale | null {
  if (typeof localStorage === 'undefined') return null;
  return tryNormalizeLanguage(localStorage.getItem('cube-language'));
}

/**
 * 读取 URL 中的 `lang` 参数
 *
 * 与服务端「`?lang=` → Cookie → Accept-Language」的优先级保持一致，
 * 便于分享带语言的登录链接。
 * @returns URL 指定的语言；未指定或不支持时返回 null
 */
export function getUrlLanguage(): CubeLocale | null {
  if (typeof window === 'undefined') return null;
  return tryNormalizeLanguage(new URLSearchParams(window.location.search).get('lang'));
}

/**
 * 是否已显式选择语言
 * @returns URL 参数或本地存储存在有效语言时返回 true
 */
export function hasExplicitLanguage(): boolean {
  return !!(getUrlLanguage() || getStoredLanguage());
}

// 获取本地存储的语言设置或使用浏览器语言
function getLanguage(): CubeLocale {
  return getUrlLanguage() ?? getStoredLanguage() ?? getBrowserLanguage();
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
 *
 * @param lang 语言代码
 * @param persist 是否写入本地偏好。用户显式切换为 true；自动跟随/站点锁定为 false
 */
export function setLanguage(lang: CubeLocale, persist = true) {
  const locale = normalizeLanguage(lang);
  i18n.global.locale.value = locale;
  if (persist && typeof localStorage !== 'undefined') {
    localStorage.setItem('cube-language', locale);
    syncQueryLanguage(locale);
  }
  document.querySelector('html')?.setAttribute('lang', locale);
  document.querySelector('html')?.setAttribute('dir', locale === 'ar-SA' ? 'rtl' : 'ltr');
}

/** 同步 URL 中已有的 lang 参数，避免刷新后被旧语言覆盖 */
function syncQueryLanguage(locale: CubeLocale) {
  if (typeof window === 'undefined') return;

  const url = new URL(window.location.href);
  if (!url.searchParams.has('lang')) return;

  url.searchParams.set('lang', locale);
  window.history.replaceState(null, '', url.pathname + url.search + url.hash);
}

/**
 * 解析站点语言下拉的可选语言
 *
 * 前端只提供已实现词表的 11 种语言；站点/租户白名单只做收窄，
 * 交集为空时回退本地全部语言，避免站点配置异常导致没有可选项。
 * @param language 站点语言设置
 * @returns 可选语言列表
 */
export function resolveSelectableLanguages(language?: SiteLanguageInput | null) {
  const allowed = new Set<CubeLocale>();
  for (const item of language?.options ?? []) {
    const normalized = tryNormalizeLanguage(item?.name);
    if (normalized) allowed.add(normalized);
  }
  if (!allowed.size) return supportedLanguages;

  const list = supportedLanguages.filter((item) => allowed.has(item.value));
  return list.length ? list : supportedLanguages;
}

/**
 * 解析站点语言
 *
 * 优先级：URL `?lang=` → 本地显式选择 → 站点 current → 站点 default → 浏览器 → 英文；
 * `locked` 时忽略本地选择强制使用 current/default；`options` 作为白名单。
 * @param language 站点语言设置
 * @returns 当前应使用的语言
 */
export function resolveSiteLocale(language?: SiteLanguageInput | null): CubeLocale {
  const allowed = new Set<CubeLocale>();
  for (const item of language?.options ?? []) {
    const normalized = tryNormalizeLanguage(item?.name);
    if (normalized) allowed.add(normalized);
  }
  const isAllowed = (value: CubeLocale | null): value is CubeLocale =>
    !!value && (allowed.size === 0 || allowed.has(value));

  const current = tryNormalizeLanguage(language?.current);
  const siteDefault = tryNormalizeLanguage(language?.default);

  if (language?.locked) {
    if (isAllowed(current)) return current;
    if (isAllowed(siteDefault)) return siteDefault;

    const lockedBrowser = getBrowserLanguage();
    if (isAllowed(lockedBrowser)) return lockedBrowser;

    return supportedLanguages.find((item) => allowed.size === 0 || allowed.has(item.value))?.value ?? 'en-US';
  }

  const explicit = getUrlLanguage() ?? getStoredLanguage();
  if (isAllowed(explicit)) return explicit;
  if (isAllowed(current)) return current;
  if (isAllowed(siteDefault)) return siteDefault;

  const browser = getBrowserLanguage();
  if (isAllowed(browser)) return browser;

  return supportedLanguages.find((item) => allowed.size === 0 || allowed.has(item.value))?.value ?? 'en-US';
}

/**
 * 应用站点下发的语言设置
 *
 * 自动跟随（无显式选择）与站点锁定不写 localStorage；本地选择被白名单排除时
 * 以服务端解析结果为准并纠正本地记录。
 * @param language 站点语言设置
 * @returns 应用后的语言
 */
export function applySiteLanguage(language?: SiteLanguageInput | null): CubeLocale {
  const resolved = resolveSiteLocale(language);
  const stored = getStoredLanguage();
  const selectable = resolveSelectableLanguages(language);
  const storedBlocked = !!stored && !!language?.options?.length && !selectable.some((item) => item.value === stored);

  if (resolved !== getCurrentLanguage()) setLanguage(resolved, false);
  if (storedBlocked && !language?.locked && typeof localStorage !== 'undefined') localStorage.setItem('cube-language', resolved);

  return resolved;
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
