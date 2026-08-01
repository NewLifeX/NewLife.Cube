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
    expect(t.cssVars.zoom).toBe('normal');
  });

  it('scales zoom when fontScale != 1', () => {
    const t = buildThemeTokens(
      { ...SYSTEM_DEFAULT_PROFILE.theme, fontScale: 1.1 },
      false,
    );
    expect(t.cssVars['--cube-font-scale']).toBe('1.1');
    expect(t.cssVars.zoom).toBe('1.1');
    expect(t.cssVars['--cube-font-size']).toBe(`${14 * 1.1}px`);
  });
});
