import { describe, expect, it } from 'vitest';
import {
  calendarWindow,
  canCreateOnCalendarCell,
  calendarRangeLabel,
  formatCalendarDate,
  layoutDaySpans,
  shiftCalendarCursor,
  startOfCalendarWeek,
  timeGridCreatePayload,
  withCalendarWindow,
} from './useCalendarMonth';

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

describe('shiftCalendarCursor（日历月导航）', () => {
  it('上一月 / 下一月保持日号', () => {
    expect(formatCalendarDate(shiftCalendarCursor(new Date(2026, 8, 29), -1))).toBe('2026-08-29');
    expect(formatCalendarDate(shiftCalendarCursor(new Date(2026, 8, 29), 1))).toBe('2026-10-29');
    expect(formatCalendarDate(shiftCalendarCursor(new Date(2026, 0, 15), 12))).toBe('2027-01-15');
  });
});

describe('日历模式导航（日 / 周 / 月）', () => {
  it('shiftCalendarCursor 按模式位移：日 ±1 天 / 周 ±7 天 / 月 ±1 月', () => {
    const c = new Date(2026, 8, 29);
    expect(formatCalendarDate(shiftCalendarCursor(c, -1, 'day'))).toBe('2026-09-28');
    expect(formatCalendarDate(shiftCalendarCursor(c, 1, 'week'))).toBe('2026-10-06');
    expect(formatCalendarDate(shiftCalendarCursor(c, -1, 'month'))).toBe('2026-08-29');
  });

  it('startOfCalendarWeek 取周一（周日最后）', () => {
    expect(formatCalendarDate(startOfCalendarWeek(new Date(2026, 8, 29)))).toBe('2026-09-28');
    expect(formatCalendarDate(startOfCalendarWeek(new Date(2026, 8, 28)))).toBe('2026-09-28');
    expect(formatCalendarDate(startOfCalendarWeek(new Date(2026, 8, 27)))).toBe('2026-09-21');
  });

  it('calendarWindow 月/周/日为本地壁钟开区间', () => {
    const cursor = new Date(2026, 8, 30);
    expect(calendarWindow('month', cursor)).toEqual({
      start: '2026-09-01T00:00:00',
      end: '2026-10-01T00:00:00',
    });
    expect(calendarWindow('week', cursor)).toEqual({
      start: '2026-09-28T00:00:00',
      end: '2026-10-05T00:00:00',
    });
    expect(calendarWindow('day', cursor)).toEqual({
      start: '2026-09-30T00:00:00',
      end: '2026-10-01T00:00:00',
    });
  });

  it('withCalendarWindow：any 且有条件时原样返回；all 追加 gte/lt 且不改入参', () => {
    const win = calendarWindow('day', new Date(2026, 8, 30));
    const anyFilter = { logic: 'any' as const, conditions: [{ field: 'Name', op: 'eq' as const, value: 'a' }] };
    expect(withCalendarWindow(anyFilter, 'Start', 'DateTime', win)).toBe(anyFilter);
    const all = { logic: 'all' as const, conditions: [{ field: 'Name', op: 'eq' as const, value: 'a' }] };
    const next = withCalendarWindow(all, 'Start', 'DateTime', win);
    expect(next).not.toBe(all);
    expect(all.conditions).toHaveLength(1);
    expect(next.conditions[1]).toMatchObject({ field: 'Start', op: 'gte', value: win.start });
    expect(next.conditions[2]).toMatchObject({ field: 'Start', op: 'lt', value: win.end });
    expect(withCalendarWindow(all, 'Start', 'String', win)).toBe(all);
  });

  it('calendarRangeLabel：月 / 周 / 日标题（含跨年周）', () => {
    const today = new Date(2026, 8, 29);
    expect(calendarRangeLabel('month', new Date(2026, 8, 29), today)).toBe('2026年9月');
    expect(calendarRangeLabel('week', new Date(2026, 8, 29), today)).toBe('2026年9月28日 – 10月4日');
    expect(calendarRangeLabel('day', new Date(2026, 8, 29), today)).toBe('2026年9月29日 今天');
    expect(calendarRangeLabel('day', new Date(2026, 8, 28), today)).toBe('2026年9月28日 周一');
    expect(calendarRangeLabel('week', new Date(2026, 11, 28), today)).toBe('2026年12月28日 – 2027年1月3日');
  });
});

describe('layoutDaySpans（日/周时间轴布局）', () => {
  const day = new Date(2026, 8, 29);
  const H = 48;

  it('单条事件按开始时间与时长定位', () => {
    const out = layoutDaySpans(
      [{ start: new Date(2026, 8, 29, 9), end: new Date(2026, 8, 29, 10) }],
      day,
      H,
    );
    expect(out[0]).toEqual({ top: 9 * H, height: H, leftPct: 0, widthPct: 100 });
  });

  it('重叠事件并列各半，相接不重叠仍全宽', () => {
    const overlap = layoutDaySpans(
      [
        { start: new Date(2026, 8, 29, 9), end: new Date(2026, 8, 29, 11) },
        { start: new Date(2026, 8, 29, 10), end: new Date(2026, 8, 29, 12) },
      ],
      day,
      H,
    );
    expect(overlap[0]).toEqual({ top: 9 * H, height: 2 * H, leftPct: 0, widthPct: 50 });
    expect(overlap[1]).toEqual({ top: 10 * H, height: 2 * H, leftPct: 50, widthPct: 50 });

    const touch = layoutDaySpans(
      [
        { start: new Date(2026, 8, 29, 9), end: new Date(2026, 8, 29, 10) },
        { start: new Date(2026, 8, 29, 10), end: new Date(2026, 8, 29, 11) },
      ],
      day,
      H,
    );
    expect(touch[0].widthPct).toBe(100);
    expect(touch[1].widthPct).toBe(100);
  });

  it('跨天裁到当日；零时长保证最小高度', () => {
    const clipped = layoutDaySpans(
      [{ start: new Date(2026, 8, 28, 23), end: new Date(2026, 8, 29, 1) }],
      day,
      H,
    );
    expect(clipped[0]).toEqual({ top: 0, height: H, leftPct: 0, widthPct: 100 });

    const instant = layoutDaySpans(
      [{ start: new Date(2026, 8, 29, 9), end: new Date(2026, 8, 29, 9) }],
      day,
      H,
    );
    expect(instant[0]).toEqual({ top: 9 * H, height: 20, leftPct: 0, widthPct: 100 });
  });
});

describe('timeGridCreatePayload（日/周点击空白新建）', () => {
  const H = 48;

  it('按点击位置取整点并夹取 0~23', () => {
    expect(timeGridCreatePayload('2026-09-29', 0, H)).toEqual({ date: '2026-09-29', hour: 0 });
    expect(timeGridCreatePayload('2026-09-29', 100, H)).toEqual({ date: '2026-09-29', hour: 2 });
    expect(timeGridCreatePayload('2026-09-29', 9 * H + 47, H)).toEqual({ date: '2026-09-29', hour: 9 });
    expect(timeGridCreatePayload('2026-09-29', 1151, H)).toEqual({ date: '2026-09-29', hour: 23 });
    expect(timeGridCreatePayload('2026-09-29', -5, H)).toEqual({ date: '2026-09-29', hour: 0 });
  });
});
