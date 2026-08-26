import { marked, Renderer } from 'marked';

/** HTML 转义，防止 AI 输出 raw HTML 注入 */
export function escapeHtml(s: string): string {
  return String(s)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

/**
 * Markdown 渲染：GFM + 单换行断行；raw HTML 与代码块一律转义。
 */
export function renderAiMarkdown(text: string): string {
  if (!text) return '';
  const renderer = new Renderer();
  renderer.html = ({ text: t }: { text: string }) => escapeHtml(t);
  renderer.code = ({ text: t, lang }: { text: string; lang?: string }) => {
    const l = (lang || '').match(/^\S+/)?.[0] || '';
    const label = l ? `<span class="ai-code-lang">${escapeHtml(l)}</span>` : '';
    const body = String(t).replace(/\n+$/, '');
    return `<pre>${label}<code class="language-${escapeHtml(l)}">${escapeHtml(body)}</code></pre>`;
  };
  return marked.parse(text, {
    gfm: true,
    breaks: true,
    async: false,
    renderer,
  }) as string;
}

