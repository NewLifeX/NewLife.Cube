/**
 * 富文本编辑器（markdown / html）
 *
 * 轻量实现：markdown 用文本域 + 简单预览；html 用文本域。
 * 完整编辑器（@wangeditor/react 等）在 CMP-3 阶段按选型替换，本组件保持接口不变。
 */
import { useState } from 'react';
import { Input, Segmented } from 'antd';

export interface RichEditorProps {
  value?: string;
  onChange?: (value: string) => void;
  markdown?: boolean;
  placeholder?: string;
  disabled?: boolean;
  rows?: number;
}

/** 极简 markdown 渲染（标题/粗体/列表/代码块），仅供预览，完整渲染由 CMP-3 接管 */
function renderMarkdown(src: string): string {
  return src
    .replace(/^### (.*)$/gm, '<h5>$1</h5>')
    .replace(/^## (.*)$/gm, '<h4>$1</h4>')
    .replace(/^# (.*)$/gm, '<h3>$1</h3>')
    .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
    .replace(/\*(.*?)\*/g, '<em>$1</em>')
    .replace(/^- (.*)$/gm, '<li>$1</li>')
    .replace(/(<li>[\s\S]*?<\/li>)/g, '<ul>$1</ul>')
    .replace(/```([\s\S]*?)```/g, '<pre><code>$1</code></pre>')
    .replace(/\n/g, '<br/>');
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
