<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import LayoutSwitcher from '@newlifex/cube-vue/core/components/LayoutSwitcher.vue';
import LanguageSwitch from '@newlifex/cube-vue/core/components/LanguageSwitch.vue';
import ThemeSwitcher from '@newlifex/cube-vue/core/components/ThemeSwitcher.vue';
import ModeSwitcher from '@newlifex/cube-vue/core/components/ModeSwitcher.vue';
import { useUserStore } from '@newlifex/cube-vue/core/stores/user';
import { useMenuStore } from '@newlifex/cube-vue/core/stores/menu';
import NotificationBell from '@newlifex/cube-vue/core/components/NotificationBell.vue';

const { t, locale } = useI18n();
const userStore = useUserStore();
const menuStore = useMenuStore();

// 用户信息
const currentUser = computed(() => userStore.userInfo);
const userName = computed(
  () => currentUser.value?.displayName || currentUser.value?.name || t('layout.administrator'),
);

// 当前页面标题
const pageTitle = computed(
  () => menuStore.activeMenu?.title || menuStore.activeMenu?.name || t('layout.dashboard'),
);

// 当前日期。按当前语言格式化，避免写死中文星期与日期模板
const currentDate = computed(() =>
  new Intl.DateTimeFormat(locale.value, {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    weekday: 'long',
  }).format(new Date()),
);
</script>

<template>
  <header class="cyber-navbar">
    <!-- 左侧标题 -->
    <div class="navbar-left">
      <h2>{{ pageTitle }}</h2>
      <p>{{ t('layout.welcomeBack', { name: userName, date: currentDate }) }}</p>
    </div>

    <!-- 右侧操作 -->
    <div class="navbar-right">
      <!-- 搜索框 -->
      <div class="search-box">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <circle cx="11" cy="11" r="8" />
          <path d="M21 21l-4.35-4.35" />
        </svg>
        <input type="text" :placeholder="t('layout.searchPlaceholder')" />
      </div>

      <!-- 模式切换（太阳/月亮） -->
      <ModeSwitcher />

      <!-- 主题选择器（调色盘） -->
      <ThemeSwitcher />
      <!-- 布局切换 -->
      <LayoutSwitcher />
      <LanguageSwitch />

      <!-- 通知按钮 -->
      <NotificationBell show-label />
    </div>
  </header>
</template>

<style lang="scss" scoped>
.cyber-navbar {
  --navbar-bg: var(--el-bg-color-overlay);
  --navbar-border: var(--el-border-color-light);
  --navbar-text: var(--el-text-color-primary);
  --navbar-text-hover: var(--el-color-primary);
  --navbar-text-muted: var(--el-text-color-secondary);
  --navbar-hover-bg: var(--el-color-primary-light-9);

  display: flex;
  justify-content: space-between;
  align-items: center;
  height: var(--layout-nav-height, 64px);
  padding: 0 32px;
  background: var(--navbar-bg);
  border-bottom: 1px solid var(--navbar-border);
}

.navbar-left {
  h2 {
    font-size: 18px;
    font-weight: 600;
    letter-spacing: -0.3px;
    margin: 0 0 2px 0;
    color: var(--navbar-text);
  }

  p {
    font-size: 12px;
    color: var(--navbar-text-muted);
    margin: 0;
  }
}

.navbar-right {
  display: flex;
  align-items: center;
  gap: 16px;
}

// 搜索框
.search-box {
  display: flex;
  align-items: center;
  gap: 8px;
  background: var(--el-fill-color-light);
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--radius-sm);
  padding: 8px 12px;
  width: 220px;
  transition: border-color 0.2s;

  &:focus-within {
    border-color: var(--el-color-primary);
  }

  svg {
    width: 14px;
    height: 14px;
    /* 使用 --el-text-color-secondary 替代未定义的 --text-muted */
    color: var(--el-text-color-secondary);
    flex-shrink: 0;
  }

  input {
    background: none;
    border: none;
    outline: none;
    /* 使用 --el-text-color-primary 替代未定义的 --text-primary */
    color: var(--el-text-color-primary);
    font-family: inherit;
    font-size: 13px;
    width: 100%;

    &::placeholder {
      /* 使用 --el-text-color-placeholder 替代未定义的 --text-muted 作为占位符色 */
      color: var(--el-text-color-placeholder);
    }
  }
}
</style>
