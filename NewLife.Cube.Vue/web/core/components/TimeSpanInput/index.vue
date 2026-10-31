<script setup lang="ts">
/**
 * 时长输入组件（ItemType=TimeSpan）
 *
 * 后端 `ItemType=TimeSpan` 的字段仍是整数秒（或按描述约定为毫秒/分钟/小时），
 * 直接给数字框让人填「22515」既不直观也容易填错，这里拆成「时 : 分 : 秒」三段输入，
 * 对外仍只暴露原始存储值，单位由字段描述推断（与列表格式化共用 `timeSpan.logic`）。
 */
import { computed } from 'vue';
import {
  joinTimeSpan,
  splitTimeSpan,
  timeSpanUnitOfDescription,
  type TimeSpanParts,
  type TimeSpanUnit,
} from './timeSpan.logic';

const props = withDefaults(
  defineProps<{
    /** 输入框编号（供表单 label 的 for 指向第一段） */
    id?: string;
    /** 存储值（秒 / 毫秒 / 分钟 / 小时，取决于单位） */
    modelValue?: number | string | null;
    /** 显式指定存储单位；缺省按 description 推断 */
    unit?: TimeSpanUnit;
    /** 字段描述，用于推断存储单位并提供悬停提示 */
    description?: string;
    /** 是否禁用（详情模式） */
    disabled?: boolean;
  }>(),
  {
    id: undefined,
    modelValue: null,
    unit: undefined,
    description: '',
    disabled: false,
  },
);

const emit = defineEmits<{
  (e: 'update:modelValue', value: number): void;
}>();

const unit = computed<TimeSpanUnit>(() => props.unit ?? timeSpanUnitOfDescription(props.description));
const parts = computed<TimeSpanParts>(() => splitTimeSpan(props.modelValue, unit.value));
const secondsPrecision = computed(() => (unit.value === 'milliseconds' ? 3 : 0));

/** 任一段变更后重新合成存储值；未填的段按 0 处理 */
function updatePart(part: keyof TimeSpanParts, value: number | undefined) {
  emit('update:modelValue', joinTimeSpan({ ...parts.value, [part]: value ?? 0 }, unit.value));
}
</script>

<template>
  <div class="timespan-input" :title="props.description || undefined">
    <el-input-number
      class="timespan-input__part"
      :id="props.id"
      :controls="false"
      :disabled="props.disabled"
      :min="0"
      :precision="0"
      :model-value="parts.hours"
      placeholder="HH"
      aria-label="HH"
      @update:model-value="(v: number | undefined) => updatePart('hours', v)"
    />
    <span class="timespan-input__sep">:</span>
    <el-input-number
      class="timespan-input__part"
      :controls="false"
      :disabled="props.disabled"
      :min="0"
      :max="59"
      :precision="0"
      :model-value="parts.minutes"
      placeholder="MM"
      aria-label="MM"
      @update:model-value="(v: number | undefined) => updatePart('minutes', v)"
    />
    <span class="timespan-input__sep">:</span>
    <el-input-number
      class="timespan-input__part"
      :controls="false"
      :disabled="props.disabled"
      :min="0"
      :max="59"
      :precision="secondsPrecision"
      :model-value="parts.seconds"
      placeholder="SS"
      aria-label="SS"
      @update:model-value="(v: number | undefined) => updatePart('seconds', v)"
    />
  </div>
</template>

<style scoped>
.timespan-input {
  display: flex;
  align-items: center;
  gap: 4px;
}

.timespan-input__part {
  width: 76px;
}

.timespan-input__sep {
  color: var(--el-text-color-secondary);
}
</style>
