/**
 * JSON 编辑器（文本域 + 格式化/校验）
 */
import { useState } from 'react';
import { App, Input, Space, Tooltip } from 'antd';
import { CheckOutlined, FormatPainterOutlined } from '@ant-design/icons';

export interface JsonEditorProps {
  value?: string;
  onChange?: (value: string) => void;
  placeholder?: string;
  disabled?: boolean;
  rows?: number;
}

export default function JsonEditor({ value, onChange, placeholder, disabled, rows = 6 }: JsonEditorProps) {
  const { message } = App.useApp();
  const [error, setError] = useState(false);

  const format = () => {
    try {
      const obj = JSON.parse(value ?? '');
      onChange?.(JSON.stringify(obj, null, 2));
      setError(false);
      message.success('格式化成功');
    } catch {
      setError(true);
      message.error('JSON 格式不正确');
    }
  };

  return (
    <Space.Compact style={{ width: '100%' }}>
      <Input.TextArea
        value={value}
        onChange={(e) => {
          onChange?.(e.target.value);
          try {
            JSON.parse(e.target.value || 'null');
            setError(false);
          } catch {
            setError(true);
          }
        }}
        placeholder={placeholder}
        disabled={disabled}
        rows={rows}
        status={error ? 'error' : undefined}
        style={{ fontFamily: 'monospace' }}
      />
      <Tooltip title="格式化 JSON">
        <Input
          type="button"
          readOnly
          value="{}"
          onClick={format}
          disabled={disabled}
          style={{ width: 44, cursor: 'pointer', textAlign: 'center' }}
        />
      </Tooltip>
      {!error && value && (
        <Tooltip title="格式正确">
          <span style={{ color: '#52c41a', padding: '6px 4px' }}>
            <CheckOutlined />
          </span>
        </Tooltip>
      )}
    </Space.Compact>
  );
}
