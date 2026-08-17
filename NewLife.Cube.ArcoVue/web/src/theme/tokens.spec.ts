import { describe, expect, it } from 'vitest';
import { SYSTEM_DEFAULT_PROFILE } from '@/core/utils/userProfile';
import {
  buildThemeTokens,
  densityClassName,
  resolveEffectiveAppearance,
} from './tokens';
describe('resolveEffectiveAppearance', () => {
  it('maps system via prefersDark', () => {
    expect(resolveEffectiveAppearance('system', true)).toBe('dark');
    expect(resolveEffectiveAppearance('system', false)).toBe('light');
    expect(resolveEffectiveAppearance('dark', false)).toBe('dark');
  });
});

describe('densityClassName', () => {
  it('returns density class', () => {
    expect(densityClassName('compact')).toBe('cube-density-compact');
    expect(densityClassName('default')).toBe('cube-density-default');
  });
});

describe('buildThemeTokens', () => {
  it('sets arco dark and css vars', () => {
    const t = buildThemeTokens(
      { ...SYSTEM_DEFAULT_PROFILE.theme, appearance: 'dark', primaryColor: '#165DFF', density: 'compact' },
      false,
    );
    expect(t.arcoTheme).toBe('dark');
    expect(t.densityClass).toBe('cube-density-compact');
    expect(t.cssVars['--cube-primary']).toBe('#165DFF');
    expect(t.cssVars['--cube-radius']).toBe('4px');
    expect(t.cssVars['--cube-font-scale']).toBe('1');
    expect(t.cssVars['font-size']).toBe('14px');
    expect(t.cssVars.zoom).toBeUndefined();
  });

  it('scales font tokens when fontScale != 1 (no CSS zoom)', () => {
    const t = buildThemeTokens(
      { ...SYSTEM_DEFAULT_PROFILE.theme, fontScale: 1.1 },
      false,
    );
    expect(t.cssVars['--cube-font-scale']).toBe('1.1');
    expect(t.cssVars.zoom).toBeUndefined();
    expect(t.cssVars['--cube-font-size']).toBe(`${14 * 1.1}px`);
    expect(t.cssVars['font-size']).toBe(`${14 * 1.1}px`);
  });

  it('exposes semantic font tokens scaled by fontScale', () => {
    const base = buildThemeTokens(SYSTEM_DEFAULT_PROFILE.theme, false);
    expect(base.cssVars['--cube-font-size-body']).toBe('14px');
    expect(base.cssVars['--cube-font-size-meta']).toBe('12px');
    expect(base.cssVars['--cube-font-size-title']).toBe('16px');
    expect(base.cssVars['--cube-font-weight-normal']).toBe('400');
    expect(base.cssVars['--cube-font-weight-medium']).toBe('500');

    const scaled = buildThemeTokens(
      { ...SYSTEM_DEFAULT_PROFILE.theme, fontScale: 1.25 },
      false,
    );
    expect(scaled.cssVars['--cube-font-size-body']).toBe(`${14 * 1.25}px`);
    expect(scaled.cssVars['--cube-font-size-meta']).toBe(`${12 * 1.25}px`);
    expect(scaled.cssVars['--cube-font-size-title']).toBe(`${16 * 1.25}px`);
    expect(scaled.cssVars['--cube-font-weight-normal']).toBe('400');
    expect(scaled.cssVars['--cube-font-weight-medium']).toBe('500');
  });

  it('生成 Arco primary 1-10 色阶与浅色阶（6 为主色，1 最浅 10 最深）', () => {
    const t = buildThemeTokens(
      { ...SYSTEM_DEFAULT_PROFILE.theme, primaryColor: '#165DFF' },
      false,
    );
    expect(t.primaryScale).toHaveLength(10);
    expect(t.primaryScale[5]).toBe('22,93,255'); // 主色 RGB 三元组
    expect(t.primaryScale[0]).toBe('231,238,255'); // primary-1（官方 #E8F3FF 近似）
    expect(t.primaryScale[9]).toBe('0,24,78'); // primary-10（官方 #000D4D 近似）
    expect(t.primaryLight).toHaveLength(4);
    expect(t.cssVars['--primary-1']).toBe('231,238,255');
    expect(t.cssVars['--color-primary-light-1']).toBe('rgb(231,238,255)');
    // 主色变更时色阶跟随（6 恒为主色）
    const orange = buildThemeTokens(
      { ...SYSTEM_DEFAULT_PROFILE.theme, primaryColor: '#ffc014' },
      false,
    );
    expect(orange.primaryScale[5]).toBe('255,192,20');
    expect(orange.cssVars['--primary-6']).toBe('255,192,20'); // RGB 三元组（Arco 组件经 rgb(var(--primary-6)) 消费）
  });

  it('暗色模式生成反向色阶（1 最深 10 最浅，浅色阶为主色半透明）', () => {
    const t = buildThemeTokens(
      { ...SYSTEM_DEFAULT_PROFILE.theme, appearance: 'dark', primaryColor: '#165DFF' },
      false,
    );
    expect(t.primaryScale[5]).toBe('22,93,255'); // 主色恒在 6
    expect(t.primaryScale[0]).toBe('0,24,78'); // primary-1 最深（暗色反转）
    expect(t.primaryScale[9]).toBe('190,210,255'); // primary-10 最浅
    expect(t.primaryLight[0]).toMatch(/^rgba\(/); // 暗色浅色阶为主色半透明
    expect(t.cssVars['--color-primary-light-1']).toMatch(/^rgba\(/);
  });
});
