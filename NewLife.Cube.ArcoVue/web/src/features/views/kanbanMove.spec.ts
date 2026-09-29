import { describe, expect, it } from 'vitest';
import {
  dragStartedFromControl,
  kanbanCardDraggable,
  kanbanDropAllowed,
  kanbanGroupDragMeta,
  kanbanPatchValue,
} from './kanbanMove';

describe('kanbanMove（OSC-260926c2b8 看板跨列拖放）', () => {
  it('编辑字段非只读才可拖，并沿用元数据字段名', () => {
    expect(kanbanGroupDragMeta([{ name: 'Type', readOnly: false, required: true }], 'type')).toEqual({
      field: 'Type',
      required: true,
    });
    expect(kanbanGroupDragMeta([{ name: 'Type', readOnly: true }], 'Type')).toBeNull();
    expect(kanbanGroupDragMeta([], 'Type')).toBeNull();
  });

  it('迷你看板、无编辑权、审批中且字段不可写时不可拖', () => {
    const base = { compact: false, canDragGroup: true, groupField: 'Type', locked: false, writable: [] as string[] };
    expect(kanbanCardDraggable(base)).toBe(true);
    expect(kanbanCardDraggable({ ...base, compact: true })).toBe(false);
    expect(kanbanCardDraggable({ ...base, canDragGroup: false })).toBe(false);
    expect(kanbanCardDraggable({ ...base, locked: true })).toBe(false);
    expect(kanbanCardDraggable({ ...base, locked: true, writable: ['type'] })).toBe(true);
  });

  it('同列不写库；必填时未分组拒绝；非必填可清空', () => {
    expect(kanbanDropAllowed('1', '1', false)).toBe('noop');
    expect(kanbanDropAllowed('1', '__ungrouped__', true)).toBe('refuse');
    expect(kanbanDropAllowed('1', '__ungrouped__', false)).toBe('move');
    expect(kanbanDropAllowed('1', '2', true)).toBe('move');
  });

  it('布尔列写成 true/false，未分组写成 null，其它列保留 key', () => {
    expect(kanbanPatchValue('Boolean', 'true')).toBe(true);
    expect(kanbanPatchValue('Boolean', '0')).toBe(false);
    expect(kanbanPatchValue('Int32', '2')).toBe('2');
    expect(kanbanPatchValue('Int32', '__ungrouped__')).toBeNull();
  });

  it('按钮上的按下不开始拖', () => {
    const button = { closest: (sel: string) => (sel.includes('button') ? {} : null) };
    const plain = { closest: () => null };
    expect(dragStartedFromControl(button as unknown as EventTarget)).toBe(true);
    expect(dragStartedFromControl(plain as unknown as EventTarget)).toBe(false);
    expect(dragStartedFromControl(null)).toBe(false);
  });
});
