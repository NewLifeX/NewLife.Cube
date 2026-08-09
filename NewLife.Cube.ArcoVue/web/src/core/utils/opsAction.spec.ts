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
