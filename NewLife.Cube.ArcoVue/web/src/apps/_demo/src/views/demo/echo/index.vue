<template>
  <div style="padding: 24px">
    <a-result status="success" title="Apps 整页覆写样例" subtitle="路径 /Demo/Echo → apps/_demo/.../demo/echo/index.vue">
      <template #extra>
        <a-typography-paragraph>
          本页用于 OSC-0003 验收「apps 整页覆写」机制。真实菜单若配置 url=/Demo/Echo 将进入此页而非 DefaultList。
        </a-typography-paragraph>
      </template>
    </a-result>
  </div>
</template>

<script setup lang="ts">
/** OSC-0003 demo：apps 整页覆写 */
</script>
