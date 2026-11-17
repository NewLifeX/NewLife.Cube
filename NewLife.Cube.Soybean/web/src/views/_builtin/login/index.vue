<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { getPaletteColorByNumber, mixColor } from '@sa/color';
import { useAppStore } from '@/store/modules/app';
import { useThemeStore } from '@/store/modules/theme';
import { useSiteStore } from '@/store/modules/site';
import { $t } from '@/locales';
import PwdLogin from './modules/pwd-login.vue';

defineOptions({ name: 'LoginPage' });

const appStore = useAppStore();
const themeStore = useThemeStore();
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

const bgThemeColor = computed(() =>
  themeStore.darkMode ? getPaletteColorByNumber(themeStore.themeColor, 600) : themeStore.themeColor
);

const bgColor = computed(() => {
  const COLOR_WHITE = '#ffffff';

  const ratio = themeStore.darkMode ? 0.5 : 0.2;

  return mixColor(COLOR_WHITE, themeStore.themeColor, ratio);
});

const backgroundStyle = computed(() => {
  if (!siteStore.loginBackground) return { backgroundColor: bgColor.value };

  return {
    backgroundColor: bgColor.value,
    backgroundImage: `url("${siteStore.loginBackground}")`,
    backgroundSize: 'cover',
    backgroundPosition: 'center'
  };
});
</script>

<template>
  <div class="relative size-full flex-center overflow-hidden" :style="backgroundStyle">
    <WaveBg v-if="!siteStore.loginBackground" :theme-color="bgThemeColor" />
    <ElCard class="relative z-4 w-auto rd-12px">
      <div class="w-400px lt-sm:w-300px">
        <header class="flex-y-center justify-between">
          <img
            v-if="showLogo"
            :src="siteStore.logo"
            class="size-64px object-contain lt-sm:size-48px"
            alt="logo"
            @error="logoBroken = true"
          />
          <SystemLogo v-else class="size-64px lt-sm:size-48px" />
          <h3 class="text-26px text-primary font-500 lt-sm:text-20px">{{ siteStore.siteName }}</h3>
          <div class="i-flex-col">
            <ThemeSchemaSwitch
              :theme-schema="themeStore.themeScheme"
              :show-tooltip="false"
              class="text-20px lt-sm:text-18px"
              @switch="themeStore.toggleThemeScheme"
            />
            <LangSwitch
              v-if="themeStore.header.multilingual.visible"
              :lang="appStore.locale"
              :lang-options="appStore.localeOptions"
              :show-tooltip="false"
              @change-lang="appStore.changeLocale"
            />
          </div>
        </header>
        <main class="pt-24px">
          <h3 class="text-18px text-primary font-medium">{{ $t('page.login.pwdLogin.title') }}</h3>
          <p v-if="siteStore.loginTip" class="pt-8px text-14px text-#888">{{ siteStore.loginTip }}</p>
          <div class="pt-24px">
            <PwdLogin />
          </div>
        </main>
      </div>
    </ElCard>
    <footer v-if="siteStore.copyright" class="absolute bottom-16px z-4 text-12px text-#888" v-html="siteStore.copyright" />
  </div>
</template>
