import type { LoginConfig } from '@cube/api-core';
import type { FieldMeta } from '../types/field';

export const ACCOUNT_TABS = ['profile', 'password', 'security', 'binds'] as const;
export type AccountTab = (typeof ACCOUNT_TABS)[number];

const TAB_SET = new Set<string>(ACCOUNT_TABS);

/** 非法 tab 回落 profile */
export function parseAccountTab(raw: unknown): AccountTab {
  const s = String(raw ?? '').trim().toLowerCase();
  if (TAB_SET.has(s)) return s as AccountTab;
  return 'profile';
}

function pickRedirect(cfg: LoginConfig | null | undefined): boolean {
  if (!cfg) return false;
  const raw = cfg as LoginConfig & Record<string, unknown>;
  const v = raw.redirectUserToSso ?? raw.RedirectUserToSso;
  return v === true;
}

function pickCenter(cfg: LoginConfig | null | undefined): string {
  if (!cfg) return '';
  const raw = cfg as LoginConfig & Record<string, unknown>;
  const v = raw.ssoUserCenter ?? raw.SsoUserCenter;
  return String(v ?? '').trim();
}

/**
 * SSO 用户中心外跳。仅 http(s)；profile → /Admin/User/Info，password → /Admin/User/ChangePassword。
 */
export function resolveSsoAccountUrl(
  cfg: LoginConfig | null | undefined,
  kind: 'profile' | 'password',
): string | null {
  if (!pickRedirect(cfg)) return null;
  const center = pickCenter(cfg).replace(/\/+$/, '');
  if (!center) return null;
  const lower = center.toLowerCase();
  if (!lower.startsWith('http://') && !lower.startsWith('https://')) return null;
  const path = kind === 'password' ? '/Admin/User/ChangePassword' : '/Admin/User/Info';
  return `${center}${path}`;
}

export type ProfileForm = {
  id: number;
  displayName: string;
  sex: number;
  mail: string;
  mobile: string;
  name: string;
  roleNames: string;
};

function pickStr(row: Record<string, unknown>, camel: string, pascal: string): string {
  const v = row[camel] ?? row[pascal];
  return v == null ? '' : String(v);
}

function pickNum(row: Record<string, unknown>, camel: string, pascal: string): number {
  const v = row[camel] ?? row[pascal];
  const n = Number(v);
  return Number.isFinite(n) ? n : 0;
}

export function pickProfileForm(data: unknown): ProfileForm {
  const row = data && typeof data === 'object' ? (data as Record<string, unknown>) : {};
  const sexRaw = row.sex ?? row.Sex;
  let sex = 0;
  if (typeof sexRaw === 'number' && Number.isFinite(sexRaw)) sex = sexRaw;
  else if (sexRaw === '男') sex = 1;
  else if (sexRaw === '女') sex = 2;
  else {
    const n = Number(sexRaw);
    if (Number.isFinite(n)) sex = n;
  }
  return {
    id: pickNum(row, 'id', 'ID'),
    displayName: pickStr(row, 'displayName', 'DisplayName'),
    sex,
    mail: pickStr(row, 'mail', 'Mail'),
    mobile: pickStr(row, 'mobile', 'Mobile'),
    name: pickStr(row, 'name', 'Name'),
    roleNames: pickStr(row, 'roleNames', 'RoleNames') || pickStr(row, 'roleName', 'RoleName'),
  };
}

/** 仅提交允许改的资料字段 */
export function buildProfilePayload(form: ProfileForm): Record<string, unknown> {
  return {
    id: form.id,
    displayName: form.displayName,
    sex: form.sex,
    mail: form.mail,
    mobile: form.mobile,
  };
}

function strField(name: string, displayName: string, extra?: Partial<FieldMeta>): FieldMeta {
  return { name, displayName, typeName: 'String', ...extra };
}

/** 资料 Tab：与实体编辑抽屉同一套 FieldInput 元数据 */
export const ACCOUNT_PROFILE_FIELDS: FieldMeta[] = [
  strField('name', '登录名', { readOnly: true }),
  strField('roleNames', '角色', { readOnly: true }),
  strField('displayName', '昵称'),
  {
    name: 'sex',
    displayName: '性别',
    typeName: 'Int32',
    dataSource: { '0': '未知', '1': '男', '2': '女' },
  },
  strField('mail', '邮箱', { itemType: 'mail' }),
  strField('mobile', '手机', { itemType: 'mobile' }),
];

/** 密码 Tab 字段（控件用 a-input-password，布局与抽屉 FieldInput 同行居中） */
export const ACCOUNT_PASSWORD_FIELDS: { name: 'oldPassword' | 'newPassword' | 'newPassword2'; displayName: string; description?: string }[] = [
  { name: 'oldPassword', displayName: '原密码' },
  {
    name: 'newPassword',
    displayName: '新密码',
    description: '8 位起且含数字、大小写字母和符号',
  },
  { name: 'newPassword2', displayName: '确认新密码' },
];

export type AccountFooterKind = 'save' | 'password' | 'ssoProfile' | 'ssoPassword';

/** 资料/密码 Tab 底部操作卡；安全/绑定无常驻保存 */
export function accountFooterKind(
  tab: AccountTab,
  ssoProfile: boolean,
  ssoPassword: boolean,
): AccountFooterKind | null {
  if (tab === 'profile') return ssoProfile ? 'ssoProfile' : 'save';
  if (tab === 'password') return ssoPassword ? 'ssoPassword' : 'password';
  return null;
}

export function accountFooterLabel(kind: AccountFooterKind | null): string {
  if (kind === 'save') return '保存';
  if (kind === 'password') return '修改密码';
  if (kind === 'ssoProfile' || kind === 'ssoPassword') return '前往用户中心';
  return '';
}

/** 资料可写字段回写（只读 name/roleNames 忽略） */
export function applyProfileField(form: ProfileForm, name: string, value: unknown): void {
  if (name === 'displayName' || name === 'mail' || name === 'mobile') {
    form[name] = value == null ? '' : String(value);
    return;
  }
  if (name === 'sex') {
    const n = Number(value);
    form.sex = Number.isFinite(n) ? n : 0;
  }
}
