<script setup lang="ts">
import { ref } from 'vue';
import Navbar from './Navbar/index.vue';
import Content from './Content/index.vue';
import Sider from './Sider/index.vue';
import { useUserStore } from 'cube-front/core/stores/user';

const userStore = useUserStore();
const collapsed = ref(false);

const navbarHeight = 56;
const menuWidth = collapsed.value ? 64 : 240;
const paddingLeft = { paddingLeft: menuWidth + 'px' };
const paddingTop = { paddingTop: navbarHeight + 'px' };

const paddingStyle = { ...paddingLeft, ...paddingTop };
</script>

<template>
  <div class="layout">
    <div class="layoutNavbar">
      <Navbar :currentUser="userStore.userInfo" :logout="userStore.logout" :collapsed="collapsed" />
    </div>
    <div class="layoutMain">
      <div class="layoutSidebar" :style="{ ...paddingTop, width: menuWidth + 'px' }">
        <Sider />
      </div>
      <div class="layoutContent" :style="paddingStyle">
        <div class="layoutContentWrapper">
          <Content>
            <slot></slot>
          </Content>
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
$nav-size-height: 56px;
$layout-max-width: 1100px;

.layout {
  width: 100%;
  height: 100%;
  min-height: 100vh;
  background:
    radial-gradient(ellipse 70% 50% at 15% 5%, var(--el-color-primary-light-9) 0%, transparent 55%),
    radial-gradient(ellipse 50% 40% at 85% 95%, var(--el-fill-color-light) 0%, transparent 55%),
    linear-gradient(180deg, var(--el-bg-color-page) 0%, var(--el-fill-color-light) 50%, var(--el-bg-color-page) 100%);
}

.layoutNavbar {
  position: fixed;
  width: 100%;
  min-width: $layout-max-width;
  top: 0;
  left: 0;
  height: $nav-size-height;
  z-index: 100;
}

.layoutMain {
  .layoutSidebar {
    position: fixed;
    height: 100%;
    top: 0;
    left: 0;
    z-index: 99;
    box-sizing: border-box;
    transition: width 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  }

  .layoutContent {
    min-width: $layout-max-width;
    min-height: 100vh;
    height: 100vh;
    transition: padding-left 0.3s cubic-bezier(0.4, 0, 0.2, 1);
    box-sizing: border-box;
    padding-right: 12px;
    padding-bottom: 12px;
  }

  .layoutContentWrapper {
    height: 100%;
    background: rgba(255, 255, 255, 0.78);
    backdrop-filter: blur(12px);
    -webkit-backdrop-filter: blur(12px);
    border-radius: var(--el-border-radius-large, 16px);
    padding: 24px 16px 0 24px;
    box-sizing: border-box;
    border: 1px solid rgba(255, 255, 255, 0.55);
    box-shadow:
      0 4px 24px var(--el-box-shadow-light),
      0 1px 3px rgba(0, 0, 0, 0.02);
  }
}
</style>
