import { describe, expect, it } from 'vitest';
import { mapStartPageToSpa, resolveStartPage } from './startPage';

describe('mapStartPageToSpa', () => {
  it('maps classic user info / index to /home', () => {
    expect(mapStartPageToSpa('/Admin/User/Info')).toBe('/home');
    expect(mapStartPageToSpa('~/Admin/Index/Main')).toBe('/home');
    expect(mapStartPageToSpa('/Admin/Index')).toBe('/home');
  });

  it('keeps SPA admin leaves', () => {
    expect(mapStartPageToSpa('/Admin/Cube')).toBe('/Admin/Cube');
    expect(mapStartPageToSpa('/Admin/Sys')).toBe('/Admin/Sys');
    expect(mapStartPageToSpa('/Admin/Db')).toBe('/Admin/Db');
  });

  it('drops MVC action to entity list', () => {
    expect(mapStartPageToSpa('/Admin/User/Detail')).toBe('/Admin/User');
    expect(mapStartPageToSpa('/Admin/Role/Edit')).toBe('/Admin/Role');
  });
});

describe('resolveStartPage', () => {
  it('prefers redirect', () => {
    expect(resolveStartPage({ startPage: '/Admin/User/Info' }, '/dashboard')).toBe('/dashboard');
  });

  it('maps startPage', () => {
    expect(resolveStartPage({ startPage: '/Admin/User/Info' })).toBe('/home');
    expect(resolveStartPage({ startPage: '/Admin/Cube' })).toBe('/Admin/Cube');
    expect(resolveStartPage({ startPage: '/object/Cube' })).toBe('/object/Cube');
  });
});
