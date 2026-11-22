/**
 * 响应式布局判定（SKIN-11 屏幕宽度基础兼容）
 *
 * 皮肤布局共用同一断点口径，避免各皮肤各写一套 magic number。
 */

/** 窄屏断点（像素）。小于该宽度时侧边栏自动折叠，内容区按 100% 宽度渲染 */
export const MOBILE_BREAKPOINT = 768;

/**
 * 是否需要自动折叠侧边栏
 *
 * @param width 视口宽度（像素）
 * @returns 窄屏返回 true；非法宽度返回 false
 * @example
 * ```ts
 * if (shouldAutoCollapseSidebar(window.innerWidth)) appStore.collapsed = true;
 * ```
 */
export function shouldAutoCollapseSidebar(width: number): boolean {
  return Number.isFinite(width) && width > 0 && width < MOBILE_BREAKPOINT;
}
