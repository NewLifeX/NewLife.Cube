import { describe, expect, it } from 'vitest';
import {
  compileAutomationGraph,
  normalizeTriggerConfig,
  parseAutomationGraph,
  validateFoundTargetChain,
} from './automationGraph';

describe('compileAutomationGraph', () => {
  it('compiles start→end when filter empty and no actions', () => {
    const g = compileAutomationGraph({ triggerKind: 'Insert', filter: { logic: 'all', conditions: [] } });
    expect(g.version).toBe(1);
    expect(g.nodes.map((n) => n.type)).toEqual(['start', 'end']);
    expect(g.edges).toEqual([{ id: 'e0', source: 'n0', target: 'n1' }]);
    expect(g.nodes[0].data?.triggerKind).toBe('insert');
  });

  it('inserts filter node when conditions exist', () => {
    const g = compileAutomationGraph({
      triggerKind: 'update',
      filter: { logic: 'all', conditions: [{ field: 'Name', op: 'eq', value: 'a' }] },
      actions: [{ type: 'notify', data: { channel: 'InApp' } }],
    });
    expect(g.nodes.map((n) => n.type)).toEqual(['start', 'filter', 'notify', 'end']);
    expect(g.edges).toHaveLength(3);
  });

  it('drops illegal action types and keeps legal ones', () => {
    const g = compileAutomationGraph({
      triggerKind: 'insert',
      actions: [
        { type: 'approval' },
        { type: 'notify', data: {} },
        { type: 'loop' },
      ],
    });
    expect(g.nodes.map((n) => n.type)).toEqual(['start', 'notify', 'end']);
  });

  it('caps actions at 20', () => {
    const actions = Array.from({ length: 22 }, () => ({ type: 'notify', data: {} }));
    const g = compileAutomationGraph({ triggerKind: 'insert', actions });
    expect(g.nodes.filter((n) => n.type === 'notify')).toHaveLength(20);
  });
});

describe('parseAutomationGraph', () => {
  it('round-trips a linear graph', () => {
    const src = compileAutomationGraph({
      triggerKind: 'button',
      filter: { logic: 'any', conditions: [{ field: 'Age', op: 'gt', value: 1 }] },
      actions: [{ type: 'delay', data: { minutes: 5 } }],
    });
    const p = parseAutomationGraph(src);
    expect(p.error).toBeUndefined();
    expect(p.triggerKind).toBe('button');
    expect(p.filter.conditions).toHaveLength(1);
    expect(p.actions).toEqual([{ type: 'delay', data: { minutes: 5 } }]);
  });

  it('flags fork graphs as rebuild-only', () => {
    const p = parseAutomationGraph({
      version: 1,
      nodes: [
        { id: 'n0', type: 'start', data: {} },
        { id: 'n1', type: 'notify', data: {} },
        { id: 'n2', type: 'end', data: {} },
      ],
      edges: [
        { id: 'e0', source: 'n0', target: 'n1' },
        { id: 'e1', source: 'n0', target: 'n2' },
      ],
    });
    expect(p.error).toBe('请用表单重建');
  });

  it('surfaces unsupported version', () => {
    const g = compileAutomationGraph({ triggerKind: 'insert' });
    const p = parseAutomationGraph({ ...g, version: 2 });
    expect(p.error).toBe('不支持的图版本');
  });
});

describe('normalizeTriggerConfig', () => {
  it('clamps dateArrive offsetMinutes', () => {
    const o = normalizeTriggerConfig('dateArrive', { offsetMinutes: 99999 });
    expect(o.offsetMinutes).toBe(10080);
    const n = normalizeTriggerConfig('dateArrive', { offsetMinutes: -20000 });
    expect(n.offsetMinutes).toBe(-10080);
  });

  it('truncates button label to 12 chars and defaults requirePermission', () => {
    const o = normalizeTriggerConfig('button', { label: '这是一个非常长的按钮文案', requirePermission: 'xxx' });
    expect(String(o.label).length).toBe(12);
    expect(o.requirePermission).toBe('detail');
  });

  it('cleans fieldChange empty watchFields', () => {
    const o = normalizeTriggerConfig('fieldChange', { watchFields: ['', ' Name ', null] });
    expect(o.watchFields).toEqual(['Name']);
  });

  it('caps delay minutes at 10080 on compile', () => {
    const g = compileAutomationGraph({
      triggerKind: 'insert',
      actions: [{ type: 'delay', data: { minutes: 99999 } }],
    });
    expect(g.nodes.find((n) => n.type === 'delay')?.data?.minutes).toBe(10080);
  });

  it('validateFoundTargetChain requires findRecords before found target', () => {
    expect(
      validateFoundTargetChain([{ type: 'addComment', data: { target: 'found' } }], {
        addComment: '添加评论',
      }),
    ).toHaveLength(1);
    expect(
      validateFoundTargetChain([
        { type: 'findRecords', data: {} },
        { type: 'addComment', data: { target: 'found' } },
      ]),
    ).toHaveLength(0);
  });
});
