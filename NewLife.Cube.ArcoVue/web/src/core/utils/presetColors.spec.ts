import { describe, expect, it } from 'vitest';
import { DEFAULT_PRIMARY_COLOR, PRESET_THEME_COLORS } from './presetColors';

describe('PRESET_THEME_COLORS', () => {
  it('恰含 13 个官方品牌色', () => {
    expect(PRESET_THEME_COLORS).toHaveLength(13);
  });

  it('key 唯一', () => {
    const keys = PRESET_THEME_COLORS.map((c) => c.key);
    expect(new Set(keys).size).toBe(keys.length);
  });

  it('色值为合法 hex 且官方中文名非空', () => {
    for (const c of PRESET_THEME_COLORS) {
      expect(c.color).toMatch(/^#[0-9A-Fa-f]{6}$/);
      expect(c.name.trim().length).toBeGreaterThan(0);
    }
  });

  it('含默认主题色极客蓝 #165DFF', () => {
    expect(PRESET_THEME_COLORS.some((c) => c.key === 'arcoblue' && c.color === DEFAULT_PRIMARY_COLOR)).toBe(true);
  });

  it('不含中性灰', () => {
    expect(PRESET_THEME_COLORS.some((c) => c.key === 'gray')).toBe(false);
  });
});
