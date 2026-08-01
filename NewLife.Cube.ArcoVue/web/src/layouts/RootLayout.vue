<template>
  <a-config-provider :update-at-scroll="true">
    <component :is="layoutComp" />
  </a-config-provider>
</template>

<script setup lang="ts">
import { computed, type Component } from 'vue';
import { resolveLayoutMode } from '@/core/utils/userProfile';
import { useUserProfileStore } from '@/stores/userProfile';
import { useShellAuth } from './useShellAuth';
import SideLayout from './side.vue';
import TopLayout from './top.vue';
import MixLayout from './mix.vue';

useShellAuth();

const profileStore = useUserProfileStore();

const layoutMap: Record<string, Component> = {
  side: SideLayout,
  top: TopLayout,
  mix: MixLayout,
};

const layoutComp = computed(() => {
  const mode = resolveLayoutMode(profileStore.layout.mode);
  return layoutMap[mode] || SideLayout;
});
</script>
