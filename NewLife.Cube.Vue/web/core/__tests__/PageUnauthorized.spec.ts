/**
 * PageUnauthorized（未授权页）组件测试
 *
 * 验证：
 *   1. 渲染本地资源 —— 无任何带 src 的在线图片（防 CDN 图标回归），本地图标徽章 + 401 状态码；
 *   2. 标题 / 描述 / 按钮文案走 i18n（zh-CN 词表）；
 *   3. 点击按钮跳转配置的登录页 —— 默认 /login，自定义 reLoginParams.loginPageUrl 跟随配置。
 *
 * 运行：pnpm test:unit core/__tests__/PageUnauthorized.spec.ts
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import { createMemoryHistory, createRouter, type Router } from 'vue-router';
import PageUnauthorized from '../pages/PageUnauthorized.vue';
import { testI18n } from './helpers/i18n';

// configure 桩：可变 reLoginParams 驱动「默认登录页 / 自定义登录页」两分支
const h = vi.hoisted(() => ({
  config: {
    auth: {
      reLoginParams: { loginPageUrl: '/login' } as { loginPageUrl?: string } | undefined,
    },
  },
}));

vi.mock('../configure', () => ({ getConfig: () => h.config }));

async function mountPage(): Promise<{ wrapper: ReturnType<typeof mount>; router: Router }> {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/unauthorized', name: 'unauthorized', component: PageUnauthorized },
      { path: '/login', name: 'login', component: { template: '<div class="login-page" />' } },
      { path: '/sso-login', name: 'sso-login', component: { template: '<div />' } },
    ],
  });
  await router.push('/unauthorized?redirect=%2Fwork-orders%2FWO1');
  await router.isReady();
  const wrapper = mount(PageUnauthorized, { global: { plugins: [ElementPlus, testI18n, router] } });
  await flushPromises();
  return { wrapper, router };
}

describe('PageUnauthorized — 未授权页', () => {
  beforeEach(() => {
    h.config.auth.reLoginParams = { loginPageUrl: '/login' };
  });

  it('应该只渲染本地资源与本地图标 当页面挂载', async () => {
    const { wrapper } = await mountPage();

    // 回归守卫：原实现引用 jsdelivr 在线 404.png（内网/离线裂图），页面不得再含任何带 src 的资源
    expect(wrapper.find('img').exists()).toBe(false);
    expect(wrapper.findAll('[src]')).toHaveLength(0);

    // 本地图标徽章 + 401 状态码 + 登录按钮齐备
    expect(wrapper.find('[data-cy="page-unauthorized"]').exists()).toBe(true);
    expect(wrapper.find('.status-badge svg').exists()).toBe(true);
    expect(wrapper.find('.status-code').text()).toBe('401');
    expect(wrapper.find('[data-cy="unauthorized-login"]').exists()).toBe(true);

    wrapper.unmount();
  });

  it('应该用 i18n 词表渲染标题、描述与按钮文案 当使用 zh-CN', async () => {
    const { wrapper } = await mountPage();

    expect(wrapper.find('.title').text()).toBe('登录已失效');
    expect(wrapper.find('.desc').text()).toBe('抱歉，您未登录或身份认证已失效，请点击下方按钮重新登录！');
    expect(wrapper.find('[data-cy="unauthorized-login"]').text()).toContain('立即跳转');

    wrapper.unmount();
  });

  it('应该跳转默认登录页 /login 当点击重新登录按钮', async () => {
    const { wrapper, router } = await mountPage();

    await wrapper.find('[data-cy="unauthorized-login"]').trigger('click');
    await flushPromises();

    expect(router.currentRoute.value.path).toBe('/login');
    expect(router.currentRoute.value.query.redirect).toBe('/work-orders/WO1');

    wrapper.unmount();
  });

  it('应该跟随自定义 loginPageUrl 当配置了 reLoginParams', async () => {
    h.config.auth.reLoginParams = { loginPageUrl: '/sso-login' };
    const { wrapper, router } = await mountPage();

    await wrapper.find('[data-cy="unauthorized-login"]').trigger('click');
    await flushPromises();

    expect(router.currentRoute.value.path).toBe('/sso-login');

    wrapper.unmount();
  });
});
