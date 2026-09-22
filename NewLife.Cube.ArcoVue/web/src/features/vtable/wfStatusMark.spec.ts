import { describe, expect, it } from 'vitest';
import { wfMarkKind } from './wfStatusMark';

describe('wfMarkKind 审批徽标几何图标', () => {
  it('五态映射到对称几何，不依赖 Unicode 字框', () => {
    expect(wfMarkKind('none')).toBe('ring');
    expect(wfMarkKind('running')).toBe('ringDot');
    expect(wfMarkKind('approved')).toBe('check');
    expect(wfMarkKind('rejected')).toBe('cross');
    expect(wfMarkKind('withdrawn')).toBe('undo');
    expect(wfMarkKind('cancelled')).toBe('ring');
  });
});
