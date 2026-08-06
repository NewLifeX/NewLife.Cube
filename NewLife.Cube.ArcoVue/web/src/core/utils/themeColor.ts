/**
 * 从 Arco 语义 token 读取具体色值供 canvas 渲染（VTable/VTable Gantt 不支持 CSS 变量）。
 * Arco 变量多为 "r,g,b" 三元组（--primary-*）→ 转 rgb()；已是 rgb()/hex 原样返回；未定义回落 fallback。
 */
export function themeColor(varName: string, fallback: string): string {
  if (typeof document === 'undefined') return fallback;
  const v = getComputedStyle(document.body).getPropertyValue(varName).trim();
  if (!v) return fallback;
  if (/^\d+(?:\s*,\s*\d+){2}$/.test(v)) return `rgb(${v})`;
  return v;
}
