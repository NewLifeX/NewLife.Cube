import { FormRule } from "@form-create/element-ui";
import { ColumnConfig, ColumnProp, FormType } from "../form/model/form";

export interface TableColumn {
  // 字段名
	prop: string;
  // 字段中文名
  label?: string;
  // 组件
	component?: keyof FormType;
  // 宽度
  width?: string | number;
  // 是否勾选显示
  isCheck?: boolean;
  // 自定义组件插槽
  slot?: string;
  // 是否必填
	required?: boolean;
  // 参数
	props?: {
    [k in string]: any;
  } & ColumnProp<TableColumn['component']>;
  // 规则
  rules?: FormRule[];
  // 排序下标
  index?: number;
  // 是否渲染
  if?: boolean;
  // 是否可排序
  sort?: boolean;
  // 单元格链接，支持 {Id}/{Name} 变量替换
  url?: string;
  // 链接目标
  target?: string;
  // 数据动作
  dataAction?: string;
}

export enum Auth {
  'LOOK' = 1,
  'ADD' = 2,
  'SET' = 4,
  'DEL' = 8,
  'EXPORT' = 16,
  'IMPORT' = 32,
  'ALL' = 255,
}

interface Param {
	pageIndex?: number;
	pageSize?: number;
}

export interface TableMoreProps {
	total?: number;
  loading?: boolean;
  isBorder?: boolean;
  isSerialNo?: boolean;
  isSelection?: boolean;
  isOperate?: boolean;
	operateWidth?: number;
	authId?: number;
	data: EmptyObjectType[];
	stat?: EmptyObjectType | null;
	columns: TableColumn[];
	search: ColumnConfig[];
	param: Param;
	pagerVisible: boolean;
	searchData: EmptyObjectType;
}
