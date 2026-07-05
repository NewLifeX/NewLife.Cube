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
.list-page-header {
  padding: 0 0 6px;
  display: flex;
}

.lph-left {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.lph-title {
  font-family: 'Libre Baskerville', Georgia, serif;
  font-size: 27px;
  font-weight: 700;
  color: var(--el-text-color-primary);
  letter-spacing: -0.025em;
  margin: 0;
  line-height: 1.2;
}

.lph-subtitle {
  font-family: 'Fira Sans', system-ui, sans-serif;
  font-size: 13.5px;
  color: var(--el-text-color-secondary);
  margin: 0;
}
</style>
