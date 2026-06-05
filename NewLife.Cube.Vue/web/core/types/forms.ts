import type { FormItemRule } from 'element-plus';

/** 表单验证规则集合 */
export interface FormValidationRules {
  [key: string]: FormItemRule | FormItemRule[] | undefined;
}

/** 表单操作类型 */
export interface FormOperations {
  validate: () => Promise<boolean>;
  resetFields: () => void;
  clearValidate: () => void;
}

/** 对话框状态 */
export interface DialogState {
  visible: boolean;
  title: string;
  type: 'add' | 'edit' | 'view';
}
