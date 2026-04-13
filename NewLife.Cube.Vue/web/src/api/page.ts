import { PageProps } from '../model/api/common';
import { Column } from '../model/api/page';
import request from '/@/utils/request';

export enum ColumnKind {
	LIST = 1,
	DETAIL,
	ADD,
	EDIT,
	SEARCH
}
/**
 * （不建议写成 request.post(xxx)，因为这样 post 时，无法 params 与 data 同时传参）
 *
 * 登录api接口集合
 * @method signIn 用户登录
 * @method signOut 用户退出登录
 */
export function usePageApi() {
	return {
    getColumns: (type: string, kind: ColumnKind) => {
			return request<Column[]>({
				url: `${type}/GetFields`,
				method: 'get',
				params: {
					kind
				}
			});
		},
		getTableData: <T extends {}>(type: string, params: EmptyObjectType & PageProps) => {
			return request<T[]>({
				url: type,
				method: 'get',
				params
			});
		},
		getTableDetail: <T extends {}>(type: string, id: number, data?: EmptyObjectType) => {
			return request<T>({
				url: type + '/Detail',
				method: 'get',
				params: {
					id,
					...data
				}
			});
		},
		getTableDetailByUrl: <T extends {}>(url: string, id: number, data?: EmptyObjectType) => {
			return request<T>({
				url: url,
				method: 'get',
				params: {
					id,
					...data
				}
			});
		},
		setTableItem: (type: string, data: EmptyObjectType) => {
			return request({
				url: type,
				method: 'put',
				data
			});
		},
		addTableItem: (type: string, data: EmptyObjectType) => {
			return request({
				url: type,
				method: 'post',
				data
			});
		},
		delTableItem: (type: string, id: number) => {
			return request({
				url: type,
				method: 'delete',
				params: {
					id
				}
			});
		},
		lookUp: (codes: string) => {
			return request<{ [k in string]: EmptyObjectType[] }>({
				url: '/Cube/Lookup',
				method: 'get',
				params: {
					codes
				}
			});
		},
		upload: (data: { r: string, file: File }) => {
			const formData = new FormData();
			formData.append('file', data.file);
			return request<{ [k in string]: EmptyObjectType[] }>({
				url: '/Admin/File/UploadLayui',
				method: 'post',
				headers: {
					"Content-Type": 'multipart/form-data'
				},
				data: formData,
				params: {
					r: 'user'
				}
			});
		},
		/** 获取导出下载 URL */
		getExportUrl: (type: string, format: string): string => {
			const baseUrl = import.meta.env.DEV ? '/base-api' : (import.meta.env.VITE_API_URL || '');
			return `${baseUrl}/${type}/Export${format}`;
		},
		/** 导入文件 */
		importFile: (type: string, file: File) => {
			const formData = new FormData();
			formData.append('file', file);
			return request<any>({
				url: `${type}/ImportFile`,
				method: 'post',
				headers: {
					"Content-Type": 'multipart/form-data'
				},
				data: formData
			});
		},
		/** 批量删除选中项 */
		deleteSelect: (type: string, keys: string[]) => {
			return request({
				url: `${type}/DeleteSelect`,
				method: 'post',
				params: {
					keys: keys.join(',')
				}
			});
		},
		/** 批量删除全部 */
		deleteAll: (type: string) => {
			return request({
				url: `${type}/DeleteAll`,
				method: 'post',
			});
		},
		/** 获取图表数据 */
		getChartData: (type: string) => {
			return request<any[]>({
				url: `${type}/GetChartData`,
				method: 'get',
			});
		},
	};
}