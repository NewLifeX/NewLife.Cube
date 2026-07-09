<script setup lang="ts">
/**
 * UserProfile - 统一用户头像/资料组件
 *
 * 两种变体：
 *   navbar  - 顶部栏模式：全高触发按钮，头像 + 用户名 + 箭头，下拉向下
 *   sidebar - 侧边栏模式：底部紧凑触发，头像 + 用户名 + 箭头，下拉向上
 *
 * Slots:
 *   #extra-options — 下拉菜单额外选项（渲染在用户信息卡之前），可嵌入功能按钮
 *
 * Props:
 *   variant  — 显示变体，'navbar' | 'sidebar'
 *   dropup   — 是否向上弹出，侧边栏底部使用时设 true
 */
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import { useUserStore } from 'cube-front/core/stores/user';
import { getConfig } from 'cube-front/core/configure';

interface Props {
  /** 显示变体 */
  variant?: 'navbar' | 'sidebar';
  /** 下拉方向：默认向下，sidebar 底部使用时设 true 向上弹出 */
  dropup?: boolean;
}

withDefaults(defineProps<Props>(), {
  variant: 'navbar',
  dropup: false,
});

const router = useRouter();
const userStore = useUserStore();

const baseUrl = getConfig().request.baseUrl ?? '';

const menuRef = ref<HTMLElement | null>(null);
const menuOpen = ref(false);
const avatarError = ref(false);

const currentUser = computed(() => userStore.userInfo);
const userId = computed(() => currentUser.value?.id);
const userName = computed(
  () => currentUser.value?.displayName || currentUser.value?.name || '用户',
);
const userInitial = computed(() => userName.value.charAt(0).toUpperCase());
const avatarUrl = computed(() => (userId.value ? `${baseUrl}/Cube/Avatar/${userId.value}` : ''));

function toggleMenu() {
  menuOpen.value = !menuOpen.value;
}

function closeMenu() {
  menuOpen.value = false;
}

function handleAvatarError() {
  avatarError.value = true;
}

function handleProfile() {
  closeMenu();
  router.push('/profile').catch(() => {});
}

function handleLogout() {
  closeMenu();
  userStore.logout();
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
  <div ref="menuRef" class="user-profile" :class="[`variant-${variant}`, { dropup }]">
    <!-- 触发按钮 -->
    <button class="profile-trigger" :class="{ active: menuOpen }" @click="toggleMenu">
      <!-- 头像 -->
      <div class="avatar-wrapper">
        <img
          v-if="avatarUrl && !avatarError"
          :src="avatarUrl"
          :alt="userName"
          class="avatar-img"
          @error="handleAvatarError"
        />
        <span v-else class="avatar-initial">{{ userInitial }}</span>
      </div>
      <!-- 用户名 -->
      <span class="profile-username">{{ userName }}</span>
      <!-- 展开箭头 -->
      <svg
        class="profile-chevron"
        :class="{ open: menuOpen }"
        width="12"
        height="12"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="2.5"
      >
        <path d="M6 9l6 6 6-6" />
      </svg>
    </button>

    <!-- 下拉面板 -->
    <Transition name="profile-fade">
      <div v-if="menuOpen" class="profile-dropdown">
        <!-- 额外选项插槽（在用户信息卡之前） -->
        <template v-if="$slots['extra-options']">
          <div class="extra-options">
            <slot name="extra-options" />
          </div>
          <div class="dropdown-divider" />
        </template>

        <!-- 用户信息卡 -->
        <div class="dropdown-header">
          <div class="dropdown-avatar">
            <img
              v-if="avatarUrl && !avatarError"
              :src="avatarUrl"
              :alt="userName"
              class="dropdown-avatar-img"
              @error="handleAvatarError"
            />
            <span v-else class="dropdown-avatar-initial">{{ userInitial }}</span>
          </div>
          <div class="dropdown-info">
            <div class="dropdown-name">{{ userName }}</div>
            <div class="dropdown-role">{{ currentUser?.roleName || '用户' }}</div>
          </div>
        </div>
        <div class="dropdown-divider" />

        <!-- 个人中心 -->
        <button class="dropdown-item" @click="handleProfile">
          <svg
            width="14"
            height="14"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
          >
            <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2" />
            <circle cx="12" cy="7" r="4" />
          </svg>
          <span>个人中心</span>
        </button>

        <!-- 退出登录 -->
        <button class="dropdown-item danger" @click="handleLogout">
          <svg
            width="14"
            height="14"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
          >
            <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4" />
            <polyline points="16 17 21 12 16 7" />
            <line x1="21" y1="12" x2="9" y2="12" />
          </svg>
          <span>退出登录</span>
        </button>
      </div>
    </Transition>
  </div>
</template>

<style lang="scss" scoped>
/* ───────── 容器 ───────── */
.user-profile {
  position: relative;

  &.variant-navbar {
    height: 100%;
    display: flex;
    align-items: center;
  }

  &.variant-sidebar {
    width: 100%;
  }
}

/* ───────── 触发按钮 ───────── */
.profile-trigger {
  display: flex;
  align-items: center;
  gap: 8px;
  border: none;
  background: transparent;
  cursor: pointer;
  font-family: inherit;
  color: var(--cube-layout-breadcrumb-item-color, var(--cube-layout-menu-item-color));
  transition:
    background 0.15s,
    color 0.15s;
  white-space: nowrap;

  &:hover,
  &.active {
    background: var(--cube-layout-menu-item-hover-bg, var(--el-color-primary-light-9));
    color: var(--cube-layout-menu-item-active-color, var(--el-color-primary));
  }
}

/* navbar 变体：撑满顶部栏高度 */
.variant-navbar .profile-trigger {
  height: 100%;
  padding: 0 16px;
  border-radius: 0;
}

/* sidebar 变体：宽度撑满，带圆角 */
.variant-sidebar .profile-trigger {
  width: 100%;
  padding: 12px 16px;
  border-radius: var(--el-border-radius-small, 8px);
}

/* ───────── 头像 ───────── */
.avatar-wrapper {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  overflow: hidden;
  flex-shrink: 0;
  background: linear-gradient(135deg, var(--el-color-primary) 0%, var(--el-color-primary-dark-2) 100%);
  display: flex;
  align-items: center;
  justify-content: center;
}

.avatar-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.avatar-initial {
  font-size: 13px;
  font-weight: 700;
  color: var(--el-color-white);
  line-height: 1;
}

/* ───────── 用户名 ───────── */
.profile-username {
  font-size: 13px;
  font-weight: 500;
  color: inherit;
}

/* ───────── 箭头 ───────── */
.profile-chevron {
  color: var(--el-text-color-secondary);
  transition: transform 0.2s;
  flex-shrink: 0;
  margin-left: auto;

  &.open {
    transform: rotate(180deg);
  }
}

/* sidebar 变体箭头旋转方向相反（默认向上，打开后向下） */
.dropup .profile-chevron {
  transform: rotate(180deg);

  &.open {
    transform: rotate(0deg);
  }
}

/* ───────── 下拉面板 ───────── */
.profile-dropdown {
  position: absolute;
  min-width: 220px;
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color);
  border-radius: var(--el-border-radius-base);
  box-shadow: var(--el-box-shadow);
  z-index: 300;
  overflow: visible;
}

