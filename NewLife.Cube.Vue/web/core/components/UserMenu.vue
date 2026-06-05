<script setup lang="ts">
/**
 * UserMenu - 用户菜单组件
 *
 * 特性：
 * - 显示用户头像（首字母）和用户名
 * - 点击展开下拉菜单，包含个人中心和退出选项
 */
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useUserStore } from 'cube-front/core/stores/user';

interface MenuOption {
  id: string;
  label: string;
  icon: string;
  danger?: boolean;
}

const userStore = useUserStore();

const menuRef = ref<HTMLElement | null>(null);
const menuOpen = ref(false);

const currentUser = computed(() => userStore.userInfo);
const userInitial = computed(() =>
  (currentUser.value?.displayName || currentUser.value?.name || 'U').charAt(0).toUpperCase()
);
const userName = computed(() => currentUser.value?.displayName || currentUser.value?.name || '');

const menuOptions: MenuOption[] = [
  { id: 'profile', label: '个人中心', icon: 'user' },
  { id: 'logout', label: '退出登录', icon: 'logout', danger: true },
];

function toggleMenu() {
  menuOpen.value = !menuOpen.value;
}

function closeMenu() {
  menuOpen.value = false;
}

function handleOptionClick(option: MenuOption) {
  if (option.id === 'logout') {
    userStore.logout();
  } else if (option.id === 'profile') {
    console.info('[UserMenu] Navigate to profile');
  }
  closeMenu();
}

function handleClickOutside(e: MouseEvent) {
  if (menuRef.value && !menuRef.value.contains(e.target as Node)) {
    closeMenu();
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside);
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
});
</script>

<template>
  <div ref="menuRef" class="user-menu" @click.stop>
    <button class="user-menu-trigger" @click="toggleMenu">
      <div class="user-avatar">{{ userInitial }}</div>
      <span class="user-name">{{ userName }}</span>
      <svg
        class="chevron"
        :class="{ open: menuOpen }"
        width="12"
        height="12"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="2"
      >
        <path d="M6 9l6 6 6-6" />
      </svg>
    </button>

    <Transition name="menu-fade">
      <div v-if="menuOpen" class="user-dropdown">
        <div class="dropdown-header">
          <div class="dropdown-avatar">{{ userInitial }}</div>
          <div class="dropdown-info">
            <div class="dropdown-name">{{ userName }}</div>
            <div class="dropdown-role">{{ currentUser?.role || '用户' }}</div>
          </div>
        </div>
        <div class="dropdown-divider"></div>
        <button
          v-for="opt in menuOptions"
          :key="opt.id"
          class="dropdown-item"
          :class="{ danger: opt.danger }"
          @click="handleOptionClick(opt)"
        >
          <svg v-if="opt.icon === 'user'" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2" />
            <circle cx="12" cy="7" r="4" />
          </svg>
          <svg v-else-if="opt.icon === 'logout'" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
            <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4" />
            <polyline points="16 17 21 12 16 7" />
            <line x1="21" y1="12" x2="9" y2="12" />
          </svg>
          <span>{{ opt.label }}</span>
        </button>
      </div>
    </Transition>
  </div>
</template>

<style lang="scss" scoped>
.user-menu {
  position: relative;
}

.user-menu-trigger {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 4px 10px 4px 4px;
  border: none;
  border-radius: 8px;
  background: transparent;
  cursor: pointer;
  transition: background 0.15s;
  font-family: inherit;

  &:hover {
    background: var(--navbar-hover-bg, rgba(0, 0, 0, 0.06));
  }
}

.user-avatar {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--accent, #4ec685) 0%, var(--accent-hover, #3db873) 100%);
  color: var(--navbar-bg, #fff);
  font-size: 12px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
}

.user-name {
  font-size: 13px;
  font-weight: 500;
  color: var(--navbar-text, var(--text-secondary));
  white-space: nowrap;
}

.chevron {
  color: var(--navbar-text-muted, var(--text-muted));
  transition: transform 0.2s;

  &.open {
    transform: rotate(180deg);
  }
}

.user-dropdown {
  position: absolute;
  right: 0;
  top: calc(100% + 8px);
  min-width: 200px;
  background: var(--bg-elevated, #fff);
  border: 1px solid var(--border-subtle, #e2e0dc);
  border-radius: var(--radius-md, 10px);
  box-shadow: var(--shadow-lg, 0 10px 40px rgba(0, 0, 0, 0.12));
  z-index: 300;
  overflow: hidden;
}

.dropdown-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px;
}

.dropdown-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--accent) 0%, var(--accent-hover) 100%);
  color: #fff;
  font-size: 16px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
}

.dropdown-info {
  flex: 1;
}

.dropdown-name {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}

.dropdown-role {
  font-size: 12px;
  color: var(--text-muted);
  margin-top: 2px;
}

.dropdown-divider {
  height: 1px;
  background: var(--border-subtle);
}

.dropdown-item {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  padding: 12px 16px;
  border: none;
  background: transparent;
  color: var(--text-secondary);
  font-size: 13px;
  font-family: inherit;
  cursor: pointer;
  transition: background 0.12s, color 0.12s;
  text-align: left;

  &:hover {
    background: var(--accent-muted);
    color: var(--text-primary);
  }

  &.danger {
    color: var(--color-danger, #dc2626);

    &:hover {
      background: var(--color-danger-bg, #fef2f2);
    }
  }
}

.menu-fade-enter-active,
.menu-fade-leave-active {
  transition: opacity 0.12s, transform 0.12s;
}

.menu-fade-enter-from,
.menu-fade-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>