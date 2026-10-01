import { describe, expect, it } from 'vitest';
import {
  confirmText,
  isApiActionUrl,
  opsHttpMethod,
  showUserSecurityActions,
} from './opsRequest';

describe('opsRequest', () => {
  it('dataAction uses POST, empty uses GET', () => {
    expect(opsHttpMethod('action')).toBe('POST');
    expect(opsHttpMethod('  action  ')).toBe('POST');
    expect(opsHttpMethod('')).toBe('GET');
    expect(opsHttpMethod(undefined)).toBe('GET');
  });

  it('detects /api/ action urls', () => {
    expect(isApiActionUrl('/api/Admin/AccessRule/Unblock?id=1')).toBe(true);
    expect(isApiActionUrl('/Admin/Log?userId=1')).toBe(false);
    expect(isApiActionUrl('')).toBe(false);
  });

  it('picks confirm copy from the display name', () => {
    expect(confirmText('马上执行')).toBe('确认立即执行该作业？');
    expect(confirmText('解封时间')).toBe('确认解除封禁？');
    expect(confirmText('强制下线')).toBe('确认强制该用户下线？');
    expect(confirmText('日志')).toBe('确认执行该操作？');
  });

  it('shows user security actions only for system edit of an existing user', () => {
    expect(showUserSecurityActions('Admin/User', 'edit', { isSystem: true }, 5)).toBe(true);
    expect(showUserSecurityActions('/Admin/User/', 'detail', { isSystem: true }, '8')).toBe(true);
    expect(showUserSecurityActions('Admin/User', 'add', { isSystem: true }, 5)).toBe(false);
    expect(showUserSecurityActions('Admin/User', 'edit', { isSystem: false }, 5)).toBe(false);
    expect(showUserSecurityActions('Admin/User', 'edit', { isSystem: true }, 0)).toBe(false);
    expect(showUserSecurityActions('Admin/Role', 'edit', { isSystem: true }, 5)).toBe(false);
  });
});
