<template>
  <n-menu
    :options="menuOptions"
    :value="activeKey"
    :collapsed="appStore.collapsed"
    :collapsed-width="64"
    :collapsed-icon-size="20"
    @update:value="handleSelect"
  />
</template>

<script setup lang="ts">
import { ref, onMounted, h, type Component as VueComponent } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { NIcon, type MenuOption } from 'naive-ui';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';
import type { MenuItem } from '@cube/api-core';

const route = useRoute();
const router = useRouter();
const appStore = useAppStore();
const userStore = useUserStore();

const menuOptions = ref<MenuOption[]>([]);
const activeKey = ref<string>('');

/** 将后端菜单树转换为 NaiveUI 菜单选项 */
function toMenuOptions(menus: MenuItem[]): MenuOption[] {
  return menus
    .filter((m) => m.visible !== false)
    .map((m) => {
      const option: MenuOption = {
        key: m.url,
        label: m.displayName,
      };
      if (m.children?.length) {
        option.children = toMenuOptions(m.children);
      }
      return option;
    });
}

function handleSelect(key: string) {
  activeKey.value = key;
  const path = key.startsWith('/') ? key : `/${key}`;
  router.push(path);
}

// 监听路由变化更新当前选中
router.afterEach((to) => {
  activeKey.value = to.path.replace(/^\//, '');
});

onMounted(async () => {
  try {
    const menus = userStore.menus.length ? userStore.menus : await userStore.fetchMenus();
    menuOptions.value = toMenuOptions(menus);
    activeKey.value = route.path.replace(/^\//, '');
  } catch {
    /* 菜单加载失败静默处理 */
  }
});
</script>
