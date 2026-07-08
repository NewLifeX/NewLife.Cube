/**
 * 统一 API Composable — useCubeApi
 *
 * 合并了全局 API 客户端实例（cubeApi）与通用页面 API（usePageApi），
 * 提供从实例创建到 CRUD 方法的一站式导出。
 *
 * @example
 * ```ts
 * // 方式一：直接使用全局 cubeApi 实例（自定义请求）
 * import { cubeApi } from 'cube-front/core/composables/useCubeApi';
 * await cubeApi.client.post('/Area/Controller/Action', data);
 *
 * // 方式二：使用 usePageApi 获取 CRUD 方法集
 * import { usePageApi } from 'cube-front/core/composables/useCubeApi';
 * const api = usePageApi('ProcessCard', 'ProcessCard');
 * const { data } = await api.getList({ pageIndex: 0, pageSize: 20 });
 * ```
 */
import { createCubeApi } from '@cube/api-core';
import { getConfig } from '../configure';
import { ElMessage } from 'element-plus';
import type { ApiResponse, PageParams } from '@cube/api-core';

// ── 全局 API 客户端实例 ──────────────────────────────────────────────

const cfg = getConfig();

/**
 * cubeApi 全局实例
 *
 * 基于 @cube/api-core 创建，提供通用 CRUD 及认证 API。
 * baseUrl 统一从 cube-front 配置系统获取，
 * Token 存储使用 localStorage（与 cube-front core/utils/token.ts 一致）。
 */
const cubeApi = createCubeApi({
  baseURL: cfg.request.baseUrl,
  tokenStorage: 'localStorage',
  onFieldError: (fieldErrors) => {
    // 统一展示字段级验证错误（如"编码不可以为空！"），无需每个页面单独处理
    ElMessage.error(fieldErrors.map(e => e.message).join('；'));
  },
  onUnauthorized: () => {
    // cube-front 已处理 401 → 清除 token 并跳转登录页
    // 此处无需额外处理，仅做兜底
    if (!window.location.pathname.startsWith('/login')) {
      window.location.href = '/';
    }
  },
});

// ── 通用页面 API Composable ─────────────────────────────────────────

/**
 * 构建 API 路径前缀
 * @param area - 区域名称（PascalCase，如 'ProcessCard'）
 * @param controller - 控制器名称（PascalCase，如 'ProcessCard'）
 * @returns 路径前缀，示例：'/ProcessCard/ProcessCard'
 */
function buildApiPath(area: string, controller: string): string {
  return `/${area}/${controller}`;
}

/**
 * 创建通用页面 API
 *
 * 根据区域（Area）和控制器（Controller）生成 API 路径前缀，
 * 返回完整的 CRUD 操作方法集。业务页面直接使用此 composable，
 * 无需为每个模块创建独立的 API 文件。
 *
 * @param area - 区域名称（PascalCase，如 'ProcessCard'）
 * @param controller - 控制器名称（PascalCase，如 'ProcessCard'）
 * @returns 完整的 CRUD 操作方法集
 *
 * @example
 * ```ts
 * const api = usePageApi('ProcessCard', 'ProcessCard');
 *
 * // 获取分页列表
 * const { data, page } = await api.getList({ pageIndex: 0, pageSize: 20 });
 *
 * // 新增
 * await api.add({ code: 'PC-001', productName: 'xxx' });
 *
 * // 编辑
 * await api.update({ id: 1, code: 'PC-001' });
 *
 * // 删除
 * await api.remove(1);
 *
 * // 查看详情
 * const detail = await api.getDetail(1);
 * ```
 */
export function usePageApi(area: string, controller: string) {
  const type = buildApiPath(area, controller);

  return {
    /** 获取页面元数据（字段配置 + 页面设置） */
    getPage: () => cubeApi.page.getPage(type),

    /** 获取指定类型的字段列表 */
    getFields: (kind: number) => cubeApi.page.getFields(type, kind),

    /** 分页列表查询 */
    getList: <T = Record<string, unknown>>(params: PageParams) =>
      cubeApi.page.getList<T>(type, params),

    /** 查看详情 */
    getDetail: <T = Record<string, unknown>>(id: number | string) =>
      cubeApi.page.getDetail<T>(type, id),

    /** 新增 */
    add: (data: Record<string, unknown>) =>
      cubeApi.page.add(type, data),

    /** 编辑 */
    update: (data: Record<string, unknown>) =>
      cubeApi.page.update(type, data),

    /** 删除单条 */
    remove: (id: number | string) =>
      cubeApi.page.remove(type, id),

    /** 批量删除 */
    deleteSelect: (keys: (number | string)[]) =>
      cubeApi.page.deleteSelect(type, keys),

    /** 上传文件 */
    uploadFile: (file: File, options?: { id?: number; title?: string; }) =>
      cubeApi.page.uploadFile(type, file, options),

    /** 导入文件 */
    importFile: (file: File) =>
      cubeApi.page.importFile(type, file),

    /** 获取导出下载 URL */
    getExportUrl: (format: string) =>
      cubeApi.page.getExportUrl(type, format),

    /** 获取图表数据 */
    getChartData: () =>
      cubeApi.page.getChartData(type),

    /** 调用自定义 Action */
    getAction: <T = unknown>(action: string) =>
      cubeApi.client.request<ApiResponse<T>>({
        url: `${type}/${action}`,
        method: 'get',
      }).then(res => res.data),

    /**
     * 字典/枚举查询
     * @param codes - C# 类型全名字符串，多个用逗号分隔
     * @returns ApiResponse，data 为 { 类型全名: [{ label, value }] }
     * @example api.lookup('SmartMES.Data.Equipments.EquipmentKinds')
     */
    lookup: (codes: string) =>
      cubeApi.page.lookup(codes) as Promise<ApiResponse<Record<string, Array<{ label: string; value: number }>>>>,
  };
}

export default cubeApi;
