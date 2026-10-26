/**
 * 多语言词表键集合校验
 *
 * 所有语言必须与英文源文（en-US）拥有完全相同的键集合，避免新增文案漏翻导致运行期回退源文。
 *
 * 运行：pnpm test:unit core/__tests__/localeKeys.spec.ts
 */
import { describe, expect, it } from 'vitest';
import ar from '../i18n/locales/ar';
import de from '../i18n/locales/de';
import enUS from '../i18n/locales/en-US';
import es from '../i18n/locales/es';
import fr from '../i18n/locales/fr';
import ja from '../i18n/locales/ja';
import ko from '../i18n/locales/ko';
import pt from '../i18n/locales/pt';
import ru from '../i18n/locales/ru';
import zhCN from '../i18n/locales/zh-CN';
import zhTW from '../i18n/locales/zh-TW';

const LOCALES = { ar, de, es, fr, ja, ko, pt, ru, 'zh-CN': zhCN, 'zh-TW': zhTW };

describe('多语言词表键集合', () => {
  const baseKeys = Object.keys(enUS).sort();

  it('英文源文非空', () => {
    expect(baseKeys.length).toBeGreaterThan(1000);
  });

  for (const [name, messages] of Object.entries(LOCALES)) {
    it(`${name} 与英文源文键集合一致`, () => {
      const keys = Object.keys(messages).sort();
      const missing = baseKeys.filter((key) => !(key in messages));
      const extra = keys.filter((key) => !(key in enUS));

      expect(missing, `缺少键：${missing.slice(0, 10).join(', ')}`).toEqual([]);
      expect(extra, `多余键：${extra.slice(0, 10).join(', ')}`).toEqual([]);
      expect(keys).toEqual(baseKeys);
    });
  }
});
