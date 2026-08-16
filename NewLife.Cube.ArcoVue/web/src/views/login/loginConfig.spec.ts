import { describe, expect, it } from 'vitest';
import type { LoginConfig } from '@cube/api-core';
import {
  buildSsoLoginUrl,
  extractMfaToken,
  parseHashTokens,
  resolveLoginTabs,
  resolveOAuthProviders,
} from './loginConfig';

describe('resolveLoginTabs', () => {
  it('only password when login.password true', () => {
    const cfg: LoginConfig = { login: { password: true, sms: false, mail: false } };
    expect(resolveLoginTabs(cfg).map((t) => t.key)).toEqual(['password']);
  });

  it('three channels when all enabled', () => {
    const cfg: LoginConfig = { login: { password: true, sms: true, mail: true } };
    expect(resolveLoginTabs(cfg).map((t) => t.key)).toEqual(['password', 'sms', 'mail']);
  });

  it('empty when all channels false', () => {
    const cfg: LoginConfig = { login: { password: false, sms: false, mail: false } };
    expect(resolveLoginTabs(cfg)).toEqual([]);
  });
});

describe('resolveOAuthProviders', () => {
  it('prefers oauth over providers', () => {
    const cfg: LoginConfig = {
      oauth: [{ name: 'github', nickName: 'GitHub' }],
      providers: [{ name: 'legacy' }],
    };
    expect(resolveOAuthProviders(cfg)[0].name).toBe('github');
  });

  it('accepts backend oAuth camelCase', () => {
    const cfg = {
      oAuth: [{ name: 'NewLife', nickName: '用户中心' }],
    } as LoginConfig;
    expect(resolveOAuthProviders(cfg)[0].name).toBe('NewLife');
  });
});

describe('buildSsoLoginUrl', () => {
  it('includes source=front-end and encoded return', () => {
    const url = buildSsoLoginUrl('github', '/home', 'http://localhost:5183');
    expect(url).toContain('source=front-end');
    expect(url).toContain('/Sso/Login/github?');
    expect(url).toContain(encodeURIComponent('http://localhost:5183/login?redirect=%2Fhome'));
  });
});

describe('extractMfaToken / parseHashTokens', () => {
  it('extracts mfa_required token', () => {
    expect(extractMfaToken('mfa_required:abc123')).toBe('abc123');
    expect(extractMfaToken('用户名或密码错误')).toBeNull();
  });

  it('parses hash tokens', () => {
    expect(parseHashTokens('#token=aaa&refreshToken=bbb')).toEqual({
      token: 'aaa',
      refreshToken: 'bbb',
    });
  });
});
