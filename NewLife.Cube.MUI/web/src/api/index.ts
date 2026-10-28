import { createCubeApi } from '@newlifex/cube';
import type { CubeApi } from '@newlifex/cube';

export const api: CubeApi = createCubeApi({
  baseURL: import.meta.env.DEV ? '' : (import.meta.env.VITE_API_URL ?? ''),
  onUnauthorized() {
    window.location.href = '/login';
  },
});

export default api;
