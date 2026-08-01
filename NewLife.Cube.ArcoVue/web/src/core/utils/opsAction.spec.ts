import { describe, expect, it } from 'vitest';
import {
  buildOpsParts,
  formatOpsLabel,
  resolveOpsActionByRatio,
} from './opsAction';

describe('opsAction', () => {
  it('builds and formats ops label by flags', () => {
    expect(formatOpsLabel(buildOpsParts({ canViewDetail: true, canEdit: true }))).toBe(
      '详情 · 编辑',
    );
    expect(formatOpsLabel(buildOpsParts({}))).toBe('-');
  });

  it('resolves single action without ratio', () => {
    expect(resolveOpsActionByRatio(0.9, { canEdit: true })).toBe('edit');
  });

  it('splits three actions by horizontal thirds', () => {
    const flags = { canViewDetail: true, canEdit: true, canDelete: true };
    expect(resolveOpsActionByRatio(0.1, flags)).toBe('detail');
    expect(resolveOpsActionByRatio(0.5, flags)).toBe('edit');
    expect(resolveOpsActionByRatio(0.9, flags)).toBe('delete');
    expect(resolveOpsActionByRatio(1, flags)).toBe('delete');
  });
});
