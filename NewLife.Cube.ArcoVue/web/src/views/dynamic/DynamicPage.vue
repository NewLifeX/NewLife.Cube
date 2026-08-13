<template>
  <component :is="overrideComp" v-if="overrideComp" />
  <DefaultList v-else :type="typePath" :auth-id="authId" />
</template>

<script setup lang="ts">
/**
 * DynamicPage — 薄宿主
 * 契约：仅接收 type / authId；不读取布局/主题 store。
 */
import { defineAsyncComponent } from 'vue';
import { useDynamicPage } from './useDynamicPage';

/** 异步拉 DefaultList，避免把 VTable 打进 DynamicPage 主 chunk */
const DefaultList = defineAsyncComponent(() => import('@/views/crud/DefaultList.vue'));

const props = defineProps<{
  type?: string;
  authId?: number;
}>();

const {
  typePath,
  authId,
  overrideComp,
} = useDynamicPage(props);
</script>
