/** 解析颜色为 [r,g,b]：支持 #hex、rgb()、rgba() */
export function parseColor(str: string): [number, number, number] | null {
  const hex = /#([0-9a-fA-F]{6})/.exec(str);
  if (hex) {
    const n = parseInt(hex[1], 16);
    return [(n >> 16) & 255, (n >> 8) & 255, n & 255];
  }
  const rgb = /(\d+)[^,]*,\s*(\d+)[^,]*,\s*(\d+)/.exec(str);
  if (rgb) return [Number(rgb[1]), Number(rgb[2]), Number(rgb[3])];
  return null;
}

/** 给已解析色值加透明度，供 canvas 渐变（如冻结列示意线） */
export function withAlpha(color: string, alpha: number): string {
  const rgb = parseColor(color);
  if (!rgb) return color;
  const a = Math.min(1, Math.max(0, alpha));
  return `rgba(${rgb[0]}, ${rgb[1]}, ${rgb[2]}, ${a})`;
}

/**
 * 从 Arco 语义 token 读取具体色值供 canvas 渲染（VTable/VTable Gantt 不支持 CSS 变量）。
 * Arco 变量多为 "r,g,b" 三元组（--primary-*）→ 转 rgb()。
 * 暗色模式下 Arco 语义色多为半透明 rgba（如 --color-fill-2 = rgba(255,255,255,0.08)）；
 * canvas 不支持半透明叠加合成，需基于容器背景（--color-bg-2，可能为 hex 或 rgb()）合成不透明色。
 */
export function themeColor(varName: string, fallback: string): string {
  if (typeof document === 'undefined') return fallback;
  const v = getComputedStyle(document.body).getPropertyValue(varName).trim();
  if (!v) return fallback;
  const rgba = /rgba\(\s*(\d+)[^,]*,\s*(\d+)[^,]*,\s*(\d+)[^,]*,\s*([\d.]+)\s*\)/.exec(v);
  if (rgba) {
    const r = Number(rgba[1]);
    const g = Number(rgba[2]);
    const b = Number(rgba[3]);
    const a = Number(rgba[4]);
    const bg = getComputedStyle(document.body).getPropertyValue('--color-bg-2').trim();
    const [br, bgc, bb] = parseColor(bg) ?? [255, 255, 255];
    return `rgb(${Math.round(r * a + br * (1 - a))}, ${Math.round(g * a + bgc * (1 - a))}, ${Math.round(b * a + bb * (1 - a))})`;
  }
  if (/^\d+(?:\s*,\s*\d+){2}$/.test(v)) return `rgb(${v})`;
  return v;
}
