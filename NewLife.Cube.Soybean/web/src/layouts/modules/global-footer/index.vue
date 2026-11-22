<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { useSiteStore } from '@/store/modules/site';

defineOptions({
  name: 'GlobalFooter'
});

const siteStore = useSiteStore();

onMounted(() => {
  siteStore.load();
});

const copyright = computed(() => siteStore.copyright || 'Powered by NewLife.Cube');
</script>

<template>
  <DarkModeContainer class="h-full flex-center">
    <span class="text-12px" v-html="copyright" />
  </DarkModeContainer>
</template>
