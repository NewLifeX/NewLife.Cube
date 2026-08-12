import { ref, computed, watch, type Ref } from 'vue';
import type { LovMetaItem, LovEnumOption, LovListMeta } from '@newlifex/cube-vue/core/types/lov';
import {
  getMeta,
  getCachedMeta,
  registerSelectedRow,
  getSelectedLabel,
  resolveSelectedLabel,
} from '@newlifex/cube-vue/core/components/LovSelect/lovStore';

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
   * 可注入的 meta 加载函数，便于单测隔离。
   * 注入后会绕过 lovStore 缓存，直接调用该函数获取 meta。
   * 测试时传入确定性 mock，无需 mock 整个 lov-api 网络层。
   */
  fetchMeta?: (code: string) => Promise<{ meta: LovMetaItem[]; inlineEnums?: Record<string, LovEnumOption[]> | null }>;
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
  /** 翻译缓存（兼容旧接口，实际由 lovStore 统一管理） */
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

/**
 * LovSelect 的逻辑 + 事件层（与 UI 模板解耦）。
 *
 * 设计要点：
 *  - 不依赖 Vue 的 emit，所有「事件」以「返回值」形式暴露（onEnumChange/onTableSelect/...），
 *    组件只负责把返回值转成 emit —— 因此本 hook 可在 Vitest 中脱离渲染直接测试。
 *  - 元数据缓存统一委托给 lovStore（getMeta），避免双重缓存。
 *  - fetchMeta 可注入，测试传入确定性 mock 即可，无需 mock 整个 lov-api 网络层。
 *  - watch 留在 hook 内（类型解析、code 切换重载、modelValue 回显），属于可测的纯逻辑。
 *  - 不含 onMounted，加载动作由组件在 onMounted 中显式调用 loadMeta()。
 */
export function useLovSelect(opts: UseLovSelectOptions): UseLovSelectReturn {
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

  // 翻译缓存（兼容旧接口引用；lovStore 内部维护 labelCache 为真正权威）
  const translateCache = new Map<string, string>();

  function applyMeta(metaData: { meta: LovMetaItem[]; inlineEnums?: Record<string, LovEnumOption[]> | null }) {
    const theMeta = metaData.meta?.[0];
    if (!theMeta) return;
    lovMeta.value = theMeta;
    metaInlineEnums.value = metaData.inlineEnums || {};
    resolvedType.value = theMeta.type;
    if (theMeta.type === 'ENUM') {
      options.value = theMeta.options || [];
    } else if (theMeta.type === 'LIST') {
      // 将 inlineEnums 写入 translateCache（兼容旧接口，LovSelectTable 仍读此 Map）
      if (metaData.inlineEnums) {
        for (const [enumLovCode, items] of Object.entries(metaData.inlineEnums)) {
          for (const item of items) {
            translateCache.set(`${enumLovCode}:${item.value}`, item.label);
          }
        }
      }
    }
    // meta 解析完成后，如果外部已经传了 modelValue，需要把 displayText 回显出来。
    updateDisplayText();
  }

  async function loadMeta() {
    const code = opts.code.value;
    if (!code) return;

    // 测试注入模式：绕过 lovStore，直接调用 fetchMeta
    if (opts.fetchMeta) {
      loading.value = true;
      try {
        const json = await opts.fetchMeta(code);
        applyMeta(json);
      } catch (err) {
        console.error('LovSelect: 加载元数据失败', err);
      } finally {
        loading.value = false;
      }
      return;
    }

    // 正常模式：走 lovStore（自带缓存 + inflight 合并）
    // 先检查是否已缓存（同步），避免不必要的 loading 态
    const cached = getCachedMeta(code);
    if (cached) {
      lovMeta.value = cached;
      resolvedType.value = cached.type;
      if (cached.type === 'ENUM') {
        options.value = cached.options || [];
      }
      updateDisplayText();
      return;
    }

    loading.value = true;
    try {
      const meta = await getMeta(code);
      lovMeta.value = meta;
      resolvedType.value = meta.type;
      if (meta.type === 'ENUM') {
        options.value = meta.options || [];
      } else if (meta.type === 'LIST' && meta.inlineEnums) {
        // 把 meta 随附的 inlineEnums 写入 translateCache（关闭态外部流入回显兜底，
        // 与 applyMeta 测试注入路径的同类逻辑一致，确保 ListSingleEcho 等不打开弹窗的场景也能显示文本 label）
        for (const [enumLovCode, items] of Object.entries(meta.inlineEnums)) {
          for (const item of items) {
            translateCache.set(`${enumLovCode}:${item.value}`, item.label);
          }
        }
      }
      updateDisplayText();
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

    // 登记到 lovStore（最高优先级，确保回显精确）
    const code = opts.code.value;
    if (code) {
      registerSelectedRow(code, row, { valueField, labelField });
    }

    selectedValue.value = val;
    displayText.value = row[labelField] != null ? String(row[labelField]) : String(val ?? '');
    dialogVisible.value = false;
    return val;
  }

  function onTableMultiConfirm(vals: string[]): LovValue {
    selectedValues.value = vals;
    dialogVisible.value = false;
    // 异步补全 displayText（lovStore 会在 listData 加载后自动映射 label）
    updateDisplayText();
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
    const code = opts.code.value;
    const val = opts.modelValue.value;

    if (Array.isArray(val)) {
      // 多选：异步解析各值 label，同步先用当前缓存
      const labels: string[] = [];
      for (const v of val) {
        labels.push(getSelectedLabel(code, v));
      }
      displayText.value = labels.join('、');
      // 异步兜底：缺失的 label 由 lovStore 拉取 listData 后补全
      if (labels.some((l, i) => l === String(val[i]))) {
        Promise.all(val.map((v) => resolveSelectedLabel(code, v))).then((resolved) => {
          displayText.value = resolved.join('、');
        });
      }
    } else {
      // 单选
      displayText.value = getSelectedLabel(code, val as string | number);
      // 异步兜底
      if (displayText.value === String(val)) {
        resolveSelectedLabel(code, val as string | number).then((label) => {
          displayText.value = label;
        });
      }
    }
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