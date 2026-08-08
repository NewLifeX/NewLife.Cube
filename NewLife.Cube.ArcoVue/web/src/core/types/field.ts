/** 表单 / 编辑控件类型 */
export type ControlType =
  | 'input'
  | 'textarea'
  | 'inputNumber'
  | 'switch'
  | 'datePicker'
  | 'timePicker'
  | 'select'
  | 'lov'
  | 'lovMulti'
  | 'cascader'
  | 'upload'
  | 'image'
  | 'json'
  | 'richHtml'
  | 'richMarkdown'
  | 'color'
  | 'icon'
  | 'email'
  | 'tel'
  | 'url'
  | 'readonly';

export type SearchControlType =
  | 'text'
  | 'number'
  | 'date'
  | 'datetime'
  | 'time'
  | 'lov'
  | 'lovMulti'
  | 'switch'
  | 'fileExists'
  | 'select'
  | 'cascader';

export type ListControlType =
  | 'text'
  | 'number'
  | 'boolean'
  | 'date'
  | 'time'
  | 'color'
  | 'icon'
  | 'image'
  | 'json'
  | 'html'
  | 'lov'
  | 'file'
  | 'select'
  | 'readonly'
  | 'url';

export interface FieldOption {
  value: string | number;
  label: string;
}

/** 统一字段元数据（由 DataField 归一） */
export interface FieldMeta {
  name: string;
  displayName?: string;
  /** 表单分组（对应后端 DataField.Category） */
  category?: string;
  typeName: string;
  itemType?: string;
  length?: number;
  precision?: number;
  scale?: number;
  nullable?: boolean;
  primaryKey?: boolean;
  readOnly?: boolean;
  required?: boolean;
  visible?: boolean;
  description?: string;
  lovCode?: string;
  multiple?: boolean;
  options?: FieldOption[];
  dataSource?: Record<string, string>;
  maxWidth?: number;
  url?: string;
  target?: string;
}

export interface SearchFieldMeta extends FieldMeta {
  searchType: SearchControlType;
}
