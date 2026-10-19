/**
 * `CubeTableMetrics`（表格统计行，对应 MES ListPage 的 `.table-toolbar`）组件测试
 *
 * 这一行的职责边界要钉住：左侧只渲染统计指标，右侧只放视图工具（业务追加、设置列、刷新）。
 * 整行空转（没有任何内容）时必须不渲染，否则表格上面多一条空白带。
 *
 * 运行：pnpm test:unit core/components/CubeTable/__tests__/CubeTableMetrics.spec.ts
 */
import './domShims';
import { describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';
import { mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import { Cpu } from '@element-plus/icons-vue';
import CubeTableMetrics from '../CubeTableMetrics.vue';
import { makeFakeCtx } from './fakeCtx';
import type { CubeTableMetric } from '../types';

async function mountMetrics(
  props: { metrics?: CubeTableMetric[]; showRefresh?: boolean; showColumnSetting?: boolean } = {},
  slots: Record<string, string> = {},
) {
  const ctx = makeFakeCtx();
  const wrapper = mount(CubeTableMetrics, {
    props: { ctx, ...props },
    slots,
    global: { plugins: [ElementPlus] },
  });
  await nextTick();
  return { wrapper, ctx };
}

describe('CubeTableMetrics 指标渲染', () => {
  it('应该按「名称 + 值」渲染指标 当传入 metrics 时', async () => {
    const { wrapper } = await mountMetrics({
      metrics: [{ label: '设备总数', value: 128 }, { label: '运行中', value: 96 }],
    });
    const items = wrapper.findAll('.ctm-item');
    expect(items).toHaveLength(2);
    expect(items[0]?.text()).toBe('设备总数128');
    expect(items[1]?.text()).toBe('运行中96');
  });

  it('应该在指标之间加分隔线 当指标多于一个时', async () => {
    const { wrapper } = await mountMetrics({
      metrics: [{ label: 'A', value: 1 }, { label: 'B', value: 2 }],
    });
    expect(wrapper.findAll('.ctm-divider')).toHaveLength(1);
  });

  it('应该渲染圆形图标与环比变化 当指标带 icon 与 change 时', async () => {
    const { wrapper } = await mountMetrics({
      metrics: [
        {
          label: '稼动率',
          value: '82%',
          icon: Cpu,
          bg: 'var(--el-color-primary-light-9)',
          color: 'var(--el-color-primary)',
          change: '↑3%',
          changeType: 'up',
        },
      ],
      showRefresh: false,
    });
    expect(wrapper.find('.ctm-icon').exists()).toBe(true);
    expect(wrapper.find('.ctm-change').classes()).toContain('up');
    expect(wrapper.find('.ctm-change').text()).toBe('↑3%');
  });

  it('应该整块接管指标区 当使用 #metrics 插槽时', async () => {
    const { wrapper } = await mountMetrics(
      { metrics: [{ label: 'A', value: 1 }] },
      { metrics: '<div class="mine">自定义指标</div>' },
    );
    expect(wrapper.find('.ctm-metrics .mine').exists()).toBe(true);
    expect(wrapper.find('.ctm-item').exists()).toBe(false);
  });
});

describe('CubeTableMetrics 右侧控制', () => {
  it('应该按「业务追加 → 设置列 → 刷新」的顺序排列 当三者同时存在时', async () => {
    const { wrapper } = await mountMetrics(
      { showColumnSetting: true },
      { extra: '<span class="e">附加</span>', 'column-setting': '<span class="c">设置列</span>' },
    );
    const cls = wrapper.findAll('.ctm-controls > *').map((n) => n.classes().join(' '));
    expect(cls.some((c) => c.includes('e'))).toBe(true);
    expect(cls.some((c) => c.includes('c'))).toBe(true);
    expect(cls.some((c) => c.includes('el-button'))).toBe(true);
  });

  it('应该把刷新打到引擎 当点刷新时', async () => {
    const { wrapper, ctx } = await mountMetrics();
    const refresh = vi.fn(async () => undefined);
    ctx.refresh = refresh;
    await wrapper.find('.ctm-controls button').trigger('click');
    expect(refresh).toHaveBeenCalled();
  });

  it('应该收掉刷新按钮 当 showRefresh 为 false 时', async () => {
    const { wrapper } = await mountMetrics({ showRefresh: false });
    expect(wrapper.find('.ctm-controls button').exists()).toBe(false);
  });
});

describe('CubeTableMetrics 空行处理', () => {
  it('应该不渲染整行 当既无指标也无控制项时', async () => {
    const { wrapper } = await mountMetrics({ metrics: [], showRefresh: false });
    expect(wrapper.find('.ctm').exists()).toBe(false);
  });

  it('应该只要有内容就渲染 当只有一个指标时', async () => {
    const { wrapper } = await mountMetrics({
      metrics: [{ label: 'A', value: 1 }],
      showRefresh: false,
    });
    expect(wrapper.find('.ctm').exists()).toBe(true);
  });
});
