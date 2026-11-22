<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useSiteStore } from '@/store/modules/site';

defineOptions({ name: 'GlobalLogo' });

interface Props {
  /** Whether to show the title */
  showTitle?: boolean;
}

withDefaults(defineProps<Props>(), {
  showTitle: true
});

const siteStore = useSiteStore();

onMounted(() => {
  siteStore.load();
});

/** 后台配置的 Logo 加载失败时退回内置默认 Logo */
const logoBroken = ref(false);
const showLogo = computed(() => !!siteStore.logo && !logoBroken.value);
watch(
  () => siteStore.logo,
  () => {
    logoBroken.value = false;
  }
);
</script>

<template>
  <RouterLink to="/" class="w-full flex-center nowrap-hidden">
    <img v-if="showLogo" :src="siteStore.logo" class="size-32px object-contain" alt="logo" @error="logoBroken = true" />
    <SystemLogo v-else class="size-32px" />
    <h2 v-show="showTitle" class="pl-8px text-16px text-primary font-bold transition duration-300 ease-in-out">
      {{ siteStore.siteName }}
    </h2>
  </RouterLink>
</template>