/* navbar 变体：右对齐向下弹出 */
.variant-navbar .profile-dropdown {
  right: 0;
  top: 100%;
}

/* sidebar 变体：左对齐向上弹出，宽度与触发区一致 */
.variant-sidebar .profile-dropdown {
  left: 0;
  right: 0;
  min-width: 0; // 不覆盖 left/right 确定的宽度
  bottom: 100%;
  margin-bottom: 4px;
}

/* dropup 属性覆盖方向 */
.dropup .profile-dropdown {
  bottom: 100%;
  top: auto;
  margin-bottom: 4px;
}

/* ───────── 额外选项插槽区域 ───────── */
.extra-options {
  padding: 8px;
  display: flex;
  flex-direction: column;
  gap: 2px;

  // 侧边栏上下文中，子下拉菜单向右展开（左对齐），避免超出屏幕左侧
  :deep(.sw-menu) {
    right: auto;
    left: 0;
  }
}

/* ───────── 下拉头部 ───────── */
.dropdown-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px 16px;
}

.dropdown-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  overflow: hidden;
  background: linear-gradient(135deg, var(--el-color-primary) 0%, var(--el-color-primary-dark-2) 100%);
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.dropdown-avatar-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.dropdown-avatar-initial {
  font-size: 16px;
  font-weight: 700;
  color: var(--el-color-white);
}

.dropdown-info {
  flex: 1;
  min-width: 0;
}

.dropdown-name {
  font-size: 14px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.dropdown-role {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-top: 2px;
}

.dropdown-divider {
  height: 1px;
  background: var(--el-border-color-light);
}

/* ───────── 默认下拉选项 ───────── */
.dropdown-item {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  padding: 11px 16px;
  border: none;
  background: transparent;
  color: var(--cube-layout-menu-item-color);
  font-size: 13px;
  font-family: inherit;
  cursor: pointer;
  transition:
    background 0.12s,
    color 0.12s;
  text-align: left;

  &:hover {
    background: var(--el-color-primary-light-9);
    color: var(--el-text-color-primary);
  }

  &.danger {
    color: var(--el-color-danger);

    &:hover {
      background: var(--el-fill-color-lighter);
    }
  }
}

/* ───────── 过渡动画 ───────── */
.profile-fade-enter-active,
.profile-fade-leave-active {
  transition:
    opacity 0.15s,
    transform 0.15s;
}

.variant-navbar {
  .profile-fade-enter-from,
  .profile-fade-leave-to {
    opacity: 0;
    transform: translateY(-6px);
  }
}

.variant-sidebar,
.dropup {
  .profile-fade-enter-from,
  .profile-fade-leave-to {
    opacity: 0;
    transform: translateY(6px);
  }
}
</style>
