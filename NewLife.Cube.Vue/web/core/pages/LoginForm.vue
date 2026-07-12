<script setup lang="ts">
import { ref } from 'vue';
import { ElMessage } from 'element-plus';
import { User, Lock } from '@element-plus/icons-vue';
import { usePasswordRules } from '../composables/usePasswordRules';
import type { LoginConfig } from '@cube/api-core';

const props = defineProps<{
  /** 后端登录配置（主要消费 security.passwordStrength 动态生成密码规则） */
  loginConfig?: LoginConfig;
}>();

const emit = defineEmits<{
  /** 客户端校验通过后，把凭据交给容器处理登录 */
  (e: 'submit', payload: { username: string; password: string }): void;
}>();

// 表单状态（展示组件内部维护，不向上依赖）
const form = ref<{ username: string; password: string }>({ username: '', password: '' });
const errors = ref<{ username?: string; password?: string }>({});
const submitting = ref<boolean>(false);

// 密码规则：由 strength + 当前输入派生（逻辑来自 usePasswordRules，可独立单测）
const { passwordRuleDefs, passwordRules, showPasswordHints } = usePasswordRules(
  () => props.loginConfig?.security?.passwordStrength,
  () => form.value.password,
);

function clearError(field: 'username' | 'password'): void {
  errors.value[field] = undefined;
}

/**
 * 客户端校验（用户名 + 动态密码规则）。
 * 校验通过才 emit('submit')，容器只负责与服务端交互。
 */
function handleSubmit(): void {
  errors.value = {};
  let valid = true;
  const username = form.value.username.trim();
  const password = form.value.password;

  if (!username) {
    errors.value.username = '请输入用户名';
    valid = false;
  } else if (username.length < 2) {
    errors.value.username = '用户名至少 2 个字符';
    valid = false;
  }

  if (!password) {
    errors.value.password = '请输入密码';
    valid = false;
  } else {
    // 动态规则：根据后端 passwordStrength 生成；无规则（* 或空）时回退为「至少 5 位」兜底
    const failed = passwordRuleDefs.value.find((r) => !r.test(password));
    if (failed) {
      errors.value.password = `密码需${failed.label}`;
      valid = false;
    }
  }

  if (!valid) {
    ElMessage.warning('请检查输入信息');
    return;
  }

  // 校验通过，把凭据交给容器处理登录
  emit('submit', { username: form.value.username, password: form.value.password });
}
</script>

<template>
  <form class="login-form" @submit.prevent="handleSubmit">
    <!-- 用户名 -->
    <div class="input-group">
      <label class="input-label" for="login-username">用户名</label>
      <el-input
        id="login-username"
        data-cy="username"
        v-model="form.username"
        placeholder="请输入用户名"
        size="large"
        :prefix-icon="User"
        clearable
        :class="{ 'is-error': !!errors.username }"
        @input="clearError('username')"
      />
      <span v-if="errors.username" class="input-error">{{ errors.username }}</span>
    </div>

    <!-- 密码 -->
    <div class="input-group">
      <label class="input-label" for="login-password">密码</label>
      <el-input
        id="login-password"
        data-cy="password"
        v-model="form.password"
        type="password"
        placeholder="请输入密码"
        size="large"
        :prefix-icon="Lock"
        show-password
        :class="{ 'is-error': !!errors.password }"
        @input="clearError('password')"
        @keyup.enter="handleSubmit"
      />
      <span v-if="errors.password" class="input-error">{{ errors.password }}</span>

      <!-- 密码强度提示（实时反馈） -->
      <div v-if="showPasswordHints" class="password-hints">
        <div
          v-for="(rule, idx) in passwordRules"
          :key="idx"
          class="password-hint-item"
          :class="{ satisfied: rule.satisfied }"
        >
          <span class="hint-icon">{{ rule.satisfied ? '✓' : '○' }}</span>
          <span class="hint-text">{{ rule.label }}</span>
        </div>
      </div>
    </div>

    <!-- 登录按钮 -->
    <button
      type="submit"
      class="login-btn"
      :class="{ loading: submitting }"
      :disabled="submitting"
    >
      <span class="login-btn-text">登 录</span>
      <span class="login-btn-spinner"></span>
    </button>
  </form>
