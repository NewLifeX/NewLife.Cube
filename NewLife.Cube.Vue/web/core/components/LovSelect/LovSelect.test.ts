/**
 * useLovSelect 逻辑/事件层单测（Vitest）。
 *
 * 与设计目标一致：本文件只测「逻辑 + 事件返回值」，不渲染 DOM、不做像素比较
 * （UI 渲染交给 LovSelect.ct.spec.ts）。通过注入确定性 fetchMeta mock，
 * 无需 mock 整个 lov-api 网络层，测试稳定且可读。
 *
 * 覆盖：
 *  1. ENUM meta 加载后解析类型并注入选项
 *  2. LIST meta 加载后解析类型并播种 translateCache
 *  3. code 切换触发重载并重置状态
 *  4. modelValue 回显同步到内部选择状态
 *  5. 事件动作返回值（onEnumChange / onEnumMultiChange / onTableSelect / onTableMultiConfirm）
 */
import { describe, it, expect, vi, afterEach } from 'vitest';
import { effectScope, ref, type EffectScope } from 'vue';
import { flushPromises } from '@vue/test-utils';
import { useLovSelect } from './useLovSelect';
import type { LovMetaResponse } from '../types/lov';

// request 被 lov-api 间接依赖，桩掉避免加载 universal-cookie 等缺失依赖
vi.mock('@newlifex/cube-vue/core/utils/request', () => ({
  default: { get: vi.fn(), post: vi.fn() },
  get: vi.fn(),
  post: vi.fn(),
}));

function enumMeta(): LovMetaResponse {
  return {
    meta: [
      {
        lovCode: 'Enum.Test.Status',
        type: 'ENUM',
        name: '状态',
        options: [
          { value: '0', label: '草稿' },
          { value: '1', label: '启用' },
        ],
      },
    ],
    inlineEnums: {},
  };
}

function listMeta(): LovMetaResponse {
  return {
    meta: [
      {
        lovCode: 'List.Test.User',
        type: 'LIST',
        name: '用户',
        valueField: 'id',
        labelField: 'name',
        listConfig: null,
        searchFields: [],
        tableColumns: [],
      },
    ],
    inlineEnums: {
      'Enum.Test.Status': [{ value: '0', label: '草稿' }],
    },
  };
}

describe('useLovSelect（逻辑/事件层）', () => {
  let scope: EffectScope | null = null;
  afterEach(() => {
    scope?.stop();
    scope = null;
  });

  function setup(initialCode = 'Enum.Test.Status') {
    const code = ref(initialCode);
    const modelValue = ref<string | number | string[] | undefined>(undefined);
    const multiple = ref(false);
    const fetchMeta = vi.fn(async (c: string) => (c.startsWith('List') ? listMeta() : enumMeta()));
    scope = effectScope();
    const api = scope.run(() => useLovSelect({ code, modelValue, multiple, fetchMeta }))!;
    return { api, code, modelValue, multiple, fetchMeta };
  }

  it('ENUM meta 加载后解析类型并注入选项', async () => {
    const { api, fetchMeta } = setup();
    expect(api.resolvedType.value).toBeNull();
    await api.loadMeta();
    expect(fetchMeta).toHaveBeenCalledWith('Enum.Test.Status');
    expect(api.resolvedType.value).toBe('ENUM');
    expect(api.options.value).toHaveLength(2);
    expect(api.loading.value).toBe(false);
  });

  it('LIST meta 加载后解析类型并播种 translateCache', async () => {
    const { api } = setup('List.Test.User');
    await api.loadMeta();
    expect(api.resolvedType.value).toBe('LIST');
    expect(api.listMeta.value?.type).toBe('LIST');
    expect(api.translateCache.get('Enum.Test.Status:0')).toBe('草稿');
  });

  it('code 切换触发重载并重置状态', async () => {
    const { api, code } = setup();
    await api.loadMeta();
    expect(api.resolvedType.value).toBe('ENUM');
    code.value = 'List.Test.User';
    await flushPromises();
    expect(api.resolvedType.value).toBe('LIST');
    expect(api.options.value).toHaveLength(0);
  });

  it('modelValue 回显：同步到内部选择状态', async () => {
    const { api, modelValue } = setup();
    await api.loadMeta();
    modelValue.value = '1';
    await flushPromises();
    expect(api.selectedValue.value).toBe('1');
  });

  it('onEnumChange 返回正确 emit 值', () => {
    const { api } = setup();
    expect(api.onEnumChange('0')).toBe('0');
  });

  it('onEnumMultiChange 返回正确 emit 值', () => {
    const { api } = setup();
    expect(api.onEnumMultiChange(['0', '1'])).toEqual(['0', '1']);
  });

  it('onTableSelect 计算值+标签、更新 displayText、关闭弹窗', async () => {
    const { api } = setup('List.Test.User');
    await api.loadMeta();
    const v = api.onTableSelect({ id: 2, name: '编辑' });
    expect(v).toBe(2);
    expect(api.displayText.value).toBe('编辑');
    expect(api.dialogVisible.value).toBe(false);
  });

  it('onTableMultiConfirm 更新选中集合、关闭弹窗并返回', async () => {
    const { api } = setup('List.Test.User');
    await api.loadMeta();
    const v = api.onTableMultiConfirm(['1', '2']);
    expect(v).toEqual(['1', '2']);
    expect(api.selectedValues.value).toEqual(['1', '2']);
    expect(api.dialogVisible.value).toBe(false);
  });
});
