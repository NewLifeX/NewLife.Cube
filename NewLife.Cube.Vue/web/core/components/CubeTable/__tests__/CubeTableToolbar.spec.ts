/**
 * `CubeTableToolbar`（合并工具栏）组件测试
 *
 * 钉住：操作/指标/视图控制在同一行；插槽转发；无内容不渲染。
 *
 * 运行：pnpm test:unit core/components/CubeTable/__tests__/CubeTableToolbar.spec.ts
 */
import './domShims';
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import CubeTableToolbar from '../CubeTableToolbar.vue';
import { makeFakeActions, makeFakeCtx, type FakeCtxOptions } from './fakeCtx';
import type { CubeToolbarFlags, CubeTableMetric } from '../types';
import { testI18n } from '../../../__tests__/helpers/i18n';

interface MountOptions extends FakeCtxOptions {
  flags?: CubeToolbarFlags;
  metrics?: CubeTableMetric[];
  showRefresh?: boolean;
  showColumnSetting?: boolean;
  slots?: Record<string, string>;
}

function mountToolbar(options: MountOptions = {}) {
  const ctx = makeFakeCtx(options);
  const actions = makeFakeActions();
  const wrapper = mount(CubeTableToolbar, {
    props: {
      ctx,
      actions,
      flags: options.flags ?? {},
      metrics: options.metrics ?? [],
      showRefresh: options.showRefresh ?? true,
      showColumnSetting: options.showColumnSetting ?? false,
    },
    slots: options.slots ?? {},
    global: { plugins: [ElementPlus, testI18n] },
  });
  return { wrapper, ctx, actions };
}

describe('CubeTableToolbar 结构', () => {
  it('应该把操作、指标、视图控制放在同一行', () => {
    const { wrapper } = mountToolbar({
      metrics: [{ label: '设备总数', value: 128 }],
    });

    const toolbar = wrapper.find('.ctt');
    expect(toolbar.exists()).toBe(true);
    expect(toolbar.find('.cta.cta--inline').exists()).toBe(true);
    expect(toolbar.find('.ctm-metrics').text()).toContain('设备总数');
    expect(toolbar.find('[data-role="advanced"]').exists()).toBe(true);
    expect(toolbar.find('.ctt-controls').exists()).toBe(true);
  });

  it('应该转发业务操作/指标/工具插槽', () => {
    const { wrapper } = mountToolbar({
      slots: {
        actions: '<button class="biz">业务</button>',
        metrics: '<span class="my-metric">自定义指标</span>',
        'toolbar-extra': '<button class="extra">图表</button>',
        'column-setting': '<button class="cols">设置列</button>',
      },
    });

    expect(wrapper.find('.cta-left .biz').exists()).toBe(true);
    expect(wrapper.find('.ctm-metrics .my-metric').exists()).toBe(true);
    expect(wrapper.find('.ctt-controls .extra').exists()).toBe(true);
    expect(wrapper.find('.ctt-controls .cols').exists()).toBe(true);
  });

  it('应该隐藏高级菜单 当页面显式关闭时', () => {
    const { wrapper } = mountToolbar({ flags: { advanced: false } });
    expect(wrapper.find('[data-role="advanced"]').exists()).toBe(false);
  });

  it('应该整行不渲染 当没有任何操作、指标与控制时', () => {
    const { wrapper } = mountToolbar({
      flags: { add: false, advanced: false },
      showRefresh: false,
      showColumnSetting: false,
    });
    expect(wrapper.find('.ctt').exists()).toBe(false);
  });
});
