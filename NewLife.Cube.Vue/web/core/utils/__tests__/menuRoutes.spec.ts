/**
 * 菜单路由视图解析测试（M3 自定义页接管路径）
 *
 * 只验证「有自定义视图 → 用视图；没有 → 回退 AutoPage」这条判定，
 * 不启动路由器，避免把菜单接口与登录态拖进单测。
 *
 * 运行：pnpm test:unit core/utils/__tests__/menuRoutes.spec.ts
 */
import { describe, expect, it } from 'vitest';
import { hasMatchingCustomView, resolvePageComponent } from '../menuRoutes';
import AutoPage from '../../views/AutoPage.vue';

const loadDefault = async (loader: () => Promise<{ default: unknown }>) => (await loader()).default;

describe('menuRoutes 自定义视图解析', () => {
  it('应该解析到业务自定义页 当存在 admin/parameter/index.vue 时', async () => {
    const loader = resolvePageComponent('/Admin/Parameter');

    expect(typeof loader).toBe('function');
    const component = await loadDefault(loader);
    expect(component).toBeTruthy();
    expect(component).not.toBe(AutoPage);
  });

  it('应该支持带应用名的三段路径 当应用名参与视图查找时', async () => {
    const component = await loadDefault(resolvePageComponent('/cube-admin/admin/parameter'));

    expect(component).toBeTruthy();
    expect(component).not.toBe(AutoPage);
  });

  it('应该回退 AutoPage 当没有匹配的自定义视图时', async () => {
    const component = await loadDefault(resolvePageComponent('/Admin/NotExists'));

    expect(component).toBe(AutoPage);
  });

  it('应该缓存同一路径的加载器 当重复解析时', () => {
    expect(resolvePageComponent('/Admin/Parameter')).toBe(resolvePageComponent('/Admin/Parameter'));
  });

  it('应该准确判断自定义视图是否存在 供隐藏页入口使用', () => {
    expect(hasMatchingCustomView('/Admin/Parameter')).toBe(true);
    expect(hasMatchingCustomView('/Admin/NotExists')).toBe(false);
    expect(hasMatchingCustomView('')).toBe(false);
  });
});
