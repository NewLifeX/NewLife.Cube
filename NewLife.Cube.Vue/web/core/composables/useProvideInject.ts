/**
 * Vue 3 Provide/Inject 安全管理工具
 *
 * 这个模块提供了一套完整的 provide/inject 管理解决方案，解决了以下问题：
 * 1. 检查某个 key 是否已经被注入过
 * 2. 确保外部注入的值具有更高优先级
 * 3. 类型安全的注入和提供
 * 4. 防止重复注入和意外覆盖
 *
 * @example 基本使用
 * ```typescript
 * // 在应用初始化时
 * appProvide(app, LayoutKey, MyLayout);
 *
 * // 在组件中使用
 * const Layout = useLayoutRequired(DefaultLayout);
 * ```
 *
 * @example 外部覆盖
 * ```typescript
 * // 外部可以覆盖内部提供的值
 * appProvide(app, LayoutKey, CustomLayout, { override: true });
 * ```
 */
import { inject, type InjectionKey, type App, type Component } from 'vue';

// 全局跟踪已提供的 keys，用于避免重复注入和检查注入状态
const globalProvidedKeys = new Set<string | symbol>();

/**
 * 检查某个 key 是否已经被 provide
 *
 * 这个函数用于判断某个注入键是否已经在应用中被提供过，
 * 可以避免重复注入或在需要时进行条件性注入。
 *
 * @param key - 要检查的注入键，可以是字符串或 Symbol
 * @returns 如果键已经被提供则返回 true，否则返回 false
 *
 * @example
 * ```typescript
 * if (!hasProvided(LayoutKey)) {
 *   appProvide(app, LayoutKey, DefaultLayout);
 * }
 * ```
 *
 * @example 在配置函数中使用
 * ```typescript
 * const configure = (app, { hasProvided, provide }) => {
 *   if (hasProvided(LayoutKey)) {
 *     console.log('Layout already provided');
 *   } else {
 *     provide(app, LayoutKey, CustomLayout);
 *   }
 * };
 * ```
 */
export function hasProvided(key: string | symbol): boolean {
  return globalProvidedKeys.has(key);
}

/**
 * 安全地使用 inject，提供默认值和类型安全
 *
 * 这个函数是 Vue 的 inject 函数的安全包装器，提供了更好的类型安全性
 * 和错误处理。它可以处理工厂函数作为默认值的情况。
 *
 * @template T - 注入值的类型
 * @param key - 注入键，可以是 InjectionKey、字符串或 Symbol
 * @param defaultValue - 默认值，当注入失败时使用
 * @param options - 配置选项
 * @param options.treatDefaultAsFactory - 是否将默认值当作工厂函数处理
 * @returns 注入的值或默认值，可能为 undefined
 *
 * @example 基本使用
 * ```typescript
 * const theme = safeInject(ThemeKey, { color: 'blue' });
 * ```
 *
 * @example 使用工厂函数
 * ```typescript
 * const config = safeInject(
 *   ConfigKey,
 *   () => ({ mode: 'development' }),
 *   { treatDefaultAsFactory: true }
 * );
 * ```
 *
 * @example 在组件中使用
 * ```typescript
 * const Layout = safeInject(LayoutKey, DefaultLayout);
 * if (Layout) {
 *   // 安全地使用 Layout 组件
 * }
 * ```
 */
export function safeInject<T>(
  key: InjectionKey<T> | string | symbol,
  defaultValue?: T,
  options?: {
    treatDefaultAsFactory?: boolean;
  }
): T | undefined {
  const treatDefaultAsFactory = options?.treatDefaultAsFactory ?? false;

  if (treatDefaultAsFactory && typeof defaultValue === 'function') {
    return inject(key, defaultValue, true);
  } else {
    return inject(key, defaultValue);
  }
}

/**
 * 安全地使用 inject，确保返回值不为 undefined
 *
 * 这个函数是 safeInject 的严格版本，它保证返回值不会是 undefined。
 * 如果注入失败且没有有效的默认值，它会抛出错误。
 *
 * @template T - 注入值的类型
 * @param key - 注入键，可以是 InjectionKey、字符串或 Symbol
 * @param defaultValue - 默认值，当注入失败时使用（必须提供）
 * @param options - 配置选项
 * @param options.treatDefaultAsFactory - 是否将默认值当作工厂函数处理
 * @returns 注入的值或默认值，保证不为 undefined
 * @throws {Error} 当注入失败且没有有效默认值时抛出错误
 *
 * @example 基本使用
 * ```typescript
 * // 确保总是有 Layout 组件可用
 * const Layout = safeInjectRequired(LayoutKey, DefaultLayout);
 * // Layout 现在保证不为 undefined
 * ```
 *
 * @example 使用工厂函数
 * ```typescript
 * const config = safeInjectRequired(
 *   ConfigKey,
 *   () => ({ mode: 'production' }),
 *   { treatDefaultAsFactory: true }
 * );
 * ```
 *
 * @example 在关键组件中使用
 * ```typescript
 * const router = safeInjectRequired(RouterKey, createRouter());
 * // 如果没有提供 router 且 createRouter() 返回 undefined，会抛出错误
 * ```
 */
