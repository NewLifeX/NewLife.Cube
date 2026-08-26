import { describe, expect, it } from 'vitest';
import {
  ACCOUNT_PROFILE_FIELDS,
  accountFooterKind,
  accountFooterLabel,
  applyProfileField,
  buildProfilePayload,
  parseAccountTab,
  pickProfileForm,
  resolveSsoAccountUrl,
} from './accountCenter';

describe('parseAccountTab', () => {
  it('falls back to profile', () => {
    expect(parseAccountTab('password')).toBe('password');
    expect(parseAccountTab('nope')).toBe('profile');
    expect(parseAccountTab(undefined)).toBe('profile');
  });
});

describe('resolveSsoAccountUrl', () => {
  it('returns null when redirect is off or center empty', () => {
    expect(resolveSsoAccountUrl({}, 'profile')).toBeNull();
    expect(resolveSsoAccountUrl({ redirectUserToSso: true, ssoUserCenter: '' }, 'profile')).toBeNull();
    expect(resolveSsoAccountUrl({ redirectUserToSso: false, ssoUserCenter: 'https://sso.example' }, 'profile')).toBeNull();
  });

  it('joins Info / ChangePassword paths', () => {
    const cfg = { redirectUserToSso: true, ssoUserCenter: 'https://sso.example/' };
    expect(resolveSsoAccountUrl(cfg, 'profile')).toBe('https://sso.example/Admin/User/Info');
    expect(resolveSsoAccountUrl(cfg, 'password')).toBe('https://sso.example/Admin/User/ChangePassword');
  });

  it('rejects non-http schemes', () => {
    expect(
      resolveSsoAccountUrl({ redirectUserToSso: true, ssoUserCenter: 'javascript:alert(1)' }, 'profile'),
    ).toBeNull();
  });
});

describe('pickProfileForm', () => {
  it('reads PascalCase and builds payload without name', () => {
    const form = pickProfileForm({ ID: 9, DisplayName: '张三', Sex: 1, Mail: 'a@b.c', Mobile: '1', Name: 'admin' });
    expect(form.id).toBe(9);
    expect(form.displayName).toBe('张三');
    expect(form.name).toBe('admin');
    const body = buildProfilePayload(form);
    expect(body).toEqual({ id: 9, displayName: '张三', sex: 1, mail: 'a@b.c', mobile: '1' });
    expect(body).not.toHaveProperty('name');
  });
});

describe('account form layout helpers', () => {
  it('profile fields cover login/role/nickname/sex/mail/mobile', () => {
    expect(ACCOUNT_PROFILE_FIELDS.map((f) => f.name)).toEqual([
      'name',
      'roleNames',
      'displayName',
      'sex',
      'mail',
      'mobile',
    ]);
    expect(ACCOUNT_PROFILE_FIELDS.find((f) => f.name === 'name')?.readOnly).toBe(true);
    expect(ACCOUNT_PROFILE_FIELDS.find((f) => f.name === 'sex')?.dataSource).toEqual({
      '0': '未知',
      '1': '男',
      '2': '女',
    });
  });

  it('footer kind follows tab and SSO', () => {
    expect(accountFooterKind('profile', false, false)).toBe('save');
    expect(accountFooterKind('password', false, false)).toBe('password');
    expect(accountFooterKind('profile', true, false)).toBe('ssoProfile');
    expect(accountFooterKind('password', false, true)).toBe('ssoPassword');
    expect(accountFooterKind('security', false, false)).toBeNull();
    expect(accountFooterKind('binds', false, false)).toBeNull();
    expect(accountFooterLabel('save')).toBe('保存');
    expect(accountFooterLabel('password')).toBe('修改密码');
    expect(accountFooterLabel('ssoProfile')).toBe('前往用户中心');
  });

  it('applyProfileField writes editable keys only', () => {
    const form = pickProfileForm({ Name: 'admin', DisplayName: 'a', Sex: 0 });
    applyProfileField(form, 'displayName', '李四');
    applyProfileField(form, 'sex', '1');
    applyProfileField(form, 'name', 'hack');
    expect(form.displayName).toBe('李四');
    expect(form.sex).toBe(1);
    expect(form.name).toBe('admin');
  });
});
