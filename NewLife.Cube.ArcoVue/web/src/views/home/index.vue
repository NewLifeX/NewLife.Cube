<template>
  <div>
    <a-page-header title="首页" subtitle="系统概览" :show-back="false" />
    <a-grid :cols="{ xs: 1, sm: 2, md: 4 }" :col-gap="16" :row-gap="16" style="padding: 0 16px;">
      <a-grid-item v-for="card in stats" :key="card.title">
        <a-card hoverable>
          <a-statistic :title="card.title" :value="card.value" />
        </a-card>
      </a-grid-item>
    </a-grid>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

const stats = ref([
  { title: '用户总数', value: 0 },
  { title: '今日访问', value: 0 },
  { title: '在线用户', value: 0 },
  { title: '系统消息', value: 0 },
]);
</script>
