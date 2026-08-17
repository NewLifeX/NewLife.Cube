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
  /** Arco primary 1-10 色阶（RGB 三元组 "r,g,b"），6 为主色；覆盖 body 上 Arco 默认色阶 */
  primaryScale: string[];
  /** Arco 浅色阶（rgb(...) 格式，对应 --color-primary-light-1~4） */
  primaryLight: string[];
  arcoTheme: 'dark' | null;
}

/** hex → [r,g,b]（0-255） */
function hexToRgb(hex: string): [number, number, number] {
  let h = hex.replace('#', '');
  if (h.length === 3) h = h.split('').map((c) => c + c).join('');
  const n = parseInt(h, 16);
  return [(n >> 16) & 255, (n >> 8) & 255, n & 255];
}

/** [r,g,b] → [h,s,l]（h∈[0,1)，s/l∈[0,1]） */
function rgbToHsl([r, g, b]: [number, number, number]): [number, number, number] {
  const rn = r / 255;
  const gn = g / 255;
  const bn = b / 255;
  const max = Math.max(rn, gn, bn);
  const min = Math.min(rn, gn, bn);
  const l = (max + min) / 2;
  if (max === min) return [0, 0, l];
  const d = max - min;
  const s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
  let h = 0;
  if (max === rn) h = ((gn - bn) / d + (gn < bn ? 6 : 0)) / 6;
  else if (max === gn) h = ((bn - rn) / d + 2) / 6;
  else h = ((rn - gn) / d + 4) / 6;
  return [h, s, l];
}

/** [h,s,l] → [r,g,b]（0-255） */
function hslToRgb(h: number, s: number, l: number): [number, number, number] {
  if (s === 0) {
    const v = Math.round(l * 255);
    return [v, v, v];
  }
  const hue2rgb = (p: number, q: number, t: number) => {
    let tt = t;
    if (tt < 0) tt += 1;
    if (tt > 1) tt -= 1;
    if (tt < 1 / 6) return p + (q - p) * 6 * tt;
    if (tt < 1 / 2) return q;
    if (tt < 2 / 3) return p + (q - p) * (2 / 3 - tt) * 6;
    return p;
  };
  const q = l < 0.5 ? l * (1 + s) : l + s - l * s;
  const p = 2 * l - q;
  return [
    Math.round(hue2rgb(p, q, h + 1 / 3) * 255),
    Math.round(hue2rgb(p, q, h) * 255),
    Math.round(hue2rgb(p, q, h - 1 / 3) * 255),
  ];
}

/**
 * 生成 Arco primary 1-10 色阶（RGB 三元组 "r,g,b"，下标 5 即主色）。
 * 亮度步进基于 Arco 官方默认蓝 #165DFF 色阶反推（HSL 亮度增量），对任意主色得到视觉连贯的梯度。
 * 暗色模式方向反转：亮色 1 最浅 10 最深；暗色 1 最深 10 最浅（Arco 暗色色阶）。
 */
export function buildPrimaryScale(hex: string, dark = false): string[] {
  const [h, s, l] = rgbToHsl(hexToRgb(hex));
  const deltas = dark
    ? [-0.39, -0.31, -0.21, -0.1, -0.08, 0, 0.08, 0.16, 0.25, 0.33]
    : [0.41, 0.33, 0.25, 0.16, 0.08, 0, -0.1, -0.21, -0.31, -0.39];
  return deltas.map((d) => {
    const [r, g, b] = hslToRgb(h, s, Math.max(0, Math.min(1, l + d)));
    return `${r},${g},${b}`;
  });
}

/** 纯函数：由 theme 偏好生成 DOM 注入所需 token */
export function buildThemeTokens(
  theme: ThemePrefs,
  prefersDark = false,
): ThemeTokenResult {
  const effectiveAppearance = resolveEffectiveAppearance(theme.appearance, prefersDark);
  const scale = theme.fontScale > 0 ? theme.fontScale : 1;
  const dark = effectiveAppearance === 'dark';
  const primaryScale = buildPrimaryScale(theme.primaryColor, dark);
  const primaryVars: Record<string, string> = {};
  primaryScale.forEach((v, i) => {
    primaryVars[`--primary-${i + 1}`] = v;
  });
  // Arco 浅色阶（--color-primary-light-N）：亮色为 primary-N 的 rgb()；暗色为主色半透明（Arco 暗色风格）
  const primaryLight: string[] = dark
    ? [0.2, 0.35, 0.5, 0.65].map((a) => `rgba(${primaryScale[5]}, ${a})`)
    : primaryScale.slice(0, 4).map((v) => `rgb(${v})`);
  primaryLight.forEach((v, i) => {
    primaryVars[`--color-primary-light-${i + 1}`] = v;
  });
  return {
    effectiveAppearance,
    densityClass: densityClassName(theme.density),
    cssVars: {
      '--cube-primary': theme.primaryColor,
      '--primary-6': theme.primaryColor,
      ...primaryVars,
      '--cube-radius': `${theme.radius}px`,
      '--border-radius-small': `${Math.max(0, theme.radius - 2)}px`,
      '--border-radius-medium': `${theme.radius}px`,
      '--cube-font-scale': String(scale),
      '--cube-font-size': `${14 * scale}px`,
      /* 语义字体 Token（OSC-0007）：组件优先消费，避免散落临时字号/字重 */
      '--cube-font-size-body': `${14 * scale}px`,
      '--cube-font-size-meta': `${12 * scale}px`,
      '--cube-font-size-title': `${16 * scale}px`,
      '--cube-font-weight-normal': '400',
      '--cube-font-weight-medium': '500',
      /* 字号缩放只用 CSS 变量，禁止对布局根使用 CSS zoom：
       * zoom 会改变绘制/布局比例，易在视口留白或被 overflow 裁切工具栏/分页（且 html zoom 会导致弹层偏移） */
      'font-size': `${14 * scale}px`,
    },
    primaryScale,
    primaryLight,
    arcoTheme: effectiveAppearance === 'dark' ? 'dark' : null,
  };
}
