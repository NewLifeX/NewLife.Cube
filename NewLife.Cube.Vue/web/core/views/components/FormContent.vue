<script setup lang="ts">
interface SelectOption {
  value: string;
  label: string;
}

interface FormField {
  key: string;
  label: string;
  type: 'text' | 'email' | 'tel' | 'select' | 'textarea' | 'radio' | 'switch' | 'datetime';
  required?: boolean;
  fullWidth?: boolean;
  placeholder?: string;
  options?: SelectOption[];
  error?: string;
}

interface Props {
  title?: string;
  fields?: FormField[];
  modelValue?: Record<string, unknown>;
}

const props = withDefaults(defineProps<Props>(), {
  title: '基本信息',
  fields: () =>
    [
      { key: 'username', label: '用户名', type: 'text', required: true },
      { key: 'realname', label: '真实姓名', type: 'text' },
      { key: 'email', label: '邮箱', type: 'email', required: true },
      { key: 'phone', label: '手机号', type: 'tel' },
      { key: 'remark', label: '备注', type: 'textarea', fullWidth: true },
    ] satisfies FormField[],
  modelValue: () => ({}),
});

const emit = defineEmits<{
  'update:modelValue': [val: Record<string, unknown>];
}>();

function updateField(key: string, value: unknown) {
  emit('update:modelValue', { ...props.modelValue, [key]: value });
}

function getValue<T = any>(key: string): T {
  return (props.modelValue?.[key] as T) ?? ('' as T);
}
</script>

<template>
  <div class="form-content">
    <div class="fmc-header">
      <h2 class="fmc-title">{{ title }}</h2>
    </div>
    <div class="fmc-body">
      <div class="fmc-grid">
        <div
          v-for="field in fields"
          :key="field.key"
          class="fmc-field"
          :class="{ 'fmc-field--full': field.fullWidth }"
        >
          <label :for="`fmc-${field.key}`" class="fmc-label">
            {{ field.label }}
            <span v-if="field.required" class="fmc-required" aria-hidden="true">*</span>
          </label>

          <textarea
            v-if="field.type === 'textarea'"
            :id="`fmc-${field.key}`"
            class="fmc-input fmc-textarea"
            :class="{ 'fmc-input--error': field.error }"
            :placeholder="field.placeholder ?? '请输入...'"
            :value="String(getValue(field.key))"
            rows="3"
            @input="updateField(field.key, ($event.target as HTMLTextAreaElement).value)"
          ></textarea>

          <select
            v-else-if="field.type === 'select'"
            :id="`fmc-${field.key}`"
            class="fmc-input"
            :class="{ 'fmc-input--error': field.error }"
            :value="String(getValue(field.key))"
            @change="updateField(field.key, ($event.target as HTMLSelectElement).value)"
          >
            <option value="">请选择</option>
            <option v-for="opt in field.options" :key="opt.value" :value="opt.value">
              {{ opt.label }}
            </option>
          </select>

          <div v-else-if="field.type === 'radio'" class="fmc-radio-group">
            <label v-for="opt in field.options" :key="opt.value" class="fmc-radio-label">
              <input
                type="radio"
                :name="field.key"
                :value="opt.value"
                :checked="getValue(field.key) === opt.value"
                @change="updateField(field.key, opt.value)"
              />
              {{ opt.label }}
            </label>
          </div>

          <!-- 开关组件 -->
          <div v-else-if="field.type === 'switch'" class="fmc-switch-wrapper">
            <el-switch
              :model-value="Boolean(getValue(field.key))"
              @change="updateField(field.key, $event)"
            />
          </div>

          <!-- 日期选择器 -->
          <el-date-picker
            v-else-if="field.type === 'datetime'"
            class="fmc-date-picker"
            type="datetime"
            :model-value="getValue(field.key)"
            placeholder="选择日期时间"
            format="YYYY-MM-DD HH:mm:ss"
            value-format="YYYY-MM-DDTHH:mm:ss"
            @update:model-value="updateField(field.key, $event)"
          />

          <input
            v-else
            :id="`fmc-${field.key}`"
            class="fmc-input"
            :class="{ 'fmc-input--error': field.error }"
            :type="field.type"
            :placeholder="field.placeholder ?? '请输入...'"
            :value="String(getValue(field.key))"
            @input="updateField(field.key, ($event.target as HTMLInputElement).value)"
          />

          <p v-if="field.error" class="fmc-error-msg" role="alert">{{ field.error }}</p>
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.form-content {
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--el-border-radius-base);
  box-shadow: var(--el-box-shadow-light);
  overflow: hidden;
}

.fmc-header {
  padding: 14px 18px;
  border-bottom: 1px solid var(--el-border-color-light);
}

.fmc-title {
  font-family: 'Libre Baskerville', Georgia, serif;
  font-size: 14px;
  font-weight: 700;
  color: var(--el-text-color-primary);
  margin: 0;
}

.fmc-body {
  padding: 20px;
}

.fmc-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px 24px;
}

.fmc-field {
  display: flex;
  flex-direction: column;

  &--full {
    grid-column: 1 / -1;
  }
}

.fmc-label {
  display: block;
  font-family: 'Fira Sans', system-ui, sans-serif;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-bottom: 5px;
  font-weight: 500;
}

.fmc-required {
  color: var(--el-color-danger);
  margin-left: 2px;
}

.fmc-input {
  width: 100%;
  height: 34px;
  padding: 0 10px;
  border: 1px solid var(--el-border-color-light);
  border-radius: 6px;
  font-family: 'Fira Sans', system-ui, sans-serif;
  font-size: 13px;
  color: var(--el-text-color-primary);
  background: var(--el-bg-color);
  box-sizing: border-box;
  transition:
    border-color 0.15s var(--ease),
    box-shadow 0.15s var(--ease);
  outline: none;

  &:focus {
    border-color: var(--el-color-primary);
    box-shadow: 0 0 0 3px rgba(29, 112, 64, 0.1);
  }

  &--error {
    border-color: var(--el-color-danger);

    &:focus {
      box-shadow: 0 0 0 3px rgba(220, 38, 38, 0.1);
    }
  }
}

.fmc-textarea {
  height: auto;
  padding: 8px 10px;
  resize: vertical;
  min-height: 80px;
}

.fmc-radio-group {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
  padding-top: 6px;
}

.fmc-radio-label {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-family: 'Fira Sans', system-ui, sans-serif;
  font-size: 13px;
  color: var(--el-text-color-primary);
  cursor: pointer;
}

.fmc-switch-wrapper {
  display: flex;
  align-items: center;
  padding-top: 2px;
}

.fmc-date-picker {
  width: 100%;
}

.fmc-error-msg {
  margin: 4px 0 0;
  font-size: 11.5px;
  color: var(--el-color-danger);
}
</style>
