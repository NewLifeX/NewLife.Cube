import { computed, ref } from 'vue';
import type { ControlType, FieldMeta } from '@/core/types/field';
import {
  resolveControl,
  resolveNumberPrecision,
  resolveNumberStep,
} from '@/core/utils/fieldControl';
import { normalizeDataSource } from '@/core/utils/viewMapping';
import {
  type DateKind,
  fromPickerValue,
  inferDateKind,
  toPickerValue,
} from '@/core/utils/datetime';
import { bitmaskToKeys, isBitmaskMultiSelect, keysToBitmask } from '@/core/utils/bitmaskSelect';
import cubeApi from '@/api';

/** FieldInput 组件 props 类型（与 FieldInput.vue defineProps 泛型逐字一致） */
interface FieldInputProps {
  field: FieldMeta;
  modelValue?: unknown;
  disabled?: boolean;
  /** 上传所属实体路径 */
  typePath?: string;
  controlOverride?: ControlType;
}

/** FieldInput 组件 emits 类型（与 FieldInput.vue defineEmits 泛型逐字一致） */
interface FieldInputEmits {
  'update:modelValue': [unknown];
}

type FieldInputEmit = <K extends keyof FieldInputEmits>(event: K, ...args: FieldInputEmits[K]) => void;

/** FieldInput 组件全部业务 TS：控件解析、取值回写与上传（自 FieldInput.vue script setup 原样搬移） */
export function useFieldInput(props: FieldInputProps, emit: FieldInputEmit) {
  const control = computed(
    () => props.controlOverride ?? resolveControl(props.field),
  );
  const strValue = computed(() =>
    props.modelValue == null ? '' : String(props.modelValue),
  );
  const numValue = computed(() => {
    if (props.modelValue == null || props.modelValue === '') return undefined;
    return Number(props.modelValue);
  });
  const displayText = computed(() => strValue.value || '-');
  const precision = computed(() => resolveNumberPrecision(props.field));
  const step = computed(() => resolveNumberStep(props.field));
  /** 枚举/状态字典：按 label 去重并优先数字键（后端 PrepareForApi 同时物化数字键与名称键） */
  const dsNorm = computed(() =>
    props.field.dataSource && Object.keys(props.field.dataSource).length
      ? normalizeDataSource(props.field.dataSource)
      : null,
  );
  const selectOptions = computed(() => dsNorm.value?.options ?? []);
  const selectValue = computed(() => {
    if (control.value === 'selectMulti') {
      if (isBitmaskMultiSelect(props.field)) {
        const keys = selectOptions.value.map((o) => String(o.value));
        return bitmaskToKeys(props.modelValue, keys);
      }
      if (Array.isArray(props.modelValue)) return props.modelValue.map(String);
      if (props.modelValue == null || props.modelValue === '') return [];
      return String(props.modelValue)
        .split(',')
        .map((s) => s.trim())
        .filter(Boolean);
    }
    if (props.modelValue == null || props.modelValue === '') return undefined;
    const s = String(props.modelValue);
    // 回显兼容：名称键（如「男」）映射回规范数字键（如「1」），避免选中态丢失
    return dsNorm.value?.canonicalByKey.get(s) ?? s;
  });

  /** 日期种类与 picker 字符串值：壁钟时间，避免时区漂移 */
  const dateKind = computed<DateKind>(() =>
    control.value === 'datePicker' || control.value === 'timePicker'
      ? inferDateKind(props.field)
      : 'datetime',
  );
  const pickerFormat = computed(() =>
    dateKind.value === 'date' ? 'YYYY-MM-DD' : dateKind.value === 'time' ? 'HH:mm:ss' : 'YYYY-MM-DD HH:mm:ss',
  );
  const pickerValue = computed(() => {
    if (control.value !== 'datePicker' && control.value !== 'timePicker') return undefined;
    if (props.modelValue == null || props.modelValue === '') return undefined;
    return toPickerValue(props.modelValue, dateKind.value);
  });

  const inputType = computed(() => {
    switch (control.value) {
      case 'email':
        return 'email';
      case 'tel':
        return 'tel';
      case 'url':
        return 'url';
      default:
        return 'text';
    }
  });

  function emitValue(v: unknown) {
    emit('update:modelValue', v);
  }

  /** 颜色控件：色块 + 隐藏 native color input + 色号（对齐外观设置自定义主色） */
  const colorInputRef = ref<HTMLInputElement | null>(null);
  const colorValue = computed(() => strValue.value || '#000000');
  function openColorPicker() {
    if (props.disabled) return;
    colorInputRef.value?.click();
  }
  function onColorInput(e: Event) {
    const v = (e.target as HTMLInputElement).value;
    emitValue(v);
  }

  /** picker 输出 → naive 本地字符串提交后端 */
  function onPickerChange(v: unknown) {
    if (v == null || v === '') {
      emitValue(undefined);
      return;
    }
    emitValue(fromPickerValue(v, dateKind.value));
  }

  /** 下拉值尽量还原数值/布尔，兼容实体字段类型；Int64 超安全整数保留字符串避免精度丢失（OSC-0009） */
  function onSelect(v: unknown) {
    if (control.value === 'selectMulti') {
      if (isBitmaskMultiSelect(props.field)) {
        emitValue(keysToBitmask(v));
        return;
      }
      emitValue(Array.isArray(v) ? v.map(String) : []);
      return;
    }
    if (v == null || v === '') {
      emitValue(undefined);
      return;
    }
    const s = String(v);
    const tn = props.field.typeName;
    if (tn === 'Boolean') {
      emitValue(s === 'true' || s === '1');
      return;
    }
    if (tn === 'Int64' || tn === 'UInt64') {
      const n = Number(s);
      emitValue(/^-?\d+$/.test(s.trim()) && Number.isSafeInteger(n) ? n : s);
      return;
    }
    if (tn === 'Int32' || tn === 'Decimal' || tn === 'Double' || tn === 'Single') {
      const n = Number(s);
      emitValue(Number.isNaN(n) ? s : n);
      return;
    }
    emitValue(s);
  }

  async function onUpload(option: { fileItem: { file?: File }; onSuccess: () => void; onError: () => void }) {
    const file = option.fileItem.file;
    if (!file || !props.typePath) {
      option.onError();
      return;
    }
    try {
      const res = await cubeApi.page.uploadFile(props.typePath, file, { id: 0 });
      const data = (res.data || {}) as Record<string, unknown>;
      const url =
        data.url ??
        data.filePath ??
        data.path ??
        data.Url ??
        data.FilePath ??
        (typeof res.data === 'string' ? res.data : null);
      if (!url) {
        option.onError();
        return;
      }
      emitValue(String(url));
      option.onSuccess();
    } catch {
      option.onError();
    }
  }

  return {
    control,
    displayText,
    strValue,
    emitValue,
    numValue,
    precision,
    step,
    pickerValue,
    dateKind,
    pickerFormat,
    onPickerChange,
    selectValue,
    selectOptions,
    onSelect,
    onUpload,
    inputType,
    colorInputRef,
    colorValue,
    openColorPicker,
    onColorInput,
  };
}
