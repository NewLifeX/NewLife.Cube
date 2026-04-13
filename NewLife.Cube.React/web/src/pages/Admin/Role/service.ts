import { request } from '@umijs/max';
import { SortOrder } from 'antd/es/table/interface';
import { RequestData } from '@ant-design/pro-components';

/** 查询列数据 */
export async function queryColumns(): Promise<ResponseStructure<CubeColumn[]>> {
  return request<ResponseStructure<CubeColumn[]>>('/Admin/Role/GetFields', {
    method: 'GET',
  });
}

/** 查询数据 */
export async function query(
  params: {
    // query
    /** 当前的页码 */
    current?: number;
    /** 页面的容量 */
    pageSize?: number;
    /** 关键字 */
    keyword?: string;
    /** 更新日期 */
    updateTimeRange?: string[];
  },
  sort: Record<string, SortOrder>,
  options?: { [key: string]: any },
): Promise<RequestData<API.RoleListItem>> {
  let ext = {};
  if (params.keyword) {
    ext = {
      Q: params.keyword,
    };
  }
  if (Object.keys(sort).length > 0) {
    ext = {
      ...ext,
      sort: Object.keys(sort)[0],
      desc: sort[Object.keys(sort)[0]] === 'descend' ? 'True' : 'False',
    };
  }
  if (params.updateTimeRange && params.updateTimeRange.length === 2) {
    ext = {
      ...ext,
      dtStart: params.updateTimeRange[0],
      dtEnd: params.updateTimeRange[1],
    };
  }
  const res = await request<ResponseStructure<API.RoleListItem[]>>('/Admin/Role', {
    method: 'GET',
    params: {
      pageIndex: params.current,
      pageSize: params.pageSize,
      limit: params.pageSize,
      offset: (params.current || 1) - 1,
      ...ext,
    },
    ...(options || {}),
  });
  return {
    data: res.data,
    success: res.code === 0,
    total: res.pager?.totalCount || 0,
  };
}
