import { request } from '@umijs/max';
import type { SortOrder } from 'antd/es/table/interface';
import type { RequestData } from '@ant-design/pro-components';

/** 字段类型枚举 */
export enum ColumnKind {
  LIST = 1,
  DETAIL = 2,
  ADD = 3,
  EDIT = 4,
  SEARCH = 5,
}

/** 获取字段信息 */
export async function getFields(type: string, kind: ColumnKind) {
  return request<ResponseStructure<CubeColumn[]>>(`${type}/GetFields`, {
    method: 'GET',
    params: { kind },
  });
}

/** 查询列表数据 */
export async function queryList(
  type: string,
  params: {
    current?: number;
    pageSize?: number;
    [key: string]: any;
  },
  sort: Record<string, SortOrder>,
): Promise<RequestData<Record<string, any>>> {
  const ext: Record<string, any> = {};

  // 排序
  if (Object.keys(sort).length > 0) {
    const sortKey = Object.keys(sort)[0];
    ext.sort = sortKey;
    ext.desc = sort[sortKey] === 'descend' ? 'True' : 'False';
  }

  // 搜索参数（过滤自带参数）
  const { current, pageSize, ...searchParams } = params;
  Object.entries(searchParams).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '') {
      ext[k] = v;
    }
  });

  const res = await request<ResponseStructure<Record<string, any>[]>>(type, {
    method: 'GET',
    params: {
      pageIndex: current,
      pageSize: pageSize,
      ...ext,
    },
  });
  return {
    data: res.data,
    success: res.code === 0,
    total: res.pager?.totalCount || 0,
  };
}

/** 获取单条详情 */
export async function getDetail(type: string, id: string | number) {
  return request<ResponseStructure<Record<string, any>>>(`${type}/Detail`, {
    method: 'GET',
    params: { id },
  });
}

/** 添加 */
export async function addItem(type: string, data: Record<string, any>) {
  return request<ResponseStructure<any>>(type, {
    method: 'POST',
    data,
  });
}

/** 修改 */
export async function updateItem(type: string, data: Record<string, any>) {
  return request<ResponseStructure<any>>(type, {
    method: 'PUT',
    data,
  });
}

/** 删除 */
export async function deleteItem(type: string, id: string | number) {
  return request<ResponseStructure<any>>(type, {
    method: 'DELETE',
    params: { id },
  });
}

/** 批量删除 */
export async function deleteSelect(type: string, keys: string[]) {
  return request<ResponseStructure<any>>(`${type}/DeleteSelect`, {
    method: 'POST',
    params: { keys: keys.join(',') },
  });
}

/** 导入文件 */
export async function importFile(type: string, file: File) {
  const formData = new FormData();
  formData.append('file', file);
  return request<ResponseStructure<any>>(`${type}/ImportFile`, {
    method: 'POST',
    data: formData,
  });
}

/** 获取导出下载 URL */
export function getExportUrl(type: string, format: string): string {
  return `${API_URL}/${type}/Export${format}`;
}

/** 获取图表数据 */
export async function getChartData(type: string) {
  return request<ResponseStructure<any[]>>(`${type}/GetChartData`, {
    method: 'GET',
  });
}
