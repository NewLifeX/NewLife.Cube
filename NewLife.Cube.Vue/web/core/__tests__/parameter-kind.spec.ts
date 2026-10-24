/**
 * parameter-kind.ts 单元测试
 *
 * 纯函数测试，无需 DOM 环境，覆盖所有已知值+边界值。
 */
import { describe, it, expect } from 'vitest';
import { getKind } from '../../apps/cube-admin/src/views/admin/parameter/parameter-kind';
import i18n from '../i18n';

/** 标签文案跟随 locale，断言用词条拼期望值 */
const tr = (key: string): string => i18n.global.t(key);

describe('getKind()', () => {
  it('普通 (0)', () => {
    const r = getKind(0);
    expect(r.text).toBe(tr('param.kindNormal'));
    expect(r.type).toBe('');
  });

  it('系统 (1)', () => {
    const r = getKind(1);
    expect(r.text).toBe(tr('param.kindSystem'));
    expect(r.type).toBe('warning');
  });

  it('用户 (2)', () => {
    const r = getKind(2);
    expect(r.text).toBe(tr('param.kindUser'));
    expect(r.type).toBe('info');
  });

  it('字符串数字', () => {
    expect(getKind('0').text).toBe(tr('param.kindNormal'));
    expect(getKind('1').text).toBe(tr('param.kindSystem'));
    expect(getKind('2').text).toBe(tr('param.kindUser'));
  });

  it('未知值回退', () => {
    const r = getKind(99);
    expect(r.text).toBe(tr('param.kindUnknown'));
    expect(r.type).toBe('info');
  });

  it('null 回退', () => {
    const r = getKind(null);
    expect(r.text).toBe(tr('param.kindUnknown'));
  });

  it('undefined 回退', () => {
    const r = getKind(undefined);
    expect(r.text).toBe(tr('param.kindUnknown'));
  });

  it('NaN 回退', () => {
    const r = getKind(NaN);
    expect(r.text).toBe(tr('param.kindUnknown'));
  });
});