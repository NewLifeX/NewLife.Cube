<script setup lang="ts">
import { ref, computed } from 'vue';
import { ElMessage } from 'element-plus';
import { User, Lock, Message } from '@element-plus/icons-vue';
import { usePasswordRules } from '../composables/usePasswordRules';
import type { LoginConfig } from '@cube/api-core';

/**
 * 注册表单展示组件（纯展示，业务提交由容器层 LoginPage 处理）
 *
 * 支持用户名/邮箱/手机三种注册方式：
 * - 用户名注册：category=password（需用户名+密码，邮箱/手机按开关必填）
 * - 邮箱注册：category=mail（需邮箱+验证码，密码可选）
 * - 手机注册：category=mobile（需手机+验证码，密码可选）
 *
 * 开启邮箱/手机验证（requireMailVerify/requireMobileVerify）时，
 * 对应联系方式强制必填，注册后由后端进入待激活流程（发送激活邮件/短信）。
 */
const props = defineProps<{
  /** 后端登录配置（主要消费 register 能力与 security.passwordStrength） */
  loginConfig?: LoginConfig;
  /** 是否正在提交（由父组件控制，展示 loading 并禁用按钮） */
  submitting?: boolean;
}>();

const emit = defineEmits<{
  /** 客户端校验通过后，把注册数据交给容器处理 */
  (e: 'submit', payload: RegisterPayload): void;
  /** 请求发送验证码（channel: 'mail' | 'sms'，account 为邮箱或手机号） */
  (e: 'sendCode', channel: 'mail' | 'sms', account: string): void;
}>();

/** 注册提交载荷 */
export interface RegisterPayload {
  category: 'password' | 'mail' | 'mobile';
  username?: string;
  email?: string;
  mobile?: string;
  password: string;
  confirmPassword: string;
  code?: string;
  captchaId?: string;
  captchaCode?: string;
}

// 表单状态（展示组件内部维护，不向上依赖）
const form = ref<{
  username: string;
  email: string;
  mobile: string;
  password: string;
  confirmPassword: string;
  mailCode: string;
  mobileCode: string;
}>({ username: '', email: '', mobile: '', password: '', confirmPassword: '', mailCode: '', mobileCode: '' });
const errors = ref<Record<string, string | undefined>>({});

// 密码规则：由 strength + 当前输入派生
const { passwordRuleDefs, showPasswordHints } = usePasswordRules(
  () => props.loginConfig?.security?.passwordStrength,
  () => form.value.password,
  () => props.loginConfig?.security?.passwordComplexity !== false,
);

/** 需要邮箱验证（注册必须填邮箱） */
const requireMailVerify = computed<boolean>(() => props.loginConfig?.register?.requireMailVerify === true);
/** 需要手机验证（注册必须填手机） */
const requireMobileVerify = computed<boolean>(() => props.loginConfig?.register?.requireMobileVerify === true);

/** 校验手机号 */
function isMobile(v: string): boolean {
  return /^1[3-9]\d{9}$/.test(v);
}

/** 校验邮箱 */
function isMail(v: string): boolean {
  return /^[\w.+-]+@[\w-]+(\.[\w-]+)+$/.test(v);
}

function clearError(field: string): void {
  errors.value[field] = undefined;
}

/** 发送邮箱注册验证码 */
function sendMailCode(): void {
  const mail = form.value.email.trim();
  if (!mail) {
    errors.value.email = '请输入邮箱';
    ElMessage.warning('请先输入邮箱');
    return;
  }
  if (!isMail(mail)) {
    errors.value.email = '邮箱格式不正确';
    return;
  }
  emit('sendCode', 'mail', mail);
}

/** 发送手机注册验证码 */
function sendMobileCode(): void {
  const mobile = form.value.mobile.trim();
  if (!mobile) {
    errors.value.mobile = '请输入手机号';
    ElMessage.warning('请先输入手机号');
    return;
  }
  if (!isMobile(mobile)) {
    errors.value.mobile = '手机号格式不正确';
    return;
  }
  emit('sendCode', 'sms', mobile);
}

/**
 * 客户端校验并确定注册方式
 * - 填了手机验证码 → category=mobile
 * - 填了邮箱验证码 → category=mail
 * - 否则 → category=password（需用户名+密码）
 * 校验通过才 emit('submit')，容器只负责与服务端交互。
 */
