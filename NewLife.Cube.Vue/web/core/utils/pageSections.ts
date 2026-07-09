import type { App } from 'vue';
import { defineAsyncComponent } from 'vue';
import { PageSectionRegistryKey, SectionKeyMap } from '@newlifex/cube-vue/core/composables/useSections';
import type { Component } from 'vue';

type GlobLoader = () => Promise<{ default: unknown }>;
type GlobModule = Record<string, GlobLoader>;
type SectionRegistry = Record<string, Record<string, GlobLoader>>;

/**
 * 解析 glob key，提取路由路径和 Section 名称。
 *
 * 规则：
 *   - 匹配 `./views/<folderPath>/<PascalCaseName>.vue`（与 core/views 保持一致）
 *   - 文件名必须大写字母开头（PascalCase），否则跳过（排除 index.vue、form.vue 等）
 *   - 文件名去掉 .vue 后须在 SectionKeyMap 中存在，否则跳过
 *
 * 示例：
 *   './views/user/ListSearchBar.vue' → { routePath: '/user', sectionName: 'ListSearchBar' }
 *   './views/order/list/FormContent.vue' → { routePath: '/order/list', sectionName: 'FormContent' }
 */
function parseGlobKey(key: string): { routePath: string; sectionName: string } | null {
  const match = key.match(/^\.\/views\/(.+)\/([A-Z][A-Za-z]+)\.vue$/);
  if (!match) return null;
  const [, folderPath, sectionName] = match;
  return { routePath: '/' + folderPath, sectionName };
}

/**
 * 将 import.meta.glob 结果注册为全局页面 Section 覆盖注册表。
 *
 * 在应用启动时（initApp 回调中）调用，一次性注册所有页面的覆盖组件：
 *
 * ```typescript
 * import { registerPageSections } from '@newlifex/cube-vue/core/utils/pageSections';
 * const viewModules = import.meta.glob('./views/**\/*.vue');
 * initApp((app) => {
 *   registerPageSections(app, viewModules);
 * });
 * ```
 *
 * 注意：`import.meta.glob` 调用必须写在应用代码中（Vite 静态分析限制），
 *       不能在框架核心代码中自动执行。
 *
 * 目录约定：各子应用使用 `src/views/` 文件夹存放页面 Section 覆盖组件，
 *            与框架的 `core/views/` 保持一致的约定。
 *
 * @param app     Vue App 实例
 * @param modules import.meta.glob 结果
 */
export function registerPageSections(app: App, modules: GlobModule): void {
  const registry: SectionRegistry = {};

  for (const [key, loader] of Object.entries(modules)) {
    const parsed = parseGlobKey(key);
    if (!parsed) continue;

    const { routePath, sectionName } = parsed;
    if (!SectionKeyMap[sectionName]) continue; // 非 Section 文件，跳过

    registry[routePath] ??= {};
    registry[routePath][sectionName] = loader;
  }

  app.provide(PageSectionRegistryKey, registry);
}

/**
 * （供 DefaultListPage / DefaultFormPage 内部使用）
 * 将注册表中当前路由对应的覆盖组件 provide 给子组件树。
 *
 * 须在 <script setup> 中调用，且在 inject 语句之前执行。
 */
export function applyPageSectionOverrides(
  routePath: string,
  registry: Record<string, Record<string, GlobLoader>>,
  provideKey: (key: symbol, comp: Component) => void,
): void {
  const overrides = registry[routePath] ?? {};
  for (const [name, loader] of Object.entries(overrides)) {
    const injectionKey = SectionKeyMap[name];
    if (injectionKey) {
      provideKey(
        injectionKey as unknown as symbol,
        defineAsyncComponent(loader as () => Promise<{ default: Component }>),
      );
    }
  }
}
