<template>
  <n-config-provider :theme="theme" :locale="zhCN" :date-locale="dateZhCN">
    <n-dialog-provider>
      <n-notification-provider>
        <n-message-provider>
          <n-loading-bar-provider>
            <router-view />
          </n-loading-bar-provider>
        </n-message-provider>
      </n-notification-provider>
    </n-dialog-provider>
  </n-config-provider>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { zhCN, dateZhCN, darkTheme } from 'naive-ui';
import { useAppStore } from '@/stores/app';

const appStore = useAppStore();
const theme = computed(() => (appStore.darkMode ? darkTheme : null));
</script>
