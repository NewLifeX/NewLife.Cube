<template>
  <component
    :is="matchedMenu ? resolvedDefaultListPageComponent : resolvedPageNotFoundComponent"
    v-bind="matchedMenu ? { title: matchedMenu.title ?? matchedMenu.name } : {}"
  />
</template>

<script setup lang="ts">
import { computed, defineAsyncComponent, inject, watchEffect } from 'vue';
import type { Component } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useMenuStore, type FlatMenuItem } from '@newlifex/cube-vue/core/stores/menu';
import {
  DefaultListPageKey,
  PageNotFoundKey,
  PageSectionRegistryKey,
} from '@newlifex/cube-vue/core/composables/useSections';
import { registerMenuRoutes } from '@newlifex/cube-vue/core/utils/menuRoutes';
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

watchEffect(() => {
  const menus = menuStore.flatMenus;
  if (!menus?.length || !matchedMenu.value) {
    return;
  }

  registerMenuRoutes(router, menus, route.path);
  menuStore.setActiveMenuByPath(route.path);
});
</script>
