<script setup lang="ts">
import ModeSwitcher from 'cube-front/core/components/ModeSwitcher.vue';
import ThemeSwitcher from 'cube-front/core/components/ThemeSwitcher.vue';
import LayoutSwitcher from 'cube-front/core/components/LayoutSwitcher.vue';
import LogoBrand from 'cube-front/core/components/LogoBrand.vue';
import UserProfile from 'cube-front/core/components/UserProfile.vue';
</script>

<template>
  <header class="navbar">
    <!-- 左侧品牌 -->
    <div class="navbar-left">
      <LogoBrand />
    </div>

    <!-- 中间菜单 -->
    <div class="navbar-center"></div>

    <!-- 右侧用户 -->
    <div class="navbar-right">
      <!-- 模式切换（太阳/月亮） -->
      <ModeSwitcher />

      <!-- 主题选择器（调色盘） -->
      <ThemeSwitcher />

      <!-- 布局切换 -->
      <LayoutSwitcher />

      <UserProfile variant="navbar" />
    </div>
  </header>
</template>

<style lang="scss" scoped>
$brand-width: 220px;

.navbar {
  --navbar-bg: rgba(255, 255, 255, 0.82);
  --navbar-border: rgba(226, 224, 220, 0.6);
  --navbar-text: var(--text-secondary);
  --navbar-text-hover: var(--text-primary);
  --navbar-text-muted: var(--text-muted);
  --navbar-hover-bg: var(--accent-muted);

  display: flex;
  align-items: center;
  height: 56px;
  background: rgba(255, 255, 255, 0.82);
  backdrop-filter: blur(14px);
  -webkit-backdrop-filter: blur(14px);
  border-bottom: 1px solid rgba(226, 224, 220, 0.6);
  box-shadow: 0 1px 6px rgba(0, 0, 0, 0.03);
}

.navbar-left {
  width: $brand-width;
  height: 56px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
}

.navbar-center {
  flex: 1;
  height: 56px;
  overflow: hidden;
  min-width: 0;
}

.navbar-right {
  display: flex;
  align-items: center;
  gap: 12px;
  padding-right: 20px;
  flex-shrink: 0;
  height: 56px;
}
</style>
