<template>
  <component :is="layoutComp" />
  <template v-if="!embed">
    <AppearanceDrawer
      v-model:visible="appearanceVisible"
    />
    <InboxDrawer v-model:visible="inboxVisible" />
    <AiAssistant />
  </template>
</template>

<script setup lang="ts">
import { computed, type Component } from 'vue';
import { resolveLayoutMode } from '@/core/utils/userProfile';
import { isEmbedMode } from '@/core/utils/embedMode';
import { useUserProfileStore } from '@/stores/userProfile';
import { useAppStore } from '@/stores/app';
import { useShellAuth } from './useShellAuth';
import { useDocumentTitle } from './useDocumentTitle';
import SideLayout from './side.vue';
import TopLayout from './top.vue';
import MixLayout from './mix.vue';
import EmbedLayout from './EmbedLayout.vue';
import AppearanceDrawer from '@/views/settings/AppearanceDrawer.vue';
import InboxDrawer from '@/views/inbox/InboxDrawer.vue';
import AiAssistant from '@/views/ai/AiAssistant.vue';

useShellAuth();
useDocumentTitle();

const profileStore = useUserProfileStore();
const appStore = useAppStore();
const embed = computed(() => isEmbedMode());

const layoutMap: Record<string, Component> = {
  side: SideLayout,
  top: TopLayout,
  mix: MixLayout,
};

const layoutComp = computed(() => {
  if (embed.value) return EmbedLayout;
  const mode = resolveLayoutMode(profileStore.layout.mode);
  return layoutMap[mode] || SideLayout;
});

const appearanceVisible = computed({
  get: () => appStore.appearanceDrawerVisible,
  set: (v: boolean) => {
    appStore.appearanceDrawerVisible = v;
  },
});

const inboxVisible = computed({
  get: () => appStore.inboxDrawerVisible,
  set: (v: boolean) => {
    appStore.inboxDrawerVisible = v;
  },
});
</script>
