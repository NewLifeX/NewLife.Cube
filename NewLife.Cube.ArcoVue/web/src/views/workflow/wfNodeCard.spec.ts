import { describe, expect, it } from 'vitest';
import type { WfGraphNodeData } from '@/core/types/workflow';
import { branchInfosOf, insertNodeTitle, nodeAssigneeLine, nodeCardInfo, recipientSummary } from './wfNodeCard';

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
  it('六种选人：部门负责人/提交人自选/表单人员', () => {
    expect(recipientSummary({ kind: 'manager' })).toBe('部门负责人');
    expect(recipientSummary({ kind: 'starterPick' })).toBe('提交人自选');
    expect(recipientSummary({ kind: 'field', field: 'OwnerId' })).toBe('OwnerId');
    expect(recipientSummary({ kind: 'field', field: '' })).toBe('未指定字段');
    // 旧图无 kind：users 非空推导
    expect(recipientSummary({ users: [1] })).toBe('指定人员');
  });
});

describe('nodeAssigneeLine', () => {
  it('按人员 / 角色 / 部门 / 管理人员显示', () => {
    expect(nodeAssigneeLine({ to: { kind: 'users', users: [1, 2] }, toLabels: ['张三', '李四'] })).toBe('人员：张三、李四');
    expect(nodeAssigneeLine({ to: { kind: 'roles', roles: [1] }, toLabels: ['管理员'] })).toBe('角色：管理员');
    expect(nodeAssigneeLine({ to: { kind: 'departments', departments: [3] }, toLabels: ['研发部'] })).toBe('部门：研发部');
    expect(nodeAssigneeLine({ to: { kind: 'manager' } })).toBe('管理人员：部门负责人');
    expect(nodeAssigneeLine({ to: { kind: 'users', users: [] } })).toBe('人员：未指定');
  });
});

describe('insertNodeTitle', () => {
  it('人话格式：审批人 · 签核方式；知会/办理前缀', () => {
    expect(insertNodeTitle('oa.approve', 'or', 'roles', ['管理员'])).toBe('管理员 · 或签');
    expect(insertNodeTitle('oa.approve', 'and', 'users', ['张三', '李四'])).toBe('张三/李四 · 会签');
    expect(insertNodeTitle('oa.approve', 'sequence', 'users', ['张三', '李四', '王五'])).toBe(
      '张三、李四等3人 · 依次',
    );
    expect(insertNodeTitle('oa.approve', 'or', 'manager', [])).toBe('部门负责人 · 或签');
    expect(insertNodeTitle('oa.cc', 'or', 'departments', ['研发部'])).toBe('知会·研发部');
    expect(insertNodeTitle('oa.handle', 'or', 'manager', [])).toBe('办理·部门负责人');
  });
});

