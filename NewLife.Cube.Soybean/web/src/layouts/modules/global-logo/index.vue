<script setup lang="ts">
import { onMounted } from 'vue';
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
</script>

<template>
  <RouterLink to="/" class="w-full flex-center nowrap-hidden">
    <img v-if="siteStore.logo" :src="siteStore.logo" class="size-32px object-contain" alt="logo" />
    <SystemLogo v-else class="size-32px" />
    <h2 v-show="showTitle" class="pl-8px text-16px text-primary font-bold transition duration-300 ease-in-out">
      {{ siteStore.siteName }}
    </h2>
  </RouterLink>
</template>
