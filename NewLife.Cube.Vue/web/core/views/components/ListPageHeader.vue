<script setup lang="ts">
import { computed } from 'vue';
import { useMenuStore } from 'cube-front/core/stores/menu';

interface Props {
  title?: string;
  subtitle?: string;
}

const props = defineProps<Props>();
const menuStore = useMenuStore();

const pageTitle = computed(
  () => props.title ?? menuStore.activeMenu?.title ?? menuStore.activeMenu?.name ?? '',
);
</script>

<template>
  <div class="list-page-header">
    <div class="lph-left">
      <h1 class="lph-title">{{ pageTitle }}</h1>
      <p v-if="subtitle" class="lph-subtitle">{{ subtitle }}</p>
    </div>
  </div>
</template>

<style lang="scss" scoped>
/**
 * - 大标题使用无衬线字体，负字间距
 * - 副标题使用浅色调
 */
.list-page-header {
  padding: 0 0 8px;
  display: flex;
}

.lph-left {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.lph-title {
  font-family: var(--el-font-family);
  font-size: 24px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  letter-spacing: -0.02em;
  margin: 0;
  line-height: 1.3;
}

.lph-subtitle {
  font-family: var(--el-font-family);
  font-size: 13px;
  font-weight: 400;
  color: var(--el-text-color-secondary);
  margin: 0;
  line-height: 1.4;
}
</style>
