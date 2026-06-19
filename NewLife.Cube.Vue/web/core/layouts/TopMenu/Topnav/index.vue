<script setup lang="ts">
import { ref, computed } from 'vue';
import { storeToRefs } from 'pinia';
import { useMenuStore, type TreeMenuItem } from 'cube-front/core/stores/menu';
import { getConfig } from 'cube-front/core/configure';
import { openMenuTab } from 'cube-front/core/utils/menuTab';
import ThemeSwitcher from 'cube-front/core/components/ThemeSwitcher.vue';
import LayoutSwitcher from 'cube-front/core/components/LayoutSwitcher.vue';
import ModeSwitcher from 'cube-front/core/components/ModeSwitcher.vue';
import NotificationBell from 'cube-front/core/components/NotificationBell.vue';
import SearchBar from 'cube-front/core/components/SearchBar.vue';
import UserProfile from 'cube-front/core/components/UserProfile.vue';

const menuStore = useMenuStore();
const { treeMenus, topLevelActiveMenu } = storeToRefs(menuStore);

const config = getConfig();
const title = config.base.title;
const logo = computed(() => config.base.logo);
const openMenuId = ref<string | null>(null);

const isMenuActive = (menu: TreeMenuItem) => topLevelActiveMenu.value?.id === menu.id;
const hasChildren = (menu: TreeMenuItem) =>
  Array.isArray(menu.children) && menu.children.length > 0;

const closeMenu = () => {
  openMenuId.value = null;
};

const handleNavItemClick = (menu: TreeMenuItem) => {
  if (!hasChildren(menu)) {
    openMenuTab({ url: menu.path, title: menu.name });
    menuStore.setActiveMenu(menu);
    closeMenu();
  } else {
    openMenuId.value = openMenuId.value === menu.id ? null : menu.id;
  }
};

const handleSubItemClick = (menu: TreeMenuItem) => {
  openMenuTab({ url: menu.path, title: menu.name });
  menuStore.setActiveMenu(menu);
  closeMenu();
};

const handleMouseEnter = (menu: TreeMenuItem) => {
  if (hasChildren(menu)) {
    openMenuId.value = menu.id;
  }
};

const handleMouseLeave = () => {
  openMenuId.value = null;
};

const handleMegaMouseEnter = () => {
  // 保持在菜单展开状态
};

const handleMegaMouseLeave = () => {
  openMenuId.value = null;
};
</script>

