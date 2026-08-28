/**
 * URL/取值工具单元测试
 */
import { describe, expect, it } from 'vitest';
import { toPascalCase, toCamelCase, toPascalAndCamel, routeToApiPrefix, getValueByKey, resolveUrl } from '@/utils/url';

describe('toPascalCase / toCamelCase', () => {
  it('基本转换', () => {
    expect(toPascalCase('device-profile')).toBe('DeviceProfile');
    expect(toPascalCase('user')).toBe('User');
    expect(toCamelCase('User_Name')).toBe('userName');
    expect(toCamelCase('UserName')).toBe('userName');
  });
});

describe('routeToApiPrefix', () => {
  it('路由转 API 前缀（带前导斜杠）', () => {
    expect(routeToApiPrefix('/admin/user')).toBe('/Admin/User');
    expect(routeToApiPrefix('/device/device-profile')).toBe('/Device/DeviceProfile');
    expect(routeToApiPrefix('/')).toBe('/');
  });
});

describe('getValueByKey 大小写容错', () => {
  const row = { id: 1, Name: '张三', mobile: '138' };

  it('直接命中', () => {
    expect(getValueByKey(row, 'Name')).toBe('张三');
    expect(getValueByKey(row, 'id')).toBe(1);
  });

  it('Pascal ↔ camel 翻转', () => {
    expect(getValueByKey(row, 'name')).toBe('张三');
    expect(getValueByKey(row, 'Mobile')).toBe('138');
  });

  it('全大写/全小写回退', () => {
    const row2 = { ID: 5, uuid: 'x' };
    expect(getValueByKey(row2, 'id')).toBe(5);
    expect(getValueByKey(row2, 'UUID')).toBe('x');
  });

  it('不存在返回 undefined', () => {
    expect(getValueByKey(row, 'Nope')).toBeUndefined();
  });
});

describe('resolveUrl 变量替换', () => {
  it('替换 {Id} 模板', () => {
    expect(resolveUrl('/Admin/User/Detail?id={Id}', { Id: 42 })).toBe('/Admin/User/Detail?id=42');
    expect(resolveUrl('/Admin/User/Detail?id={id}', { id: 7 })).toBe('/Admin/User/Detail?id=7');
  });
});
