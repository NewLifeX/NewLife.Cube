<!--
  找回密码（忘记密码）页

  两步流程，与后端 /Auth/* 约定一致：
  1. 输入手机号或邮箱 → POST /Auth/SendCode { action: 'reset' }，渠道按账号格式自动推断（可手动切换）
  2. 输入验证码 + 新密码 → POST /Auth/ResetPassword（新密码由 @newlifex/cube 自动 RSA 挑战加密）

  重置成功后返回登录页。校验逻辑抽到 `core/utils/forgotPassword.ts`，可脱离 i18n 单测。
-->
<template>
  <div class="forgot-page">
    <div class="forgot-card">
      <!-- 品牌区 -->
      <div class="brand">
        <div class="brand-icon">🔒</div>
        <h1 class="brand-name">{{ t('forgot.title') }}</h1>
        <p class="brand-hint">{{ t('forgot.hint') }}</p>
      </div>

      <!-- 第一步：账号 -->
      <form v-if="step === 'account'" class="forgot-form" @submit.prevent="handleSendCode">
        <div class="input-group">
          <label class="input-label" for="forgot-username">{{ t('forgot.username') }}</label>
          <el-input
            id="forgot-username"
            data-cy="forgot-username"
            v-model="form.username"
            :placeholder="t('forgot.usernamePlaceholder')"
            size="large"
            clearable
            :class="{ 'is-error': !!errors.username }"
            @input="onUsernameInput"
          />
          <span v-if="errors.username" class="input-error">{{ errors.username }}</span>
        </div>

        <!-- 发码渠道（按账号格式自动选中，允许手动纠正） -->
        <div class="channel-row">
          <button
            type="button"
            class="channel-btn"
            :class="{ active: form.channel === 'Sms' }"
            data-cy="forgot-channel-sms"
            @click="form.channel = 'Sms'"
          >
            {{ t('forgot.channelSms') }}
          </button>
          <button
            type="button"
            class="channel-btn"
            :class="{ active: form.channel === 'Mail' }"
            data-cy="forgot-channel-mail"
            @click="form.channel = 'Mail'"
          >
            {{ t('forgot.channelMail') }}
          </button>
        </div>

        <button type="submit" class="primary-btn" :disabled="sending">
          {{ sending ? t('activate.sending') : t('activate.sendCode') }}
        </button>

        <button type="button" class="link-btn" @click="goLogin">{{ t('activate.backLogin') }}</button>
      </form>

      <!-- 第二步：验证码 + 新密码 -->
      <form v-else class="forgot-form" @submit.prevent="handleReset">
        <div class="input-group">
          <label class="input-label" for="forgot-code">{{ t('security.code') }}</label>
          <div class="code-row">
            <el-input
              id="forgot-code"
              data-cy="forgot-code"
              v-model="form.code"
              :placeholder="t('security.codePlaceholder')"
              size="large"
              maxlength="6"
              :class="{ 'is-error': !!errors.code }"
              @input="clearError('code')"
            />
            <button
              type="button"
              class="code-btn"
              data-cy="forgot-resend"
              :disabled="sending || countdown > 0"
              @click="handleSendCode"
            >
              {{ countdown > 0 ? `${countdown}s` : t('login.resend') }}
            </button>
          </div>
          <span v-if="errors.code" class="input-error">{{ errors.code }}</span>
        </div>

        <div class="input-group">
          <label class="input-label" for="forgot-new-password">{{ t('user.newPassword') }}</label>
          <el-input
            id="forgot-new-password"
            data-cy="forgot-new-password"
            v-model="form.newPassword"
            type="password"
            show-password
            :placeholder="t('security.enterPassword')"
            size="large"
            :class="{ 'is-error': !!errors.newPassword }"
            @input="clearError('newPassword')"
          />
          <span v-if="errors.newPassword" class="input-error">{{ errors.newPassword }}</span>
        </div>

        <div class="input-group">
          <label class="input-label" for="forgot-confirm-password">{{ t('user.confirmPassword') }}</label>
          <el-input
            id="forgot-confirm-password"
            data-cy="forgot-confirm-password"
            v-model="form.confirmPassword"
            type="password"
            show-password
            :placeholder="t('security.enterPassword')"
            size="large"
            :class="{ 'is-error': !!errors.confirmPassword }"
            @input="clearError('confirmPassword')"
          />
          <span v-if="errors.confirmPassword" class="input-error">{{ errors.confirmPassword }}</span>
        </div>

        <button type="submit" class="primary-btn" :disabled="submitting">
          {{ submitting ? t('activate.submitting') : t('forgot.submit') }}
        </button>

        <button type="button" class="link-btn" data-cy="forgot-prev" @click="backToAccount">
          {{ t('forgot.prev') }}
        </button>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onBeforeUnmount, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { resetPassword, sendCode } from '../utils/loginApi';
import {
  guessChannel,
  validateResetAccount,
  validateResetConfirm,
  type ResetPasswordField,
  type ResetPasswordIssue,
  type ResetPasswordForm,
} from '../utils/forgotPassword';

const router = useRouter();
const { t } = useI18n();

/** 当前步骤：第一步填账号发码，第二步填验证码与新密码 */
const step = ref<'account' | 'verify'>('account');
/** 发码中 */
const sending = ref<boolean>(false);
/** 重置提交中 */
const submitting = ref<boolean>(false);
/** 重发倒计时（秒） */
const countdown = ref<number>(0);

