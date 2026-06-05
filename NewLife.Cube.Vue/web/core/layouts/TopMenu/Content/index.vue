<script setup lang="ts">
// 无需特殊逻辑
</script>

<template>
  <div class="tm-content">
    <slot></slot>
  </div>
</template>

<style lang="scss" scoped>
.tm-content {
  height: 100%;
  overflow: auto;
  padding: 16px;

  &::-webkit-scrollbar {
    width: 6px;
  }

  &::-webkit-scrollbar-thumb {
    background: var(--scrollbar-thumb, #c8d4c8);
    border-radius: 3px;
  }
}
</style>
