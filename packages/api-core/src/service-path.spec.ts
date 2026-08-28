import { describe, expect, it } from 'vitest';
import { getServiceBaseUrl, isServiceApiPath } from './service-path';

describe('getServiceBaseUrl', () => {
  it('strips trailing /api from absolute and relative base', () => {
    expect(getServiceBaseUrl('http://host:7116/api')).toBe('http://host:7116');
    expect(getServiceBaseUrl('/api')).toBe('');
    expect(getServiceBaseUrl('')).toBe('');
    expect(getServiceBaseUrl('/api/')).toBe('');
  });
});

describe('isServiceApiPath', () => {
  it('recognizes Auth/Sso/Mfa/OAuth prefixes', () => {
    expect(isServiceApiPath('/Auth/Login')).toBe(true);
    expect(isServiceApiPath('/Sso/Login')).toBe(true);
    expect(isServiceApiPath('/Mfa/Verify')).toBe(true);
    expect(isServiceApiPath('/OAuth/Callback')).toBe(true);
  });

  it('recognizes CubeController service actions without /api', () => {
    expect(isServiceApiPath('/Cube/MenuTree')).toBe(true);
    expect(isServiceApiPath('/Cube/Lookup')).toBe(true);
    expect(isServiceApiPath('/Cube/UserProfile')).toBe(true);
    expect(isServiceApiPath('/Cube/ViewProfile')).toBe(true);
    expect(isServiceApiPath('/Cube/ViewProfileTemplate')).toBe(true);
    expect(isServiceApiPath('/Cube/EntityComment')).toBe(true);
    expect(isServiceApiPath('/Cube/Setting')).toBe(true);
    expect(isServiceApiPath('/Cube/Automation')).toBe(true);
    expect(isServiceApiPath('/Cube/Automation/Run')).toBe(true);
    expect(isServiceApiPath('/Cube/Automation/Runs')).toBe(true);
    expect(isServiceApiPath('/Cube/Widget')).toBe(true);
    expect(isServiceApiPath('/Cube/Widget/Query')).toBe(true);
    expect(isServiceApiPath('/Cube/Widget/Sources')).toBe(true);
  });

  it('treats Cube area entity controllers as non-service (need /api)', () => {
    expect(isServiceApiPath('/Cube/App')).toBe(false);
    expect(isServiceApiPath('/Cube/App/GetPage')).toBe(false);
    expect(isServiceApiPath('/Admin/User/GetPage')).toBe(false);
  });
});
