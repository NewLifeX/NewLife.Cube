import { describe, expect, it } from 'vitest';
import { escapeHtml, renderAiMarkdown } from './aiMarkdown';

describe('escapeHtml', () => {
  it('转义标签与引号', () => {
    expect(escapeHtml('<script>alert(1)</script>')).toBe(
      '&lt;script&gt;alert(1)&lt;/script&gt;',
    );
    expect(escapeHtml('"x"')).toBe('&quot;x&quot;');
  });
});

describe('renderAiMarkdown', () => {
  it('空输入返回空串', () => {
    expect(renderAiMarkdown('')).toBe('');
  });

  it('raw HTML 被转义而非执行', () => {
    const html = renderAiMarkdown('<img onerror=alert(1)>');
    expect(html).toContain('&lt;img');
    expect(html).not.toContain('<img onerror');
  });

  it('script 标签被转义', () => {
    const html = renderAiMarkdown('<script>alert(1)</script>');
    expect(html).toContain('&lt;script&gt;');
  });

  it('单换行 breaks 为 br', () => {
    const html = renderAiMarkdown('a\nb');
    expect(html).toMatch(/a\s*<br\s*\/?>\s*b/i);
  });
});