const form = reactive<ResetPasswordForm>({
  username: '',
  channel: 'Sms',
  code: '',
  newPassword: '',
  confirmPassword: '',
});

const errors = reactive<Record<ResetPasswordField, string | undefined>>({
  username: undefined,
  code: undefined,
  newPassword: undefined,
  confirmPassword: undefined,
});

/** 倒计时定时器（组件卸载时清理，避免泄漏） */
let timer: ReturnType<typeof setInterval> | null = null;

/** 清空某个字段的错误提示 */
function clearError(field: ResetPasswordField): void {
  errors[field] = undefined;
}

/** 清空全部错误提示 */
function clearErrors(): void {
  errors.username = undefined;
  errors.code = undefined;
  errors.newPassword = undefined;
  errors.confirmPassword = undefined;
}

/** 把校验问题落到对应字段上 */
function applyIssue(issue: ResetPasswordIssue): void {
  errors[issue.field] = t(issue.key, issue.params ?? {});
}

/** 账号输入时按格式自动切换发码渠道（用户仍可手动纠正） */
function onUsernameInput(value: string): void {
  clearError('username');
  form.channel = guessChannel(value);
}

/** 返回登录页 */
function goLogin(): void {
  router.push('/login');
}

/** 回到第一步（保留已填账号） */
function backToAccount(): void {
  clearErrors();
  form.code = '';
  form.newPassword = '';
  form.confirmPassword = '';
  step.value = 'account';
}

/** 启动 60 秒重发倒计时 */
function startCountdown(seconds = 60): void {
  if (timer) clearInterval(timer);
  countdown.value = seconds;
  timer = setInterval(() => {
    countdown.value -= 1;
    if (countdown.value <= 0 && timer) {
      clearInterval(timer);
      timer = null;
    }
  }, 1000);
}

/**
 * 发送验证码（第一步提交与第二步「重新发送」共用）
 *
 * 成功才前进到第二步；失败时停在当前步骤并提示，避免用户填了验证码却收不到。
 */
async function handleSendCode(): Promise<void> {
  if (sending.value || (step.value === 'verify' && countdown.value > 0)) return;

  clearErrors();
  const issue = validateResetAccount(form.username);
  if (issue) {
    applyIssue(issue);
    return;
  }

  form.username = form.username.trim();
  sending.value = true;
  try {
    const res = await sendCode(form.channel, form.username, 'reset');
    if (res.code === 0) {
      step.value = 'verify';
      startCountdown();
      ElMessage.success(t('security.codeSent'));
    } else {
      ElMessage.error(res.message || t('security.codeSendFailed'));
    }
  } catch (err: unknown) {
    ElMessage.error(err instanceof Error ? err.message : t('security.codeSendFailed'));
  } finally {
    sending.value = false;
  }
}