</template>

<style scoped>
.login-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

/* 输入框组 */
.input-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.input-label {
  font-size: 13px;
  font-weight: 500;
  color: var(--el-text-color-regular);
  letter-spacing: 0.3px;
}

/* ── el-input 样式覆盖 ── */
.login-form :deep(.el-input__wrapper) {
  background: var(--el-color-primary-light-9);
  border: 1px solid var(--el-border-color);
  border-radius: 12px;
  height: 48px;
  padding: 0 16px;
  box-shadow: none !important;
  transition: background 0.25s ease, border-color 0.25s ease, box-shadow 0.25s ease;
}

.login-form :deep(.el-input__wrapper:hover) {
  border-color: var(--el-color-primary-light-5);
}

.login-form :deep(.el-input__wrapper.is-focus) {
  background: var(--el-bg-color-overlay);
  border-color: var(--el-color-primary);
  box-shadow: 0 0 0 3px var(--el-color-primary-light-8) !important;
}

.login-form :deep(.el-input__prefix-inner) {
  color: var(--el-text-color-placeholder);
  transition: color 0.2s ease;
}

.login-form :deep(.el-input__wrapper.is-focus .el-input__prefix-inner) {
  color: var(--el-color-primary);
}

.login-form :deep(.el-input__inner) {
  color: var(--el-text-color-primary);
}

.login-form :deep(.el-input__inner::placeholder) {
  color: var(--el-text-color-placeholder);
}

/* 错误状态 */
.login-form :deep(.el-input.is-error .el-input__wrapper) {
  border-color: var(--el-color-danger) !important;
  box-shadow: 0 0 0 3px var(--el-color-danger-light-8) !important;
}

.input-error {
  font-size: 12px;
  color: var(--el-color-danger);
}

/* ── 密码强度提示 ── */
.password-hints {
  margin-top: 6px;
  display: flex;
  flex-direction: column;
  gap: 3px;
  padding: 0 2px;
}

.password-hint-item {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: var(--el-text-color-placeholder);
  transition: color 0.25s ease;
  line-height: 1.5;
}

.password-hint-item.satisfied {
  color: var(--el-color-success);
}

.hint-icon {
  width: 14px;
  text-align: center;
  font-size: 11px;
  flex-shrink: 0;
}

.hint-text {
  flex: 1;
}

/* ==================== 登录按钮 ==================== */
.login-btn {
  width: 100%;
  height: 48px;
  border: none;
  border-radius: 12px;
  background: linear-gradient(135deg, var(--el-color-primary), var(--el-color-primary-dark-2));
  color: var(--el-color-white);
  font-size: 15px;
  font-weight: 600;
  font-family: inherit;
  cursor: pointer;
  transition: box-shadow 0.3s ease, transform 0.15s ease;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  overflow: hidden;
  letter-spacing: 0.3px;
  box-shadow: 0 4px 12px var(--el-color-primary-light-7);
}

.login-btn:hover {
  box-shadow: 0 8px 24px var(--el-color-primary-light-5);
  transform: translateY(-1px);
}

.login-btn:active {
  transform: translateY(0);
}

.login-btn:disabled {
  cursor: not-allowed;
  opacity: 0.8;
}

/* Loading 状态：隐藏文字，显示 spinner */
.login-btn.loading .login-btn-text {
  opacity: 0;
}

.login-btn-spinner {
  display: none;
  position: absolute;
  width: 20px;
  height: 20px;
  border: 2px solid var(--el-color-white);
  border-top-color: transparent;
  border-radius: 50%;
  opacity: 0.5;
  animation: btn-spin 0.7s linear infinite;
}

.login-btn.loading .login-btn-spinner {
  display: block;
}

@keyframes btn-spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
