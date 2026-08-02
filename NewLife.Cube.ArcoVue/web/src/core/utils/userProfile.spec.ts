import { describe, expect, it } from 'vitest';
import {
  SYSTEM_DEFAULT_PROFILE,
  mergeProfile,
  prefsFromWire,
  prefsToWirePayload,
  resolveContentWidth,
  resolveLayoutMode,
} from './userProfile';

describe('resolveLayoutMode', () => {
  it('accepts side/top/mix', () => {
    expect(resolveLayoutMode('side')).toBe('side');
    expect(resolveLayoutMode('top')).toBe('top');
    expect(resolveLayoutMode('mix')).toBe('mix');
  });

  it('falls back to side for invalid', () => {
    expect(resolveLayoutMode('weird')).toBe('side');
    expect(resolveLayoutMode(null)).toBe('side');
  });
});

describe('resolveContentWidth', () => {
  it('accepts standard/wide/fluid', () => {
    expect(resolveContentWidth('standard')).toBe('standard');
    expect(resolveContentWidth('wide')).toBe('wide');
    expect(resolveContentWidth('fluid')).toBe('fluid');
  });

  it('migrates legacy fixed → standard', () => {
    expect(resolveContentWidth('fixed')).toBe('standard');
  });

  it('falls back to fluid for invalid', () => {
    expect(resolveContentWidth('weird')).toBe('fluid');
    expect(resolveContentWidth(null)).toBe('fluid');
  });
});

describe('mergeProfile', () => {
  it('fills missing fields with system defaults', () => {
    const m = mergeProfile({ layout: { mode: 'top' } as never });
    expect(m.layout.mode).toBe('top');
    expect(m.layout.showTabs).toBe(SYSTEM_DEFAULT_PROFILE.layout.showTabs);
    expect(m.theme.appearance).toBe('light');
    expect(m.workspace.pageSize).toBe(20);
  });

  it('maps migration aliases for density/radius/fontScale', () => {
    const m = mergeProfile({
      theme: {
        density: 'comfortable',
        radius: 'lg',
        fontScale: 'large',
      } as never,
    });
    expect(m.theme.density).toBe('default');
    expect(m.theme.radius).toBe(8);
    expect(m.theme.fontScale).toBe(1.125);
  });
});

describe('prefsFromWire / prefsToWirePayload', () => {
  it('parses wire JSON columns', () => {
    const prefs = prefsFromWire({
      layoutJson: '{"mode":"mix","showTabs":false}',
      themeJson: '{"appearance":"dark","primaryColor":"#00B42A"}',
      workspaceJson: '{"defaultView":"card","pageSize":50}',
    });
    expect(prefs.layout.mode).toBe('mix');
    expect(prefs.layout.showTabs).toBe(false);
    expect(prefs.theme.appearance).toBe('dark');
    expect(prefs.theme.primaryColor).toBe('#00B42A');
    expect(prefs.workspace.pageSize).toBe(50);
  });

  it('null wire → defaults', () => {
    expect(prefsFromWire(null)).toEqual(SYSTEM_DEFAULT_PROFILE);
  });

  it('save payload shape contains three Json strings', () => {
    const payload = prefsToWirePayload(SYSTEM_DEFAULT_PROFILE);
    expect(JSON.parse(payload.layoutJson!)).toMatchObject({ mode: 'side' });
    expect(JSON.parse(payload.themeJson!)).toMatchObject({ appearance: 'light' });
    expect(JSON.parse(payload.workspaceJson!)).toMatchObject({ pageSize: 20 });
  });
});