/** 提交重置：校验通过后调用 /Auth/ResetPassword */
async function handleReset(): Promise<void> {
  clearErrors();
  const issue = validateResetConfirm(form);
  if (issue) {
    applyIssue(issue);
    return;
  }

  submitting.value = true;
  try {
    const res = await resetPassword({
      username: form.username,
      code: form.code.trim(),
      newPassword: form.newPassword,
      confirmPassword: form.confirmPassword,
    });
    if (res.code === 0) {
      ElMessage.success(t('forgot.success'));
      router.push('/login');
    } else {
      ElMessage.error(res.message || t('forgot.failed'));
    }
  } catch (err: unknown) {
    ElMessage.error(err instanceof Error ? err.message : t('forgot.failed'));
  } finally {
    submitting.value = false;
  }
}

onBeforeUnmount(() => {
  if (timer) clearInterval(timer);
  timer = null;
});
</script>

<style scoped>
.forgot-page {
  position: relative;
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--el-bg-color);
  padding: 20px;
}

.forgot-card {
  width: 420px;
  max-width: 100%;
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-light);
  border-radius: 24px;
  box-shadow: var(--el-box-shadow-light);
  padding: 40px 36px 32px;
}

.brand {
  display: flex;
  flex-direction: column;
  align-items: center;
  margin-bottom: 28px;
}

.brand-icon {
  width: 56px;
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 26px;
  border-radius: 50%;
  background: var(--el-color-primary-light-9);
  margin-bottom: 12px;
}

.brand-name {
  margin: 0;
  font-size: 24px;
  font-weight: 700;
  color: var(--el-text-color-primary);
}

.brand-hint {
  margin: 8px 0 0;
  font-size: 14px;
  color: var(--el-text-color-regular);
  text-align: center;
}

.forgot-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.channel-row {
  display: flex;
  gap: 8px;
}

.channel-btn {
  flex: 1;
  height: 40px;
  border: 1px solid var(--el-border-color);
  border-radius: 10px;
  background: transparent;
  color: var(--el-text-color-secondary);
  font-size: 14px;
  cursor: pointer;
  transition: all 0.25s ease;
}

.channel-btn.active {
  border-color: var(--el-color-primary);
  color: var(--el-color-primary);
  background: var(--el-color-primary-light-9);
}

.input-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.input-label {
  font-size: 13px;
  font-weight: 500;
  color: var(--el-text-color-regular);
}

.forgot-form :deep(.el-input__wrapper) {
  background: var(--el-color-primary-light-9);
  border: 1px solid var(--el-border-color);
  border-radius: 12px;
  height: 48px;
  padding: 0 16px;
  box-shadow: none !important;
  transition: all 0.25s ease;
}

.forgot-form :deep(.el-input__wrapper.is-focus) {
  background: var(--el-bg-color-overlay);
  border-color: var(--el-color-primary);
  box-shadow: 0 0 0 3px var(--el-color-primary-light-8) !important;
}

.forgot-form :deep(.el-input.is-error .el-input__wrapper) {
  border-color: var(--el-color-danger) !important;
}

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
  white-space: nowrap;
}

.code-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.input-error {
  font-size: 12px;
  color: var(--el-color-danger);
}

.primary-btn {
  width: 100%;
  height: 46px;
  border: none;
  border-radius: 12px;
  background: linear-gradient(135deg, var(--el-color-primary), var(--el-color-primary-dark-2));
  color: var(--el-color-white);
  font-size: 15px;
  font-weight: 600;
  letter-spacing: 4px;
  cursor: pointer;
  transition: all 0.3s ease;
}

.primary-btn:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 6px 20px var(--el-color-primary-light-5);
}

.primary-btn:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.link-btn {
  border: none;
  background: transparent;
  color: var(--el-color-primary);
  font-size: 13px;
  cursor: pointer;
  align-self: center;
}

.link-btn:hover {
  text-decoration: underline;
}
</style>
