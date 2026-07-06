/**
 * Vue 应用初始化模块
 *
 * 这个模块负责创建和配置 Vue 应用实例，包括：
 * - 创建应用实例和根组件
 * - 注册全局插件（路由、状态管理、国际化、UI库）
 * - 提供默认的依赖注入值
 * - 支持外部配置和依赖注入覆盖
 * - 挂载应用到 DOM
 *
 * @example 基本使用
 * ```typescript
 * import { initApp } from './core/initApp';
 *
 * // 使用默认配置初始化应用
 * initApp();
 * ```
 *
 * @example 使用自定义配置
 * ```typescript
 * import { initApp } from './core/initApp';
 * import CustomLayout from './CustomLayout.vue';
 * import { LayoutKey } from './core/composables/useProvideInject';
 *
 * initApp((app, { provide, hasProvided }) => {
 *   // 检查是否已经提供了布局
 *   if (!hasProvided(LayoutKey)) {
 *     provide(app, LayoutKey, CustomLayout);
 *   }
 *
 *   // 或者强制覆盖默认布局
 *   provide(app, LayoutKey, CustomLayout, { override: true });
 * });
 * ```
 */
import { type App as App2 } from 'vue';
import { createApp } from 'vue';
import { createPinia } from 'pinia';
import ElementPlus from 'element-plus';
import 'element-plus/dist/index.css';
import App from './App.vue';
import router from './router';
import i18n from './i18n';
import './global.css';
import {
  appProvide,
  hasProvided,
  LayoutKey,
  getProvidedKeys,
} from './composables/useProvideInject';
import { currentComponent } from './composables/useLayout';
import { registerPageSections } from './utils/pageSections';
import autoSectionModules from 'virtual:cube-front-sections';
import CyberLayout from './layouts/CyberLayout/index.vue';

/**
 * 应用配置选项接口
 *
 * 定义了可以通过配置函数传递的选项类型。
 * 这个接口可以扩展以支持更多的配置选项。
 */
export interface AppConfigOptions {
  /** 自定义布局组件 */
  Layout?: unknown;
  /** 自定义主题配置 */
  Theme?: unknown;
  /** 其他自定义配置项 */
  [key: string]: unknown;
}

/**
 * 高级配置函数类型
 *
 * 这个函数类型定义了在应用初始化过程中可以执行的配置操作。
 * 配置函数会在插件安装完成后、应用挂载前执行。
 *
 * @param app - Vue 应用实例
 * @param utils - 提供的工具函数集合
 * @param utils.provide - 安全的依赖注入提供函数
 * @param utils.hasProvided - 检查某个键是否已经被提供的函数
 * @param utils.getProvidedKeys - 获取所有已提供键的函数
 */
export type ConfigureFunction = (
  app: App2<Element>,
  utils: {
    provide: typeof appProvide;
    hasProvided: typeof hasProvided;
    getProvidedKeys: typeof getProvidedKeys;
  },
) => void;

/**
 * initApp 选项接口
 *
 * @property configure - 可选的应用配置函数，在插件安装后、挂载前执行
 * @property sections  - 可选的额外 Section 模块映射（优先级高于插件自动扫描结果）。
 *                       通常无需手动传入；Vite 插件 `vite:cube-front-sections` 会自动
 *                       扫描子应用的 `src/views/` 目录并通过虚拟模块 `virtual:cube-front-sections`
 *                       提供给框架。仅在需要手动覆盖时使用此字段。
 *
 * @example 标准子应用入口（无需配置，插件自动处理）
 * ```typescript
 * import { initApp } from 'cube-front/core/initApp';
 * initApp();
 * ```
 *
 * @example 手动补充额外 Section（与插件自动发现合并）
 * ```typescript
 * initApp({
 *   sections: {
 *     './views/special/ListToolbar.vue': () => import('./views/special/ListToolbar.vue'),
 *   },
 * });
 * ```
 */
