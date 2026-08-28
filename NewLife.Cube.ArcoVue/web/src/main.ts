import { createApp, defineComponent, h } from 'vue';
import { createPinia } from 'pinia';
import ArcoVue from '@arco-design/web-vue';
import '@arco-design/web-vue/dist/arco.css';
import '@/theme/base.css';
import '@/theme/density.css';
import App from './App.vue';
import router from './router';
import { registerPageSectionsFromGlob } from '@/core/composables/useSections';
import { registerPlatformWidgets } from '@/features/widget';
import { ICON_COMPONENTS, FALLBACK_ICON } from '@/core/utils/iconComponents';
import { useUserProfileStore } from '@/stores/userProfile';

registerPageSectionsFromGlob(import.meta.glob('./apps/*/src/views/**/[A-Z]*.vue'));
registerPlatformWidgets();

/**
 * 统一图标体系（OSC-0017）：全局 `<icon-park :type="kebab-case名" />` 动态渲染。
 * 按需引入（iconComponents.ts 仅打包用到的图标），避免全量 install 膨胀 bundle。
 */
const IconPark = defineComponent({
  name: 'icon-park',
  props: {
    type: { type: String, required: true },
    size: { type: [Number, String], default: '1em' },
    theme: { type: String, default: 'outline' },
    fill: { type: [String, Array], default: 'currentColor' },
    spin: { type: Boolean, default: false },
    strokeWidth: { type: Number, default: 4 },
    strokeLinecap: { type: String, default: 'round' },
    strokeLinejoin: { type: String, default: 'round' },
  },
  setup(props, { attrs }) {
    return () => {
      const C = ICON_COMPONENTS[props.type] || FALLBACK_ICON;
      // 仅透传图标有效 props；type 只用于查表，不落到 SVG 根元素
      const { type: _type, ...iconProps } = props;
      return h(C, { ...attrs, ...iconProps });
    };
  },
});

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
app.use(router);
app.use(ArcoVue);
app.component('icon-park', IconPark);

// 尽早应用本地偏好，避免首屏闪烁
useUserProfileStore(pinia).bootstrapLocal();

app.mount('#app');

// mount 后 #cube-scale-root 已存在，再刷一次主题（清理历史 zoom、同步 CSS 变量）
useUserProfileStore(pinia).applyVisual();
