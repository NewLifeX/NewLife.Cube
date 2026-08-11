<script setup lang="ts">
/**
 * LogoBrand - Logo 和品牌名称组件
 * 用于导航栏左侧的品牌展示
 */
import { ref, computed } from 'vue';
import { getConfig } from '@newlifex/cube-vue/core/configure';

interface Props {
  /** 是否折叠（折叠时隐藏标题） */
  collapsed?: boolean;
  /** logo 最大高度（px）：正方形 logo 按此值显示方框，长方形 logo 高度按此值等比缩放 */
  logoHeight?: number;
}

const props = withDefaults(defineProps<Props>(), {
  collapsed: false,
  logoHeight: 40,
});

const config = getConfig();
const brandTitle = config.base.title || '魔方系统';
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
// 长方形 logo 只显示图标不显示标题，正方形 logo 才显示标题
const showLogoTitle = computed(() => {
  if (props.collapsed) return false; // 折叠时隐藏
  if (logoAspectRatio.value === null) return true; // 加载中默认显示
  const ratio = logoAspectRatio.value;
  return ratio > 0.9 && ratio < 1.1; // 正方形显示标题，长方形隐藏标题
});

// 图片容器尺寸：正方形 logo 显示 logoHeight x logoHeight 方框；长方形 logo 高度为 logoHeight、宽度按比例自适应
// 避免长方形 logo 被 object-fit: contain 等比缩小进小方框而显示过小
const imgWrapStyle = computed(() => {
  const r = logoAspectRatio.value;
  const h = props.logoHeight;
  if (r === null) return { width: `${h}px`, height: `${h}px` }; // 加载中/未知：按最大高度显示方框
  const isSquare = r > 0.9 && r < 1.1;
  if (isSquare) return { width: `${h}px`, height: `${h}px` };
  // 长方形 logo：高度 = logoHeight，宽度按比例；折叠侧边栏仅 64px 宽，需限制最大宽度防止溢出
  const maxWidth = props.collapsed ? 48 : 180;
  const width = Math.min(Math.round(h * r), maxWidth);
  return { width: `${width}px`, height: `${h}px` };
});
</script>

<template>
  <div class="logo-brand">
    <!-- 图片 logo -->
    <div v-if="brandLogo" class="logo-img-wrap" :style="imgWrapStyle">
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
  width: 32px;
  height: 32px;
  border-radius: 6px;
  overflow: hidden;

  img {
    width: 100%;
    height: 100%;
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
