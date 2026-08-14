import { describe, expect, it } from 'vitest';
import { detectPageKind, isValidEntityPageMeta } from './pageKind';
import type { PageKindProbes } from './pageKind';

function probes(over: Partial<PageKindProbes> = {}): PageKindProbes {
  return {
    getPage: async () => {
      throw new Error('no GetPage');
    },
    getObjectProbe: async () => ({ fields: null, body: null }),
    ...over,
  };
}

describe('isValidEntityPageMeta', () => {
  it('list / search / addForm 数组或 setting 对象 → true', () => {
    expect(isValidEntityPageMeta({ list: [] })).toBe(true);
    expect(isValidEntityPageMeta({ search: [] })).toBe(true);
    expect(isValidEntityPageMeta({ addForm: [] })).toBe(true);
    expect(isValidEntityPageMeta({ setting: {} })).toBe(true);
  });

  it('字符串 / 数组根 / code+message 无 data → false', () => {
    expect(isValidEntityPageMeta('html')).toBe(false);
    expect(isValidEntityPageMeta([])).toBe(false);
    expect(isValidEntityPageMeta({ code: 404, message: 'not found' })).toBe(false);
    expect(isValidEntityPageMeta(null)).toBe(false);
    expect(isValidEntityPageMeta({})).toBe(false);
  });
});

describe('detectPageKind', () => {
  it('Admin/Index → home（短路，不请求探测）', async () => {
    let called = false;
    const p = probes({
      getPage: async () => {
        called = true;
        return null;
      },
    });
    expect(await detectPageKind('/Admin/Index', p)).toBe('home');
    expect(called).toBe(false);
  });

  it('Admin/Db 与 Admin/File → custom（本号新增行）', async () => {
    expect(await detectPageKind('/Admin/Db', probes())).toBe('custom');
    expect(await detectPageKind('admin/file', probes())).toBe('custom');
  });

  it('GetPage 有效元数据 → entity', async () => {
    const p = probes({ getPage: async () => ({ data: { list: [], setting: {} } }) });
    expect(await detectPageKind('/Admin/User', p)).toBe('entity');
  });

  it('GetPage 失败 + 对象 GET → object', async () => {
    const p = probes({
      getPage: async () => {
        throw new Error('404');
      },
      getObjectProbe: async () => ({
        fields: [{ name: 'Debug', typeName: 'Boolean' }],
        body: { Debug: false },
      }),
    });
    expect(await detectPageKind('/Admin/Cube', p)).toBe('object');
  });

  it('GetPage 失败 + 分页列表形 GET → unknown', async () => {
    const p = probes({
      getPage: async () => {
        throw new Error('404');
      },
      getObjectProbe: async () => ({
        fields: [],
        body: { data: [{ id: 1 }], page: { totalCount: 1 } },
      }),
    });
    expect(await detectPageKind('/Admin/Unknown', p)).toBe('unknown');
  });
});
