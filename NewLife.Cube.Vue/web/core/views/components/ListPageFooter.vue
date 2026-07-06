<template>
  <div class="list-page-footer">
    <slot></slot>
  </div>
</template>

<style lang="scss" scoped>
/*
 * 深色背景，小字体，使用浅色调
 */
.list-page-footer {
  padding: 8px 0;
  color: var(--el-text-color-secondary);
  font-family: var(--el-font-family);
  font-size: 12px;
  font-weight: 400;
}
</style>
