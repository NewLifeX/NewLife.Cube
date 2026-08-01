import type { Appearance, Density, ThemePrefs } from '@/core/utils/userProfile';

export type ResolvedAppearance = 'light' | 'dark';

/** system → 依据 prefers-color-scheme 解析为 light/dark */
export function resolveEffectiveAppearance(
  appearance: Appearance,
  prefersDark: boolean,
): ResolvedAppearance {
  if (appearance === 'system') return prefersDark ? 'dark' : 'light';
  return appearance;
}

export function densityClassName(density: Density): string {
  return density === 'compact' ? 'cube-density-compact' : 'cube-density-default';
}

export interface ThemeTokenResult {
  effectiveAppearance: ResolvedAppearance;
  densityClass: string;
  cssVars: Record<string, string>;
  arcoTheme: 'dark' | null;
}

/** 纯函数：由 theme 偏好生成 DOM 注入所需 token */
export function buildThemeTokens(
  theme: ThemePrefs,
  prefersDark = false,
): ThemeTokenResult {
  const effectiveAppearance = resolveEffectiveAppearance(theme.appearance, prefersDark);
  const scale = theme.fontScale > 0 ? theme.fontScale : 1;
  return {
    effectiveAppearance,
    densityClass: densityClassName(theme.density),
    cssVars: {
      '--cube-primary': theme.primaryColor,
      '--primary-6': theme.primaryColor,
      '--cube-radius': `${theme.radius}px`,
      '--border-radius-small': `${Math.max(0, theme.radius - 2)}px`,
      '--border-radius-medium': `${theme.radius}px`,
      '--cube-font-scale': String(scale),
      '--cube-font-size': `${14 * scale}px`,
      /* Arco 组件多为 px 字号，靠 zoom 整体缩放才生效 */
      zoom: scale === 1 ? 'normal' : String(scale),
      'font-size': `${14 * scale}px`,
    },
    arcoTheme: effectiveAppearance === 'dark' ? 'dark' : null,
  };
}
