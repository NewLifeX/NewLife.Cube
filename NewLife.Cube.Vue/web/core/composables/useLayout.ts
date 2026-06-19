import { ref, computed, type Component } from 'vue';
import TopMenuLayout from '../layouts/TopMenu/index.vue';
import CyberLayout from '../layouts/CyberLayout/index.vue';

/**
 * 布局选项描述
 */
export interface LayoutOption {
  id: string;
  label: string;
  icon: string;
  description?: string;
  component: Component;
}

export const STORAGE_KEY = 'cube-layout';

/**
 * 内置布局列表（模块级常量，HMR 不会改变定义）
 */
export const BUILT_IN_LAYOUTS: LayoutOption[] = [
  {
    id: 'top-menu',
    label: '顶部菜单',
    icon: '⊟',
    description: '顶部导航栏 + 内容区布局',
    component: TopMenuLayout,
  },
  {
    id: 'cyber',
    label: '赛博风格',
    icon: '◉',
    description: '深色科技风格 + 霓虹发光效果',
    component: CyberLayout,
  },
];

// 模块级响应式状态
export const registeredLayouts = ref<LayoutOption[]>(BUILT_IN_LAYOUTS);
export const currentLayoutId = ref<string>('');

// 计算当前布局组件
const currentLayout = computed<LayoutOption | undefined>(() =>
  registeredLayouts.value.find((l) => l.id === currentLayoutId.value),
);

export const currentComponent = computed<Component | undefined>(
  () => currentLayout.value?.component,
);

// 从 localStorage 恢复布局选择
function restoreLayoutSelection() {
  const stored = localStorage.getItem(STORAGE_KEY);
  const valid = stored && registeredLayouts.value.some((l) => l.id === stored);
  currentLayoutId.value = valid ? stored! : (registeredLayouts.value[0]?.id ?? '');
}

// 初始化时恢复布局选择
restoreLayoutSelection();

/**
 * 注册自定义布局
 *
 * 将新布局添加到 registeredLayouts 中，
 * 并支持设为当前布局。
 *
 * @param option - 布局选项
 * @param setAsCurrent - 是否立即切换为该布局（默认 false）
 *
 * @example
 * ```typescript
 * import { registerLayout } from 'cube-front/core/composables/useLayout';
 * import AuroraLayout from './layouts/AuroraLayout/index.vue';
 *
 * registerLayout({
 *   id: 'aurora',
 *   label: '极光蓝绿',
 *   icon: '◉',
 *   description: '极光蓝绿风格布局',
 *   component: AuroraLayout,
 * }, true);
 * ```
 */
export function registerLayout(option: LayoutOption, setAsCurrent = false): void {
  // 避免重复注册
  const exist = registeredLayouts.value.find((l) => l.id === option.id);
  if (exist) {
    exist.component = option.component;
    exist.label = option.label;
    exist.icon = option.icon;
    exist.description = option.description;
  } else {
    registeredLayouts.value.push({ ...option });
  }

  if (setAsCurrent) {
    currentLayoutId.value = option.id;
    localStorage.setItem(STORAGE_KEY, option.id);
  }
}

/**
 * 模块级 setLayout，供外部在 setup 外使用
 */
export function setLayout(id: string): void {
  if (!registeredLayouts.value.some((l) => l.id === id)) return;
  currentLayoutId.value = id;
  localStorage.setItem(STORAGE_KEY, id);
}

/**
 * 布局组合式函数
 * 在 Topnav / RootLayout 等组件中使用
 */
export function useLayout() {
  function setLayoutFromHook(id: string): void {
    setLayout(id);
  }

  // 返回数组而非 ref，模板中 Vue 会自动处理
  return {
    layouts: registeredLayouts.value,
    currentLayoutId,
    currentLayout,
    currentComponent,
    setLayout: setLayoutFromHook,
  };
}