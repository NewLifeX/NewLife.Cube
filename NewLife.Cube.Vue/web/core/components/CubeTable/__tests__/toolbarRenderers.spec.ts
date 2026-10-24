/**
 * 工具栏自定义渲染器注册表测试
 *
 * 钉住三件事：按名注册可复用、未注册名字只告警一次、requiresSelection 与勾选数联动。
 *
 * 运行：pnpm test:unit core/components/CubeTable/__tests__/toolbarRenderers.spec.ts
 */
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  clearToolbarRenderers,
  getToolbarRenderer,
  registerToolbarRenderer,
  toolbarDisabled,
} from '../toolbarRenderers';
import type { BackendToolbarItem } from '../../../engine/types';

const component = { name: 'FakeRenderer', template: '<span />' };

afterEach(() => clearToolbarRenderers());

describe('toolbarRenderers', () => {
  it('应该按名字取回已注册的渲染器与透传属性', () => {
    registerToolbarRenderer('advanced', { component, props: { mode: 'full' } });

    expect(getToolbarRenderer('advanced')).toEqual({ component, props: { mode: 'full' } });
    expect(getToolbarRenderer('ADVANCED')).toBeUndefined();
  });

  it('应该忽略空名字 当注册名或查询名为空时', () => {
    registerToolbarRenderer('', { component });

    expect(getToolbarRenderer('')).toBeUndefined();
    expect(getToolbarRenderer(null)).toBeUndefined();
  });

  it('应该只告警一次 当连续取未注册的渲染器时', () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    try {
      expect(getToolbarRenderer('missing')).toBeUndefined();
      expect(getToolbarRenderer('missing')).toBeUndefined();
      expect(warn).toHaveBeenCalledTimes(1);
    } finally {
      warn.mockRestore();
    }
  });

  it('应该重新注册后清除已告警状态', () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    try {
      getToolbarRenderer('retry');
      registerToolbarRenderer('retry', { component });
      expect(getToolbarRenderer('retry')).toBeDefined();
      expect(warn).toHaveBeenCalledTimes(1);
    } finally {
      warn.mockRestore();
    }
  });
});

describe('toolbarDisabled', () => {
  const item: BackendToolbarItem = {
    name: 'batch',
    kind: 'request',
    requiresSelection: true,
  };

  it('应该在没有勾选时禁用需要选中数据的按钮', () => {
    expect(toolbarDisabled(item, 0)).toBe(true);
    expect(toolbarDisabled(item, 1)).toBe(false);
  });

  it('应该不限制普通按钮 当未声明 requiresSelection 时', () => {
    expect(toolbarDisabled({ name: 'plain', kind: 'link' }, 0)).toBe(false);
  });
});
