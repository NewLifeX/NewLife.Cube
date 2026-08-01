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
  | 'numberRange'
  | 'dateRange'
  | 'datetimeRange'
  | 'timeRange'
  | 'lov'
  | 'lovMulti'
  | 'switch'
  | 'fileExists'
  | 'select';

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
