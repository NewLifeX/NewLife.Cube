/**
 * 富文本编辑器（markdown / html）
 *
 * markdown 用文本域 + marked 渲染预览（复用 AI 助手同款 marked + DOMPurify，零新增依赖）；
 * html 用文本域。完整 WYSIWYG 编辑器（@wangeditor/react 等）在 CMP-3 阶段按选型替换，本组件保持接口不变。
 */
import { useState } from 'react';
import { Input, Segmented } from 'antd';
import { marked } from 'marked';
import DOMPurify from 'dompurify';

export interface RichEditorProps {
  value?: string;
  onChange?: (value: string) => void;
  markdown?: boolean;
  placeholder?: string;
  disabled?: boolean;
  rows?: number;
}

/** markdown → 安全 HTML（复用 marked + DOMPurify，与 AI 助手一致） */
function renderMarkdown(src: string): string {
  const html = marked.parse(src || '', { breaks: true, gfm: true }) as string;
  return DOMPurify.sanitize(html);
}

export default function RichEditor({ value, onChange, markdown, placeholder, disabled, rows = 8 }: RichEditorProps) {
  const [mode, setMode] = useState<'edit' | 'preview'>('edit');

  if (markdown) {
    return (
      <div>
        <Segmented
          size="small"
          options={[
            { label: '编辑', value: 'edit' },
            { label: '预览', value: 'preview' },
          ]}
          value={mode}
          onChange={(v) => setMode(v as 'edit' | 'preview')}
          style={{ marginBottom: 4 }}
        />
        {mode === 'edit' ? (
          <Input.TextArea
            value={value}
            onChange={(e) => onChange?.(e.target.value)}
            placeholder={placeholder}
            disabled={disabled}
            rows={rows}
          />
        ) : (
          <div
            data-preview
            dangerouslySetInnerHTML={{ __html: renderMarkdown(value ?? '') }}
            style={{ border: '1px solid #d9d9d9', borderRadius: 6, padding: 8, minHeight: 120 }}
          />
        )}
      </div>
    );
  }

  return (
    <Input.TextArea
      value={value}
      onChange={(e) => onChange?.(e.target.value)}
      placeholder={placeholder}
      disabled={disabled}
      rows={rows}
    />
  );
}
