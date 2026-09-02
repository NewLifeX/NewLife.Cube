<template>
  <Workbench :slug="slug" />
</template>

<script setup lang="ts">
/**
 * 命名工作台菜单页（OSC-260902ef43）：菜单 Url=/Workbench/{slug} 由 menuRoutes 前缀分流到本页。
 * /home（默认工作台）直接渲染 Workbench（无 slug），不经本页。
 */
import Workbench from './Workbench.vue';

defineOptions({ name: 'WorkbenchPage' });

const props = defineProps<{ slug?: string }>();
const slug = props.slug ?? '';
</script>
