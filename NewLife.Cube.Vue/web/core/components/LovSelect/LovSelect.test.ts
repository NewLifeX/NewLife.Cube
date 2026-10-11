/**
 * useLovSelect 逻辑/事件层单测（Vitest）。
 *
 * 与设计目标一致：本文件只测「逻辑 + 事件返回值」，不渲染 DOM、不做像素比较
 * （UI 渲染交给 LovSelect.ct.spec.ts）。
 *
 * 覆盖两条路径：
 *  A. 注入 fetchMeta（测试隔离）：绕过 lovStore，直接喂确定性 meta，验证
 *     meta 解析 / code 切换 / modelValue 回显 / 事件返回值。
 *  B. 走 lovStore（生产路径）：vi.mock('./lovStore')，验证 loadMeta 调用
 *     lovStore.getMeta、onTableSelect 调用 registerSelectedRow，确认双缓存已消除。
 *
 * 覆盖：
 *  1. ENUM meta 加载后解析类型并注入选项
 *  2. LIST meta 加载后解析类型并播种 translateCache
 *  3. code 切换触发重载并重置状态
 *  4. modelValue 回显同步到内部选择状态
 *  5. 事件动作返回值（onEnumChange / onEnumMultiChange / onTableSelect / onTableMultiConfirm）
 *  6. 生产路径：loadMeta 委托 lovStore.getMeta
 *  7. 生产路径：onTableSelect 登记选中行到 lovStore.registerSelectedRow
 */
import { describe, it, expect, vi, afterEach } from 'vitest';
import { effectScope, ref, type EffectScope } from 'vue';
import { flushPromises } from '@vue/test-utils';
import { useLovSelect, type UseLovSelectOptions } from './useLovSelect';
import type { LovMetaResponse, LovMetaItem, LovListMeta } from '../../types/lov';
// 桩掉 request 避免加载 universal-cookie 等缺失依赖（即便 lovStore 被 mock，导入链仍可能触发）
vi.mock('@newlifex/cube-vue/core/utils/request', () => ({
  default: { get: vi.fn(), post: vi.fn() },
  get: vi.fn(),
  post: vi.fn(),
}));

// 路径 A/B 共用：生产路径（不注入 fetchMeta 时）走 lovStore，整体 mock 便于断言调用
import {
  getMeta,
  getCachedMeta,
  registerSelectedRow,
  getSelectedLabel,
  resolveSelectedLabel,
} from '@newlifex/cube-vue/core/components/LovSelect/lovStore';
vi.mock('@newlifex/cube-vue/core/components/LovSelect/lovStore');

