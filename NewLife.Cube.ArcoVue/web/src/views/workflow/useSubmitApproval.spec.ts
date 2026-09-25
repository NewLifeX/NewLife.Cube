import { describe, expect, it } from 'vitest';
import {
  buildSubmitSteps,
  buildXorHint,
  filesFromUploadChange,
  normalizeWfTypePath,
  picksReady,
  stepTag,
  wfDefinitionIdOf,
} from './useSubmitApproval';

describe('normalizeWfTypePath（提交审批 TypePath 对齐）', () => {
  it('去掉列表页前导 /，与定义 Admin/Department 对齐', () => {
    expect(normalizeWfTypePath('/Admin/Department')).toBe('Admin/Department');
    expect(normalizeWfTypePath('Admin/Department')).toBe('Admin/Department');
    expect(normalizeWfTypePath('///Admin/Department')).toBe('Admin/Department');
  });

  it('空/空白安全', () => {
    expect(normalizeWfTypePath('')).toBe('');
    expect(normalizeWfTypePath(null)).toBe('');
    expect(normalizeWfTypePath('  /Admin/User  ')).toBe('Admin/User');
  });
});

describe('wfDefinitionIdOf（雪花 Id 禁止 Number 丢精度）', () => {
  it('字符串原样保留，超 MAX_SAFE_INTEGER 不经 Number', () => {
    const sid = '7501276170837360640';
    expect(wfDefinitionIdOf(sid)).toBe(sid);
    // Number 会丢精度 → 不能走这条路径
    expect(String(Number(sid))).not.toBe(sid);
  });

  it('空值安全', () => {
    expect(wfDefinitionIdOf(null)).toBe('');
    expect(wfDefinitionIdOf(undefined)).toBe('');
    expect(wfDefinitionIdOf('')).toBe('');
  });
});

const chainJson = JSON.stringify({
  version: 1,
  nodes: [
    { id: 'start', type: 'oa.start', data: {} },
    {
      id: 'n1',
      type: 'oa.approve',
      data: { name: '提交人自选 · 或签', mode: 'or', to: { kind: 'starterPick', scope: 'all', multiple: false } },
    },
    { id: 'h1', type: 'oa.handle', data: { name: '办理·财务', mode: 'or', to: { kind: 'manager' } } },
    { id: 'end', type: 'oa.end', data: {} },
  ],
  edges: [
    { source: 'start', target: 'n1' },
    { source: 'n1', target: 'h1' },
    { source: 'h1', target: 'end' },
  ],
});

describe('filesFromUploadChange（发起附件）', () => {
  it('读取 change 的第一个参数（文件列表），忽略当前项', () => {
    const file = new File(['a'], '合同.pdf', { type: 'application/pdf' });
    const picked = filesFromUploadChange([{ uid: '1', file }], { uid: '1', file });
    expect(picked).toEqual([file]);
  });

  it('没有原始 File 时不上传', () => {
    expect(filesFromUploadChange([{ uid: '1', name: '合同.pdf' }])).toEqual([]);
    expect(filesFromUploadChange(undefined)).toEqual([]);
  });
});

describe('buildSubmitSteps / picksReady（提交人自选门禁）', () => {
  it('步骤预览跳过 start/end；starterPick 未选齐 canSubmit 前提 false', () => {
    const steps = buildSubmitSteps(chainJson);
    expect(steps.map((s) => s.id)).toEqual(['n1', 'h1']);
    expect(steps[0].kind).toBe('starterPick');
    expect(steps[0].multiple).toBe(false);
    expect(steps[1].kind).toBe('manager');

    expect(picksReady(steps, {})).toBe(false);
    expect(picksReady(steps, { n1: [7] })).toBe(true);
    // 单选节点必须恰好 1 人
    expect(picksReady(steps, { n1: [7, 8] })).toBe(false);
    expect(picksReady(steps, { n1: [] })).toBe(false);
  });

  it('空/非法图安全', () => {
    expect(buildSubmitSteps(null)).toEqual([]);
    // 非法 JSON 回落默认图（start→审批→end）→ 仅 1 个可展示步骤
    expect(buildSubmitSteps('not-json').map((s) => s.id)).toEqual(['n1']);
  });
});

describe('buildXorHint / stepTag（XOR 预告与行标签）', () => {
  const xorJson = JSON.stringify({
    version: 1,
    nodes: [
      { id: 'start', type: 'oa.start', data: {} },
      { id: 'x', type: 'oa.xor', data: { name: '金额分流', cases: [{ target: 'na' }], defaultTarget: 'nb' } },
      { id: 'na', type: 'oa.approve', data: { name: '财务' } },
      { id: 'nb', type: 'oa.approve', data: { name: '主管' } },
      { id: 'end', type: 'oa.end', data: {} },
    ],
    edges: [
      { source: 'start', target: 'x' },
      { source: 'x', target: 'na' },
      { source: 'x', target: 'nb' },
      { source: 'na', target: 'end' },
      { source: 'nb', target: 'end' },
    ],
  });

  it('预告行：条件按第一条记录判断，将走：财务，都不命中走：主管', () => {
    const steps = buildSubmitSteps(xorJson);
    expect(buildXorHint(steps)).toBe('条件按第一条记录判断，将走：财务，都不命中走：主管');
    expect(stepTag(steps.find((s) => s.id === 'x')!)).toBe('条件分流');
  });

  it('无 XOR 时预告为空；模式标签映射', () => {
    const steps = buildSubmitSteps(chainJson);
    expect(buildXorHint(steps)).toBe('');
    expect(stepTag(steps[0])).toBe('提交人自选');
    // 办理节点只显示或签/会签；依次仅审批节点
    expect(stepTag({ ...steps[1], mode: 'and' })).toBe('会签');
    expect(stepTag({ ...steps[0], type: 'oa.approve', kind: 'users', mode: 'sequence' })).toBe('依次');
  });
});
