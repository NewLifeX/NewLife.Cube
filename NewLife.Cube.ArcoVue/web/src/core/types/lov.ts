export interface LovEnumOption {
  value: string;
  label: string;
  extra?: string | null;
}

export interface LovListConfig {
  requestUrl: string;
  method: string;
  pageable: boolean;
  pageNumField: string | null;
  pageSizeField: string | null;
  dataPath: string | null;
  totalPath: string | null;
  fixedParams: Record<string, string> | null;
}

export interface LovSearchField {
  field: string;
  title: string;
  componentType: string;
  paramType: string;
  required: boolean;
  defaultValue: string | null;
  refLovCode: string | null;
}

export interface LovTableColumn {
  field: string;
  title: string;
  width: number;
  align: string;
  sortable: boolean;
  refLovCode: string | null;
  formatType: string | null;
}

export interface LovEnumMeta {
  lovCode: string;
  type: 'ENUM';
  name: string;
  options: LovEnumOption[];
}

export interface LovListMeta {
  lovCode: string;
  type: 'LIST';
  name: string;
  valueField: string | null;
  labelField: string | null;
  listConfig: LovListConfig | null;
  searchFields: LovSearchField[];
  tableColumns: LovTableColumn[];
}

export type LovMetaItem = LovEnumMeta | LovListMeta;

export interface LovMetaResponse {
  meta: LovMetaItem[];
  inlineEnums?: Record<string, LovEnumOption[]> | null;
}

export interface LovListDataRequest {
  lovCode: string;
  params?: Record<string, unknown>;
  pageNum?: number;
  pageSize?: number;
}

export interface LovListDataResponse<T = Record<string, unknown>> {
  data: T[];
  total: number;
}

export type LovBatchLabelResponse = Record<string, string>;
