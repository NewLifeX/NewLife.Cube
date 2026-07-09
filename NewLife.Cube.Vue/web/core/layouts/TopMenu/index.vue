<script setup lang="ts">
import { computed } from 'vue';
import { useMenuStore, type TreeMenuItem } from '@newlifex/cube-vue/core/stores/menu';
import Topnav from './Topnav/index.vue';
import Content from './Content/index.vue';

const menuStore = useMenuStore();
const activeMenu = computed(() => menuStore.activeMenu);

// 面包屑路径
const breadcrumbPath = computed(() => {
  if (!activeMenu.value) return [];
  const path: Array<{ name: string; title?: string }> = [];
  let current: TreeMenuItem | undefined = activeMenu.value;
  while (current) {
    path.unshift({ name: current.name, title: current.title });
    current = current.parentMenu;
  }
  return path;
});
</script>

<template>
  <div class="tm-layout">
    <!-- 顶部导航 -->
    <Topnav />

    <!-- 主体区域 -->
    <div class="body-wrap">
      <!-- 主内容 -->
      <div class="main-wrap">
        <!-- 面包屑子头部 -->
        <div class="sub-hd">
          <nav class="bc">
            <template v-for="(item, idx) in breadcrumbPath" :key="idx">
              <span v-if="idx < breadcrumbPath.length - 1" class="bc-s">
                {{ item.title || item.name }}
              </span>
              <span v-if="idx < breadcrumbPath.length - 1" class="bc-sep">›</span>
              <span v-else class="bc-cur">{{ item.title || item.name }}</span>
            </template>
          </nav>
        </div>

        <!-- 内容区 -->
        <Content>
          <slot></slot>
        </Content>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
$tn-h: 60px;

.tm-layout {
  width: 100%;
  height: 100vh;
  display: flex;
  flex-direction: column;
  background: var(--bg, #f4f5f1);
  overflow: hidden;
}

.body-wrap {
  display: flex;
  height: calc(100vh - #{$tn-h});
  overflow: hidden;
}

.main-wrap {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.sub-hd {
  background: var(--card, #fff);
  border-bottom: 1px solid var(--bd, #e0e6da);
  padding: 0 16px;
  display: flex;
  align-items: center;
  height: 40px;
  gap: 6px;
  flex-shrink: 0;
}

.bc {
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 13px;
}

.bc-s {
  color: var(--el-text-color-placeholder);
}

.bc-cur {
  color: var(--el-text-color-primary);
  font-weight: 500;
}

.bc-sep {
  color: var(--el-text-color-placeholder);
  font-size: 11px;
}
</style>
