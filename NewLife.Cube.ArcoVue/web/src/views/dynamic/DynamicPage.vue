<template>
  <component :is="overrideComp" v-if="overrideComp" />
  <DefaultList v-else :type="typePath" :auth-id="authId" />
</template>

<script setup lang="ts">
/**
 * DynamicPage — 薄宿主
 * 契约：仅接收 type / authId；不读取布局/主题 store。
 */
import { computed, defineAsyncComponent, ref, watch, type Component } from 'vue';
import { useRoute } from 'vue-router';
import { getSectionLoader } from '@/core/composables/useSections';
import { routeToApiPrefix } from '@/core/utils/url';

/** 异步拉 DefaultList，避免把 VTable 打进 DynamicPage 主 chunk */
const DefaultList = defineAsyncComponent(() => import('@/views/crud/DefaultList.vue'));

const props = defineProps<{
  type?: string;
  authId?: number;
}>();

const route = useRoute();

const typePath = computed(() => {
  if (props.type) return props.type;
  const metaType = route.meta.typePath as string | undefined;
  if (metaType) return metaType;
  return routeToApiPrefix(route.path);
});

const authId = computed(() => props.authId ?? (route.meta.menuId as number | undefined));

const overrideComp = ref<Component | null>(null);

async function resolveOverride() {
  const loader = getSectionLoader(typePath.value, 'DefaultListPage');
  if (!loader) {
    overrideComp.value = null;
    return;
  }
  overrideComp.value = defineAsyncComponent(loader as () => Promise<{ default: Component }>);
}

watch(typePath, resolveOverride, { immediate: true });
</script>
