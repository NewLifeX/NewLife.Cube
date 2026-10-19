/**
 * PageHome（Vue 工作台）组件测试
 *
 * 用 mock 的 API 驱动真实 useWorkbench，验证：
 *   1. KPI/内容卡/快捷入口/个人信息/系统信息渲染的是后端真实数据；
 *   2. KPI 点击按后端 url 跳转；
 *   3. 加载失败展示错误态并可重试；
 *   4. 无数据展示空状态。
 *
 * 运行：pnpm test:unit core/__tests__/PageHome.spec.ts
 */
import '../components/CubeTable/__tests__/domShims';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import { createMemoryHistory, createRouter } from 'vue-router';
import PageHome from '../pages/PageHome.vue';

const h = vi.hoisted(() => ({
  getAction: vi.fn(),
}));

vi.mock('../composables/useCubeApi', () => ({
  usePageApi: () => ({ getAction: h.getAction }),
}));

const dashboardData = {
  user: { name: 'admin', displayName: '管理员', roles: ['Administrators'] },
  kpis: [
    { name: 'UserCount', label: '用户总数', value: '12', trend: '注册用户', color: 'blue', url: '/Admin/User' },
    { name: 'OnlineCount', label: '在线用户', value: '3', trend: '当前在线', color: 'cyan', url: '' },
  ],
  widgets: [
    {
      name: 'Profile',
      title: '个人信息',
      icon: 'fa-user-circle',
      cols: 4,
      category: '个人',
      widgetType: 'Content',
      data: {
        name: 'admin',
        displayName: '管理员',
        roleNames: 'Administrators,运营',
        online: true,
        logins: 12,
        lastLogin: '2026-10-20 06:00:00',
        lastLoginIP: '127.0.0.1',
        registerTime: '2025-01-01 08:00:00',
      },
    },
    {
      name: 'QuickLink',
      title: '快捷入口',
      icon: 'fa-th-large',
      cols: 4,
      category: '通用',
      widgetType: 'Content',
      data: { links: [{ name: '用户管理', url: '/Admin/User', icon: 'fa-users' }] },
    },
    {
      name: 'Monitor',
      title: '性能监控',
      icon: 'fa-line-chart',
      cols: 8,
      category: '系统',
      widgetType: 'Chart',
      data: null,
    },
    {
      name: 'SysInfo',
      title: '系统信息',
      icon: 'fa-server',
      cols: 4,
      category: '系统',
      widgetType: 'Kv',
      data: {
        操作系统: 'Windows 11',
        更多信息: '<a href="/Admin/Index/Main">查看完整服务器信息 »</a>',
      },
    },
    {
      name: 'LoginLog',
      title: '登录与在线',
      icon: 'fa-users',
      cols: 4,
      category: '系统',
      widgetType: 'Content',
      data: {
        logins: [{ createTime: '2026-10-20 05:00:00', userName: 'admin', action: '登录', createIP: '127.0.0.1' }],
        onlines: [{ name: 'admin', createTime: '2026-10-20 05:00:00', oAuthProvider: '本地' }],
      },
    },
  ],
  quickLinks: [{ name: '用户管理', url: '/Admin/User' }],
  profile: { displayName: '管理员', roleNames: 'Administrators', logins: 12 },
  sysInfo: { 操作系统: 'Windows 11' },
};

async function createTestRouter() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', name: 'home', component: { template: '<div />' } },
      { path: '/Admin/User', name: 'user', component: { template: '<div />' } },
      { path: '/Admin/Index/Main', name: 'main', component: { template: '<div />' } },
    ],
  });
  await router.push('/');
  await router.isReady();
  return router;
}

async function mountHome() {
  const router = await createTestRouter();
  const wrapper = mount(PageHome, { global: { plugins: [ElementPlus, router] } });
  await flushPromises();
  return { wrapper, router };
}

describe('PageHome — 工作台真实数据渲染', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    h.getAction.mockImplementation((action: string) => {
      if (action === 'Dashboard') return Promise.resolve({ code: 0, message: '', data: dashboardData });
      return Promise.resolve({
        code: 0,
        message: '',
        data: { xs: ['10:00:00'], series: [[10], [20]], memUsed: 512, memTotal: 1024 },
      });
    });
  });

  it('渲染欢迎横幅、KPI 与全部内容卡 当 Dashboard 返回聚合数据', async () => {
    const { wrapper } = await mountHome();

    expect(wrapper.find('[data-cy="workbench-banner"]').text()).toContain('管理员');
    expect(wrapper.find('[data-cy="workbench-kpi-UserCount"]').text()).toContain('12');
    expect(wrapper.find('[data-cy="workbench-kpi-UserCount"]').text()).toContain('用户总数');
    expect(wrapper.findAll('[data-cy="workbench-widget"]')).toHaveLength(5);
    expect(wrapper.find('[data-cy="workbench-monitor"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('用户管理');
    expect(wrapper.text()).toContain('上次登录');
    expect(wrapper.text()).toContain('2026-10-20 06:00:00');
    expect(wrapper.text()).toContain('Windows 11');
    expect(wrapper.find('a[href="/Admin/Index/Main"]').text()).toBe('查看完整服务器信息 »');

    wrapper.unmount();
  });

  it('按 KPI 的 url 跳转 当点击指标卡', async () => {
    const { wrapper, router } = await mountHome();

    await wrapper.find('[data-cy="workbench-kpi-UserCount"]').trigger('click');
    await flushPromises();

    expect(router.currentRoute.value.path).toBe('/Admin/User');

    wrapper.unmount();
  });

  it('展示错误态并可重试 当 Dashboard 返回业务错误', async () => {
    h.getAction.mockResolvedValue({ code: 500, message: '数据库不可用', data: null });
    const { wrapper } = await mountHome();

    expect(wrapper.text()).toContain('工作台加载失败');
    expect(wrapper.text()).toContain('数据库不可用');

    h.getAction.mockResolvedValue({ code: 0, message: '', data: dashboardData });
    await wrapper.find('button').trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-cy="workbench-banner"]').exists()).toBe(true);

    wrapper.unmount();
  });

  it('展示空状态 当后端返回空工作台', async () => {
    h.getAction.mockResolvedValue({ code: 0, message: '', data: { kpis: [], widgets: [] } });
    const { wrapper } = await mountHome();

    expect(wrapper.text()).toContain('暂无工作台数据');

    wrapper.unmount();
  });
});
