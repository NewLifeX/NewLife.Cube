import type { AxiosRequestConfig } from 'axios';
import type { ApiResponse, SiteConfig } from './types';

type RequestFn = <T>(config: AxiosRequestConfig) => Promise<ApiResponse<T>>;

/**
 * 站点配置 API（/Cube/Site，匿名可访问）
 *
 * @param request 服务接口请求函数（不带 /api 前缀）
 */
export function createSiteApi(request: RequestFn) {
  return {
    /**
     * 获取站点配置
     *
     * 返回品牌、语言、版本与功能开关。品牌字段由服务端按当前租户合并，
     * 前端拿到的就是可直接渲染的有效值。
     *
     * @param tenant 租户标识（编号/编码/名称），为空时取当前请求上下文
     * @param noCache 是否跳过浏览器缓存（后台修改品牌配置后需要立即读取新值）
     */
    getSite: (tenant?: string, noCache = false) =>
      request<SiteConfig>({
        url: '/Cube/Site',
        method: 'get',
        params: {
          ...(tenant ? { tenant } : {}),
          // 服务端下发 max-age=60，追加时间戳绕过浏览器缓存
          ...(noCache ? { _t: Date.now() } : {}),
        },
      }),
  };
}
