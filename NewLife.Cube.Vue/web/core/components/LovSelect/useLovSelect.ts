import { ref, computed, watch, type Ref } from 'vue';
import type { LovMetaItem, LovEnumOption, LovListMeta, LovMetaResponse } from '@newlifex/cube-vue/core/types/lov';
import { fetchLovMeta } from '@newlifex/cube-vue/core/utils/lov-api';

/** LovSelect 对外 emit 的 modelValue 类型 */
export type LovValue = string | number | string[] | undefined;

export interface UseLovSelectOptions {
  /** 值集编码（响应式） */
  code: Ref<string>;
  /** 当前值（响应式） */
  modelValue: Ref<LovValue>;
  /** 是否多选（响应式） */
  multiple: Ref<boolean>;
  /**
   * 可注入的 meta 加载函数，便于单测隔离（默认走真实 fetchLovMeta）。
   * 测试时传入确定性 mock，无需 mock 整个 lov-api 网络层。
   */
  fetchMeta?: (code: string) => Promise<LovMetaResponse>;
}

export interface UseLovSelectReturn {
  loading: Ref<boolean>;
  lovMeta: Ref<LovMetaItem | null>;
  resolvedType: Ref<'ENUM' | 'LIST' | null>;
  options: Ref<LovEnumOption[]>;
  selectedValue: Ref<string | number | undefined>;
  selectedValues: Ref<string[]>;
  dialogVisible: Ref<boolean>;
  displayText: Ref<string>;
  listMeta: Ref<LovListMeta | null>;
  metaInlineEnums: Ref<Record<string, LovEnumOption[]>>;
  /** 翻译缓存，与 LovSelectTable 共享引用 */
  translateCache: Map<string, string>;
  /** 加载并解析元数据（组件在 onMounted 中调用；hook 本身不含生命周期，便于纯逻辑单测） */
  loadMeta: () => Promise<void>;
  /** 枚举单选：返回应 emit 的值（组件负责 emit） */
  onEnumChange: (val: string | number | undefined) => LovValue;
  /** 枚举多选：返回应 emit 的值 */
  onEnumMultiChange: (vals: string[]) => LovValue;
  /** 列表单选：根据行计算值+标签、更新 displayText、关闭弹窗，返回应 emit 的值 */
  onTableSelect: (row: Record<string, unknown>) => LovValue;
  /** 列表多选确认：更新选中集合、关闭弹窗，返回应 emit 的值 */
  onTableMultiConfirm: (vals: string[]) => LovValue;
  openDialog: () => void;
  closeDialog: () => void;
  /** 将外部 modelValue 同步到内部选择状态（供 watch 调用） */
  syncFromModelValue: () => void;
}

// 全局 meta 缓存（与后端值集配置一一对应，跨实例共享，避免重复请求）
const metaCache = new Map<string, LovMetaResponse>();

/**
 * LovSelect 的逻辑 + 事件层（与 UI 模板解耦）。
 *
 * 设计要点：
 *  - 不依赖 Vue 的 emit，所有「事件」以「返回值」形式暴露（onEnumChange/onTableSelect/...），
 *    组件只负责把返回值转成 emit —— 因此本 hook 可在 Vitest 中脱离渲染直接测试。
 *  - fetchMeta 可注入，测试传入确定性 mock 即可，无需 mock 整个 lov-api 网络层。
 *  - watch 留在 hook 内（类型解析、code 切换重载、modelValue 回显），属于可测的纯逻辑。
 *  - 不含 onMounted，加载动作由组件在 onMounted 中显式调用 loadMeta()。
 */
