import { describe, expect, it } from 'vitest';
import {
  applyDescriptionDataSourceIfNeeded,
  parseDescriptionDataSource,
} from './descriptionDataSource';
import { resolveControl } from './fieldControl';
import { toFieldMeta } from './fieldNormalize';
import type { FieldMeta } from '../types/field';

describe('parseDescriptionDataSource', () => {
  it('parses N表示… pairs (EnableUserOnline)', () => {
    const ds = parseDescriptionDataSource(
      '是否记录用户在线信息，0表示不记录，1表示仅记录已登录用户，2表示记录所有访客。默认2',
    );
    expect(ds).toEqual({
      '0': '不记录',
      '1': '仅记录已登录用户',
      '2': '记录所有访客',
    });
  });

  it('parses N=… pairs including combinations (CaptchaScene)', () => {
    const ds = parseDescriptionDataSource(
      '位掩码：0=不启用，1=登录，2=注册，4=发验证码（防短信轰炸），可组合，如3=登录+注册均需验证码，默认0',
    );
    expect(ds?.['0']).toBe('不启用');
    expect(ds?.['1']).toBe('登录');
    expect(ds?.['2']).toBe('注册');
    expect(ds?.['4']).toContain('发验证码');
    expect(ds?.['3']).toContain('登录+注册');
  });

  it('parses -1 Unspecified style', () => {
    const ds = parseDescriptionDataSource(
      'token的cookies默认模式（ -1 Unspecified，0 None，1 Lax，2 Strict）',
    );
    expect(ds).toMatchObject({
      '-1': 'Unspecified',
      '0': 'None',
      '1': 'Lax',
      '2': 'Strict',
    });
  });

  it('returns undefined when fewer than 2 pairs', () => {
    expect(parseDescriptionDataSource('默认7200秒')).toBeUndefined();
    expect(parseDescriptionDataSource('页面允许导出的最大行数，默认10_000_000')).toBeUndefined();
  });
});

describe('applyDescriptionDataSourceIfNeeded + resolveControl', () => {
  it('Int32 with description pairs → select', () => {
    const field: FieldMeta = {
      name: 'EnableUserOnline',
      typeName: 'Int32',
      description:
        '是否记录用户在线信息，0表示不记录，1表示仅记录已登录用户，2表示记录所有访客。默认2',
    };
    applyDescriptionDataSourceIfNeeded(field);
    expect(field.dataSource?.['0']).toBe('不记录');
    expect(resolveControl(field)).toBe('select');
  });

  it('does not override existing dataSource', () => {
    const field: FieldMeta = {
      name: 'X',
      typeName: 'Int32',
      description: '0表示A，1表示B',
      dataSource: { '9': '已有' },
    };
    applyDescriptionDataSourceIfNeeded(field);
    expect(field.dataSource).toEqual({ '9': '已有' });
  });

  it('plain Int32 stays inputNumber', () => {
    const field: FieldMeta = {
      name: 'TokenExpire',
      typeName: 'Int32',
      description: '访问令牌AccessToken的有效期，默认7200秒',
    };
    applyDescriptionDataSourceIfNeeded(field);
    expect(field.dataSource).toBeUndefined();
    expect(resolveControl(field)).toBe('inputNumber');
  });
});

describe('toFieldMeta + singleSelect dataSourceMap', () => {
  it('singleSelect + dataSourceMap resolves to local select not lov', () => {
    const meta = toFieldMeta({
      name: 'DefaultRole',
      typeName: 'String',
      itemType: 'singleSelect',
      dataSourceMap: { 普通用户: '普通用户', 管理员: '管理员' },
    } as never);
    expect(meta.dataSource?.['普通用户']).toBe('普通用户');
    expect(resolveControl(meta)).toBe('select');
  });

  it('Int32 description pairs via toFieldMeta', () => {
    const meta = toFieldMeta({
      name: 'CaptchaScene',
      typeName: 'Int32',
      description: '位掩码：0=不启用，1=登录，2=注册，4=发验证码，默认0',
    } as never);
    expect(resolveControl(meta)).toBe('select');
    expect(meta.dataSource?.['1']).toBe('登录');
  });
});
