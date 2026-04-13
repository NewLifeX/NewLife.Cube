<template>
  <div>
    <h2 style="margin-bottom: 8px">欢迎回来，{{ userStore.displayName }}</h2>
    <p style="color: var(--td-text-color-secondary); margin-bottom: 24px">{{ appStore.siteTitle }} 管理控制台</p>

    <t-row :gutter="16">
      <t-col v-for="stat in stats" :key="stat.label" :span="3">
        <t-card :bordered="true">
          <t-statistic :title="stat.label" :value="stat.value" :prefix="stat.prefix" />
        </t-card>
      </t-col>
    </t-row>
  </div>
</template>

<script setup lang="ts">
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';

const appStore = useAppStore();
const userStore = useUserStore();

const stats = [
  { label: '用户数', value: '-', prefix: '' },
  { label: '角色数', value: '-', prefix: '' },
  { label: '在线数', value: '-', prefix: '' },
  { label: '今日访问', value: '-', prefix: '' },
];
</script>
