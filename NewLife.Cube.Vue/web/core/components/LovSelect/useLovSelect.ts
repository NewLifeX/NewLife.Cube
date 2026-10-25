import { ref, computed, watch, type Ref, type ComputedRef } from 'vue';
import type { LovMetaItem, LovEnumOption, LovListMeta, LovSelectOption, LovValueType } from '@newlifex/cube-vue/core/types/lov';
import {
  getMeta,
  getCachedMeta,
  registerSelectedRow,
  getSelectedLabel,
  resolveSelectedLabel,
} from '@newlifex/cube-vue/core/components/LovSelect/lovStore';

/** LovSelect 对外 emit 的 modelValue 类型（多选可为 string[] 或 number[]·承 ADR-064） */
export type LovValue = string | number | Array<string | number> | undefined;

export interface UseLovSelectOptions {
  /** 值集编码（响应式） */
  code: Ref<string>;
  /** 当前值（响应式） */
  modelValue: Ref<LovValue>;
  /** 是否多选（响应式） */
  multiple: Ref<boolean>;
  /** 级联父级值（响应式，可选）。非空时按 option.parent 过滤 ENUM 选项，父变则清空不再合法的子选择 */
  parent?: Ref<string | number | undefined>;
  /**
   * 输出值类型（响应式，可选·承 ADR-064）。
   * auto（默认）=ENUM 出数字·LIST 原生；number/string=强制归一。
   * 归一同时作用于「下拉选项 value」与「外部传入 modelValue」，保证选中/回显/emit 三者类型一致。
   */
  valueType?: Ref<LovValueType>;
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
  /** 按 parent 过滤 + 按 valueType 归一后的可选项（渲染 el-option 用·无 parent 时=全量·承 ADR-064） */
  filteredOptions: ComputedRef<LovSelectOption[]>;
  selectedValue: Ref<string | number | undefined>;
  selectedValues: Ref<Array<string | number>>;
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
  onEnumMultiChange: (vals: Array<string | number>) => LovValue;
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
  const selectedValues = ref<Array<string | number>>(
    Array.isArray(opts.modelValue.value) ? [...(opts.modelValue.value as Array<string | number>)] : [],
  );
  const options = ref<LovEnumOption[]>([]);

  // ── 输出值类型归一（承 ADR-064）：同一 mode 同时作用于「选项 value」与「外部 modelValue」，
  //    保证 el-select 选中匹配 / 回显 / emit 三者类型一致。auto：ENUM→number，LIST→native。
  function outputMode(): 'number' | 'string' | 'native' {
    const v = opts.valueType?.value ?? 'auto';
    if (v === 'number') return 'number';
    if (v === 'string') return 'string';
    return resolvedType.value === 'ENUM' ? 'number' : 'native';
  }
  function applyMode(raw: string | number, m: 'number' | 'string' | 'native'): string | number {
    if (m === 'native') return raw;
    if (m === 'number') {
      const n = Number(raw);
      return Number.isNaN(n) ? raw : n;
    }
    return String(raw);
  }
  /** 按当前 mode 归一单值（null/'' 原样返回，交由必填/清空逻辑处理） */
  function coerce(v: string | number | undefined): string | number | undefined {
    if (v == null || v === '') return v;
    return applyMode(v, outputMode());
  }

  const filteredOptions = computed<LovSelectOption[]>(() => {
    const p = opts.parent?.value;
    const list =
      p == null || p === ''
        ? options.value
        : options.value.filter((o) => o.parent != null && String(o.parent) === String(p));
    const m = outputMode();
    return list.map((o) => ({ value: applyMode(o.value, m), label: o.label }));
  });

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
    // meta 解析完成后，若外部已传 modelValue：按 valueType 归一后同步选择态并回显（承 ADR-064）
    syncFromModelValue();
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
        console.error('LovSelect: failed to load metadata', err);
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
      syncFromModelValue();
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
      syncFromModelValue();
    } catch (err) {
      console.error('LovSelect: failed to load metadata', err);
    } finally {
      loading.value = false;
    }
  }

  function onEnumChange(val: string | number | undefined): LovValue {
    return val;
  }

  function onEnumMultiChange(vals: Array<string | number>): LovValue {
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

    const out = coerce(val); // 承 ADR-064：按 valueType 归一 LIST 选中值
    selectedValue.value = out;
    displayText.value = row[labelField] != null ? String(row[labelField]) : String(val ?? '');
    dialogVisible.value = false;
    return out;
  }

  function onTableMultiConfirm(vals: string[]): LovValue {
    const m = outputMode();
    const out = vals.map((v) => applyMode(v, m)); // 承 ADR-064：按 valueType 归一多选值
    selectedValues.value = out;
    dialogVisible.value = false;
    // 异步补全 displayText（lovStore 会在 listData 加载后自动映射 label）
    updateDisplayText();
    return out;
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
    // 空串/空数组同属「未选中」，不解析 label（否则会把空值当成待翻译的原始值）
    if (val === '' || (Array.isArray(val) && val.length === 0)) {
      displayText.value = '';
      return;
    }

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
    const m = outputMode();
    if (Array.isArray(val)) {
      selectedValues.value = val.filter((v) => v != null && v !== '').map((v) => applyMode(v, m));
    } else {
      // 空串/空值代表「未选中」，不能交给 applyMode 归一：ENUM 下 Number('') === 0，
      // 会把值为 0 的选项（如状态「草稿」）误显示成已选中——外部清空筛选条件后下拉框仍显示旧值。
      selectedValue.value = val == null || val === '' ? undefined : applyMode(val, m);
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

  // 级联：父值变化后，清掉不再属于当前父的已选子值（单选+多选）
  if (opts.parent) {
    watch(opts.parent, () => {
      const inSet = (v: string | number | undefined) =>
        v != null && v !== '' && filteredOptions.value.some((o) => String(o.value) === String(v));
      if (selectedValue.value != null && !inSet(selectedValue.value)) {
        selectedValue.value = undefined;
        displayText.value = '';
      }
      if (selectedValues.value.length) {
        selectedValues.value = selectedValues.value.filter((v) => inSet(v));
      }
    });
  }

  return {
    loading,
    lovMeta,
    resolvedType,
    options,
    filteredOptions,
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