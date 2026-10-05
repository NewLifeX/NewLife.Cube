/**
 * 玻璃面板样式生成器（OSC-261004d7f4）
 *
 * 透明玻璃质感统一来源：工具栏 / 悬停卡片 / 其它浮层共用。
 * 与视图自定义配置「背景色」联动：chrome.bgColor/bgOpacity/bgBlur → rgba 背景 + backdrop blur；
 * 未配置时使用默认白底 62% + 70%（→14px）模糊。
 */
import type { ViewChrome } from '@/core/utils/viewProfile';

export const GLASS_DEFAULT_COLOR = '#FFFFFF';
export const GLASS_DEFAULT_DARK_COLOR = '#202228';
export const GLASS_DEFAULT_OPACITY = 62;
export const GLASS_DEFAULT_BLUR = 70;

type GlassChrome = Pick<ViewChrome, 'bgPreset' | 'bgColor' | 'bgOpacity' | 'bgBlur'> | null | undefined;

function toRgba(hex: string, opacity: number): string {
  const m = /^#([0-9a-f]{6})$/i.exec(hex.trim());
  if (!m) return hex;
  const n = parseInt(m[1], 16);
  const r = (n >> 16) & 255;
  const g = (n >> 8) & 255;
  const b = n & 255;
  const a = Math.min(100, Math.max(0, opacity)) / 100;
  return `rgba(${r}, ${g}, ${b}, ${a})`;
}

/** 运行环境是否支持 backdrop-filter（不支持时降级为纯半透明背景） */
export function supportsBackdropFilter(): boolean {
  return (
    typeof CSS !== 'undefined' &&
    typeof CSS.supports === 'function' &&
    CSS.supports('backdrop-filter', 'blur(1px)')
  );
}

/**
 * 生成玻璃面板内联样式。
 * @param chrome 视图 chrome（自定义配置「背景色」）
 * @param opts.dark 暗色主题（默认底色不同）
 * @param opts.supports 是否支持 backdrop-filter；缺省走环境探测
 */
export function glassPanelStyle(
  chrome?: GlassChrome,
  opts?: { dark?: boolean; supports?: boolean },
): Record<string, string> {
  const dark = opts?.dark ?? false;
  const supports = opts?.supports ?? supportsBackdropFilter();

  const custom = chrome?.bgPreset === 'custom' && !!chrome.bgColor;
  const color = custom ? chrome!.bgColor! : dark ? GLASS_DEFAULT_DARK_COLOR : GLASS_DEFAULT_COLOR;
  const opacity = custom ? (chrome!.bgOpacity ?? 100) : GLASS_DEFAULT_OPACITY;
  const blur = custom ? (chrome!.bgBlur ?? 0) : GLASS_DEFAULT_BLUR;

  const style: Record<string, string> = {};
  if (color === 'transparent') style.backgroundColor = 'transparent';
  else if (color.startsWith('#')) style.backgroundColor = toRgba(color, opacity);
  else style.backgroundColor = color;

  if (blur > 0 && supports) {
    const px = Math.round(blur / 5);
    style.backdropFilter = `blur(${px}px) saturate(160%)`;
    style.WebkitBackdropFilter = style.backdropFilter;
  }
  return style;
}
