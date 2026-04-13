import { FormRule } from "@form-create/element-ui";
import { DefineComponent } from "vue";
import { forms } from '../component';

export type FormType = typeof forms;
export type InstanceProps<T extends DefineComponent> = InstanceType<T>['$props'];
export type ColumnProp<T> = T extends keyof FormType ? InstanceType<FormType[T]>['$props'] : undefined;

export interface ColumnConfig {
  /** 字段名 */
	prop: string | string[];
  /** 字段中文名称 */
  label?: string;
  /** 组件 */
	component?: keyof FormType;
  /** 是否渲染 */
	if?: boolean | ((data: EmptyObjectType) => boolean);
  /** 是否显示 */
	show?: boolean | ((data: EmptyObjectType) => boolean);
  /** 自定义组件插槽 */
  slot?: string;
  /** 必填 */
	required?: boolean;
  /** 组件参数 */
	props?: ColumnProp<ColumnConfig['component']>;
  /** 校验规则 */
  rules?: FormRule[];
  /** 排序下标 */
  index?: number;
  /** 所占列数 */
	col?: number | Col;
  /** 所属分组 */
	group?: string;
  // isCheck?: boolean;
}

export interface Col {
  xs: number;
  sm: number;
  md: number;
  lg: number;
  xl: number;
}

export interface FormProps {
  config: ColumnConfig[];
  modelValue: EmptyObjectType;
}
export interface FormEmits {
  (e: 'update:modelValue', val: EmptyObjectType): void;
  (e: 'change', ...val: any[] ): void;
}