import { describe, expect, it } from 'vitest';
import { canCreateOnCalendarCell, formatCalendarDate } from './useCalendarMonth';

describe('canCreateOnCalendarCell（OSC-260926c2b8 日历空白日新建门禁）', () => {
  it('有新增权 + 本月格 + 已配置开始字段 → 可建', () => {
    expect(canCreateOnCalendarCell(true, true, true)).toBe(true);
  });

  it('无新增权或未传入 → 不可建（不打开抽屉、无请求）', () => {
    expect(canCreateOnCalendarCell(false, true, true)).toBe(false);
    expect(canCreateOnCalendarCell(undefined, true, true)).toBe(false);
  });

  it('非本月补齐格 → 不可建', () => {
    expect(canCreateOnCalendarCell(true, false, true)).toBe(false);
  });

  it('未配置开始字段 → 不可建（保持现有警告空态）', () => {
    expect(canCreateOnCalendarCell(true, true, false)).toBe(false);
  });

  it('全排列与三条件合取等价', () => {
    for (const canAdd of [true, false, undefined]) {
      for (const inMonth of [true, false]) {
        for (const hasStart of [true, false]) {
          expect(canCreateOnCalendarCell(canAdd, inMonth, hasStart)).toBe(
            !!canAdd && inMonth && hasStart,
          );
        }
      }
    }
  });
});

describe('formatCalendarDate（壁钟 YYYY-MM-DD）', () => {
  it('本地年月日零填充', () => {
    expect(formatCalendarDate(new Date(2026, 0, 5))).toBe('2026-01-05');
    expect(formatCalendarDate(new Date(2026, 11, 31))).toBe('2026-12-31');
  });
});
