import { createApp } from 'vue';
import { createPinia } from 'pinia';
import ArcoVue from '@arco-design/web-vue';
import '@arco-design/web-vue/dist/arco.css';
import '@/theme/density.css';
import App from './App.vue';
import router from './router';
import { registerPageSectionsFromGlob } from '@/core/composables/useSections';
import { useUserProfileStore } from '@/stores/userProfile';

registerPageSectionsFromGlob(import.meta.glob('./apps/*/src/views/**/[A-Z]*.vue'));

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
app.use(router);
app.use(ArcoVue);

// 尽早应用本地偏好，避免首屏闪烁
useUserProfileStore(pinia).bootstrapLocal();

app.mount('#app');
