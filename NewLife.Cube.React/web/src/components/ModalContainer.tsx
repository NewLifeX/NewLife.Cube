/**
 * Modal 容器（antd Modal 薄封装，统一确认交互样式）
 */
import { Modal } from 'antd';
import type { ModalProps } from 'antd';

export interface ModalContainerProps extends ModalProps {
  /** 确认按钮文案 */
  okText?: string;
  /** 取消按钮文案 */
  cancelText?: string;
}

export default function ModalContainer({ okText = '确定', cancelText = '取消', ...rest }: ModalContainerProps) {
  return <Modal {...rest} okText={okText} cancelText={cancelText} />;
}
