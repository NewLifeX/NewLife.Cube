/**
 * 微前端路由注册中心
 * 负责各子应用路由的注册、匹配和导航
 */

import type { Router } from 'vue-router';
import type { ConfigRoute } from './typings';
import microAppConfigs from 'virtual:cube-front-micro-apps';

// 定义应用信息类型
interface AppInfo {
  name: string;
  prefix?: string;
  routes: ConfigRoute[];
}

// 存储所有已注册的应用及其路由信息
const appRegistry = new Map<string, AppInfo>();

// 存储路由前缀与应用的映射
const prefixMap = new Map<string, string>();

// 添加路由加载状态标记
let routesInitialized = false;

/** 微应用配置类型 */
export interface MicroAppConfig {
  name: string;
  packageName: string;
  prefix?: string;
}

/** 微应用配置项类型 */
export interface MicroAppConfigItem {
  name: string;
  module: () => Promise<{
    routes: ConfigRoute[];
  }>;
  prefix?: string;
}

/**
 * 注册应用路由
 * @param {string} appName - 应用名称
 * @param {Array} routes - 应用路由配置
 * @param {Function?} loadApp - 加载应用的方法
 * @param {string?} appPrefix - 应用路由前缀
 */
export function registerAppRoutes(
  appName: string,
  router: Router,
  routes: ConfigRoute[],
  appPrefix?: string,
): void {
  if (!appName || !routes) {
    console.error('注册应用路由失败：缺少必要参数');
    return;
  }

  // 检查应用是否已存在
  const existingApp = appRegistry.get(appName);
  if (existingApp) {
    // 合并路由而不是覆盖
    existingApp.routes.push(...routes);
    console.log(`应用 ${appName} 路由已合并，新增 ${routes.length} 个路由`);
  } else {
    // 注册新应用信息
    appRegistry.set(appName, {
      name: appName,
      routes,
      prefix: appPrefix,
    });
  }

  // 如果有前缀，建立前缀与应用的映射
  if (appPrefix) {
    prefixMap.set(appPrefix, appName);
  }

  routes.forEach((route) => {
    router.addRoute(route);
  });

  console.log(`应用 ${appName} 路由注册成功`);
}

/**
 * 匹配路由所属的应用
 * @param {string} path - 当前路径
 * @returns {Object|null} - 匹配的应用信息
 */
export function matchAppByRoute(path: string): AppInfo | null | undefined {
  if (!path) return null;

  // 获取路径的第一级
  const firstSegment = path.split('/')[1] || '';

  // 1. 优先匹配前缀
  const appName = prefixMap.get(firstSegment);
  if (appName) {
    return appRegistry.get(appName);
  }

  // 2. 遍历所有应用，查找匹配的路由
  for (const [_, appInfo] of appRegistry) {
    // 检查应用的路由是否匹配当前路径
    const matchedRoute = appInfo.routes.find((route) => {
      // 简单路由匹配逻辑，实际可能需要更复杂的匹配算法
      const routePath = route.path;
      return path === routePath || path.startsWith(`${routePath}/`);
    });

    if (matchedRoute) {
      return appInfo;
    }
  }

  return null;
}

/**
 * 获取所有已注册的应用信息
 * @returns {Array} - 应用信息列表
 */
export function getRegisteredApps(): AppInfo[] {
  return Array.from(appRegistry.values());
}

/**
 * 检查微应用路由是否已初始化完成
 * @returns {boolean} - 是否初始化完成
 */
export function isRoutesInitialized(): boolean {
  return routesInitialized;
}

/** 注册微前端应用，并注册路由 */
export async function initAppRoutes(router: Router): Promise<void> {
  // 重置初始化标记
  routesInitialized = false;
  console.log('开始初始化微应用路由...', microAppConfigs);
  try {
    // 遍历所有微应用配置
    for (const appConfig of microAppConfigs) {
      try {
        // 调用 module 函数获取模块内容
        const appModule = await appConfig.module();

        if (appModule.routes) {
          console.log('加载应用模块成功', appConfig.name, appModule);

          // 注册应用路由
          registerAppRoutes(
            appConfig.name,
            router,
            appModule.routes,
            // 如果需要前缀，可以从配置中获取
            appConfig.prefix,
          );
        } else {
          console.warn(`应用 ${appConfig.name} 没有导出路由配置`);
        }
      } catch (error) {
        console.error(`加载应用 ${appConfig.name} 失败:`, error);
      }
    }

    // 所有应用路由加载完成
    routesInitialized = true;
    console.log('所有微应用路由初始化完成');
  } catch (error) {
    console.error('初始化微应用路由失败:', error);
    throw error;
  }
}
