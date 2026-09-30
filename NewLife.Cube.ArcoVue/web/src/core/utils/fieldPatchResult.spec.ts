import { describe, expect, it } from 'vitest';
import { readFieldPatchResult } from './fieldPatchResult';

describe('readFieldPatchResult', () => {
  it('读取 camelCase data', () => {
    expect(readFieldPatchResult({ data: { ok: 2, fail: 0, errors: [] } })).toEqual({
      ok: 2,
      fail: 0,
      errors: [],
    });
  });

  it('读取 PascalCase Data 包裹', () => {
    expect(
      readFieldPatchResult({
        Data: { Ok: 0, Fail: 1, Errors: [{ Id: '3', Message: '更新未写入数据库' }] },
      }),
    ).toEqual({
      ok: 0,
      fail: 1,
      errors: [{ id: '3', message: '更新未写入数据库' }],
    });
  });

  it('直接是结果对象时也能读 PascalCase', () => {
    expect(
      readFieldPatchResult({ Ok: 1, Fail: 1, Errors: [{ Id: '9', Message: '数据不存在' }] }),
    ).toEqual({
      ok: 1,
      fail: 1,
      errors: [{ id: '9', message: '数据不存在' }],
    });
  });
});
