import { computed, defineAsyncComponent, ref, watch, type Component } from 'vue';
import { useRoute } from 'vue-router';
import { getSectionLoader } from '@/core/composables/useSections';
import { routeToApiPrefix } from '@/core/utils/url';

/** DynamicPage 组件 props 类型（与 DynamicPage.vue defineProps 泛型逐字一致） */
interface DynamicPageProps {
  type?: string;
  authId?: number;
}

/**
 * DynamicPage 页面全部业务 TS：typePath 解析与视图区段覆盖组件加载（自 DynamicPage.vue script setup 原样搬移）。
 * 契约：仅接收 type / authId；不读取布局/主题 store。
 */
export function useDynamicPage(props: DynamicPageProps) {
  const route = useRoute();

  const typePath = computed(() => {
    if (props.type) return props.type;
    const metaType = route.meta.typePath as string | undefined;
    if (metaType) return metaType;
    return routeToApiPrefix(route.path);
  });

  const authId = computed(() => props.authId ?? (route.meta.menuId as number | undefined));

  const overrideComp = ref<Component | null>(null);

  async function resolveOverride() {
    const loader = getSectionLoader(typePath.value, 'DefaultListPage');
    if (!loader) {
      overrideComp.value = null;
      return;
    }
    overrideComp.value = defineAsyncComponent(loader as () => Promise<{ default: Component }>);
  }

  watch(typePath, resolveOverride, { immediate: true });

  return {
    typePath,
    authId,
    overrideComp,
  };
}
