<template>
  <Workbench />
</template>

<script setup lang="ts">
/**
 * /home 工作台（OSC-26082815a1）。/Admin/Index 仍走 DefaultHome 监控页。
 */
import Workbench from './Workbench.vue';

defineOptions({ name: 'Home' });
</script>
