<script setup lang="ts">
import Sidebar from './Sidebar/index.vue';
</script>

<template>
  <div class="cyber-layout">
    <!-- 侧边栏 -->
    <Sidebar />

    <!-- 主内容区 -->
    <main class="cyber-content">
      <slot></slot>
    </main>
  </div>
</template>

<style lang="scss" scoped>
.cyber-layout {
  display: flex;
  height: 100vh;
  overflow: hidden;
  background: var(--el-bg-color);
  color: var(--el-text-color-primary);
  font-family: 'DM Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
}

.cyber-content {
  flex: 1;
  min-width: 0;
  overflow: auto;
  padding: 16px 20px;
  background:
    radial-gradient(ellipse at 10% 10%, var(--el-color-primary-light-9) 0%, transparent 40%),
    radial-gradient(ellipse at 90% 90%, var(--el-color-info-light-9) 0%, transparent 40%),
    var(--el-bg-color);
  scrollbar-width: thin;
  scrollbar-color: var(--border-subtle) transparent;

  &::-webkit-scrollbar {
    width: 8px;
    height: 8px;
  }
  &::-webkit-scrollbar-track {
    background: transparent;
  }
  &::-webkit-scrollbar-thumb {
    background: var(--border-subtle);
    border-radius: 4px;
  }
}
</style>