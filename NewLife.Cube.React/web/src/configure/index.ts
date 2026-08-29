/**
 * 配置系统（对齐 Vue 皮肤 configure/index.ts）
 *
 * 配置优先级（从低到高）：
 * 1. 默认配置 defaultConfig
 * 2. 运行时配置 window._CUBE_CONFIG_（k8s 部署时注入，最高优先级）
 */
import type { CubeConfig } from './types';
import { defaultConfig } from './defaultConfig';

/**
 * 浅合并配置对象
 */
function mergeConfig(base: CubeConfig, patch: Partial<CubeConfig>): CubeConfig {
  const result: CubeConfig = {
    ...base,
    ...patch,
    base: { ...base.base, ...patch.base },
    request: { ...base.request, ...patch.request },
    auth: { ...base.auth, ...patch.auth },
    ui: { ...base.ui, ...patch.ui },
    menu: { ...base.menu, ...patch.menu },
    theme: { ...base.theme, ...patch.theme },
  };
  if (patch.ui?.layout) {
    result.ui.layout = {
      header: { ...base.ui.layout.header, ...patch.ui.layout.header },
      sider: { ...base.ui.layout.sider, ...patch.ui.layout.sider },
      footer: { ...base.ui.layout.footer, ...patch.ui.layout.footer },
    };
  }
  return result;
}

/**
 * 获取当前配置
 *
 * @example
 * ```ts
 * import { getConfig } from '@/configure';
 * const { base, request } = getConfig();
 * ```
 */
export function getConfig(): CubeConfig {
  let result: CubeConfig = { ...defaultConfig };

  // 合并运行时配置（最高优先级）
  if (typeof window !== 'undefined' && (window as unknown as { _CUBE_CONFIG_?: Partial<CubeConfig> })._CUBE_CONFIG_) {
    result = mergeConfig(result, (window as unknown as { _CUBE_CONFIG_: Partial<CubeConfig> })._CUBE_CONFIG_);
  }

  return result;
}

// 导出类型与默认配置
export type { CubeConfig, EnvConfig, BaseConfig, RequestConfig, AuthConfig, UIConfig, LayoutConfig, MenuConfig } from './types';
export { defaultConfig };
