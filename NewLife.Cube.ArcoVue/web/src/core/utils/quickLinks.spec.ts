import { describe, expect, it } from 'vitest';
import type { MenuItem } from '@cube/api-core';
import {
  menuLeavesForPins,
  readQuickLinkPins,
  resolveQuickLinksDisplay,
} from './quickLinks';

function menu(partial: Partial<MenuItem> & { name: string; url?: string }): MenuItem {
  return {
    id: 1,
    displayName: partial.displayName || partial.name,
    parentID: 0,
    url: partial.url || '',
    visible: true,
    children: [],
    ...partial,
  };
}

describe('readQuickLinkPins', () => {
  it('reads pins from query', () => {
    const pins = readQuickLinkPins({
      pins: [{ name: '用户', url: '/Admin/User', icon: 'fa-users' }],
    });
    expect(pins).toHaveLength(1);
    expect(pins[0].url).toBe('/Admin/User');
    expect(pins[0].icon).toBe('peoples');
  });

  it('returns empty for missing pins', () => {
    expect(readQuickLinkPins({})).toEqual([]);
    expect(readQuickLinkPins(null)).toEqual([]);
  });
});

describe('resolveQuickLinksDisplay', () => {
  const server = [{ name: '默认', url: '/Admin/Cube', icon: 'setting' }];
  const allowed = [
    { name: '用户', url: '/Admin/User', icon: 'peoples' },
    { name: '日志', url: '/Admin/Log', icon: 'history' },
  ];

  it('uses server when no pins', () => {
    expect(resolveQuickLinksDisplay([], server, allowed)).toEqual(server);
  });

  it('filters pins by allowed menus', () => {
    const pins = [
      { name: '用户', url: '/Admin/User', icon: 'peoples' },
      { name: '已撤权', url: '/Gone', icon: 'app' },
    ];
    expect(resolveQuickLinksDisplay(pins, server, allowed)).toEqual([pins[0]]);
  });

  it('falls back to server when all pins revoked', () => {
    const pins = [{ name: '已撤权', url: '/Gone', icon: 'app' }];
    expect(resolveQuickLinksDisplay(pins, server, allowed)).toEqual(server);
  });
});

describe('menuLeavesForPins', () => {
  it('collects visible menus with url', () => {
    const menus = [
      menu({
        name: '系统',
        children: [
          menu({ name: 'User', displayName: '用户', url: 'Admin/User', id: 2 }),
          menu({ name: 'Hidden', url: '/Admin/X', visible: false, id: 3 }),
        ],
      }),
    ];
    const leaves = menuLeavesForPins(menus);
    expect(leaves.map((l) => l.url)).toEqual(['/Admin/User']);
    expect(leaves[0].name).toBe('用户');
  });
});