<template>
  <header class="topnav">
    <!-- Logo -->
    <div class="tn-logo">
      <div class="tn-mark">
        <img v-if="logo" :src="logo" :alt="title" class="tn-logo-img" />
        <svg v-else viewBox="0 0 17 17" xmlns="http://www.w3.org/2000/svg">
          <path
            d="M2 1C1.45 1 1 1.45 1 2v5h3V4h3V1H2zm6 0v3h3v3h3V2c0-.55-.45-1-1-1H8zM1 8v6c0 .55.45 1 1 1h6v-3H5V8H1zm11 3v3H9v3h5c.55 0 1-.45 1-1v-5h-3z"
          />
        </svg>
      </div>
      <div class="tn-name">{{ title }}</div>
    </div>

    <!-- 导航菜单 -->
    <nav class="tn-nav">
      <div
        v-for="menu in treeMenus"
        :key="menu.id"
        class="tn-item"
        :class="{ open: openMenuId === menu.id }"
        @mouseenter="handleMouseEnter(menu)"
        @mouseleave="handleMouseLeave"
      >
        <!-- 导航链接包装器 -->
        <div class="tn-link-wrap">
          <span
            class="tn-link"
            :class="{ active: isMenuActive(menu) && openMenuId !== menu.id }"
            @click="handleNavItemClick(menu)"
          >
            {{ menu.title || menu.name }}
            <svg
              v-if="hasChildren(menu)"
              class="chv"
              width="10"
              height="10"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2.5"
            >
              <path d="M6 9l6 6 6-6" />
            </svg>
          </span>

          <!-- 悬浮桥梁 - 连接导航和下拉菜单 -->
          <div
            v-if="hasChildren(menu) && openMenuId === menu.id"
            class="mega-bridge"
            @mouseenter="handleMegaMouseEnter"
            @mouseleave="handleMegaMouseLeave"
          ></div>

          <!-- 巨型下拉菜单 -->
          <div
            v-show="hasChildren(menu) && openMenuId === menu.id"
            class="mega"
            @mouseenter="handleMegaMouseEnter"
            @mouseleave="handleMegaMouseLeave"
          >
            <div class="mega-cols">
              <template v-for="(col, colIdx) in menu.children" :key="col.id">
                <div v-if="colIdx > 0" class="mega-col-sep"></div>
                <div class="mega-col">
                  <div class="mc-title">{{ col.title || col.name }}</div>
                  <template v-if="col.children && col.children.length > 0">
                    <div
                      v-for="item in col.children"
                      :key="item.id"
                      class="mc-item"
                      @click="handleSubItemClick(item)"
                    >
                      {{ item.title || item.name }}
                    </div>
                  </template>
                  <div v-else class="mc-item" @click="handleSubItemClick(col)">
                    {{ col.title || col.name }}
                  </div>
                </div>
              </template>
            </div>
          </div>
        </div>
      </div>
    </nav>

    <!-- 右侧操作区 -->
    <div class="tn-acts">
      <!-- 搜索按钮 -->
      <SearchBar mode="icon" />

      <div class="tn-dvd"></div>

      <!-- 模式切换（太阳/月亮） -->
      <ModeSwitcher />

      <!-- 主题选择器（调色盘） -->
      <ThemeSwitcher />

      <!-- 布局切换 -->
      <LayoutSwitcher />

      <NotificationBell />
      <UserProfile variant="navbar" />
    </div>
  </header>

  <!-- 遮罩层 -->
  <div v-if="openMenuId" class="mega-overlay" @click="closeMenu"></div>
</template>

<style lang="scss" scoped>
$tn-h: 60px;

