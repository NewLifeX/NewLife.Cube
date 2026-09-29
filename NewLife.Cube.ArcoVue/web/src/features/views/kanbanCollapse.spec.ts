import { describe, expect, it } from 'vitest';
import { toggleCollapsed } from './useKanbanBoard';

describe('toggleCollapsed（OSC-260926c2b8 看板列折叠集合）', () => {
  it('折叠后再切换回到展开', () => {
    const once = toggleCollapsed([], 'a');
    expect(once).toEqual(['a']);

    const twice = toggleCollapsed(once, 'a');
    expect(twice).toEqual([]);
  });

  it('多列折叠互不影响，逐个可回退', () => {
    let keys: string[] = [];
    keys = toggleCollapsed(keys, 'a');
    keys = toggleCollapsed(keys, 'b');
    expect(keys).toEqual(['a', 'b']);

    keys = toggleCollapsed(keys, 'a');
    expect(keys).toEqual(['b']);
  });

  it('不修改原数组（纯函数）', () => {
    const src = ['a'];
    const out = toggleCollapsed(src, 'b');

    expect(src).toEqual(['a']);
    expect(out).toEqual(['a', 'b']);
  });
});
