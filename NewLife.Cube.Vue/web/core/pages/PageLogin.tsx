import { defineComponent, h, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { getConfig } from '../configure';

/**
 * 处理OAuth认证重定向的登录组件。
 *
 * 该组件会自动将用户重定向到配置的OAuth登录URL。
 * 它从路由查询参数中提取重定向URL（默认为'/'），
 * 将其与当前源结合，并将其作为参数传递给OAuth端点。
 *
 * 由于立即重定向到配置中定义的外部认证服务，该组件不渲染任何内容。
 *
 * @returns {JSX.Element} - 显示加载中的组件
 */
export default defineComponent({
  name: 'PageLogin',
  setup() {
    const route = useRoute();

    onMounted(() => {
      const config = getConfig();
      console.log('config', config);
      const oauthUrl = config.auth.oauthUrl;
      const baseUrl = config.request.baseUrl;
      if (!baseUrl) {
        throw new Error('config.request.baseUrl is not defined');
      }

      console.log('login', route);
      const redirect = `${location.origin}${route.query.redirect || '/'}`;
      location.href = `${baseUrl}${oauthUrl}${encodeURIComponent(redirect)}`;
    });

    return () => h('div', 'Loading...');
  },
});
