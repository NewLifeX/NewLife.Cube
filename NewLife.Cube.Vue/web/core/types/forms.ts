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

/**
 * 表单字段列配置
 *
 * 用于命令式弹窗的表单配置模式，覆盖 ModalContainer 和 useModal 的渲染需求。
 *
 * 为何定义在 core/types/ 而非复用 src/ 下的 ColumnConfig：
 * core/ 层是框架运行时，不应依赖 src/（旧版遗留代码）；
 * 此处按需精简，仅包含 ModalContainer 实际使用的字段，避免拉入 @form-create/element-ui 等重型依赖。
 */
export interface FormColumnConfig {
  /** 字段名 */
  prop: string | string[];
  /** 字段中文名称 */
  label?: string;
  /** 组件类型 */
  component?: string;
  /** 是否渲染 */
  if?: boolean | ((data: Record<string, unknown>) => boolean);
  /** 是否显示 */
  show?: boolean | ((data: Record<string, unknown>) => boolean);
  /** 自定义组件插槽 */
  slot?: string;
  /** 必填 */
  required?: boolean;
  /** 组件参数 */
  props?: Record<string, unknown>;
  /** 校验规则 */
  rules?: unknown[];
  /** 排序下标 */
  index?: number;
  /** 所占列数 */
  col?: number | Record<string, number>;
  /** 所属分组 */
  group?: string;
}
