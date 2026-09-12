<script setup lang="ts">
import { computed } from 'vue';
import { ElConfigProvider } from 'element-plus';
import { RouterView } from 'vue-router';
import RootLayout from './layouts/RootLayout.vue';
import ModalContainer from './components/ModalContainer.vue';
import { getEpLocale } from './i18n';

// initApp 里 app.use(ElementPlus, { locale }) 只在启动时取一次；
// 语言切换后，组件树内的 EP 内置文案（分页/表格空态/弹窗按钮等）靠 ConfigProvider 响应式下发
const epLocale = computed(() => getEpLocale());
</script>

<template>
  <ElConfigProvider :locale="epLocale">
    <RouterView v-slot="{ Component }">
      <RootLayout>
        <component :is="Component" />
      </RootLayout>
    </RouterView>
    <ModalContainer />
  </ElConfigProvider>
</template>
