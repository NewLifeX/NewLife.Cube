<script setup lang="ts">
// 无需特殊逻辑
</script>

<template>
  <div class="content">
    <slot></slot>
  </div>
</template>

<style lang="scss" scoped>
.content {
  height: 100%;
  overflow: auto;
}
</style>
