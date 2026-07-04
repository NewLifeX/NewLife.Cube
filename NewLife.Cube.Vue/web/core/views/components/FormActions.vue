<script setup lang="ts">
interface Props {
  showContinue?: boolean;
}

withDefaults(defineProps<Props>(), {
  showContinue: true,
});

const emit = defineEmits<{
  submit: [];
  continue: [];
  cancel: [];
}>();
</script>

<template>
  <div class="form-actions">
    <span class="fa-hint">* 为必填字段</span>
    <div class="fa-buttons">
      <button class="fa-btn fa-btn--cancel" type="button" @click="emit('cancel')">取消</button>
      <button
        v-if="showContinue"
        class="fa-btn fa-btn--continue"
        type="button"
        @click="emit('continue')"
      >
        保存并继续
      </button>
      <button class="fa-btn fa-btn--primary" type="button" @click="emit('submit')">保存</button>
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
    color: #fff;
    border: none;

    &:hover {
      background: #165c32;
    }
  }

  &--continue {
    background: var(--el-color-primary-light-9);
    color: var(--el-color-primary);
    border: 1px solid var(--el-color-primary-light-8);

    &:hover {
      background: #c8e8d5;
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
}
</style>