export function safeInjectRequired<T>(
  key: InjectionKey<T> | string | symbol,
  defaultValue: T,
  options?: {
    treatDefaultAsFactory?: boolean;
  }
): T {
  const result = safeInject(key, defaultValue, options);

  if (result === undefined) {
    throw new Error(`No provider found for injection "${String(key)}" and no valid default value provided`);
  }

  return result;
}

/**
 * 应用级别的安全 provide
 *
 * 这个函数用于在应用级别安全地提供依赖注入值。它提供了重复检查、
 * 覆盖控制和跟踪功能，避免意外的重复注入或未授权的覆盖。
 *
 * @param app - Vue 应用实例
 * @param key - 注入键，可以是字符串或 Symbol
 * @param value - 要提供的值
 * @param options - 配置选项
 * @param options.override - 是否允许覆盖已存在的值，默认为 false
 * @param options.track - 是否跟踪这个键到全局记录中，默认为 true
 * @returns 如果成功提供则返回 true，如果被阻止（已存在且不允许覆盖）则返回 false
 *
 * @example 基本使用
 * ```typescript
 * // 提供默认布局
 * appProvide(app, LayoutKey, DefaultLayout);
 * ```
 *
 * @example 允许覆盖
 * ```typescript
 * // 外部可以覆盖内部提供的布局
 * appProvide(app, LayoutKey, CustomLayout, { override: true });
 * ```
 *
 * @example 不跟踪的临时提供
 * ```typescript
 * // 提供临时值，不记录到全局跟踪中
 * appProvide(app, 'temp-config', tempConfig, { track: false });
 * ```
 *
 * @example 在配置函数中使用
 * ```typescript
 * const configure = (app, { provide }) => {
 *   // 尝试提供自定义主题
 *   const success = provide(app, ThemeKey, customTheme);
 *   if (!success) {
 *     console.log('Theme already provided by framework');
 *   }
 * };
 * ```
 */
export function appProvide(
  app: App,
  key: string | symbol,
  value: unknown,
  options?: {
    override?: boolean;
    track?: boolean;
  }
): boolean {
  const { override = false, track = true } = options || {};

  if (!override && hasProvided(key)) {
    console.warn(`Key "${String(key)}" has already been provided. Use override: true to replace it.`);
    return false;
  }

  app.provide(key, value);

  if (track) {
    globalProvidedKeys.add(key);
  }

  return true;
}

/**
 * 创建类型安全的 injection key
 *
 * 这个函数用于创建类型安全的注入键，返回一个带有类型信息的 Symbol。
 * 使用 InjectionKey 可以提供更好的 TypeScript 类型推断和编译时检查。
 *
 * @template T - 注入值的类型
 * @param description - 键的描述，用于调试和日志记录
 * @returns 类型安全的 InjectionKey
 *
 * @example 创建基本类型键
 * ```typescript
 * const ConfigKey = createInjectionKey<AppConfig>('AppConfig');
 * ```
 *
 * @example 创建组件类型键
 * ```typescript
 * const ModalKey = createInjectionKey<Component>('ModalComponent');
 * ```
 *
 * @example 创建复杂类型键
 * ```typescript
 * interface ThemeConfig {
 *   colors: Record<string, string>;
 *   fonts: string[];
 * }
 * const ThemeKey = createInjectionKey<ThemeConfig>('Theme');
 * ```
 */
export function createInjectionKey<T>(description: string): InjectionKey<T> {
  return Symbol(description) as InjectionKey<T>;
}

/**
 * 预定义的 injection keys
 *
 * 这些是框架内置的常用注入键，为常见的用例提供了开箱即用的类型安全性。
 *
 * @example 使用预定义键
 * ```typescript
 * // 在应用中提供布局
 * appProvide(app, LayoutKey, MyLayout);
 *
 * // 在组件中使用
 * const Layout = useLayoutRequired(DefaultLayout);
 * ```
 */

