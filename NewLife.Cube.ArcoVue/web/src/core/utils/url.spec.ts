import { describe, expect, it } from 'vitest';
import {
  getValueByKey,
  normalizeKeysByFields,
  normalizeMenuUrl,
  routeToApiPrefix,
  setValueByKey,
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

  it('setValueByKey writes to existing camelCase key', () => {
    const data: Record<string, unknown> = { enable: false };
    setValueByKey(data, 'Enable', true);
    expect(data.enable).toBe(true);
    expect(Object.keys(data)).toEqual(['enable']);
  });

  it('setValueByKey writes to existing PascalCase key', () => {
    const data: Record<string, unknown> = { Enable: false };
    setValueByKey(data, 'enable', true);
    expect(data.Enable).toBe(true);
    expect(Object.keys(data)).toEqual(['Enable']);
  });

  it('setValueByKey falls back to raw key when absent', () => {
    const data: Record<string, unknown> = {};
    setValueByKey(data, 'Enable', 1);
    expect(data.Enable).toBe(1);
  });

  it('normalizeKeysByFields maps camelCase data to FieldMeta.name keys', () => {
    const fields = [
      { name: 'Name' },
      { name: 'Sex' },
      { name: 'CreateTime' },
      { name: 'Missing' },
    ];
    const data = { name: 'admin', sex: 1, createTime: '2026-08-02T00:00:00' };
    expect(normalizeKeysByFields(data, fields)).toEqual({
      Name: 'admin',
      Sex: 1,
      CreateTime: '2026-08-02T00:00:00',
    });
  });

  it('normalizeKeysByFields keeps existing PascalCase keys', () => {
    const data = { Name: 'x', CreateTime: 't' };
    expect(normalizeKeysByFields(data, [{ name: 'Name' }, { name: 'CreateTime' }])).toEqual({
      Name: 'x',
      CreateTime: 't',
    });
  });
});
