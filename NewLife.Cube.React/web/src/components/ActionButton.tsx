/**
 * 动作按钮（AntD Button 薄封装，统一权限禁用）
 */
import { Button } from 'antd';
import type { ButtonProps } from 'antd';

export interface ActionButtonProps extends ButtonProps {
  /** 是否允许（false 时禁用） */
  allowed?: boolean;
}

export default function ActionButton({ allowed = true, disabled, ...rest }: ActionButtonProps) {
  return <Button {...rest} disabled={disabled || !allowed} />;
}
