<template>
  <n-layout has-sider class="layout-container">
    <!-- 侧边栏 -->
    <n-layout-sider
      bordered
      collapse-mode="width"
      :collapsed="appStore.collapsed"
      :collapsed-width="64"
      :width="240"
      show-trigger
      @collapse="appStore.collapsed = true"
      @expand="appStore.collapsed = false"
    >
      <div class="layout-logo" @click="router.push('/home')">
        <img v-if="appStore.logo" :src="appStore.logo" alt="logo" class="logo-img" />
        <span v-show="!appStore.collapsed" class="logo-text">{{ appStore.siteName }}</span>
      </div>
      <sidebar-menu />
    </n-layout-sider>

    <n-layout>
      <!-- 顶栏 -->
      <n-layout-header bordered class="layout-header">
        <div class="header-left">
          <n-breadcrumb>
            <n-breadcrumb-item v-for="item in breadcrumbs" :key="item.path">
              {{ item.meta?.title ?? item.name }}
            </n-breadcrumb-item>
          </n-breadcrumb>
        </div>
        <div class="header-right">
          <n-switch :value="appStore.darkMode" @update:value="appStore.toggleDark()">
            <template #checked>🌙</template>
            <template #unchecked>☀️</template>
          </n-switch>
          <n-dropdown :options="userMenuOptions" @select="handleUserMenu">
            <n-button quaternary>
              <n-avatar v-if="userStore.avatar" :src="userStore.avatar" round size="small" />
              <span style="margin-left: 8px">{{ userStore.displayName }}</span>
            </n-button>
          </n-dropdown>
        </div>
      </n-layout-header>

      <!-- 内容区 -->
      <n-layout-content class="layout-content">
        <router-view v-slot="{ Component }">
          <transition name="fade" mode="out-in">
            <keep-alive>
              <component :is="Component" />
            </keep-alive>
          </transition>
        </router-view>
      </n-layout-content>
    </n-layout>
  </n-layout>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';
import SidebarMenu from './SidebarMenu.vue';
import api from '@/api';

const route = useRoute();
const router = useRouter();
const appStore = useAppStore();
const userStore = useUserStore();

const breadcrumbs = computed(() => route.matched.filter((r) => r.meta?.title));

const userMenuOptions = [
  { label: '个人信息', key: 'profile' },
  { label: '退出登录', key: 'logout' },
];

async function handleUserMenu(key: string) {
  if (key === 'logout') {
    await userStore.logout();
    router.push('/login');
  } else if (key === 'profile') {
    router.push('/personal');
  }
}

onMounted(async () => {
  // 获取站点信息
  try {
    const res = await api.user.getSiteInfo();
    if (res.data) appStore.setSiteInfo(res.data);
  } catch { /* ignore */ }

  // 获取用户信息
  if (!userStore.userInfo) {
    try { await userStore.fetchUserInfo(); } catch { /* ignore */ }
  }
});
</script>

<style scoped>
.layout-container {
  height: 100vh;
}

.layout-logo {
  display: flex;
  align-items: center;
  height: 48px;
  padding: 0 16px;
  cursor: pointer;
  overflow: hidden;
}

.logo-img {
  width: 32px;
  height: 32px;
  flex-shrink: 0;
}

.logo-text {
  margin-left: 10px;
  font-size: 16px;
  font-weight: 600;
  white-space: nowrap;
}

.layout-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 48px;
  padding: 0 16px;
}

.header-left {
  display: flex;
  align-items: center;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.layout-content {
  padding: 16px;
  overflow: auto;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
