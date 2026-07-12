import { defineComponent, h } from 'vue';
import LoginPage from './LoginPage.vue';

/**
 * 登录页包装器（薄包装器模式）
 *
 * 原先此组件包含 OAuth 跳转逻辑，现已迁移至 LoginPage.vue。
 * 此组件仅作为向后兼容的包装器，保持 core/routes/index.ts 的导入路径不变。
 *
 * @returns 渲染 LoginPage.vue 的组件
 */
export default defineComponent({
  name: 'PageLogin',
  setup() {
    return () => h(LoginPage);
  },
});
