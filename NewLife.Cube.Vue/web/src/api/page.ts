/**
 * 列表 CRUD 及数据操作 API — 薄包装层（委托到 @cube/api-core cubeApi）
 *
 * 保持原有 usePageApi() 调用形式不变，方便存量代码零改动迁移。
 */
import cubeApi from './index';
import type { PageParams } from '@cube/api-core';

export { FieldKind as ColumnKind } from '@cube/api-core';

export function usePageApi() {
return {
getColumns: (type: string, kind: number) => cubeApi.page.getFields(type, kind),
getTableData: <T extends object>(type: string, params: Record<string, unknown> & PageParams) =>
cubeApi.page.getList<T>(type, params),
getTableDetail: <T extends object>(type: string, id: number, data?: Record<string, unknown>) =>
cubeApi.page.getDetail<T>(type, id, data),
getTableDetailByUrl: <T extends object>(url: string, id: number, data?: Record<string, unknown>) =>
cubeApi.page.getDetail<T>(url, id, data),
setTableItem: (type: string, data: Record<string, unknown>) => cubeApi.page.update(type, data),
addTableItem: (type: string, data: Record<string, unknown>) => cubeApi.page.add(type, data),
delTableItem: (type: string, id: number) => cubeApi.page.remove(type, id),
lookUp: (codes: string) => cubeApi.page.lookup(codes),
/** 上传文件，type 为实体路径前缀 */
upload: (type: string, file: File, options?: { id?: number; title?: string }) =>
cubeApi.page.uploadFile(type, file, options),
/** 获取导出下载 URL */
getExportUrl: (type: string, format: string): string => cubeApi.page.getExportUrl(type, format),
/** 导入文件 */
importFile: (type: string, file: File) => cubeApi.page.importFile(type, file),
/** 按 ID 批量删除 */
deleteSelect: (type: string, keys: string[]) => cubeApi.page.deleteSelect(type, keys),
/** 按条件删除 */
deleteAll: (type: string, params?: Record<string, unknown>) => cubeApi.page.deleteAll(type, params),
/** 获取图表数据 */
getChartData: (type: string) => cubeApi.page.getChartData(type),
/** 获取页面元数据 */
getPageMeta: (type: string) => cubeApi.page.getPage(type),
};
}
