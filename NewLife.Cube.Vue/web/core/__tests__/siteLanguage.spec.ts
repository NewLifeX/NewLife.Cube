import { beforeEach, describe, expect, it } from 'vitest';
import {
  applySiteLanguage,
  getCurrentLanguage,
  normalizeLanguage,
  resolveSelectableLanguages,
  resolveSiteLocale,
  setLanguage,
  tryNormalizeLanguage,
} from '../i18n';

describe('i18n — 站点语言解析', () => {
  beforeEach(() => {
    localStorage.clear();
    window.history.replaceState(null, '', '/');
    setLanguage('en-US', false);
  });

  it('归一化裸语言码、未收录区域与不支持的语种', () => {
    expect(tryNormalizeLanguage('en')).toBe('en-US');
    expect(tryNormalizeLanguage('de-AT')).toBe('de-DE');
    expect(tryNormalizeLanguage('zh-Hant')).toBe('zh-TW');
    expect(tryNormalizeLanguage('sw-KE')).toBeNull();
    expect(normalizeLanguage('sw-KE')).toBe('en-US');
  });

  it('无显式选择时采用站点 current', () => {
    expect(resolveSiteLocale({ current: 'ja-JP', default: 'de-DE' })).toBe('ja-JP');
  });

  it('站点 current 不受支持时回退 default', () => {
    expect(resolveSiteLocale({ current: 'it-IT', default: 'ko-KR' })).toBe('ko-KR');
  });

  it('本地显式选择优先于站点 current', () => {
    localStorage.setItem('cube-language', 'es-ES');
    expect(resolveSiteLocale({ current: 'ja-JP' })).toBe('es-ES');
  });

  it('本地选择被站点白名单排除时回退 current', () => {
    localStorage.setItem('cube-language', 'es-ES');
    expect(
      resolveSiteLocale({
        current: 'en-US',
        options: [{ name: 'zh-CN' }, { name: 'en-US' }],
      }),
    ).toBe('en-US');
  });

  it('站点锁定语言时忽略本地选择', () => {
    localStorage.setItem('cube-language', 'es-ES');
    expect(
      resolveSiteLocale({
        current: 'zh-CN',
        locked: true,
        options: [{ name: 'zh-CN' }, { name: 'en-US' }],
      }),
    ).toBe('zh-CN');
  });

  it('URL lang 参数优先于本地选择', () => {
    localStorage.setItem('cube-language', 'es-ES');
    window.history.replaceState(null, '', '/login?lang=ja-JP');
    expect(resolveSiteLocale({ current: 'en-US' })).toBe('ja-JP');
  });

  it('可选语言取前端词表与站点白名单的交集', () => {
    const list = resolveSelectableLanguages({
      options: [{ name: 'zh-CN' }, { name: 'en-US' }, { name: 'it-IT' }],
    });

    expect(list.map((item) => item.value)).toEqual(['zh-CN', 'en-US']);
  });

  it('白名单与词表无交集时回退全部词表', () => {
    const list = resolveSelectableLanguages({ options: [{ name: 'it-IT' }] });

    expect(list).toHaveLength(11);
  });

  it('应用站点语言时自动跟随不写本地偏好', () => {
    const resolved = applySiteLanguage({ current: 'ja-JP' });

    expect(resolved).toBe('ja-JP');
    expect(getCurrentLanguage()).toBe('ja-JP');
    expect(localStorage.getItem('cube-language')).toBeNull();
  });

  it('本地偏好被白名单排除时纠正本地记录', () => {
    localStorage.setItem('cube-language', 'es-ES');

    applySiteLanguage({ current: 'en-US', options: [{ name: 'zh-CN' }, { name: 'en-US' }] });

    expect(localStorage.getItem('cube-language')).toBe('en-US');
  });

  it('显式切换语言时同步 URL 中的 lang 参数', () => {
    window.history.replaceState(null, '', '/login?lang=zh-CN');

    setLanguage('de-DE');

    expect(window.location.search).toBe('?lang=de-DE');
    expect(localStorage.getItem('cube-language')).toBe('de-DE');
  });
});
