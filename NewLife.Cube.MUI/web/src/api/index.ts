import { createCubeApi } from '@cube/api-core';
import type { CubeApi } from '@cube/api-core';

export const api: CubeApi = createCubeApi({
  baseURL: import.meta.env.DEV ? '' : (import.meta.env.VITE_API_URL ?? ''),
  onUnauthorized() {
    window.location.href = '/login';
  },
});

export default api;
