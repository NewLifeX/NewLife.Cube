<script setup lang="ts">
import { ElConfigProvider } from 'element-plus';
import { RouterView } from 'vue-router';
import RootLayout from './layouts/RootLayout.vue';
</script>

<template>
  <ElConfigProvider>
    <RouterView v-slot="{ Component }">
      <RootLayout>
        <component :is="Component" />
      </RootLayout>
    </RouterView>
  </ElConfigProvider>
</template>