export function useLovSelect(opts: UseLovSelectOptions): UseLovSelectReturn {
  const fetchMeta = opts.fetchMeta ?? fetchLovMeta;

  const loading = ref(false);
  const lovMeta = ref<LovMetaItem | null>(null);
  const metaInlineEnums = ref<Record<string, LovEnumOption[]>>({});
  const resolvedType = ref<'ENUM' | 'LIST' | null>(null);

  const selectedValue = ref<string | number | undefined>(
    Array.isArray(opts.modelValue.value)
      ? undefined
      : (opts.modelValue.value as string | number | undefined),
  );
  const selectedValues = ref<string[]>(
    Array.isArray(opts.modelValue.value) ? (opts.modelValue.value as string[]) : [],
  );
  const options = ref<LovEnumOption[]>([]);

  const dialogVisible = ref(false);
  const displayText = ref('');
  const listMeta = computed<LovListMeta | null>(() =>
    lovMeta.value?.type === 'LIST' ? (lovMeta.value as LovListMeta) : null,
  );

  // 翻译缓存（与 LovSelectTable 共享同一个 Map 引用）
  const translateCache = new Map<string, string>();

  function applyMeta(metaData: LovMetaResponse) {
    const theMeta = metaData.meta?.[0];
    if (!theMeta) return;
    lovMeta.value = theMeta;
    metaInlineEnums.value = metaData.inlineEnums || {};
    resolvedType.value = theMeta.type;
    if (theMeta.type === 'ENUM') {
      options.value = theMeta.options || [];
    } else if (theMeta.type === 'LIST') {
      if (metaData.inlineEnums) {
        for (const [enumLovCode, items] of Object.entries(metaData.inlineEnums)) {
          for (const item of items) {
            translateCache.set(`${enumLovCode}:${item.value}`, item.label);
          }
        }
      }
    }
    // meta 解析完成（尤其是 LIST 类型）后，如果外部已经传了 modelValue，需要把 displayText 回显出来。
    // 否则 setup 阶段的 syncFromModelValue 会因为 resolvedType 尚未就绪而跳过，导致 LIST 初次回显空白。
    updateDisplayText();
  }

  async function loadMeta() {
    const code = opts.code.value;
    if (!code) return;
    if (metaCache.has(code)) {
      applyMeta(metaCache.get(code)!);
      return;
    }
    loading.value = true;
    try {
      const json = await fetchMeta(code);
      metaCache.set(code, json);
      applyMeta(json);
    } catch (err) {
      console.error('LovSelect: 加载元数据失败', err);
    } finally {
      loading.value = false;
    }
  }

  function onEnumChange(val: string | number | undefined): LovValue {
    return val;
  }

  function onEnumMultiChange(vals: string[]): LovValue {
    return vals;
  }

  function onTableSelect(row: Record<string, unknown>): LovValue {
    const lov = lovMeta.value as LovListMeta | null;
    const valueField = lov?.valueField || 'id';
    const labelField = lov?.labelField || 'name';
    const val = row[valueField] as string | number | undefined;
    selectedValue.value = val;
    displayText.value = row[labelField] != null ? String(row[labelField]) : String(val ?? '');
    dialogVisible.value = false;
    return val;
  }

  function onTableMultiConfirm(vals: string[]): LovValue {
    selectedValues.value = vals;
    dialogVisible.value = false;
    return vals;
  }

  function openDialog() {
    dialogVisible.value = true;
  }
  function closeDialog() {
    dialogVisible.value = false;
  }

  function updateDisplayText() {
    if (resolvedType.value !== 'LIST' || opts.modelValue.value == null) return;
    const cacheKey = `${opts.code.value}:${opts.modelValue.value}`;
    displayText.value = translateCache.get(cacheKey) || String(opts.modelValue.value);
  }

  function syncFromModelValue() {
    const val = opts.modelValue.value;
    if (Array.isArray(val)) {
      selectedValues.value = val as string[];
    } else {
      selectedValue.value = val as string | number | undefined;
    }
    updateDisplayText();
  }

  watch(opts.code, (newCode, oldCode) => {
    if (newCode && newCode !== oldCode) {
      loading.value = false;
      lovMeta.value = null;
      resolvedType.value = null;
      options.value = [];
      displayText.value = '';
      loadMeta();
    }
  });

  watch(opts.modelValue, () => {
    syncFromModelValue();
  });

  return {
    loading,
    lovMeta,
    resolvedType,
    options,
    selectedValue,
    selectedValues,
    dialogVisible,
    displayText,
    listMeta,
    metaInlineEnums,
    translateCache,
    loadMeta,
    onEnumChange,
    onEnumMultiChange,
    onTableSelect,
    onTableMultiConfirm,
    openDialog,
    closeDialog,
    syncFromModelValue,
  };
}
