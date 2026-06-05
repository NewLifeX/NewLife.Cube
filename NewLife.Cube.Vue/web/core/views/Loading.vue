<template>
  <div class="loading-container">
    <div class="loading-spinner"></div>
    <div class="loading-text">系统加载中，请稍候...</div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { isRoutesInitialized } from '../microAppRouter';

const route = useRoute();
const router = useRouter();
let checkInterval: number;

onMounted(() => {
  // 每500ms检查一次路由是否初始化完成
  checkInterval = window.setInterval(() => {
    if (isRoutesInitialized()) {
      clearInterval(checkInterval);
      // 重定向到原始目标页面
      const redirectPath = (route.query.redirect as string) || '/';
      router.replace(redirectPath);
    }
  }, 500);
});

onUnmounted(() => {
  if (checkInterval) {
    clearInterval(checkInterval);
  }
});
</script>

<style scoped>
.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100vh;
  width: 100vw;
}

.loading-spinner {
  width: 50px;
  height: 50px;
  border: 5px solid #f3f3f3;
  border-top: 5px solid #3498db;
  border-radius: 50%;
  animation: spin 2s linear infinite;
  margin-bottom: 20px;
}

.loading-text {
  font-size: 16px;
  color: #333;
}

@keyframes spin {
  0% {
    transform: rotate(0deg);
  }
  100% {
    transform: rotate(360deg);
  }
}
</style>