.topnav {
  --navbar-border: var(--tn-b, #224538);
  --navbar-text: var(--tn-t, #74b898);
  --navbar-text-hover: var(--tn-ac, #4ec685);
  --navbar-text-muted: rgba(255, 255, 255, 0.5);
  --navbar-hover-bg: rgba(255, 255, 255, 0.08);

  height: $tn-h;
  background: var(--tn, #1a3328);
  display: flex;
  align-items: center;
  padding: 0 18px 0 14px;
  position: relative;
  z-index: 100;
  box-shadow: 0 2px 20px rgba(0, 0, 0, 0.22);
}

// Logo
.tn-logo {
  display: flex;
  align-items: center;
  gap: 10px;
  padding-right: 18px;
  border-right: 1px solid var(--tn-b, #224538);
  margin-right: 6px;
  flex-shrink: 0;
}

.tn-mark {
  width: 32px;
  height: 32px;
  background: var(--tn-ac, #4ec685);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  overflow: hidden;

  svg {
    width: 17px;
    height: 17px;
    fill: var(--tn, #1a3328);
  }
}

.tn-logo-img {
  width: 100%;
  height: 100%;
  object-fit: contain;
  padding: 4px;
}

.tn-name {
  font-size: 17px;
  font-weight: 700;
  color: var(--navbar-text, #fff);
  letter-spacing: 0.01em;
  white-space: nowrap;
}

// 导航菜单
.tn-nav {
  display: flex;
  align-items: stretch;
  flex: 1;
  height: 100%;
  overflow: hidden;
}

.tn-item {
  position: relative;
  display: flex;
  align-items: center;
  height: 100%;

  &:hover {
    .tn-link {
      color: var(--tn-ac, #4ec685);
      background: rgba(255, 255, 255, 0.06);
    }
  }

  &.open {
    .tn-link {
      color: var(--tn-ac, #4ec685);
      background: rgba(255, 255, 255, 0.08);
    }
  }
}

.tn-link {
  display: flex;
  align-items: center;
  gap: 5px;
  padding: 0 13px;
  height: 100%;
  cursor: pointer;
  user-select: none;
  color: var(--tn-t, #74b898);
  font-size: 13.5px;
  font-weight: 500;
  white-space: nowrap;
  border-bottom: 2px solid transparent;
  transition:
    color 0.15s,
    background 0.15s;

  &:hover {
    color: var(--tn-ac, #4ec685);
    background: rgba(255, 255, 255, 0.06);
  }

  &.active {
    color: var(--tn-ac, #4ec685);
    border-bottom-color: var(--tn-ac, #4ec685);
  }
}

.tn-item.open .tn-link {
  color: var(--tn-ac, #4ec685);
  background: rgba(255, 255, 255, 0.08);
}

.chv {
  transition: transform 0.2s;
  opacity: 0.6;
}

.tn-item.open .chv {
  transform: rotate(180deg);
}

// 巨型下拉菜单
.mega {
  position: fixed;
  left: 0;
  right: 0;
  top: $tn-h;
  background: var(--bg-elevated, #fff);
  border-top: 2px solid var(--tn-ac, #4ec685);
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.12);
  padding: 22px 28px 24px;
  display: flex;
  z-index: 200;
  opacity: 0;
  transform: translateY(-8px);
  pointer-events: none;
  transition:
    opacity 0.2s ease,
    transform 0.2s ease;
}

.tn-item.open .mega,
.tn-item:hover .mega {
  opacity: 1;
  transform: translateY(0);
  pointer-events: auto;
}

// 导航链接包装器
.tn-link-wrap {
  position: relative;
  height: 100%;
  display: flex;
  align-items: center;
}

// 悬浮桥梁 - 连接导航和下拉菜单
.mega-bridge {
  position: absolute;
  left: 0;
  right: 0;
  top: 100%;
  height: 20px;
  background: transparent;
  z-index: 199;
  pointer-events: auto;
}

.mega-cols {
  display: flex;
  flex-wrap: wrap;
  flex: 1;
  gap: 0;
}

.mega-col {
  min-width: 150px;
  padding: 0 24px 0 0;
  margin: 0 0 14px;

  &:first-child {
    padding-left: 0;
  }
}

.mega-col-sep {
  width: 1px;
  background: var(--border-subtle);
  margin: 0 10px 14px;
  flex-shrink: 0;
  align-self: stretch;
}

.mc-title {
  font-size: 10.5px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.1em;
  color: var(--text-muted);
  margin-bottom: 9px;
  padding-bottom: 7px;
  border-bottom: 1px solid var(--border-subtle);
}

.mc-item {
  display: flex;
  align-items: center;
  gap: 7px;
  padding: 5px 8px;
  border-radius: 5px;
  color: var(--text-secondary);
  font-size: 13px;
  cursor: pointer;
  transition:
    background 0.12s,
    color 0.12s;
  white-space: nowrap;

  &::before {
    content: '';
    width: 4px;
    height: 4px;
    border-radius: 50%;
    background: var(--border-subtle);
    flex-shrink: 0;
    transition: background 0.12s;
  }

  &:hover {
    background: var(--accent-muted);
    color: var(--accent);

    &::before {
      background: var(--accent);
    }
  }
}

// 遮罩
.mega-overlay {
  position: fixed;
  inset: 0;
  top: $tn-h;
  background: rgba(0, 0, 0, 0.06);
  z-index: 150;
  pointer-events: none;
}

// 右侧操作区
.tn-acts {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-left: auto;
  flex-shrink: 0;
}

.tn-btn {
  width: 34px;
  height: 34px;
  border-radius: 8px;
  border: none;
  background: rgba(255, 255, 255, 0.06);
  color: var(--tn-t, #74b898);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition:
    background 0.15s,
    color 0.15s;

  &:hover {
    background: rgba(255, 255, 255, 0.12);
    color: var(--tn-ac, #4ec685);
  }
}

.tn-logout:hover {
  background: rgba(220, 38, 38, 0.15) !important;
  color: #f87171 !important;
}

.tn-dvd {
  width: 1px;
  height: 20px;
  background: var(--tn-b, #224538);
  margin: 0 3px;
}
</style>
