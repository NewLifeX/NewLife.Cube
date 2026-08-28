/**
 * RichEditor 富文本编辑器单元测试（E2 增强）
 *
 * 覆盖：markdown 编辑/预览切换（marked 渲染出 HTML 结构）、html 模式文本域回填。
 */
import { describe, expect, it } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import RichEditor from '../RichEditor';

describe('RichEditor 富文本编辑器', () => {
  it('markdown 模式渲染编辑/预览切换，预览用 marked 输出 HTML', () => {
    // JSX 属性中 \n 为字面量，需用 JS 表达式传入真实换行
    const md = '# 标题\n\n**加粗** 和 *斜体*';
    const { container } = render(<RichEditor markdown value={md} />);
    // 编辑模式为文本域
    expect(screen.getByRole('textbox')).toHaveValue(md);
    // 切到预览
    fireEvent.click(screen.getByText('预览'));
    const preview = container.querySelector('[data-preview]');
    expect(preview).not.toBeNull();
    const html = preview ? preview.innerHTML : '';
    expect(html).toContain('<h1');
    expect(html).toContain('<strong>');
    expect(html).toContain('<em>');
  });

  it('markdown 模式对脚本内容进行消毒（DOMPurify）', () => {
    const { container } = render(<RichEditor markdown value={'<script>alert(1)</script>安全文本'} />);
    fireEvent.click(screen.getByText('预览'));
    const preview = container.querySelector('[data-preview]');
    const html = preview ? preview.innerHTML : '';
    expect(html).not.toContain('<script');
    expect(html).toContain('安全文本');
  });

  it('html 模式渲染文本域并回填值', () => {
    render(<RichEditor value="<p>富文本</p>" />);
    expect(screen.getByRole('textbox')).toHaveValue('<p>富文本</p>');
  });

  it('禁用态透传文本域', () => {
    render(<RichEditor disabled value="x" />);
    expect(screen.getByRole('textbox')).toBeDisabled();
  });
});
