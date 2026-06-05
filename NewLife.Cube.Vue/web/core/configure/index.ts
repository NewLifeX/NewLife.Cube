import type { CubeFrontConfig, EnvConfig } from './types';
import { defaultConfig } from './defaultConfig';
import { deepMerge } from '../utils/object';
import { configData, currentEnv } from 'virtual:cube-front-config';

/**
 * 获取当前配置
 * 从虚拟模块导入配置数据并进行合并处理
 * 配置优先级（从低到高）：
 * 1. 默认配置 (defaultConfig)
 * 2. 通用配置 (configs/config.ts)
 * 3. 环境配置 (configs/config.{env}.ts)
 * 4. 运行时配置 (window._CUBE_CONFIG_) - k8s 部署时注入
 * @returns 当前配置
 */
export function getConfig(): CubeFrontConfig {
  let result = { ...defaultConfig };

  // 1. 合并通用配置 config.ts
  if (configData.general) {
    result = deepMerge(result, configData.general) as CubeFrontConfig;
  }

  // 2. 合并环境特定配置 config.{env}.ts
  if (configData[currentEnv]) {
    result = deepMerge(result, configData[currentEnv]) as CubeFrontConfig;
  }

  // 3. 合并运行时配置 (最高优先级)
  // 在浏览器环境中可通过 window._CUBE_CONFIG_ 覆盖配置
  if (typeof window !== 'undefined' && (window as any)._CUBE_CONFIG_) {
    result = deepMerge(result, (window as any)._CUBE_CONFIG_) as CubeFrontConfig;
  }

  return result;
}

/**
 * 合并配置
 * @param customConfig 自定义配置
 * @returns 合并后的配置
 */
export function mergeConfig(customConfig: EnvConfig): CubeFrontConfig {
  const baseConfig = getConfig();
  return deepMerge(baseConfig, customConfig) as CubeFrontConfig;
}

// 导出配置类型
export type {
  CubeFrontConfig,
  EnvConfig,
  BaseConfig, // 替代 AppConfig
  AuthConfig, // 替代 LoginConfig
  RequestConfig, // 替代 RequestConfig
  UIConfig, // 包含原 LayoutConfig 和 ThemeConfig
} from './types';

// 导出默认配置
export { defaultConfig } from './defaultConfig';
