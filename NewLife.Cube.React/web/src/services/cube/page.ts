/**
 * 页面 CRUD 及数据操作 API — 薄包装层（委托到 @cube/api-core cubeApi）
 *
 * 保持原有函数签名不变，方便存量页面零改动迁移。
 */
import cubeApi from '@/services/cubeApi';

export async function getFields(type: string, kind: number) {
  return cubeApi.page.getFields(type, kind);
}

export async function getPage(type: string) {
  return cubeApi.page.getPage(type);
}

export async function queryList<T = any>(type: string, params: Record<string, any>) {
  return cubeApi.page.getList<T>(type, params);
}

export async function getDetail<T = any>(type: string, id: number | string) {
  return cubeApi.page.getDetail<T>(type, id);
}

export async function addItem(type: string, data: Record<string, any>) {
  return cubeApi.page.add(type, data);
}

export async function updateItem(type: string, data: Record<string, any>) {
  return cubeApi.page.update(type, data);
}

export async function deleteItem(type: string, id: number | string) {
  return cubeApi.page.remove(type, id);
}

/** 批量删除，keys 为主键数组 */
export async function deleteSelect(type: string, keys: (number | string)[]) {
  return cubeApi.page.deleteSelect(type, keys);
}

export async function deleteAll(type: string, params?: Record<string, any>) {
  return cubeApi.page.deleteAll(type, params);
}

export async function importFile(type: string, file: File) {
  return cubeApi.page.importFile(type, file);
}

export function getExportUrl(type: string, format: string): string {
  return cubeApi.page.getExportUrl(type, format);
}

export async function getChartData(type: string) {
  return cubeApi.page.getChartData(type);
}

// 字段类型枚举别名（保持与 Vue 皮肤一致）
export { FieldKind as ColumnKind } from '@cube/api-core';

export async function lookup(codes: string) {
  return cubeApi.page.lookup(codes);
}

export async function uploadFile(type: string, file: File, options?: { id?: number; title?: string }) {
  return cubeApi.page.uploadFile(type, file, options);
}
