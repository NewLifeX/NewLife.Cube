<script setup lang="ts">
interface Props {
  /** 页面标题 */
  title: string;
  /** 页面副标题 */
  subtitle?: string;
}

defineProps<Props>();
</script>

<template>
  <header class="cube-page-header">
    <div class="cube-page-header__text">
      <h1 class="cube-page-header__title">{{ title }}</h1>
      <p v-if="subtitle" class="cube-page-header__subtitle">{{ subtitle }}</p>
    </div>
    <div v-if="$slots.extra" class="cube-page-header__extra">
      <slot name="extra" />
    </div>
  </header>
</template>

<style scoped>
.cube-page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
}

.cube-page-header__text {
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 0;
}

.cube-page-header__title {
  margin: 0;
  color: var(--el-text-color-primary);
  font-size: 22px;
  font-weight: 600;
  line-height: 1.3;
  letter-spacing: -0.01em;
}

.cube-page-header__subtitle {
  margin: 0;
  color: var(--el-text-color-secondary);
  font-size: 13px;
  line-height: 1.4;
}

.cube-page-header__extra {
  flex: none;
  display: flex;
  align-items: center;
  gap: 8px;
}
</style>
