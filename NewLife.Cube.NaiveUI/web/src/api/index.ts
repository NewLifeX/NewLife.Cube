import { createCubeApi } from '@cube/api-core';
import type { CubeApi } from '@cube/api-core';

/** 全局 API 实例 */
export const api: CubeApi = createCubeApi({
  baseURL: import.meta.env.DEV ? '' : (import.meta.env.VITE_API_URL ?? ''),
  onUnauthorized() {
    window.location.href = '/login';
  },
  onBusinessError(_code, message) {
    // 延迟导入避免循环依赖
    import('naive-ui').then(({ createDiscreteApi }) => {
      const { message: msg } = createDiscreteApi(['message']);
      msg.error(message);
    });
  },
});

export default api;