function enumMetaResponse(): LovMetaResponse {
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

function listMetaResponse(): LovMetaResponse {
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

function enumItem(): LovMetaItem {
  return {
    lovCode: 'Enum.Test.Status',
    type: 'ENUM',
    name: '状态',
    options: [
      { value: '0', label: '草稿' },
      { value: '1', label: '启用' },
    ],
  } as LovMetaItem;
}

function listItem(): LovListMeta {
  return {
    lovCode: 'List.Test.User',
    type: 'LIST',
    name: '用户',
    valueField: 'id',
    labelField: 'name',
    listConfig: null,
    searchFields: [],
    tableColumns: [],
  } as unknown as LovListMeta;
}

// 配置 lovStore mock 默认实现（路径 B 使用）
vi.mocked(getMeta).mockImplementation(async (code: string) =>
  String(code).startsWith('List') ? listItem() : enumItem(),
);
vi.mocked(getCachedMeta).mockReturnValue(null);
vi.mocked(registerSelectedRow).mockImplementation(() => {});
vi.mocked(getSelectedLabel).mockImplementation((_code: string, v: string | number | undefined) =>
  v == null ? '' : `label-${v}`,
);
vi.mocked(resolveSelectedLabel).mockImplementation(async (_code: string, v: string | number | undefined) =>
  v == null ? '' : `label-${v}`,
);

describe('useLovSelect（逻辑/事件层）', () => {
  let scope: EffectScope | null = null;
  afterEach(() => {
    scope?.stop();
    scope = null;
    vi.mocked(registerSelectedRow).mockClear();
    vi.mocked(getMeta).mockClear();
  });

  /**
   * @param withFetchMeta true=注入 fetchMeta（路径 A，绕过 lovStore）；
   *                      false=走 lovStore（路径 B，验证 lovStore 集成）
   */
  function setup(initialCode = 'Enum.Test.Status', withFetchMeta = true) {
    const code = ref(initialCode);
    const modelValue = ref<string | number | string[] | undefined>(undefined);
    const multiple = ref(false);
    const fetchMeta = withFetchMeta
      ? vi.fn(async (c: string) => (c.startsWith('List') ? listMetaResponse() : enumMetaResponse()))
      : undefined;
    scope = effectScope();
    const opts: UseLovSelectOptions = { code, modelValue, multiple };
    if (fetchMeta) opts.fetchMeta = fetchMeta;
    const api = scope.run(() => useLovSelect(opts))!;
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

  it('modelValue 回显：同步到内部选择状态（ENUM auto→归一为数字·ADR-064）', async () => {
    const { api, modelValue } = setup();
    await api.loadMeta();
    modelValue.value = '1';
    await flushPromises();
    expect(api.selectedValue.value).toBe(1);
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

  // ── 路径 B：走 lovStore（生产路径），验证双缓存已消除、集成正确 ──

  it('loadMeta 走 lovStore：调用 getMeta 并解析类型/选项', async () => {
    const { api } = setup('Enum.Test.Status', false);
    expect(api.resolvedType.value).toBeNull();
    await api.loadMeta();
    expect(getMeta).toHaveBeenCalledWith('Enum.Test.Status');
    expect(getCachedMeta).toHaveBeenCalledWith('Enum.Test.Status');
    expect(api.resolvedType.value).toBe('ENUM');
    expect(api.options.value).toHaveLength(2);
    expect(api.loading.value).toBe(false);
  });

  it('onTableSelect 登记选中行到 lovStore：调用 registerSelectedRow', async () => {
    const { api } = setup('List.Test.User', false);
    await api.loadMeta();
    const row = { id: 2, name: '编辑' };
    api.onTableSelect(row);
    expect(registerSelectedRow).toHaveBeenCalledWith('List.Test.User', row, {
      valueField: 'id',
      labelField: 'name',
    });
  });
});

describe('useLovSelect 级联（parent 过滤 + 父变清子 · ADR-063）', () => {
  let scope: EffectScope | null = null;
  afterEach(() => {
    scope?.stop();
    scope = null;
  });

  function parentMeta(): LovMetaResponse {
    return {
      meta: [
        {
          lovCode: 'Enum.Test.OrderType',
          type: 'ENUM',
          name: '工单类型',
          options: [
            { value: '1', label: '产线停机', parent: '1' },
            { value: '2', label: '设备故障', parent: '1' },
            { value: '3', label: '例行维保', parent: '2' },
          ],
        },
      ],
      inlineEnums: {},
    };
  }

  function setupCascade(parentVal?: number) {
    const code = ref('Enum.Test.OrderType');
    const modelValue = ref<string | number | string[] | undefined>(undefined);
    const multiple = ref(false);
    const parent = ref<string | number | undefined>(parentVal);
    const fetchMeta = vi.fn(async () => parentMeta());
    scope = effectScope();
    const api = scope.run(() => useLovSelect({ code, modelValue, multiple, parent, fetchMeta }))!;
    return { api, parent };
  }

  it('无 parent → filteredOptions 等于全量 options', async () => {
    const { api } = setupCascade(undefined);
    await api.loadMeta();
    expect(api.options.value).toHaveLength(3);
    expect(api.filteredOptions.value).toHaveLength(3);
  });

  it('有 parent → filteredOptions 只含该父的子项（本地过滤·不重新请求·ENUM 值归一为数字）', async () => {
    const { api } = setupCascade(1);
    await api.loadMeta();
    expect(api.filteredOptions.value.map((o) => o.value)).toEqual([1, 2]);
  });

  it('父值变化 → 清空不再属于新父的已选子值', async () => {
    const { api, parent } = setupCascade(1);
    await api.loadMeta();
    api.selectedValue.value = '3'; // 属于 parent=2 的类型
    parent.value = 2; // 先合法
    await flushPromises();
    expect(api.selectedValue.value).toBe('3');
    parent.value = 1; // 切到 parent=1（合法只剩 1/2）→ 应清空
    await flushPromises();
    expect(api.selectedValue.value).toBeUndefined();
  });
});

describe('useLovSelect 输出值类型 valueType（承 ADR-064）', () => {
  let scope: EffectScope | null = null;
  afterEach(() => {
    scope?.stop();
    scope = null;
  });

  function enumStatusMeta(): LovMetaResponse {
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

  function setupEnum(valueType: 'auto' | 'number' | 'string', initial?: string | number) {
    const code = ref('Enum.Test.Status');
    const modelValue = ref<string | number | string[] | undefined>(initial);
    const multiple = ref(false);
    const vt = ref<'auto' | 'number' | 'string'>(valueType);
    const fetchMeta = vi.fn(async () => enumStatusMeta());
    scope = effectScope();
    const api = scope.run(() => useLovSelect({ code, modelValue, multiple, valueType: vt, fetchMeta }))!;
    return { api, modelValue };
  }

  function setupList(valueType: 'auto' | 'number' | 'string') {
    const code = ref('List.Test.User');
    const modelValue = ref<string | number | string[] | undefined>(undefined);
    const multiple = ref(false);
    const vt = ref<'auto' | 'number' | 'string'>(valueType);
    const fetchMeta = vi.fn(async () => listMetaResponse());
    scope = effectScope();
    const api = scope.run(() => useLovSelect({ code, modelValue, multiple, valueType: vt, fetchMeta }))!;
    return { api };
  }

  it('ENUM auto → 选项值默认出数字', async () => {
    const { api } = setupEnum('auto');
    await api.loadMeta();
    expect(api.filteredOptions.value.map((o) => o.value)).toEqual([0, 1]);
  });

  it('ENUM string → 选项值保持字符串（旧行为显式回退）', async () => {
    const { api } = setupEnum('string');
    await api.loadMeta();
    expect(api.filteredOptions.value.map((o) => o.value)).toEqual(['0', '1']);
  });

  it('ENUM auto → 外部传入字符串 modelValue 回显归一为数字', async () => {
    const { api, modelValue } = setupEnum('auto', '1');
    await api.loadMeta();
    expect(api.selectedValue.value).toBe(1);
    modelValue.value = '0';
    await flushPromises();
    expect(api.selectedValue.value).toBe(0);
  });

  it('LIST auto → 保持行数据原生类型（不转数字）', async () => {
    const { api } = setupList('auto');
    await api.loadMeta();
    expect(api.onTableSelect({ id: '7', name: '张三' })).toBe('7');
  });

  it('LIST number → onTableSelect 把行 id 归一为数字', async () => {
    const { api } = setupList('number');
    await api.loadMeta();
    expect(api.onTableSelect({ id: '7', name: '张三' })).toBe(7);
  });
});
