import { createApp } from 'vue';
import pinia from '/@/stores/index';
import App from './App.vue';
import router from './router';
import { directive } from '/@/directive/index';
import { i18n } from '/@/i18n/index';
import other from '/@/utils/other';

import ElementPlus from 'element-plus';
import 'element-plus/dist/index.css';
import '/@/theme/index.scss';
import '/@/theme/tailwind.scss';
import VueGridLayout from 'vue-grid-layout';
import { Icon } from '@iconify/vue';
import formCreate from '@form-create/element-ui';
import install from '@form-create/element-ui/auto-import';
import page from '/@/components/page/index.vue';

formCreate.use(install);
const app = createApp(App);

directive(app);
other.elSvg(app);
app.component('Icon', Icon);
app.component('Page', page);
app.use(pinia).use(router).use(ElementPlus, { i18n: i18n.global.t }).use(i18n).use(VueGridLayout).use(formCreate).mount('#app');
