<template>
  <div>
    <div class="text-h5 font-weight-bold mb-4">首页</div>
    <v-row>
      <v-col v-for="card in stats" :key="card.title" cols="12" sm="6" md="3">
        <v-card>
          <v-card-text>
            <div class="text-body-2 text-medium-emphasis">{{ card.title }}</div>
            <div class="text-h4 font-weight-bold mt-1">{{ card.value }}</div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
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
