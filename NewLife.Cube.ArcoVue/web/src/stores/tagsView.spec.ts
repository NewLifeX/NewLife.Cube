import { describe, expect, it } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import type { RouteLocationNormalizedLoaded } from 'vue-router';
import { touchCachedNames, useTagsViewStore } from './tagsView';

function route(path: string, name: string): RouteLocationNormalizedLoaded {
  return {
    path,
    fullPath: path,
    name,
    meta: { title: name },
    params: {},
    query: {},
    hash: '',
    matched: [],
    redirectedFrom: undefined,
  } as RouteLocationNormalizedLoaded;
}

describe('touchCachedNames', () => {
  it('第 9 个挤掉最久的，再次进入已有名字移到末尾', () => {
    let cached: string[] = [];
    for (let i = 1; i <= 9; i++) cached = touchCachedNames(cached, `p${i}`);
    expect(cached).toEqual(['p2', 'p3', 'p4', 'p5', 'p6', 'p7', 'p8', 'p9']);
    cached = touchCachedNames(cached, 'p4');
    expect(cached).toEqual(['p2', 'p3', 'p5', 'p6', 'p7', 'p8', 'p9', 'p4']);
  });
});

describe('tagsView cached 上限', () => {
  it('visited 保留 9 个，cached 只留最近 8 个', () => {
    setActivePinia(createPinia());
    const store = useTagsViewStore();
    for (let i = 1; i <= 9; i++) store.addView(route(`/p${i}`, `P${i}`));
    expect(store.visited).toHaveLength(9);
    expect(store.cached).toEqual(['P2', 'P3', 'P4', 'P5', 'P6', 'P7', 'P8', 'P9']);
    store.addView(route('/p2', 'P2'));
    expect(store.visited).toHaveLength(9);
    expect(store.cached[store.cached.length - 1]).toBe('P2');
    store.removeView('/p9');
    expect(store.visited.some((v) => v.path === '/p9')).toBe(false);
    expect(store.cached).not.toContain('P9');
  });
});