function handleSubmit(): void {
  errors.value = {};
  let valid = true;

  const username = form.value.username.trim();
  const email = form.value.email.trim();
  const mobile = form.value.mobile.trim();
  const hasMailCode = !!form.value.mailCode.trim();
  const hasMobileCode = !!form.value.mobileCode.trim();

  let category: RegisterPayload['category'] = 'password';
  if (hasMobileCode) {
    category = 'mobile';
  } else if (hasMailCode) {
    category = 'mail';
  }

  // 按注册方式校验
  if (category === 'password') {
    if (!username) {
      errors.value.username = '请输入用户名';
      valid = false;
    } else if (username.length < 2) {
      errors.value.username = '用户名至少 2 个字符';
      valid = false;
    }
  }

  // 需要验证的开关：对应联系方式必填
  if (requireMailVerify.value && !email) {
    errors.value.email = '需要邮箱验证，请填写邮箱';
    valid = false;
  }
  if (requireMobileVerify.value && !mobile) {
    errors.value.mobile = '需要手机验证，请填写手机号';
    valid = false;
  }

  if (email && !isMail(email)) {
    errors.value.email = '邮箱格式不正确';
    valid = false;
  }
  if (mobile && !isMobile(mobile)) {
    errors.value.mobile = '手机号格式不正确';
    valid = false;
  }

  // 密码校验（用户名/邮箱/手机注册均要求，验证码注册留空则后端生成随机密码）
  if (!form.value.password) {
    errors.value.password = '请输入密码';
    valid = false;
  } else {
    const failed = passwordRuleDefs.value.find((r) => !r.test(form.value.password));
    if (failed) {
      errors.value.password = `密码需${failed.label}`;
      valid = false;
    }
  }
  if (form.value.password !== form.value.confirmPassword) {
    errors.value.confirmPassword = '两次输入密码不一致';
    valid = false;
  }

  if (!valid) {
    ElMessage.warning('请检查输入信息');
    return;
  }

  emit('submit', {
    category,
    username: username || undefined,
    email: email || undefined,
    mobile: mobile || undefined,
    password: form.value.password,
    confirmPassword: form.value.confirmPassword,
    code: hasMobileCode ? form.value.mobileCode.trim() : hasMailCode ? form.value.mailCode.trim() : undefined,
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
      <label class="input-label" for="reg-username">用户名</label>
      <el-input
        id="reg-username"
        data-cy="reg-username"
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

    <!-- 邮箱（requireMailVerify 时必填） -->
    <div class="input-group">
      <label class="input-label" for="reg-email">
        邮箱<template v-if="requireMailVerify"><span class="required">*</span></template>
      </label>
      <div class="code-row">
        <el-input
          id="reg-email"
          data-cy="reg-email"
          v-model="form.email"
          placeholder="请输入邮箱"
          size="large"
          :prefix-icon="Message"
          clearable
          :class="{ 'is-error': !!errors.email }"
          @input="clearError('email')"
        />
        <button type="button" class="code-btn" @click="sendMailCode">发送验证码</button>
      </div>
      <span v-if="errors.email" class="input-error">{{ errors.email }}</span>
    </div>

    <!-- 邮箱验证码 -->
    <div class="input-group" v-if="form.email">
      <label class="input-label" for="reg-mailcode">邮箱验证码</label>
      <el-input
        id="reg-mailcode"
        data-cy="reg-mailcode"
        v-model="form.mailCode"
        placeholder="请输入邮箱验证码"
        size="large"
        maxlength="6"
        @input="clearError('mailCode')"
      />
    </div>

    <!-- 手机（requireMobileVerify 时必填） -->
    <div class="input-group">
      <label class="input-label" for="reg-mobile">
        手机号<template v-if="requireMobileVerify"><span class="required">*</span></template>
      </label>
      <div class="code-row">
        <el-input
          id="reg-mobile"
          data-cy="reg-mobile"
          v-model="form.mobile"
          placeholder="请输入手机号"
          size="large"
          :prefix-icon="Message"
          clearable
          :class="{ 'is-error': !!errors.mobile }"
          @input="clearError('mobile')"
        />
        <button type="button" class="code-btn" @click="sendMobileCode">发送验证码</button>
      </div>
      <span v-if="errors.mobile" class="input-error">{{ errors.mobile }}</span>
    </div>

    <!-- 手机验证码 -->
    <div class="input-group" v-if="form.mobile">
      <label class="input-label" for="reg-mobilecode">短信验证码</label>
      <el-input
        id="reg-mobilecode"
        data-cy="reg-mobilecode"
        v-model="form.mobileCode"
        placeholder="请输入短信验证码"
        size="large"
        maxlength="6"
        @input="clearError('mobileCode')"
      />
    </div>

    <!-- 密码 -->
    <div class="input-group">
      <label class="input-label" for="reg-password">密码</label>
      <el-input
        id="reg-password"
        data-cy="reg-password"
        v-model="form.password"
        type="password"
        placeholder="请输入密码"
        size="large"
        :prefix-icon="Lock"
        show-password
        :class="{ 'is-error': !!errors.password }"
        @input="clearError('password')"
      />
      <span v-if="errors.password" class="input-error">{{ errors.password }}</span>
    </div>

    <!-- 确认密码 -->
    <div class="input-group">
      <label class="input-label" for="reg-confirm">确认密码</label>
      <el-input
        id="reg-confirm"
        data-cy="reg-confirm"
        v-model="form.confirmPassword"
        type="password"
        placeholder="请再次输入密码"
        size="large"
        :prefix-icon="Lock"
        show-password
        :class="{ 'is-error': !!errors.confirmPassword }"
        @input="clearError('confirmPassword')"
      />
      <span v-if="errors.confirmPassword" class="input-error">{{ errors.confirmPassword }}</span>
    </div>

    <!-- 密码强度提示 -->
    <div v-if="showPasswordHints" class="password-hints">
      <div
        v-for="(rule, idx) in passwordRuleDefs"
        :key="idx"
        class="password-hint-item"
        :class="{ satisfied: rule.test(form.password) }"
      >
        <span class="hint-icon">{{ rule.test(form.password) ? '✓' : '○' }}</span>
        <span class="hint-text">{{ rule.label }}</span>
      </div>
    </div>

    <!-- 注册按钮 -->
    <button type="submit" class="login-btn" :class="{ loading: submitting }" :disabled="submitting">
      <span class="login-btn-text">注 册</span>
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

.required {
  color: var(--el-color-danger);
  margin-left: 2px;
}

/* 联系方式 + 发码按钮行 */
.code-row {
  display: flex;
  gap: 8px;
  align-items: center;
}

.code-row .el-input {
  flex: 1;
}

.code-btn {
  flex-shrink: 0;
  height: 48px;
  padding: 0 14px;
  border: 1px solid var(--el-border-color);
  border-radius: 12px;
  background: var(--el-color-primary-light-9);
  color: var(--el-color-primary);
  font-size: 13px;
  cursor: pointer;
  transition: all 0.25s ease;
  white-space: nowrap;
}

.code-btn:hover {
  border-color: var(--el-color-primary);
  color: var(--el-color-primary);
  background: var(--el-color-primary-light-8);
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
}

/* ── 登录按钮 ── */
.login-btn {
  position: relative;
  width: 100%;
  height: 48px;
  border: none;
  border-radius: 12px;
  background: linear-gradient(135deg, var(--el-color-primary), var(--el-color-primary-dark-2));
  color: var(--el-color-white);
  font-size: 16px;
  font-weight: 600;
  letter-spacing: 6px;
  cursor: pointer;
  transition: all 0.3s ease;
  overflow: hidden;
}

.login-btn:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 6px 20px var(--el-color-primary-light-5);
}

.login-btn:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.login-btn-text {
  display: inline-block;
  transition: opacity 0.25s ease;
}

.login-btn.loading .login-btn-text {
  opacity: 0;
}

.login-btn-spinner {
  position: absolute;
  top: 50%;
  left: 50%;
  width: 20px;
  height: 20px;
  margin: -10px 0 0 -10px;
  border: 2px solid color-mix(in srgb, var(--el-color-white) 30%, transparent);
  border-top-color: var(--el-color-white);
  border-radius: 50%;
  animation: login-spin 0.8s linear infinite;
  opacity: 0;
  transition: opacity 0.25s ease;
}

.login-btn.loading .login-btn-spinner {
  opacity: 1;
}

@keyframes login-spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
