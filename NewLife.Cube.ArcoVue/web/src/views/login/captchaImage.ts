/**
 * 将 /Auth/Captcha 返回的 image 归一为可 v-html 的片段。
 * - data:image… / 裸 base64 → &lt;img src&gt;
 * - SVG / HTML 片段 → 原样（历史 SvgMathCaptcha）
 */
export function normalizeCaptchaImageHtml(raw: string | null | undefined): string {
  const s = (raw ?? '').trim();
  if (!s) return '';
  if (/^data:image\//i.test(s)) {
    return `<img src="${s}" alt="captcha" draggable="false" />`;
  }
  if (s.startsWith('<svg') || s.startsWith('<?xml') || s.startsWith('<img')) return s;
  // 无前缀的长 base64（部分实现只回 body）
  if (s.length > 80 && /^[A-Za-z0-9+/=\s]+$/.test(s)) {
    const b64 = s.replace(/\s+/g, '');
    return `<img src="data:image/png;base64,${b64}" alt="captcha" draggable="false" />`;
  }
  return s;
}
