import { describe, expect, it } from 'vitest';
import { ApiError } from '@cube/api-core';
import { formatApiError } from './apiError';

describe('formatApiError', () => {
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

  it('falls back', () => {
    expect(formatApiError(null, '保存失败')).toBe('保存失败');
  });
});
