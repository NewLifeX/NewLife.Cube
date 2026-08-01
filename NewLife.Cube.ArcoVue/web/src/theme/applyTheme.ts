import type { ThemePrefs } from '@/core/utils/userProfile';
import { buildThemeTokens } from './tokens';

const DENSITY_CLASSES = ['cube-density-default', 'cube-density-compact'];

let mediaQuery: MediaQueryList | null = null;
let mediaHandler: ((e: MediaQueryListEvent) => void) | null = null;
let lastTheme: ThemePrefs | null = null;

function prefersDarkNow(): boolean {
  if (typeof window === 'undefined' || !window.matchMedia) return false;
  return window.matchMedia('(prefers-color-scheme: dark)').matches;
}

/** 将主题偏好写入 document（arco-theme + CSS 变量 + 密度 class） */
export function applyTheme(theme: ThemePrefs, prefersDark = prefersDarkNow()): void {
  lastTheme = theme;
  if (typeof document === 'undefined') return;

  const tokens = buildThemeTokens(theme, prefersDark);
  const root = document.documentElement;
  const body = document.body;

  if (tokens.arcoTheme) {
    body.setAttribute('arco-theme', 'dark');
    root.setAttribute('arco-theme', 'dark');
  } else {
    body.removeAttribute('arco-theme');
    root.removeAttribute('arco-theme');
  }

  for (const [k, v] of Object.entries(tokens.cssVars)) {
    if (k === 'zoom') {
      // zoom 整体缩放界面（Arco 字号多为 px，仅改 root font-size 无效）
      if (v === 'normal' || v === '1') root.style.removeProperty('zoom');
      else (root.style as CSSStyleDeclaration & { zoom: string }).zoom = v;
      continue;
    }
    root.style.setProperty(k, v);
  }

  for (const c of DENSITY_CLASSES) root.classList.remove(c);
  root.classList.add(tokens.densityClass);
}

/** 监听系统外观；appearance=system 时自动重应用 */
export function watchSystemAppearance(getTheme: () => ThemePrefs): () => void {
  if (typeof window === 'undefined' || !window.matchMedia) return () => {};

  stopWatchSystemAppearance();
  mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
  mediaHandler = () => {
    const theme = getTheme();
    if (theme.appearance === 'system') applyTheme(theme, mediaQuery!.matches);
  };
  mediaQuery.addEventListener('change', mediaHandler);
  return stopWatchSystemAppearance;
}

export function stopWatchSystemAppearance(): void {
  if (mediaQuery && mediaHandler) {
    mediaQuery.removeEventListener('change', mediaHandler);
  }
  mediaQuery = null;
  mediaHandler = null;
}

export function reapplyLastTheme(): void {
  if (lastTheme) applyTheme(lastTheme);
}
