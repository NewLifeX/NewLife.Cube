<script setup lang="ts">
import { computed } from 'vue';
import { useMenuStore } from 'cube-front/core/stores/menu';
import SideMenuItem from './SideMenuItem.vue';

const menuStore = useMenuStore();

const parentMenu = computed(() => menuStore.activeMenu?.parentMenu);
const secondLevelMenus = computed(() => parentMenu.value?.children || []);
</script>

<template>
  <aside class="sider">
    <!-- 顶部标题区 -->
    <div class="sider-header">
      <div class="sider-header-icon">
        <svg
          width="18"
          height="18"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <rect x="3" y="3" width="7" height="7" rx="1.2" />
          <rect x="14" y="3" width="7" height="7" rx="1.2" />
          <rect x="3" y="14" width="7" height="7" rx="1.2" />
          <rect x="14" y="14" width="7" height="7" rx="1.2" />
        </svg>
      </div>
      <span class="sider-header-title">
        {{ parentMenu?.title || parentMenu?.name || '导航菜单' }}
      </span>
    </div>

    <!-- 分割线 -->
    <div class="sider-divider" />

    <!-- 菜单树 -->
    <nav class="sider-nav">
      <div class="sider-nav-inner">
        <SideMenuItem
          v-for="menu in secondLevelMenus"
          :key="menu.id"
          :menu="menu"
          :depth="0"
          :activeMenu="menuStore.activeMenu"
        />
      </div>
    </nav>

    <!-- 底部 -->
    <div class="sider-footer">
      <button class="sider-collapse-btn" title="折叠菜单">
        <svg
          width="16"
          height="16"
          viewBox="0 0 16 16"
          fill="none"
          stroke="currentColor"
          stroke-width="1.5"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <path d="M10 3L5 8L10 13" />
        </svg>
      </button>
    </div>
  </aside>
</template>

<style lang="scss" scoped>
.sider {
  display: flex;
  flex-direction: column;
  width: 100%;
  height: 100%;
  background: rgba(255, 255, 255, 0.78);
  backdrop-filter: blur(14px);
  -webkit-backdrop-filter: blur(14px);
  border-right: 1px solid rgba(226, 224, 220, 0.5);
  box-shadow: 1px 0 12px rgba(0, 0, 0, 0.02);
}

// ---- 顶部标题 ----
.sider-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 20px 16px 12px;
  flex-shrink: 0;
}

.sider-header-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border-radius: 10px;
  background: linear-gradient(135deg, #eff6ff 0%, #dbeafe 100%);
  color: #1e40af;
  flex-shrink: 0;
}

.sider-header-title {
  font-size: 15px;
  font-weight: 700;
  color: #1e293b;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  letter-spacing: -0.01em;
}

// ---- 分割线 ----
.sider-divider {
  height: 1px;
  margin: 0 16px;
  background: linear-gradient(to right, transparent 0%, #e2e0dc 20%, #e2e0dc 80%, transparent 100%);
  flex-shrink: 0;
}

// ---- 菜单导航 ----
.sider-nav {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  padding: 8px 0;

  scrollbar-width: thin;
  scrollbar-color: #d5cec4 transparent;

  &::-webkit-scrollbar {
    width: 4px;
  }
  &::-webkit-scrollbar-track {
    background: transparent;
  }
  &::-webkit-scrollbar-thumb {
    background: #d5cec4;
    border-radius: 4px;
    &:hover {
      background: #b8b3bf;
    }
  }
}

.sider-nav-inner {
  padding-bottom: 8px;
}

// ---- 底部 ----
.sider-footer {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 8px 16px 12px;
  flex-shrink: 0;
  border-top: 1px solid #f0ece6;
}

.sider-collapse-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border: 1px solid #e2e0dc;
  border-radius: 10px;
  background: #ffffff;
  color: #94a3b8;
  cursor: pointer;
  transition: all 0.18s ease;

  &:hover {
    background: #f8fafc;
    color: #64748b;
    border-color: #cbd5e1;
  }

  &:active {
    background: #f1f5f9;
  }
}
</style>
