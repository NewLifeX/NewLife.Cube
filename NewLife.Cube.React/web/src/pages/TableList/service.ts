import { request } from '@umijs/max';

export async function queryColumns(options?: { [key: string]: any }) {
  return request<ResponseStructure<any>>('/Admin/Role/GetColumns', {
    method: 'GET',
    ...(options || {}),
  });
}
