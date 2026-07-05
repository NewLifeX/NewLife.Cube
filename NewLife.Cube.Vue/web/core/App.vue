<script setup lang="ts">
import { ElConfigProvider } from 'element-plus';
import { RouterView } from 'vue-router';
import RootLayout from './layouts/RootLayout.vue';
import ModalContainer from './components/ModalContainer.vue';
</script>

<template>
  <ElConfigProvider>
    <RouterView v-slot="{ Component }">
      <RootLayout>
        <component :is="Component" />
      </RootLayout>
    </RouterView>
    <ModalContainer />
  </ElConfigProvider>
</template>
