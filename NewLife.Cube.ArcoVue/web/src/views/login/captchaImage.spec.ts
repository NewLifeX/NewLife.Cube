import { describe, expect, it } from 'vitest';
import { normalizeCaptchaImageHtml } from './captchaImage';

describe('normalizeCaptchaImageHtml', () => {
  it('wraps data URI as img', () => {
    const raw = 'data:image/png;base64,iVBORw0KGgo=';
    expect(normalizeCaptchaImageHtml(raw)).toBe(
      `<img src="${raw}" alt="captcha" draggable="false" />`,
    );
  });

  it('keeps svg html', () => {
    const svg = '<svg xmlns="http://www.w3.org/2000/svg"><text>1+1</text></svg>';
    expect(normalizeCaptchaImageHtml(svg)).toBe(svg);
  });

  it('wraps bare base64', () => {
    const b64 = 'A'.repeat(100);
    expect(normalizeCaptchaImageHtml(b64)).toContain('data:image/png;base64,');
    expect(normalizeCaptchaImageHtml(b64)).toContain('<img src=');
  });

  it('empty', () => {
    expect(normalizeCaptchaImageHtml('')).toBe('');
    expect(normalizeCaptchaImageHtml(null)).toBe('');
  });
});
