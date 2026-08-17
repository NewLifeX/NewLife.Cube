import { describe, expect, it } from 'vitest';
import type { LoginConfig } from '@cube/api-core';
import {
  buildSsoLoginUrl,
  extractMfaToken,
  isOAuthLoginEnabled,
  isTenantLoginEnabled,
  needSendCodeCaptcha,
  normalizeLoginAssetUrl,
  parseHashTokens,
  resolveLoginLogoUrl,
  resolveLoginTabs,
  resolveOAuthProviders,
  resolveStartPage,
  validatePasswordStrength,
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

  it('maps Remark/NickName PascalCase for tooltip', () => {
    const cfg = {
      oauth: [{ name: 'Weixin', NickName: '微信', Remark: '公众号登录' } as never],
    } as LoginConfig;
    const p = resolveOAuthProviders(cfg)[0];
    expect(p.nickName).toBe('微信');
    expect(p.remark).toBe('公众号登录');
  });

  it('does not gate on EnableOAuthServer (scheme A)', () => {
    const cfg = {
      enableOAuthServer: false,
      oauth: [{ name: 'a' }],
    } as LoginConfig;
    expect(resolveOAuthProviders(cfg)).toHaveLength(1);
    expect(isOAuthLoginEnabled(cfg)).toBe(true);
  });
});

describe('feature flags', () => {
  it('isTenantLoginEnabled requires enableTenant true', () => {
    expect(isTenantLoginEnabled({ enableTenant: true })).toBe(true);
    expect(isTenantLoginEnabled({ enableTenant: false })).toBe(false);
    expect(isTenantLoginEnabled({})).toBe(false);
  });

  it('isOAuthLoginEnabled only needs providers', () => {
    expect(isOAuthLoginEnabled({ oauth: [{ name: 'a' }] })).toBe(true);
    expect(isOAuthLoginEnabled({ enableOAuthServer: true, oauth: [] })).toBe(false);
  });
});

describe('needSendCodeCaptcha / password / startPage', () => {
  it('reads login.sendCodeCaptcha', () => {
    expect(needSendCodeCaptcha({ login: { sendCodeCaptcha: true } })).toBe(true);
    expect(needSendCodeCaptcha({ login: { captcha: true } })).toBe(false);
  });

  it('validatePasswordStrength respects * and pattern', () => {
    expect(validatePasswordStrength('abc', '*')).toBeNull();
    expect(validatePasswordStrength('Abcdef1!', '^(?=.*\\d).{8,}$')).toBeNull();
    expect(validatePasswordStrength('abcdefg', '^(?=.*\\d).{8,}$')).toBeTruthy();
  });

  it('resolveStartPage prefers redirect then maps MVC paths', () => {
    expect(resolveStartPage({ startPage: '/Admin/User/Info' }, '/dashboard')).toBe('/dashboard');
    expect(resolveStartPage({ startPage: '/Admin/User/Info' })).toBe('/home');
    expect(resolveStartPage({ startPage: '/Admin/Cube' })).toBe('/Admin/Cube');
    expect(resolveStartPage({ startPage: '/object/Cube' })).toBe('/object/Cube');
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

describe('normalizeLoginAssetUrl', () => {
  it('adds leading slash and normalizes backslash', () => {
    expect(normalizeLoginAssetUrl('Uploads/Cube/a.png')).toBe('/Uploads/Cube/a.png');
    expect(normalizeLoginAssetUrl('\\Uploads\\Cube\\a.png')).toBe('/Uploads/Cube/a.png');
    expect(normalizeLoginAssetUrl('/Uploads/Cube/a.png')).toBe('/Uploads/Cube/a.png');
  });

  it('keeps absolute urls', () => {
    expect(normalizeLoginAssetUrl('https://cdn.example/a.png')).toBe('https://cdn.example/a.png');
  });
});

describe('resolveLoginLogoUrl', () => {
  it('prefers loginLogo then logo', () => {
    expect(resolveLoginLogoUrl({ loginLogo: 'Uploads/a.png' })).toBe('/Uploads/a.png');
    expect(resolveLoginLogoUrl({ logo: 'Uploads/b.png' })).toBe('/Uploads/b.png');
    expect(resolveLoginLogoUrl({ loginLogo: '/a.png', logo: '/b.png' })).toBe('/a.png');
    expect(resolveLoginLogoUrl(null)).toBe('');
  });
});
