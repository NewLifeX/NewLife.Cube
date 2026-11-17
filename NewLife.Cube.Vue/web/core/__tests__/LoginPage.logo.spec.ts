/**
 * 登录页 Logo 兜底回归
 *
 * 历史问题：登录页 logoSrc 只读 /Cube/Site 的 logo，服务端未配置时永远渲染内置默认魔方 SVG，
 * 前端 configs/config.ts 的 base.logo 对登录页是死配置（导航栏 LogoBrand 却有兜底，口径不一致）。
 * 现口径与 LogoBrand 一致：siteStore.logo || config.base.logo，两者皆空才用内置 SVG。
 */
import { flushPromises, mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import ElementPlus from 'element-plus';
import LoginPage from '../pages/LoginPage.vue';
import i18n from '../i18n';

const h = vi.hoisted(() => ({
  /** 前端 config.base.logo（可按用例改写） */
  configLogo: '/logo.png' as string | undefined,
  /** 模拟 /Cube/Site 返回的站点配置 */
  siteConfig: null as Record<string, unknown> | null,
}));

vi.mock('../configure', () => ({
  getConfig: () => ({ base: { logo: h.configLogo }, auth: {} }),
}));

vi.mock('../stores/site', () => ({
  useSiteStore: () => ({
    get config() { return h.siteConfig; },
    load: vi.fn(async () => h.siteConfig),
  }),
}));

vi.mock('../utils/loginApi', () => ({
  fetchLoginConfig: vi.fn(async () => ({ code: 0, data: { login: { password: true } } })),
  loginByPassword: vi.fn(),
  registerByForm: vi.fn(),
  sendCode: vi.fn(),
  sendActivateCode: vi.fn(),
}));

vi.mock('vue-router', () => ({
  useRoute: () => ({ query: {} }),
  useRouter: () => ({ push: vi.fn(), replace: vi.fn() }),
}));

async function mountPage() {
  const wrapper = mount(LoginPage, {
    global: { plugins: [ElementPlus, i18n] },
  });
  await flushPromises();
  return wrapper;
}

describe('LoginPage Logo 兜底', () => {
  beforeEach(() => {
    h.configLogo = '/logo.png';
    h.siteConfig = { name: '测试站点', logo: '' };
    // jsdom 无 matchMedia；matches=true 命中 prefers-reduced-motion，跳过粒子系统（避免 canvas 依赖）
    window.matchMedia = vi.fn().mockImplementation((query: string) => ({
      matches: true,
      media: query,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    })) as unknown as typeof window.matchMedia;
  });

  it('站点 logo 为空时兜底前端 config.base.logo，不渲染默认 SVG', async () => {
    const wrapper = await mountPage();
    const img = wrapper.find('img.brand-logo');
    expect(img.exists()).toBe(true);
    expect(img.attributes('src')).toBe('/logo.png');
    expect(wrapper.find('.brand-logo--default').exists()).toBe(false);
  });

  it('站点 logo 非空时优先于 config.base.logo', async () => {
    h.siteConfig = { name: '测试站点', logo: '/tenant-logo.png' };
    const wrapper = await mountPage();
    expect(wrapper.find('img.brand-logo').attributes('src')).toBe('/tenant-logo.png');
  });

  it('站点与 config 的 logo 均为空时渲染内置默认 SVG', async () => {
    h.configLogo = '';
    const wrapper = await mountPage();
    expect(wrapper.find('img.brand-logo').exists()).toBe(false);
    expect(wrapper.find('.brand-logo--default').exists()).toBe(true);
  });

  it('绝对 URL 原样透传', async () => {
    h.siteConfig = { name: '测试站点', logo: 'https://cdn.example.com/logo.png' };
    const wrapper = await mountPage();
    expect(wrapper.find('img.brand-logo').attributes('src')).toBe('https://cdn.example.com/logo.png');
  });
});