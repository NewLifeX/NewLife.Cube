<script setup lang="ts">
import { ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { SimpleScrollbar } from '@sa/materials';
import type { RouteKey } from '@elegant-router/types';
import { GLOBAL_SIDER_MENU_ID } from '@/constants/app';
import { useAppStore } from '@/store/modules/app';
import { useRouteStore } from '@/store/modules/route';
import { useMenu } from '../../../context';
import MenuItem from '../components/menu-item.vue';

defineOptions({ name: 'VerticalMenu' });

const route = useRoute();
const appStore = useAppStore();
const routeStore = useRouteStore();
const { selectedKey, selectedKeyDummy, handleSelect } = useMenu();

// const inverted = computed(() => !themeStore.darkMode && themeStore.sider.inverted);

const expandedKeys = ref<string[]>([]);

function updateExpandedKeys() {
  if (appStore.siderCollapse || !selectedKey.value) {
    expandedKeys.value = [];
    return;
  }
  expandedKeys.value = routeStore.getSelectedMenuKeyPath(selectedKey.value);
}

watch(
  () => route.name,
  () => {
    updateExpandedKeys();
  },
  { immediate: true }
);
</script>

<template>
  <Teleport :to="`#${GLOBAL_SIDER_MENU_ID}`">
    <SimpleScrollbar>
      <ElMenu
        mode="vertical"
        :default-active="selectedKeyDummy"
        :default-openeds="expandedKeys"
        :collapse="appStore.siderCollapse"
        :collapse-transition="false"
        @select="val => handleSelect(val as RouteKey)"
      >
        <MenuItem v-for="item in routeStore.menus" :key="item.key" :item="item" :index="item.key" />
      </ElMenu>
    </SimpleScrollbar>
  </Teleport>
</template>

<style scoped></style>
