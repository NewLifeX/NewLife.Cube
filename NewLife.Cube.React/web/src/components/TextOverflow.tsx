/**
 * 文本溢出（超长省略 + Tooltip）
 */
import { Tooltip, Typography } from 'antd';

export interface TextOverflowProps {
  text?: string;
  maxWidth?: number | string;
  lines?: number;
}

export default function TextOverflow({ text, maxWidth, lines = 1 }: TextOverflowProps) {
  if (!text) return <span>-</span>;
  return (
    <Tooltip title={text}>
      <Typography.Text
        style={{ maxWidth, display: 'inline-block' }}
        ellipsis={{ tooltip: text }}
      >
        {text}
      </Typography.Text>
    </Tooltip>
  );
}
