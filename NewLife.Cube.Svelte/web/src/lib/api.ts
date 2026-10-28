import { createCubeApi, type CubeApi } from '@newlifex/cube';

let api: CubeApi | null = null;

export function getApi(): CubeApi {
  if (!api) {
    api = createCubeApi({ baseURL: '' });
  }
  return api;
}
