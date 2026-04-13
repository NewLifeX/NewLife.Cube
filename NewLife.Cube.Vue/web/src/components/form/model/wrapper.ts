export interface WrapperProps {
  wrapper: 'div' | 'dialog' | 'drawer';
  title?: string;
  visible: boolean
}

export interface WrapperEmits {
  (e: 'update:visible', val: boolean): void;
}