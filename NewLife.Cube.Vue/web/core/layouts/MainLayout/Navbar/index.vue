<script setup lang="ts">
import ModeSwitcher from '@newlifex/cube-vue/core/components/ModeSwitcher.vue';
import ThemeSwitcher from '@newlifex/cube-vue/core/components/ThemeSwitcher.vue';
import LayoutSwitcher from '@newlifex/cube-vue/core/components/LayoutSwitcher.vue';
import LogoBrand from '@newlifex/cube-vue/core/components/LogoBrand.vue';
import UserProfile from '@newlifex/cube-vue/core/components/UserProfile.vue';
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
  --navbar-border: var(--el-border-color-lighter);
  --navbar-text: var(--el-text-color-regular);
  --navbar-text-hover: var(--el-text-color-primary);
  --navbar-text-muted: var(--el-text-color-secondary);
  --navbar-hover-bg: var(--el-color-primary-light-9);

  display: flex;
  align-items: center;
  height: 56px;
  background: var(--navbar-bg);
  backdrop-filter: blur(14px);
  -webkit-backdrop-filter: blur(14px);
  border-bottom: 1px solid var(--navbar-border);
  box-shadow: 0 1px 6px var(--el-box-shadow-lighter);
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
