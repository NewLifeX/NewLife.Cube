<template>
  <div class="home-page">
    <n-page-header title="首页" subtitle="欢迎使用魔方管理平台" />
    <n-grid :cols="4" :x-gap="16" :y-gap="16" style="margin-top: 16px">
      <n-gi v-for="card in statCards" :key="card.title">
        <n-card>
          <n-statistic :label="card.title" :value="card.value" />
        </n-card>
      </n-gi>
    </n-grid>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

const statCards = ref([
  { title: '在线用户', value: '-' },
  { title: '今日访问', value: '-' },
  { title: '系统版本', value: '-' },
  { title: '运行时间', value: '-' },
]);
</script>

<style scoped>
.home-page {
  padding: 8px;
}
</style>