describe('nodeCardInfo', () => {
  it('审批卡含或签+角色徽章与「谁 · 方式」', () => {
    const n: WfGraphNodeData = {
      id: 'n1',
      type: 'oa.approve',
      data: {
        name: '管理员 · 或签',
        mode: 'or',
        to: { kind: 'roles', roles: [1] },
        toLabels: ['管理员'],
      },
    };
    const info = nodeCardInfo(n);
    expect(info.title).toBe('管理员 · 或签');
    expect(info.badges).toEqual(expect.arrayContaining(['或签', '角色']));
    expect(info.subtitle).toBe('管理员 · 或签');
    expect(info.warning).toBe(false);
  });
  it('六种选人卡片：部门负责人不告警', () => {
    const info = nodeCardInfo({
      id: 'n2',
      type: 'oa.approve',
      data: { name: 'x', mode: 'sequence', to: { kind: 'manager' } },
    });
    expect(info.badges).toEqual(expect.arrayContaining(['依次', '部门负责人']));
    expect(info.subtitle).toBe('部门负责人 · 依次');
    expect(info.warning).toBe(false);
  });
  it('办理卡无签核方式徽章，子题为接收人', () => {
    const info = nodeCardInfo({
      id: 'h1',
      type: 'oa.handle',
      data: { name: '办理·财务', mode: 'or', to: { kind: 'users', users: [5] }, toLabels: ['财务'] },
    });
    expect(info.badges).toEqual(expect.arrayContaining(['指定成员']));
    expect(info.badges).not.toContain('或签');
    expect(info.subtitle).toBe('财务');
  });
  it('知会卡含类别与名称', () => {
    const info = nodeCardInfo({
      id: 'c1',
      type: 'oa.cc',
      data: { name: '知会·张三', to: { kind: 'users', users: [2] }, toLabels: ['张三'] },
    });
    expect(info.badges).toEqual(expect.arrayContaining(['知会', '指定成员']));
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
  it('分流缺「其他情况」红色提示', () => {
    const info = nodeCardInfo({ id: 'x', type: 'oa.xor', data: { cases: [{}], defaultTarget: '' } });
    expect(info.warning).toBe(true);
    expect(info.subtitle).toContain('缺「其他情况」');
    expect(info.branchLine).toContain('缺其他情况');
  });
  it('并行分支卡片写出条数和其他情况', () => {
    const info = nodeCardInfo({
      id: 'p',
      type: 'oa.parallel',
      data: { cases: [{ target: 'a' }, { target: 'b' }], defaultTarget: 'end' },
    });
    expect(info.branchLine).toBe('3 条分支 · 其他情况已接上');
  });
});

describe('branchInfosOf 分支卡片摘要', () => {
  it('条件摘要与字段中文名；空条件显示未设置条件；其他情况显示目标名', () => {
    const graph = {
      version: 1,
      nodes: [
        { id: 'x', type: 'oa.xor', data: { cases: [{ filter: { logic: 'all', conditions: [{ field: 'Type', op: 'eq', value: '2' }] }, target: '' }, {}], defaultTarget: 'n2' } },
        { id: 'n2', type: 'oa.approve', data: { name: '部门经理' } },
      ],
      edges: [],
    };
    const list = branchInfosOf(graph, (name) => (name === 'Type' ? '类型' : name))!['x'];
    expect(list).toHaveLength(3);
    expect(list[0]).toEqual({ title: '条件1', subtitle: '类型 等于 2', warning: true });
    expect(list[1]).toEqual({ title: '条件2', subtitle: '未设置条件', warning: true });
    expect(list[2]).toEqual({ title: '其他情况', subtitle: '都不命中 → 部门经理', warning: false });
  });

  it('条件值取友好名（valueLabel 回调：枚举 2 → 部门）', () => {
    const graph = {
      version: 1,
      nodes: [
        { id: 'x', type: 'oa.xor', data: { cases: [{ filter: { logic: 'all', conditions: [{ field: 'Type', op: 'eq', value: '2' }] }, target: 'n2' }], defaultTarget: 'n2' } },
        { id: 'n2', type: 'oa.approve', data: { name: '部门经理' } },
      ],
      edges: [],
    };
    const list = branchInfosOf(graph, (name) => (name === 'Type' ? '类型' : name), (field, v) =>
      field === 'Type' && v === '2' ? '部门' : null,
    )!['x'];
    expect(list[0]).toEqual({ title: '条件1', subtitle: '类型 等于 部门', warning: false });
  });

  it('自定义条件名优先于「条件N」', () => {
    const graph = {
      version: 1,
      nodes: [
        { id: 'x', type: 'oa.xor', data: { cases: [{ name: ' 大额 ', filter: {}, target: 'n2' }], defaultTarget: 'n2' } },
        { id: 'n2', type: 'oa.approve', data: { name: '部门经理' } },
      ],
      edges: [],
    };
    const list = branchInfosOf(graph)!['x'];
    expect(list[0].title).toBe('大额');
    expect(list[1].title).toBe('其他情况');
  });

  it('缺 defaultTarget：其他情况警告', () => {
    const graph = {
      version: 1,
      nodes: [{ id: 'x', type: 'oa.xor', data: { cases: [{}], defaultTarget: '' } }],
      edges: [],
    };
    const list = branchInfosOf(graph)!['x'];
    expect(list[1].warning).toBe(true);
    expect(list[1].subtitle).toBe('未指定默认节点');
  });
});
