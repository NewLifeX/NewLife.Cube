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
  border-top: 1px solid var(--bd);
  padding: 16px 20px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: var(--card);
  border-radius: 0 0 var(--r) var(--r);
}

.fa-hint {
  font-family: 'Fira Sans', system-ui, sans-serif;
  font-size: 12px;
  color: var(--t3);
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
    background: var(--ac);
    color: #fff;
    border: none;

    &:hover {
      background: #165c32;
    }
  }

  &--continue {
    background: var(--ac-l);
    color: var(--ac);
    border: 1px solid var(--ac-b);

    &:hover {
      background: #c8e8d5;
    }
  }

  &--cancel {
    background: var(--card);
    border: 1px solid var(--bd);
    color: var(--t2);

    &:hover {
      background: var(--bg);
    }
  }
}
</style>
