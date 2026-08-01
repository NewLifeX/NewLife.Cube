import { createCubeApi } from '@cube/api-core';

const cubeApi = createCubeApi({
  baseURL: '',
  onUnauthorized() {
    if (!window.location.pathname.startsWith('/login')) {
      window.location.href = '/login';
    }
  },
  // 业务/字段错误由调用方 formatApiError 展示，避免与页面重复 toast
});

export default cubeApi;
