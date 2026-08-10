import { createCubeApi } from '@cube/api-core';
import { clearLocalProfile } from '@/core/utils/userProfile';

const cubeApi = createCubeApi({
  // WebAPI 版实体/后台接口固定 /api 前缀（如 /api/Admin/User/GetPage）；
  // /Auth /Cube 服务动作由 api-core 去掉此前缀
  baseURL: '/api',
  onUnauthorized() {
    // 全页跳转会丢内存 store；须清 localStorage，避免串用户壳偏好
    clearLocalProfile();
    if (!window.location.pathname.startsWith('/login')) {
      window.location.href = '/login';
    }
  },
  // 业务/字段错误由调用方 formatApiError 展示，避免与页面重复 toast
});

export default cubeApi;
