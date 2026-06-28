<script setup lang="ts">
/**
 * LogoBrand - Logo 和品牌名称组件
 * 用于导航栏左侧的品牌展示
 */
import { ref, computed } from 'vue';
import { getConfig } from 'cube-front/core/configure';

interface Props {
  /** 是否折叠（折叠时隐藏标题） */
  collapsed?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  collapsed: false,
});

const config = getConfig();
const brandTitle = config.base.title || 'Cube Front';
const brandLogo = computed(() => config.base.logo);

// 图片 logo 相关
const logoImgRef = ref<HTMLImageElement | null>(null);
const logoAspectRatio = ref<number | null>(null);
const onLogoLoad = (e: Event) => {
  const img = e.target as HTMLImageElement;
  if (img.naturalWidth && img.naturalHeight) {
    logoAspectRatio.value = img.naturalWidth / img.naturalHeight;
  }
};
// 根据图片宽高比和折叠状态决定是否显示标题
const showLogoTitle = computed(() => {
  if (props.collapsed) return false; // 折叠时隐藏
  if (logoAspectRatio.value === null) return true; // 加载中默认显示
  const ratio = logoAspectRatio.value;
  return ratio > 0.9 && ratio < 1.1; // 正方形隐藏标题
});
</script>

<template>
  <div class="logo-brand">
    <!-- 图片 logo -->
    <div v-if="brandLogo" class="logo-img-wrap">
      <img ref="logoImgRef" :src="brandLogo" :alt="brandTitle" @load="onLogoLoad" />
    </div>
    <span v-if="showLogoTitle" class="logo-title">{{ brandTitle }}</span>
  </div>
</template>

<style lang="scss" scoped>
.logo-brand {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  box-sizing: border-box;

  .collapsed & {
    padding: 8px 0;
    justify-content: center;
  }
}

.logo-img-wrap {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  height: 100%;
  width: auto;
  max-width: 100%;
  border-radius: 6px;
  overflow: hidden;

  img {
    height: 100%;
    width: auto;
    max-width: 100%;
    object-fit: contain;
  }
}

.logo-title {
  flex: 1;
  font-weight: 700;
  font-size: 14px;
  color: var(--el-text-color-primary);
  letter-spacing: -0.02em;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  min-width: 0;
}
</style>