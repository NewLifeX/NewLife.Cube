import { describe, expect, it } from 'vitest';
import { encodeQueryB64, mapPageKindToAiPage, parseAreaController } from './aiChatContext';

describe('parseAreaController', () => {
  it('Admin/User', () => {
    expect(parseAreaController('Admin/User')).toEqual({ area: 'Admin', controller: 'User' });
    expect(parseAreaController('/Admin/User')).toEqual({ area: 'Admin', controller: 'User' });
  });

  it('单段 User / Home', () => {
    expect(parseAreaController('User')).toEqual({ area: '', controller: 'User' });
    expect(parseAreaController('Home')).toEqual({ area: '', controller: 'Home' });
  });

  it('空 path', () => {
    expect(parseAreaController('')).toEqual({ area: '', controller: '' });
    expect(parseAreaController('/')).toEqual({ area: '', controller: '' });
  });
});

describe('encodeQueryB64', () => {
  it('空对象或失败变空串', () => {
    expect(encodeQueryB64({})).toBe('');
    expect(encodeQueryB64(undefined)).toBe('');
    expect(encodeQueryB64(null)).toBe('');
  });

  it('可往返简单对象', () => {
    const b64 = encodeQueryB64({ Q: 'a' });
    expect(b64.length).toBeGreaterThan(0);
    expect(JSON.parse(decodeURIComponent(escape(atob(b64))))).toEqual({ Q: 'a' });
  });
});

describe('mapPageKindToAiPage', () => {
  it('entity 抽屉映射', () => {
    expect(mapPageKindToAiPage('entity', null)).toBe('list');
    expect(mapPageKindToAiPage('entity', 'add')).toBe('form');
    expect(mapPageKindToAiPage('entity', 'edit')).toBe('form');
    expect(mapPageKindToAiPage('entity', 'detail')).toBe('detail');
  });

  it('其它种类', () => {
    expect(mapPageKindToAiPage('object')).toBe('object');
    expect(mapPageKindToAiPage('home')).toBe('home');
    expect(mapPageKindToAiPage('custom')).toBe('custom');
    expect(mapPageKindToAiPage('unknown')).toBe('custom');
  });
});
