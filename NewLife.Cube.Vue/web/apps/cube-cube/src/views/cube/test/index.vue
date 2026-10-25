<template>
  <div class="test-page">
    <h1>Cube-Cube test page</h1>
    <p>If you can see this page, the route is configured correctly.</p>
    <p>Current path: {{ route.path }}</p>
    <p>Current time: {{ currentTime }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();
const currentTime = ref(new Date().toLocaleString());

onMounted(() => {
  console.log('Cube-Cube test page mounted');
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
