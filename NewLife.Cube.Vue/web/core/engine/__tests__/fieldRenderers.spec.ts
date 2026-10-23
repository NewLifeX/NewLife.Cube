/**
 * 字段级渲染器注册表单元测试
 *
 * 关键行为：
 * - 同名后注册覆盖先注册；
 * - 未注册名称只告警一次（避免同一页面反复刷控制台）；
 * - 清空后回到未注册状态。
 */
import { afterEach, describe, expect, it, vi } from 'vitest';
import { defineComponent, h } from 'vue';
import {
  clearFieldRenderers,
  getFieldRenderer,
  registerFieldRenderer,
  resolveFieldRenderer,
} from '../fieldRenderers';

const Dummy = defineComponent({ render: () => h('span') });

afterEach(() => {
  clearFieldRenderers();
  vi.restoreAllMocks();
});

describe('fieldRenderers 注册表', () => {
  it('应该按名称返回已注册的渲染器', () => {
    registerFieldRenderer('orgPicker', { component: Dummy, props: { group: ['FirstId', 'LastId'] } });
    const hit = getFieldRenderer('orgPicker');
    expect(hit?.component).toBe(Dummy);
    expect(hit?.props).toEqual({ group: ['FirstId', 'LastId'] });
  });

  it('应该让后注册的同名渲染器覆盖先注册的', () => {
    const Second = defineComponent({ render: () => h('span') });
    registerFieldRenderer('same', { component: Dummy });
    registerFieldRenderer('same', { component: Second });
    expect(getFieldRenderer('same')?.component).toBe(Second);
  });

  it('应该只在首次未命中时告警', () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {});
    expect(getFieldRenderer('missing')).toBeUndefined();
    expect(getFieldRenderer('missing')).toBeUndefined();
    expect(warn).toHaveBeenCalledTimes(1);
  });

  it('应该按字段 view 解析渲染器', () => {
    registerFieldRenderer('cellTag', { component: Dummy });
    expect(resolveFieldRenderer({ view: 'cellTag' })?.component).toBe(Dummy);
    expect(resolveFieldRenderer({})).toBeUndefined();
    expect(resolveFieldRenderer(null)).toBeUndefined();
  });

  it('清空后应回到未注册状态且不残留告警记忆', () => {
    registerFieldRenderer('tmp', { component: Dummy });
    clearFieldRenderers();
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {});
    expect(getFieldRenderer('tmp')).toBeUndefined();
    expect(warn).toHaveBeenCalledTimes(1);
  });
});
