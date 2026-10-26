<template>
  <a-layout class="layout" style="min-height: 100vh">
    <a-layout-sider
      v-if="!isNarrow"
      :collapsed="appStore.collapsed"
      collapsible
      :trigger="null"
      :width="220"
      :collapsed-width="48"
    >
      <div class="logo" style="height: 48px; display: flex; align-items: center; justify-content: center; color: #fff; font-weight: bold; font-size: 16px;">
        {{ appStore.collapsed ? '魔' : (appStore.siteConfig?.name || '魔方管理平台') }}
      </div>
      <a-menu
        :selected-keys="selectedKeys"
        :open-keys="openKeys"
        :auto-open-selected="true"
        @menu-item-click="onMenuClick"
      >
        <template v-for="item in userStore.menus" :key="item.id">
          <a-sub-menu v-if="item.children?.length" :key="'sub-' + item.id">
            <template #icon><icon-apps /></template>
            <template #title>{{ item.displayName || item.name }}</template>
            <a-menu-item v-for="child in item.children" :key="child.url || child.id">
              {{ child.displayName || child.name }}
            </a-menu-item>
          </a-sub-menu>
          <a-menu-item v-else :key="item.url || item.id">
            <template #icon><icon-apps /></template>
            {{ item.displayName || item.name }}
          </a-menu-item>
        </template>
      </a-menu>
    </a-layout-sider>

    <!-- 窄屏抽屉菜单 -->
    <a-drawer
      :visible="mobileMenu"
      placement="left"
      :width="220"
      :footer="false"
      @cancel="mobileMenu = false"
    >
      <template #title>{{ appStore.siteConfig?.name || '魔方管理平台' }}</template>
      <a-menu
        :selected-keys="selectedKeys"
        :open-keys="openKeys"
        :auto-open-selected="true"
        @menu-item-click="onMenuClick"
      >
        <template v-for="item in userStore.menus" :key="item.id">
          <a-sub-menu v-if="item.children?.length" :key="'sub-' + item.id">
            <template #icon><icon-apps /></template>
            <template #title>{{ item.displayName || item.name }}</template>
            <a-menu-item v-for="child in item.children" :key="child.url || child.id">
              {{ child.displayName || child.name }}
            </a-menu-item>
          </a-sub-menu>
          <a-menu-item v-else :key="item.url || item.id">
            <template #icon><icon-apps /></template>
            {{ item.displayName || item.name }}
          </a-menu-item>
        </template>
      </a-menu>
    </a-drawer>

    <a-layout>
      <a-layout-header style="background: var(--color-bg-2); padding: 0 16px; display: flex; align-items: center; justify-content: space-between;">
        <div style="display: flex; align-items: center; gap: 12px;">
          <a-button type="text" @click="isNarrow ? (mobileMenu = true) : appStore.toggleCollapsed()">
            <template #icon>
              <icon-menu-fold v-if="!isNarrow && !appStore.collapsed" />
              <icon-menu-unfold v-else />
            </template>
          </a-button>
          <a-breadcrumb>
            <a-breadcrumb-item>
              <router-link to="/home">首页</router-link>
            </a-breadcrumb-item>
            <a-breadcrumb-item v-if="currentTitle">{{ currentTitle }}</a-breadcrumb-item>
          </a-breadcrumb>
        </div>

        <div style="display: flex; align-items: center; gap: 12px;">
          <a-switch v-model="appStore.darkMode" @change="appStore.toggleDarkMode">
            <template #checked>🌙</template>
            <template #unchecked>☀️</template>
          </a-switch>
          <a-dropdown>
            <a-button type="text">
              <a-avatar :size="28">{{ userStore.displayName?.charAt(0) || 'U' }}</a-avatar>
              <span style="margin-left: 8px;">{{ userStore.displayName }}</span>
            </a-button>
            <template #content>
              <a-doption @click="handleLogout">退出登录</a-doption>
            </template>
          </a-dropdown>
        </div>
      </a-layout-header>

      <a-layout-content class="layout-content">
        <router-view />
      </a-layout-content>
    </a-layout>
  </a-layout>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { shouldAutoCollapseSidebar } from '@newlifex/page-logic';
import {
  IconApps,
  IconMenuFold,
  IconMenuUnfold,
} from '@arco-design/web-vue/es/icon';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';

const route = useRoute();
const router = useRouter();
const appStore = useAppStore();
const userStore = useUserStore();

// 初始化
appStore.fetchSiteConfig();
if (!userStore.isLoggedIn) {
  userStore.fetchUserInfo().then(() => {
    if (userStore.isLoggedIn) userStore.fetchMenus();
    else router.push('/login');
  });
}

const selectedKeys = computed(() => {
  const path = '/' + (route.params.type as string[] || []).join('/');
  return [path];
});

const openKeys = computed(() => {
  const keys: string[] = [];
  for (const item of userStore.menus) {
    if (item.children?.length) {
      const path = '/' + (route.params.type as string[] || []).join('/');
      if (item.children.some((c) => c.url === path)) {
        keys.push('sub-' + item.id);
      }
    }
  }
  return keys;
});

const currentTitle = computed(() => route.meta.title as string || '');

function onMenuClick(key: string) {
  if (key && !key.startsWith('sub-')) {
    router.push(key);
  }
}

async function handleLogout() {
  await userStore.logout();
  router.push('/login');
}

// 窄屏使用抽屉菜单；宽屏恢复用户手动展开
const isNarrow = ref(false);
const mobileMenu = ref(false);
let autoCollapsed = false;

function syncResponsive() {
  const narrow = shouldAutoCollapseSidebar(window.innerWidth);
  isNarrow.value = narrow;
  if (narrow) {
    if (!appStore.collapsed) {
      appStore.collapsed = true;
      autoCollapsed = true;
    }
  } else {
    mobileMenu.value = false;
    if (autoCollapsed) {
      appStore.collapsed = false;
      autoCollapsed = false;
    }
  }
}

watch(
  () => route.fullPath,
  () => {
    mobileMenu.value = false;
  },
);

onMounted(() => {
  syncResponsive();
  window.addEventListener('resize', syncResponsive);
});

onBeforeUnmount(() => window.removeEventListener('resize', syncResponsive));
</script>

<style scoped>
.layout-content {
  padding: 16px;
}

@media (max-width: 767px) {
  .layout-content {
    padding: 8px;
  }
}
</style>
