import { describe, expect, it } from 'vitest';
import {
  defaultPublishSlug,
  isValidNamedSlug,
  parseNamedMenuValue,
  workbenchRoutePath,
} from './workbenchNamed';

describe('workbenchNamed 命名工作台纯函数（OSC-260902ef43）', () => {
  it('isValidNamedSlug：小写字母开头、仅 [a-z0-9-]、≤32', () => {
    expect(isValidNamedSlug('ops')).toBe(true);
    expect(isValidNamedSlug('wb-a1')).toBe(true);
    expect(isValidNamedSlug('a')).toBe(true);
    expect(isValidNamedSlug('Ops')).toBe(false);
    expect(isValidNamedSlug('1abc')).toBe(false);
    expect(isValidNamedSlug('')).toBe(false);
    expect(isValidNamedSlug('a'.repeat(33))).toBe(false);
  });

  it('defaultPublishSlug 形如 wb-<4hex>', () => {
    expect(defaultPublishSlug()).toMatch(/^wb-[0-9a-f]{4}$/);
  });

  it('parseNamedMenuValue：四类动作与命名切换，未知返回 null', () => {
    expect(parseNamedMenuValue('__publish')).toEqual({ action: 'publish' });
    expect(parseNamedMenuValue('__rename')).toEqual({ action: 'rename' });
    expect(parseNamedMenuValue('__delete')).toEqual({ action: 'delete' });
    expect(parseNamedMenuValue('__default')).toEqual({ action: 'default' });
    expect(parseNamedMenuValue('__named:ops')).toEqual({ action: 'switch', slug: 'ops' });
    expect(parseNamedMenuValue('__named:Bad')).toBeNull();
    expect(parseNamedMenuValue('anything')).toBeNull();
  });

  it('workbenchRoutePath：空 slug → /home（默认工作台）；有 slug → /Workbench/{slug}', () => {
    expect(workbenchRoutePath('')).toBe('/home');
    expect(workbenchRoutePath('ops')).toBe('/Workbench/ops');
  });
});
