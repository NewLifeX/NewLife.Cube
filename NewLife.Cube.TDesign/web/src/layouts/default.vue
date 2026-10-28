<template>
  <t-layout>
    <!-- Sidebar（宽屏） -->
    <t-aside v-if="!isNarrow" :width="appStore.collapsed ? '64px' : '232px'" style="transition: width .3s">
      <div style="height: 56px; display: flex; align-items: center; padding: 0 16px; font-weight: bold; font-size: 16px; color: var(--td-text-color-primary)">
        <span v-if="!appStore.collapsed">{{ appStore.siteTitle }}</span>
      </div>
      <t-menu :collapsed="appStore.collapsed" :value="$route.path" @change="(v: string) => $router.push(v)" expandMutex>
        <template v-for="item in userStore.menus" :key="item.name">
          <t-submenu v-if="item.children?.length" :value="item.url || item.name" :title="item.displayName || item.name">
            <t-menu-item v-for="child in item.children" :key="child.name" :value="buildUrl(child)">
              {{ child.displayName || child.name }}
            </t-menu-item>
          </t-submenu>
          <t-menu-item v-else :value="buildUrl(item)">
            {{ item.displayName || item.name }}
          </t-menu-item>
        </template>
      </t-menu>
    </t-aside>

    <!-- Sidebar（窄屏抽屉） -->
    <t-drawer v-model:visible="mobileMenu" placement="left" size="232px" :footer="false" :header="appStore.siteTitle">
      <t-menu :value="$route.path" @change="(v: string) => $router.push(v)" expandMutex>
        <template v-for="item in userStore.menus" :key="item.name">
          <t-submenu v-if="item.children?.length" :value="item.url || item.name" :title="item.displayName || item.name">
            <t-menu-item v-for="child in item.children" :key="child.name" :value="buildUrl(child)">
              {{ child.displayName || child.name }}
            </t-menu-item>
          </t-submenu>
          <t-menu-item v-else :value="buildUrl(item)">
            {{ item.displayName || item.name }}
          </t-menu-item>
        </template>
      </t-menu>
    </t-drawer>

    <t-layout>
      <!-- Header -->
      <t-header style="height: 56px; display: flex; align-items: center; padding: 0 16px; border-bottom: 1px solid var(--td-component-border)">
        <t-button theme="default" variant="text" shape="square" @click="isNarrow ? (mobileMenu = true) : (appStore.collapsed = !appStore.collapsed)">
          <template #icon><view-list-icon /></template>
        </t-button>

        <t-breadcrumb style="margin-left: 12px">
          <t-breadcrumb-item to="/">首页</t-breadcrumb-item>
          <t-breadcrumb-item v-if="$route.path !== '/'">{{ $route.path }}</t-breadcrumb-item>
        </t-breadcrumb>

        <div style="flex: 1" />

        <t-button theme="default" variant="text" shape="square" @click="appStore.toggleDark">
          <template #icon>
            <sunny-icon v-if="appStore.darkMode" />
            <moon-icon v-else />
          </template>
        </t-button>

        <t-dropdown :options="userDropdown" @click="handleUserAction" trigger="click" style="margin-left: 8px">
          <t-button theme="default" variant="text">{{ userStore.displayName }}</t-button>
        </t-dropdown>
      </t-header>

      <!-- Content -->
      <t-content class="layout-content">
        <div v-if="tabs.length > 1" class="tab-strip">
          <t-tag
            v-for="t in tabs"
            :key="t.id"
            :theme="t.id === activeTabId ? 'primary' : 'default'"
            @click="activateTab(t)"
          >
            {{ t.title }}
          </t-tag>
          <t-button variant="text" size="small" @click="closeAll">关闭全部</t-button>
        </div>
        <router-view />
      </t-content>
    </t-layout>
  </t-layout>
</template>

<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { closeAllTabs, closeTab, createTab, openTab, shouldAutoCollapseSidebar, type TabItem } from '@newlifex/cube';
import { ViewListIcon, SunnyIcon, MoonIcon } from 'tdesign-icons-vue-next';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';
import { api } from '@/api';
import type { MenuItem } from '@newlifex/cube';

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
const router = useRouter();
const route = useRoute();

const userDropdown = [{ content: '退出登录', value: 'logout' }];

function buildUrl(item: MenuItem): string {
  return item.url?.startsWith('/') ? item.url : `/${item.url ?? ''}`;
}

function handleUserAction(data: { value: string }) {
  if (data.value === 'logout') {
    userStore.logout();
    router.push('/login');
  }
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
  () => router.currentRoute.value.fullPath,
  () => {
    mobileMenu.value = false;
  },
);

onMounted(async () => {
  syncResponsive();
  window.addEventListener('resize', syncResponsive);

  try {
    const res = await api.site.getSite();
    if (res?.data) appStore.siteConfig = res.data;
  } catch { /* ignore */ }

  await userStore.fetchUserInfo();
  if (!userStore.isLoggedIn) { router.push('/login'); return; }
  await userStore.fetchMenus();
});

onBeforeUnmount(() => window.removeEventListener('resize', syncResponsive));
</script>

<style scoped>
.t-layout {
  height: 100vh;
}
.t-layout .t-aside {
  border-right: 1px solid var(--td-component-border);
}
.layout-content {
  padding: 16px;
  overflow: auto;
  background: var(--td-bg-color-page);
}

@media (max-width: 767px) {
  .layout-content {
    padding: 8px;
  }
}

.tab-strip {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 12px;
}
</style>
