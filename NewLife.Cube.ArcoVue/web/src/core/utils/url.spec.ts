import { describe, expect, it } from 'vitest';
import {
  getValueByKey,
  normalizeMenuUrl,
  routeToApiPrefix,
  toKebabCase,
  toPascalCase,
} from './url';

describe('url utils', () => {
  it('toPascalCase / toKebabCase', () => {
    expect(toPascalCase('device-profile')).toBe('DeviceProfile');
    expect(toKebabCase('DeviceProfile')).toBe('device-profile');
  });

  it('normalizeMenuUrl keeps pascal segments', () => {
    expect(normalizeMenuUrl('/Admin/User')).toBe('/Admin/User');
    expect(normalizeMenuUrl('Admin/User')).toBe('/Admin/User');
  });

  it('routeToApiPrefix maps kebab to Pascal', () => {
    expect(routeToApiPrefix('/admin/user')).toBe('/Admin/User');
    expect(routeToApiPrefix('/Admin/User')).toBe('/Admin/User');
  });

  it('getValueByKey tolerates case', () => {
    expect(getValueByKey({ Id: 1 }, 'id')).toBe(1);
    expect(getValueByKey({ id: 2 }, 'Id')).toBe(2);
  });
});
