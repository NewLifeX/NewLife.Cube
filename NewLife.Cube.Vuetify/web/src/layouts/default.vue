<template>
  <v-layout>
    <!-- 侧边栏：宽屏常驻，窄屏为抽屉 -->
    <v-navigation-drawer
      v-model="mobileMenu"
      :rail="!isNarrow && appStore.collapsed"
      :permanent="!isNarrow"
      :temporary="isNarrow"
      width="240"
    >
      <v-list-item
        :title="!isNarrow && appStore.collapsed ? '魔' : (appStore.siteConfig?.name || '魔方管理平台')"
        class="text-center font-weight-bold"
        nav
      />
      <v-divider />
      <v-list density="compact" nav>
        <template v-for="item in userStore.menus" :key="item.id">
          <v-list-group v-if="item.children?.length" :value="item.id">
            <template #activator="{ props }">
              <v-list-item v-bind="props" :title="item.displayName || item.name" prepend-icon="mdi-folder-outline" />
            </template>
            <v-list-item
              v-for="child in item.children"
              :key="child.id"
              :title="child.displayName || child.name"
              :value="child.url"
              prepend-icon="mdi-circle-small"
              @click="navigateTo(child.url)"
            />
          </v-list-group>
          <v-list-item
            v-else
            :title="item.displayName || item.name"
            :value="item.url"
            prepend-icon="mdi-file-document-outline"
            @click="navigateTo(item.url)"
          />
        </template>
      </v-list>
    </v-navigation-drawer>

    <!-- 顶栏 -->
    <v-app-bar density="compact" flat>
      <v-app-bar-nav-icon @click="isNarrow ? (mobileMenu = !mobileMenu) : appStore.toggleCollapsed()" />
      <v-breadcrumbs :items="breadcrumbs" />
      <v-spacer />
      <v-btn :icon="appStore.darkMode ? 'mdi-weather-night' : 'mdi-weather-sunny'" @click="appStore.toggleDarkMode" />
      <v-menu>
        <template #activator="{ props }">
          <v-btn v-bind="props" variant="text">
            <v-avatar size="28" color="primary">
              <span class="text-white">{{ userStore.displayName?.charAt(0) || 'U' }}</span>
            </v-avatar>
            <span class="ml-2">{{ userStore.displayName }}</span>
          </v-btn>
        </template>
        <v-list>
          <v-list-item @click="handleLogout" title="退出登录" prepend-icon="mdi-logout" />
        </v-list>
      </v-menu>
    </v-app-bar>

    <!-- 内容 -->
    <v-main>
      <v-container fluid>
        <router-view />
      </v-container>
    </v-main>
  </v-layout>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { shouldAutoCollapseSidebar } from '@newlifex/page-logic';
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

const breadcrumbs = computed(() => {
  const items = [{ title: '首页', to: '/home', disabled: false }];
  const title = route.meta.title as string;
  if (title && title !== '首页') {
    items.push({ title, to: '', disabled: true });
  }
  return items;
});

function navigateTo(url?: string) {
  if (url) router.push(url);
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
