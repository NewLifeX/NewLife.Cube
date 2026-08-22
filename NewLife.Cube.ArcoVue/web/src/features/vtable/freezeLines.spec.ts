import { describe, expect, it } from 'vitest';
import { customFreezeSides, freezeLineHeight, freezeLineXs } from './freezeLines';
import type { ColumnPref } from '@/core/utils/viewProfile';

function col(key: string, frozen: ColumnPref['frozen'] = false): ColumnPref {
  return { key, visible: true, frozen };
}

describe('customFreezeSides', () => {
  it('hides both when no custom freeze', () => {
    expect(customFreezeSides([col('a'), col('b')])).toEqual({ left: false, right: false });
  });

  it('shows only the sides that were pinned', () => {
    expect(customFreezeSides([col('a', 'left'), col('b')])).toEqual({ left: true, right: false });
    expect(customFreezeSides([col('a'), col('b', 'right')])).toEqual({ left: false, right: true });
  });
});

describe('freezeLineHeight', () => {
  it('clips to row content, not the host empty area', () => {
    expect(freezeLineHeight(180, 480)).toBe(180);
    expect(freezeLineHeight(900, 480)).toBe(480);
    expect(freezeLineHeight(0, 480)).toBe(0);
  });
});

describe('freezeLineXs', () => {
  it('returns nulls when custom freeze is off', () => {
    expect(
      freezeLineXs({
        showLeft: false,
        showRight: false,
        frozenBlockRight: 48,
        rightFrozenBlockLeft: 400,
      }),
    ).toEqual({ left: null, right: null });
  });

  it('places left line at the right edge of the left-frozen block', () => {
    expect(
      freezeLineXs({
        showLeft: true,
        showRight: false,
        frozenBlockRight: 148,
        rightFrozenBlockLeft: 400,
      }),
    ).toEqual({ left: 148, right: null });
  });

  it('places right line at the left edge of the right-frozen block (not inside ops)', () => {
    expect(
      freezeLineXs({
        showLeft: false,
        showRight: true,
        frozenBlockRight: 48,
        rightFrozenBlockLeft: 412,
      }),
    ).toEqual({ left: null, right: 412 });
  });
});