export interface InitAppOptions {
  configure?: ConfigureFunction;
  sections?: Record<string, () => Promise<{ default: unknown; }>>;
}

/**
 * 初始化 Vue 应用
 *
 * 这个函数创建并配置一个完整的 Vue 应用实例。它按照以下顺序执行：
 * 1. 创建 Vue 应用实例
 * 2. 安装核心插件（路由、状态管理、国际化、UI库）
 * 3. 执行外部配置函数（允许外部覆盖内部配置）
 * 4. 提供默认的依赖注入值（如果外部没有提供的话）
 * 5. 挂载应用到 DOM
 * 6. 暴露全局调试接口
 *
 * @param configure - 可选的配置函数，在插件安装后、应用挂载前执行
 *
 * @example 基本初始化
 * ```typescript
 * // 使用默认配置初始化应用
 * initApp();
 * ```
 *
 * @example 自定义布局
 * ```typescript
 * import CustomLayout from './layouts/CustomLayout.vue';
 *
 * initApp((app, { provide, hasProvided }) => {
 *   // 提供自定义布局，优先于默认布局
 *   provide(app, LayoutKey, CustomLayout, { override: true });
 * });
 * ```
 *
 * @example 条件性配置
 * ```typescript
 * initApp((app, { provide, hasProvided, getProvidedKeys }) => {
 *   // 检查是否已经提供了布局
 *   if (!hasProvided(LayoutKey)) {
 *     provide(app, LayoutKey, MyCustomLayout);
 *   }
 *
 *   // 调试：打印所有已提供的键
 *   console.log('已提供的依赖:', getProvidedKeys());
 * });
 * ```
 *
 * @example 提供多个依赖
 * ```typescript
 * initApp((app, { provide }) => {
 *   // 提供自定义主题
 *   provide(app, ThemeKey, {
 *     primaryColor: '#007fff',
 *     backgroundColor: '#f5f5f5'
 *   });
 *
 *   // 提供应用配置
 *   provide(app, ConfigKey, {
 *     apiUrl: 'https://api.example.com',
 *     version: '1.0.0'
 *   });
 * });
 * ```
 */
export const initApp = async (optionsOrConfigure?: InitAppOptions | ConfigureFunction) => {
  // 向后兼容：支持直接传入 configure 函数
  const options: InitAppOptions =
    typeof optionsOrConfigure === 'function'
      ? { configure: optionsOrConfigure }
      : (optionsOrConfigure ?? {});

  const { configure, sections } = options;

  const pinia = createPinia();

  const app = createApp(App);

  // 安装核心插件
  app.use(router);
  app.use(pinia);
  app.use(i18n); // 添加 i18n 插件
  app.use(ElementPlus, {
    table: {
      showOverflowTooltip: true,
    },
  });

  // 注册页面 Section 覆盖组件
  // 优先级：options.sections（手动指定）> virtual:cube-front-sections（插件自动扫描）
  const mergedSections = { ...autoSectionModules, ...(sections ?? {}) };
  if (Object.keys(mergedSections).length > 0) {
    registerPageSections(app, mergedSections);
  }

  // 执行外部配置函数，允许外部覆盖内部配置
  // 这里外部可以提供自定义的依赖注入值，优先于默认值
  await configure?.(app, {
    provide: appProvide,
    hasProvided,
    getProvidedKeys,
  });

  // 提供默认的依赖注入值
  // 只有在外部没有提供的情况下才提供默认值
  // 使用 currentComponent（根据 localStorage 恢复的布局）而不是硬编码的 CyberLayout
  if (!hasProvided(LayoutKey)) {
    const layoutComponent = currentComponent.value || CyberLayout;
    appProvide(app, LayoutKey, layoutComponent, { track: true });
  }

  app.mount('#app');

  // 全局暴露路由和状态管理实例，方便调试和外部访问
  // 在生产环境中可以考虑移除这些全局暴露
  window.router = router;
  window.store = pinia;
};