/** 布局组件注入键 - 用于提供和注入应用的主要布局组件 */
export const LayoutKey = createInjectionKey<Component>('Layout');

/** 主题配置注入键 - 用于提供和注入应用的主题配置 */
export const ThemeKey = createInjectionKey<Record<string, unknown>>('Theme');

/** 应用配置注入键 - 用于提供和注入应用的全局配置 */
export const ConfigKey = createInjectionKey<Record<string, unknown>>('Config');

/**
 * Layout 相关的专用 composable
 *
 * 这个函数提供了一个便捷的方式来注入布局组件，支持可选的默认值。
 * 如果没有提供布局且没有默认值，将返回 undefined。
 *
 * @template T - 布局组件的类型，继承自 Component
 * @param defaultLayout - 可选的默认布局组件
 * @returns 注入的布局组件或默认布局组件，可能为 undefined
 *
 * @example 基本使用
 * ```typescript
 * const Layout = useLayout();
 * if (Layout) {
 *   // 使用布局组件
 * }
 * ```
 *
 * @example 使用默认布局
 * ```typescript
 * const Layout = useLayout(DefaultLayout);
 * // Layout 现在是注入的布局或 DefaultLayout
 * ```
 *
 * @example 在组件中条件渲染
 * ```typescript
 * <template>
 *   <component :is="Layout" v-if="Layout">
 *     <slot />
 *   </component>
 *   <div v-else>
 *     <!-- 无布局时的回退内容 -->
 *   </div>
 * </template>
 *
 * <script setup>
 * const Layout = useLayout();
 * </script>
 * ```
 */
export function useLayout<T extends Component = Component>(defaultLayout?: T): T | undefined {
  return safeInject(LayoutKey, defaultLayout) as T | undefined;
}

/**
 * Layout 相关的专用 composable - 必须返回值版本
 *
 * 这个函数是 useLayout 的严格版本，保证总是返回一个布局组件。
 * 如果没有注入布局且没有提供有效的默认值，将抛出错误。
 *
 * @template T - 布局组件的类型，继承自 Component
 * @param defaultLayout - 必需的默认布局组件
 * @returns 注入的布局组件或默认布局组件，保证不为 undefined
 * @throws {Error} 当没有注入布局且默认值无效时抛出错误
 *
 * @example 基本使用
 * ```typescript
 * const Layout = useLayoutRequired(DefaultLayout);
 * // Layout 保证不为 undefined
 * ```
 *
 * @example 在组件中安全使用
 * ```typescript
 * <template>
 *   <component :is="Layout">
 *     <slot />
 *   </component>
 * </template>
 *
 * <script setup>
 * import DefaultLayout from './DefaultLayout.vue';
 *
 * const Layout = useLayoutRequired(DefaultLayout);
 * // 可以安全地使用 Layout，无需检查 undefined
 * </script>
 * ```
 *
 * @example 在路由守卫中使用
 * ```typescript
 * router.beforeEach((to, from, next) => {
 *   try {
 *     const Layout = useLayoutRequired(DefaultLayout);
 *     // 确保有可用的布局
 *     next();
 *   } catch (error) {
 *     console.error('No layout available:', error);
 *     next('/error');
 *   }
 * });
 * ```
 */
export function useLayoutRequired<T extends Component = Component>(defaultLayout: T): T {
  return safeInjectRequired(LayoutKey, defaultLayout) as T;
}

/**
 * 获取所有已提供的 keys
 *
 * 这个函数返回一个只读数组，包含所有已经通过 appProvide 函数提供的注入键。
 * 主要用于调试、日志记录或运行时检查。
 *
 * @returns 所有已提供的注入键的只读数组
 *
 * @example 调试使用
 * ```typescript
 * console.log('Provided keys:', getProvidedKeys());
 * // 输出: ['Layout', 'Theme', 'Config']
 * ```
 *
 * @example 运行时检查
 * ```typescript
 * const providedKeys = getProvidedKeys();
 * const hasTheme = providedKeys.includes(ThemeKey);
 * if (!hasTheme) {
 *   console.warn('Theme not provided, using default');
 * }
 * ```
 *
 * @example 在开发工具中使用
 * ```typescript
 * // 开发模式下暴露到全局对象
 * if (process.env.NODE_ENV === 'development') {
 *   window.__DEBUG_PROVIDED_KEYS__ = getProvidedKeys;
 * }
 * ```
 */
export function getProvidedKeys(): readonly (string | symbol)[] {
  return Array.from(globalProvidedKeys);
}
