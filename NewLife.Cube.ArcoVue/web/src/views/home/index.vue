<template>
  <DefaultHome />
</template>

<script setup lang="ts">
/**
 * /home 薄壳（OSC-2608139feb）：直接复用 DefaultHome，与 /Admin/Index 同一实现。
 */
import DefaultHome from './DefaultHome.vue';

defineOptions({ name: 'Home' });
</script>
