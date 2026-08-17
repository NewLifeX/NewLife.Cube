import { describe, expect, it, afterEach } from 'vitest';
import { ApiError } from '@cube/api-core';
import { formatApiError, setStarWebResolver } from './apiError';

describe('formatApiError', () => {
  afterEach(() => setStarWebResolver(null));

  it('prefers fieldErrors then message', () => {
    const err = new ApiError({
      code: -2,
      data: null,
      message: '添加失败！',
      fieldErrors: [
        { field: 'Code', message: '代码不可以为空！' },
        { field: 'Name', message: '名称不可以为空！' },
      ],
    });
    expect(formatApiError(err)).toBe('代码不可以为空！；名称不可以为空！');
  });

  it('appends star trace when configured', () => {
    setStarWebResolver(() => 'https://star.example.com');
    const err = new ApiError({
      code: -1,
      data: null,
      message: '失败',
      traceId: 'abc',
    });
    expect(formatApiError(err)).toBe('失败（追踪 https://star.example.com/trace?id=abc）');
  });

  it('falls back', () => {
    expect(formatApiError(null, '保存失败')).toBe('保存失败');
  });
});
