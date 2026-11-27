<script setup lang="ts">
import { ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { User, Lock } from '@element-plus/icons-vue';
import type { LoginConfig } from '@newlifex/cube';
import { getRememberLogin, setRememberLogin } from '../utils/tokenRefresh';

const { t } = useI18n();

const props = defineProps<{
  /** 后端登录配置。登录页不校验密码复杂度（仅要求非空），保留该属性以兼容父组件传参 */
  loginConfig?: LoginConfig;
  /** 是否正在提交（由父组件控制，展示 loading 并禁用按钮） */
  submitting?: boolean;
  /** 是否启用记住我与自动刷新联动 */
  rememberEnabled?: boolean;
}>();

const emit = defineEmits<{
  /** 客户端校验通过后，把凭据交给容器处理登录 */
  (e: 'submit', payload: { username: string; password: string; remember?: boolean }): void;
}>();

// 表单状态（展示组件内部维护，不向上依赖）
const form = ref<{ username: string; password: string }>({ username: '', password: '' });
const errors = ref<{ username?: string; password?: string }>({});
const remember = ref(getRememberLogin());

function updateRemember(value: unknown): void {
  remember.value = value === true;
  setRememberLogin(remember.value);
}

function clearError(field: 'username' | 'password'): void {
  errors.value[field] = undefined;
}

/**
 * 客户端校验（用户名 + 密码非空）。
 * 登录页不校验密码复杂度：存量用户可能使用弱密码（如默认 admin），复杂度只在新设密码时校验。
 * 校验通过才 emit('submit')，容器只负责与服务端交互。
 */
function handleSubmit(): void {
  errors.value = {};
  let valid = true;
  const username = form.value.username.trim();
  const password = form.value.password;

  if (!username) {
    errors.value.username = t('register.enterUsername');
    valid = false;
  } else if (username.length < 2) {
    errors.value.username = t('register.usernameTooShort');
    valid = false;
  }

  if (!password) {
    errors.value.password = t('register.enterPassword');
    valid = false;
  }

  if (!valid) {
    ElMessage.warning(t('register.checkInput'));
    return;
  }

  // 校验通过，把凭据交给容器处理登录
  emit('submit', {
    username: form.value.username,
    password: form.value.password,
    ...(props.rememberEnabled ? { remember: remember.value } : {}),
  });
}

/** 是否有密码输入（用于控制回车触发的表单提交） */
function onFormKeydown(e: KeyboardEvent): void {
  if (e.key === 'Enter' && !props.submitting) {
    handleSubmit();
  }
}
</script>

<template>
  <form class="login-form" @submit.prevent="handleSubmit" @keydown="onFormKeydown">
    <!-- 用户名 -->
    <div class="input-group">
      <label class="input-label" for="login-username">{{ t('register.username') }}</label>
      <el-input
        id="login-username"
        data-cy="username"
        v-model="form.username"
        :placeholder="t('register.usernamePlaceholder')"
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
      <label class="input-label" for="login-password">{{ t('register.password') }}</label>
      <el-input
        id="login-password"
        data-cy="password"
        v-model="form.password"
        type="password"
        :placeholder="t('register.enterPassword')"
        size="large"
        :prefix-icon="Lock"
        show-password
        :class="{ 'is-error': !!errors.password }"
        @input="clearError('password')"
      />
      <span v-if="errors.password" class="input-error">{{ errors.password }}</span>
    </div>

    <el-checkbox v-if="rememberEnabled" :model-value="remember" @update:model-value="updateRemember">
      {{ t('login.rememberMe', '记住我') }}
    </el-checkbox>

    <!-- 登录按钮 -->
    <button
      type="submit"
      class="login-btn"
      :class="{ loading: submitting }"
      :disabled="submitting"
    >
      <span class="login-btn-text">{{ t('login.tabLogin') }}</span>
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
