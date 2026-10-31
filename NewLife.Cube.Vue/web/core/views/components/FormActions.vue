<script setup lang="ts">
import { useI18n } from 'vue-i18n';

interface Props {
  showContinue?: boolean;
  /** 是否展示「* 为必填字段」图例。表单没有必填项时由调用方关闭，避免出现无意义的说明 */
  showRequiredHint?: boolean;
  /** 提交中。为 true 时禁用全部按钮，避免重复提交 */
  loading?: boolean;
}

withDefaults(defineProps<Props>(), {
  showContinue: true,
  showRequiredHint: true,
  loading: false,
});

const emit = defineEmits<{
  submit: [];
  continue: [];
  cancel: [];
}>();

const { t } = useI18n();
</script>

<template>
  <div class="form-actions">
    <span v-if="showRequiredHint" class="fa-hint">* {{ t('form.requiredMark') }}</span>
    <div class="fa-buttons">
      <button
        class="fa-btn fa-btn--cancel"
        type="button"
        :disabled="loading"
        @click="emit('cancel')"
      >
        {{ t('button.cancel') }}
      </button>
      <button
        v-if="showContinue"
        class="fa-btn fa-btn--continue"
        type="button"
        :disabled="loading"
        @click="emit('continue')"
      >
        {{ t('form.saveAndContinue') }}
      </button>
      <button
        class="fa-btn fa-btn--primary"
        type="button"
        :disabled="loading"
        @click="emit('submit')"
      >
        {{ t('button.save') }}
      </button>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.form-actions {
  border-top: 1px solid var(--el-border-color-light);
  padding: 16px 20px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: var(--el-bg-color-overlay);
  border-radius: 0 0 var(--el-border-radius-base) var(--el-border-radius-base);
}

.fa-hint {
  font-family: 'Fira Sans', system-ui, sans-serif;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.fa-buttons {
  display: flex;
  align-items: center;
  gap: 8px;
}

.fa-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  height: 36px;
  padding: 0 20px;
  border-radius: 6px;
  font-family: 'Fira Sans', system-ui, sans-serif;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  transition:
    background 0.15s var(--ease),
    border-color 0.15s var(--ease),
    color 0.15s var(--ease);

  &--primary {
    background: var(--el-color-primary);
    color: var(--el-color-white);
    border: none;

    &:hover {
      background: var(--el-color-primary-dark-2);
    }
  }

  &--continue {
    background: var(--el-color-primary-light-9);
    color: var(--el-color-primary);
    border: 1px solid var(--el-color-primary-light-8);

    &:hover {
      background: var(--el-color-primary-light-8);
    }
  }

  &--cancel {
    background: var(--el-bg-color-overlay);
    border: 1px solid var(--el-border-color-light);
    color: var(--el-text-color-regular);

    &:hover {
      background: var(--el-bg-color);
    }
  }

  &:disabled {
    cursor: not-allowed;
    opacity: 0.6;
  }
}
</style>
