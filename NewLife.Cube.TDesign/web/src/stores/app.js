import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
export const useAppStore = defineStore('app', () => {
    const collapsed = ref(false);
    const darkMode = ref(false);
    const siteInfo = ref(null);
    const siteTitle = computed(() => siteInfo.value?.displayName || '魔方管理平台');
    function toggleDark() {
        darkMode.value = !darkMode.value;
        document.documentElement.setAttribute('theme-mode', darkMode.value ? 'dark' : '');
    }
    return { collapsed, darkMode, siteInfo, siteTitle, toggleDark };
});
