/**
 * API 响应处理辅助工具
 * 提供统一的数据处理方法，简化业务代码
 */

import type { ListResponse, ApiPageInfo } from '@newlifex/cube-vue/core/types/common';

/**
 * 处理列表数据响应 - 兼容request.ts拦截器已处理的数据格式
 * @param data request拦截器处理后的数据
 * @returns 标准化的列表响应格式
 */
export function apiDataToList<T = Record<string, unknown>>(data: unknown): ListResponse<T> {
  // 情况1: request拦截器已处理，返回包含data和page的对象（分页列表）
  if (data && typeof data === 'object' && data !== null && 'data' in data && 'page' in data) {
    const response = data as { data: unknown; page: ApiPageInfo; stat?: unknown };
    const list = Array.isArray(response.data) ? response.data : [];
    const total = response.page?.totalCount || 0;
    return {
      list,
      total,
      page: response.page,
    };
  }
  // 情况2: 直接返回数组的响应（无分页信息）
  else if (Array.isArray(data)) {
    return {
      list: data,
      total: data.length,
    };
  }
  // 情况3: 单个对象响应，包装成数组格式
  else if (data && typeof data === 'object' && data !== null) {
    return {
      list: [data as T],
      total: 1,
    };
  }
  // 情况4: 其他格式，返回空数组
  else {
    return {
      list: [],
      total: 0,
    };
  }
}

/**
 * 标准的删除操作处理
 * @param apiCall API调用函数
 * @param onSuccess 成功后的回调函数
 * @param confirmMessage 确认消息（可选）
 */
export async function handleDeleteOperation(
  apiCall: () => Promise<unknown>,
  onSuccess: () => void,
  confirmMessage: string = '确认删除吗？',
): Promise<void> {
  const { ElMessageBox } = await import('element-plus');

  try {
    await ElMessageBox.confirm(confirmMessage, '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning',
    });

    await apiCall();
    onSuccess();
  } catch (error) {
    // 如果是用户取消，则什么都不做
    if (error === 'cancel') {
      return;
    }
    // 其他错误已经在 request 拦截器中处理
  }
}

/**
 * 标准的表单提交处理
 * @param formRef 表单引用
 * @param apiCall API调用函数
 * @param onSuccess 成功后的回调函数
 */
export async function handleFormSubmit(
  formRef: { validate: () => Promise<boolean> } | null,
  apiCall: () => Promise<unknown>,
  onSuccess: () => void,
): Promise<void> {
  if (!formRef) return;

  try {
    const valid = await formRef.validate();
    if (valid) {
      await apiCall();
      onSuccess();
    }
  } catch {
    // 验证失败或API调用失败，错误已在相应地方处理
  }
}

/**
 * 处理单个对象响应 - 用于获取单个实体详情
 * @param data request拦截器处理后的数据
 * @returns 单个对象或null
 */
export function apiDataToSingle<T = Record<string, unknown>>(data: unknown): T | null {
  // 情况1: 单个对象响应
  if (data && typeof data === 'object' && data !== null && !Array.isArray(data)) {
    return data as T;
  }
  // 情况2: 数组响应，取第一个
  else if (Array.isArray(data) && data.length > 0) {
    return data[0] as T;
  }
  // 情况3: 其他情况返回null
  else {
    return null;
  }
}
