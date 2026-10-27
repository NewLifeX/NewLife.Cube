<template>
  <n-layout has-sider class="layout-container">
    <!-- 侧边栏（宽屏） -->
    <n-layout-sider
      v-if="!isNarrow"
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

    <!-- 侧边栏（窄屏抽屉） -->
    <n-drawer v-model:show="mobileMenu" placement="left" :width="240">
      <n-drawer-content :native-scrollbar="false" closable body-content-style="padding: 0">
        <div class="layout-logo" @click="router.push('/home')">
          <img v-if="appStore.logo" :src="appStore.logo" alt="logo" class="logo-img" />
          <span class="logo-text">{{ appStore.siteName }}</span>
        </div>
        <sidebar-menu :collapsed="false" />
      </n-drawer-content>
    </n-drawer>

    <n-layout>
      <!-- 顶栏 -->
      <n-layout-header bordered class="layout-header">
        <div class="header-left">
          <n-button v-if="isNarrow" quaternary class="menu-trigger" @click="mobileMenu = true">☰</n-button>
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
        <div v-if="tabs.length > 1" class="tab-strip">
          <n-tag
            v-for="t in tabs"
            :key="t.id"
            :type="t.id === activeTabId ? 'success' : 'default'"
            :bordered="t.id !== activeTabId"
            closable
            @click="activateTab(t)"
          >
            {{ t.title }}
          </n-tag>
          <n-button text size="tiny" @click="closeAll">关闭全部</n-button>
        </div>
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
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { closeAllTabs, closeTab, createTab, openTab, shouldAutoCollapseSidebar, type TabItem } from '@newlifex/page-logic';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';
import SidebarMenu from './SidebarMenu.vue';
import api from '@/api';

const route = useRoute();
const router = useRouter();
const appStore = useAppStore();
const userStore = useUserStore();

// --- 多标签页 ---
const tabs = ref<TabItem[]>([]);
const activeTabId = ref('');

function syncTab() {
  const tab = createTab(route.path, route.fullPath, (route.meta.title as string) || route.path);
  tabs.value = openTab(tabs.value, tab);
  activeTabId.value = tab.id;
}

function activateTab(tab: TabItem) {
  if (tab.id === activeTabId.value) return;
  void router.push(tab.fullPath);
}

function closeTabAndNavigate(id: string) {
  const result = closeTab(tabs.value, id, activeTabId.value);
  tabs.value = result.tabs;
  if (result.nextActiveId) {
    activeTabId.value = result.nextActiveId;
    const next = tabs.value.find((t) => t.id === result.nextActiveId);
    if (next) void router.push(next.fullPath);
  }
}

function closeAll() {
  tabs.value = closeAllTabs(tabs.value);
  const home = tabs.value[0];
  if (home) {
    activeTabId.value = home.id;
    void router.push(home.fullPath);
  }
}

watch(() => route.fullPath, syncTab, { immediate: true });

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
    const res = await api.site.getSite();
    if (res.data) appStore.setSiteInfo(res.data);
  } catch { /* ignore */ }

  // 获取用户信息
  if (!userStore.userInfo) {
    try { await userStore.fetchUserInfo(); } catch { /* ignore */ }
  }

  syncResponsive();
  window.addEventListener('resize', syncResponsive);
});

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

// 路由切换后关闭抽屉，避免遮挡内容
watch(
  () => route.fullPath,
  () => {
    mobileMenu.value = false;
  },
);

onBeforeUnmount(() => window.removeEventListener('resize', syncResponsive));
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
  gap: 4px;
}

.menu-trigger {
  font-size: 18px;
  line-height: 1;
}

@media (max-width: 767px) {
  .layout-content {
    padding: 8px;
  }

  .layout-header {
    padding: 0 8px;
  }
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

.tab-strip {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 12px;
}
</style>
