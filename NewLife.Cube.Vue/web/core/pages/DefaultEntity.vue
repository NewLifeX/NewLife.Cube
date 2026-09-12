<template>
  <component
    :is="matchedMenu ? resolvedDefaultListPageComponent : (customViewComponent ?? resolvedPageNotFoundComponent)"
    v-bind="matchedMenu ? { title: matchedMenu.title ?? matchedMenu.name } : {}"
  />
</template>

<script setup lang="ts">
import { computed, defineAsyncComponent, inject, shallowRef, watch, watchEffect } from 'vue';
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
import FrameworkDefaultListPage from '@newlifex/cube-vue/core/views/index.vue';
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
watch(
  () => route.path,
  (path) => {
    if (matchedMenu.value) {
      customViewComponent.value = null;
      return;
    }
    if (hasMatchingCustomView(path)) {
      customViewComponent.value = defineAsyncComponent(
        resolvePageComponent(path) as () => Promise<Component>,
      );
    } else {
      customViewComponent.value = null;
    }
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
