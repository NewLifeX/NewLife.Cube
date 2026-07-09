<script setup lang="ts">
import { computed } from 'vue';
import { useMenuStore, type TreeMenuItem } from '@newlifex/cube-vue/core/stores/menu';

interface Props {
  title?: string;
  subtitle?: string;
}

const props = defineProps<Props>();
const menuStore = useMenuStore();

const breadcrumbs = computed<TreeMenuItem[]>(() => {
  const active = menuStore.activeMenu;
  if (!active) return [];
  const path: TreeMenuItem[] = [];
  let cur: TreeMenuItem | undefined = active;
  while (cur) {
    path.unshift(cur);
    cur = cur.parentMenu;
  }
  return path;
});

const pageTitle = computed(() => props.title ?? menuStore.activeMenu?.name ?? '');
</script>

<template>
  <div class="form-page-header">
    <div class="fph-left">
      <h1 class="fph-title">{{ pageTitle }}</h1>
      <p v-if="subtitle" class="fph-subtitle">{{ subtitle }}</p>
    </div>
    <nav v-if="breadcrumbs.length > 0" class="fph-breadcrumb" aria-label="面包屑导航">
      <span
        v-for="(item, index) in breadcrumbs"
        :key="item.id"
        class="bc-item"
        :class="{ 'bc-item--active': index === breadcrumbs.length - 1 }"
      >
        <span v-if="index > 0" class="bc-sep" aria-hidden="true">›</span>
        <span class="bc-label">{{ item.name }}</span>
      </span>
    </nav>
  </div>
</template>

<style lang="scss" scoped>
.form-page-header {
  background: var(--el-bg-color-overlay);
  border-bottom: 1px solid var(--el-border-color-light);
  padding: 20px 24px 16px;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
}

.fph-left {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.fph-title {
  font-family: 'Libre Baskerville', Georgia, serif;
  font-size: 27px;
  font-weight: 700;
  color: var(--el-text-color-primary);
  letter-spacing: -0.025em;
  margin: 0;
  line-height: 1.2;
}

.fph-subtitle {
  font-family: 'Fira Sans', system-ui, sans-serif;
  font-size: 13px;
  color: var(--el-text-color-secondary);
  margin: 0;
}

.fph-breadcrumb {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 2px;
  padding-top: 4px;
}

.bc-item {
  display: inline-flex;
  align-items: center;
  font-family: 'Fira Sans', system-ui, sans-serif;
  font-size: 13px;
  color: var(--el-text-color-secondary);

  &--active {
    color: var(--el-text-color-primary);
    font-weight: 500;
  }
}

.bc-sep {
  margin: 0 4px;
  color: var(--el-border-color-light);
}
</style>
