import { describe, expect, it } from 'vitest';
import type { WfGraphNodeData } from '@/core/types/workflow';
import { insertNodeTitle, nodeCardInfo, recipientSummary } from './wfNodeCard';

describe('recipientSummary', () => {
  it('优先显示 toLabels 名称', () => {
    expect(recipientSummary({ kind: 'roles', roles: [1] }, ['管理员'])).toBe('管理员');
    expect(recipientSummary({ kind: 'users', users: [1, 2] }, ['张三', '李四'])).toBe('张三、李四');
  });
  it('无 labels 时回落类别粗摘要', () => {
    expect(recipientSummary({ kind: 'users', users: [1, 2, 3] })).toBe('3 人');
    expect(recipientSummary({ kind: 'roles', roles: [2] })).toBe('指定角色');
    expect(recipientSummary({ kind: 'users', users: [] })).toBe('未指定');
  });
});

describe('insertNodeTitle', () => {
  it('或签/会签/知会带人名', () => {
    expect(insertNodeTitle('oa.approve', 'or', 'roles', ['管理员'])).toBe('或签·管理员');
    expect(insertNodeTitle('oa.approve', 'and', 'users', ['张三', '李四'])).toBe('会签·张三/李四');
    expect(insertNodeTitle('oa.cc', 'or', 'departments', ['研发部'])).toBe('知会·研发部');
  });
});

describe('nodeCardInfo', () => {
  it('审批卡含或签+角色徽章与具体名称', () => {
    const n: WfGraphNodeData = {
      id: 'n1',
      type: 'oa.approve',
      data: {
        name: '或签·管理员',
        mode: 'or',
        to: { kind: 'roles', roles: [1] },
        toLabels: ['管理员'],
      },
    };
    const info = nodeCardInfo(n);
    expect(info.title).toBe('或签·管理员');
    expect(info.badges).toEqual(expect.arrayContaining(['或签', '角色']));
    expect(info.subtitle).toBe('管理员');
    expect(info.warning).toBe(false);
  });
  it('知会卡含类别与名称', () => {
    const info = nodeCardInfo({
      id: 'c1',
      type: 'oa.cc',
      data: { name: '知会·张三', to: { kind: 'users', users: [2] }, toLabels: ['张三'] },
    });
    expect(info.badges).toEqual(expect.arrayContaining(['知会', '用户']));
    expect(info.subtitle).toBe('张三');
  });
  it('审批未指定时警告', () => {
    const info = nodeCardInfo({
      id: 'n1',
      type: 'oa.approve',
      data: { name: '主管', mode: 'or', to: { kind: 'users', users: [] } },
    });
    expect(info.badges).toContain('或签');
    expect(info.warning).toBe(true);
  });
  it('分流缺默认分支红色提示', () => {
    const info = nodeCardInfo({ id: 'x', type: 'oa.xor', data: { cases: [{}], defaultTarget: '' } });
    expect(info.warning).toBe(true);
    expect(info.subtitle).toContain('缺「不满足」');
  });
});
