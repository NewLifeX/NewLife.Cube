<template>
  <div class="test-page">
    <h1>Cube-Cube 测试页面</h1>
    <p>如果你能看到这个页面，说明路由配置正确</p>
    <p>当前路径: {{ $route.path }}</p>
    <p>当前时间: {{ currentTime }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';

const currentTime = ref(new Date().toLocaleString());

onMounted(() => {
  console.log('Cube-Cube 测试页面已挂载');
  // 每秒更新时间
  setInterval(() => {
    currentTime.value = new Date().toLocaleString();
  }, 1000);
});
</script>

<style scoped>
.test-page {
  padding: 20px;
  text-align: center;
}

.test-page h1 {
  color: #409eff;
  margin-bottom: 20px;
}

.test-page p {
  margin: 10px 0;
  font-size: 16px;
}
</style>
