import { describe, expect, it } from 'vitest';
import { buildOpsParts, OPS_ACTION_LABELS, opsActionColor } from './opsAction';

describe('opsAction', () => {
  it('builds ops action list by flags in canonical order', () => {
    expect(buildOpsParts({ canViewDetail: true, canEdit: true })).toEqual(['detail', 'edit']);
    expect(buildOpsParts({ canViewDetail: true, canEdit: true, canDelete: true })).toEqual([
      'detail',
      'edit',
      'delete',
    ]);
    expect(buildOpsParts({})).toEqual([]);
    expect(buildOpsParts({ canDelete: true })).toEqual(['delete']);
  });

  it('appends up to 3 automation buttons after delete', () => {
    expect(buildOpsParts({ canViewDetail: true, automationButtons: [] })).toEqual(['detail']);
    const three = [
      { id: 1, name: 'A' },
      { id: 2, name: 'B' },
      { id: 3, name: 'C' },
    ];
    expect(buildOpsParts({ canDelete: true, automationButtons: three })).toEqual([
      'delete',
      'auto:1',
      'auto:2',
      'auto:3',
    ]);
    const four = [...three, { id: 4, name: 'D' }];
    expect(buildOpsParts({ automationButtons: four })).toEqual(['auto:1', 'auto:2', 'auto:3']);
  });

  it('maps every action to a display label', () => {
    expect(OPS_ACTION_LABELS.detail).toBe('详情');
    expect(OPS_ACTION_LABELS.edit).toBe('编辑');
    expect(OPS_ACTION_LABELS.delete).toBe('删除');
  });

  it('assigns link colors by action kind', () => {
    // 详情/编辑 = 主题主色
    expect(opsActionColor('detail').token).toBe('--primary-6');
    expect(opsActionColor('edit').token).toBe('--primary-6');
    // 删除 = 警示色
    expect(opsActionColor('delete').token).toBe('--danger-6');
    expect(opsActionColor('delete').hoverToken).toBe('--danger-5');
    // 未登记的系统自定义操作 = 链接色
    expect(opsActionColor('customAction').token).toBe('--link-6');
    expect(opsActionColor('customAction').hoverToken).toBe('--link-5');
  });
});
