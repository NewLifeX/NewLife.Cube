/**
 * CyberLayout - 赛博风格布局
 *
 * 特点：
 * - 默认深色主题，支持明暗切换
 * - 绿色/青色霓虹发光效果
 * - 左侧固定侧边栏 + 右侧内容区
 *
 * 使用方式：
 * ```typescript
 * import CyberLayout from '@newlifex/cube-vue/core/layouts/CyberLayout';
 * import { LayoutKey } from '@newlifex/cube-vue/core/composables/useProvideInject';
 *
 * initApp((app, { provide }) => {
 *   provide(app, LayoutKey, CyberLayout, { override: true });
 * });
 * ```
 */

export { default as CyberLayout } from './index.vue';
export { default as CyberSidebar } from './Sidebar/index.vue';
export { default as CyberNavbar } from './Navbar/index.vue';