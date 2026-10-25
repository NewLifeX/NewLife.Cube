<template>
  <component
    :is="matchedMenu ? resolvedDefaultListPageComponent : (customViewComponent ?? entityPageComponent ?? resolvedPageNotFoundComponent)"
    v-bind="pageProps"
  />
</template>

<script setup lang="ts">
import { computed, defineAsyncComponent, inject, ref, shallowRef, watch, watchEffect } from 'vue';
import type { Component } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useMenuStore, type FlatMenuItem } from '@newlifex/cube-vue/core/stores/menu';
import {
  DefaultListPageKey,
  PageNotFoundKey,
  PageSectionRegistryKey,
} from '@newlifex/cube-vue/core/composables/useSections';
import {
  registerMenuRoutes,
  resolvePageComponent,
  hasMatchingCustomView,
} from '@newlifex/cube-vue/core/utils/menuRoutes';
import { probeEntityPage } from '@newlifex/cube-vue/core/utils/entityPage';
import FrameworkDefaultListPage from '@newlifex/cube-vue/core/views/index.vue';
import FrameworkAutoPage from '@newlifex/cube-vue/core/views/AutoPage.vue';
import FrameworkPageNotFound from './PageNotFound.vue';

type SectionRegistry = Record<string, Record<string, () => Promise<{ default: unknown }>>>;

const route = useRoute();
const router = useRouter();
const menuStore = useMenuStore();

const injectedDefaultListPageComponent = inject(DefaultListPageKey, FrameworkDefaultListPage);
const injectedPageNotFoundComponent = inject(PageNotFoundKey, FrameworkPageNotFound);
const sectionRegistry = inject(PageSectionRegistryKey, {} as SectionRegistry);

const matchedMenu = computed<FlatMenuItem | undefined>(() =>
  menuStore.flatMenus?.find((item) => item.path === route.path),
);

const resolvedDefaultListPageComponent = computed<Component>(() => {
  const loader = sectionRegistry[route.path]?.DefaultListPage;
  return loader
    ? defineAsyncComponent(loader as () => Promise<{ default: Component }>)
    : injectedDefaultListPageComponent;
});

const resolvedPageNotFoundComponent = computed<Component>(() => {
  const loader = sectionRegistry[route.path]?.PageNotFound;
  return loader
    ? defineAsyncComponent(loader as () => Promise<{ default: Component }>)
    : injectedPageNotFoundComponent;
});

// 无菜单匹配但存在对应自定义视图（如 MVC 隐藏页 Configs/ConfigData 经链接访问）。
// 用 shallowRef + watch 只在路径变化时创建一次异步组件，避免 computed 反复生成新构造器导致重挂载白屏。
const customViewComponent = shallowRef<Component | null>(null);

// 无菜单且无自定义视图时，探测后端是否存在该控制器页面（未开启多租户的租户页、被可见性过滤的隐藏页），
// 存在则按动态页渲染，与 MVC「控制器路由可直接访问」等价。
const entityPageComponent = shallowRef<Component | null>(null);
const entityPageTitle = ref('');
let probeToken = 0;

/** 页头标题：菜单名 > 实体显示名（探测所得） */
const pageProps = computed<Record<string, unknown>>(() => {
  if (matchedMenu.value) return { title: matchedMenu.value.title ?? matchedMenu.value.name };
  if (entityPageComponent.value && entityPageTitle.value) return { title: entityPageTitle.value };
  return {};
});

watch(
  () => route.path,
  async (path) => {
    customViewComponent.value = null;
    entityPageComponent.value = null;
    entityPageTitle.value = '';

    if (matchedMenu.value) return;

    if (hasMatchingCustomView(path)) {
      customViewComponent.value = defineAsyncComponent(
        resolvePageComponent(path) as () => Promise<Component>,
      );
      return;
    }

    const token = ++probeToken;
    const probe = await probeEntityPage(path);
    if (token !== probeToken) return;

    // 直接复用静态引入的 AutoPage 组件（loader 必须 resolve 到组件本身，不能包成模块对象）
    entityPageComponent.value = probe.exists ? (FrameworkAutoPage as Component) : null;
    entityPageTitle.value = probe.displayName ?? '';
  },
  { immediate: true },
);

watchEffect(() => {
  const menus = menuStore.flatMenus;
  if (!menus?.length || !matchedMenu.value) {
    return;
  }

  registerMenuRoutes(router, menus, route.path);
  menuStore.setActiveMenuByPath(route.path);
});
</script>
