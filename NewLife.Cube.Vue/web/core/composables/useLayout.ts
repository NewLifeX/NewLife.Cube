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
 * 布局组合式函数
 * 在 Topnav / RootLayout 等组件中使用
 */
export function useLayout() {
  function setLayout(id: string): void {
    if (!registeredLayouts.value.some((l) => l.id === id)) return;
    currentLayoutId.value = id;
    localStorage.setItem(STORAGE_KEY, id);
  }

  // 返回数组而非 ref，模板中 Vue 会自动处理
  return {
    layouts: registeredLayouts.value,
    currentLayoutId,
    currentLayout,
    currentComponent,
    setLayout,
  };
}