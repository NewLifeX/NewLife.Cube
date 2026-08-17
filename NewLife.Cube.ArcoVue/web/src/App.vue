<template>
  <!--
    popup-container 挂到 #cube-scale-root：弹层与页面同包含块，坐标一致。
    字号缩放仅用 CSS 变量（见 applyTheme / tokens），不对布局根使用 CSS zoom，
    避免视口留白或 overflow 裁切工具栏/分页（arco-design-vue#3346）。
  -->
  <a-config-provider :update-at-scroll="true" :popup-container="popupContainer">
    <div id="cube-scale-root" class="cube-scale-root">
      <router-view />
    </div>
  </a-config-provider>
</template>

<script setup lang="ts">
import { useFavicon } from '@/layouts/useFavicon';

/** 登录页也在 RootLayout 之外，需在 App 级同步页签图标 */
useFavicon();

function popupContainer() {
  return document.getElementById('cube-scale-root') || document.body;
}
</script>

<style>
html,
body,
#app {
  height: 100%;
  overflow: hidden;
}
.cube-scale-root {
  position: relative;
  height: 100%;
  width: 100%;
  overflow: hidden;
  box-sizing: border-box;
}
</style>
