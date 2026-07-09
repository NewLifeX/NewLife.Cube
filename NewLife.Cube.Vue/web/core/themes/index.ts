/**
 * 主题注册模块
 *
 * 提供主题 CSS 文件的动态加载机制
 */

import type { ThemeFamily } from '../composables/useTheme';

// 主题 CSS 文件映射
export const THEME_CSS_FILES: Record<ThemeFamily, () => Promise<any>> = {
  cyber: () => import('./cyber.css'),
  forest: () => import('./forest.css'),
  aurora: () => import('./aurora.css'),
  industrial: () => import('./industrial.css'),
};

// 主题 CSS 模块缓存
const loadedThemes = new Map<ThemeFamily, Promise<any>>();

/**
 * 加载主题 CSS 文件
 */
export async function loadThemeCss(family: ThemeFamily): Promise<void> {
  // 如果已加载，直接返回
  if (loadedThemes.has(family)) {
    return;
  }

  // 加载并缓存
  const loader = THEME_CSS_FILES[family];
  if (loader) {
    loadedThemes.set(family, loader());
    await loadedThemes.get(family);
  }
}

/**
 * 预加载所有主题
 */
export async function preloadAllThemes(): Promise<void> {
  await Promise.allSettled(
    Object.keys(THEME_CSS_FILES).map(family =>
      loadThemeCss(family as ThemeFamily)
    )
  );
}