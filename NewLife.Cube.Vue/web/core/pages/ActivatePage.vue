<!--
  账号激活页 — 邮箱/手机验证激活

  支持两种激活方式：
  1. 邮箱链接直达：激活邮件链接指向 /activate?token=xxx&account=xxx，本页解析后调用 GET /Auth/Activate
  2. 验证码激活：输入邮箱/手机号 + 验证码（发码走 POST /Auth/SendCode action=activate），调 POST /Auth/Activate

  激活成功后跳转登录页。
-->
<template>
  <div class="activate-page">
    <div class="activate-card">
      <!-- 品牌区 -->
      <div class="brand">
        <div class="brand-icon">🔑</div>
        <h1 class="brand-name">{{ t('activate.title') }}</h1>
        <p class="brand-hint">{{ t('activate.hint') }}</p>
      </div>

      <!-- 结果态 -->
      <div v-if="done" class="result-box">
        <div class="result-icon">✓</div>
        <div class="result-title">{{ t('activate.success') }}</div>
        <div class="result-desc">{{ t('activate.successDesc') }}</div>
        <button type="button" class="primary-btn" @click="goLogin">{{ t('activate.goLogin') }}</button>
      </div>

      <!-- 链接激活失败（无 token 时走验证码表单） -->
      <div v-else-if="linkError" class="link-error">
        <div class="result-desc">{{ linkError }}</div>
        <p class="fallback-tip">{{ t('activate.fallbackTip') }}</p>
      </div>

      <!-- 验证码激活表单 -->
      <form v-if="!done" class="activate-form" @submit.prevent="handleSubmit">
        <!-- 渠道选择 -->
        <div class="channel-row">
          <button
            type="button"
            class="channel-btn"
            :class="{ active: form.channel === 'mail' }"
            @click="form.channel = 'mail'"
          >
            {{ t('activate.channelMail') }}
          </button>
          <button
            type="button"
            class="channel-btn"
            :class="{ active: form.channel === 'sms' }"
            @click="form.channel = 'sms'"
          >
            {{ t('activate.channelSms') }}
          </button>
        </div>

        <!-- 邮箱/手机号 -->
        <div class="input-group">
          <label class="input-label" for="activate-account">
            {{ form.channel === 'mail' ? t('activate.accountMail') : t('activate.accountMobile') }}
          </label>
          <el-input
            id="activate-account"
            data-cy="activate-account"
            v-model="form.account"
            :placeholder="form.channel === 'mail' ? t('activate.accountMailPlaceholder') : t('activate.accountMobilePlaceholder')"
            size="large"
            :class="{ 'is-error': !!errors.account }"
            @input="clearError('account')"
          />
          <span v-if="errors.account" class="input-error">{{ errors.account }}</span>
        </div>

        <!-- 验证码 -->
        <div class="input-group">
          <label class="input-label" for="activate-code">{{ t('activate.code') }}</label>
          <div class="code-row">
            <el-input
              id="activate-code"
              data-cy="activate-code"
              v-model="form.code"
              :placeholder="t('activate.codePlaceholder')"
              size="large"
              maxlength="6"
              :class="{ 'is-error': !!errors.code }"
              @input="clearError('code')"
            />
            <button type="button" class="code-btn" :disabled="sending" @click="handleSendCode">
              {{ sending ? t('activate.sending') : t('activate.sendCode') }}
            </button>
          </div>
          <span v-if="errors.code" class="input-error">{{ errors.code }}</span>
        </div>

        <!-- 激活按钮 -->
        <button type="submit" class="primary-btn" :disabled="submitting">
          {{ submitting ? t('activate.submitting') : t('activate.submit') }}
        </button>

        <!-- 返回登录 -->
        <button type="button" class="link-btn" @click="goLogin">{{ t('activate.backLogin') }}</button>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { activateByLink, activateByCode, sendCode } from '../utils/loginApi';

const route = useRoute();
const router = useRouter();
const { t } = useI18n();

/** 是否已完成激活 */
const done = ref<boolean>(false);
/** 链接激活失败原因 */
const linkError = ref<string>('');
/** 提交中 */
const submitting = ref<boolean>(false);
/** 发码中 */
const sending = ref<boolean>(false);

const form = reactive<{ channel: 'mail' | 'sms'; account: string; code: string }>({
  channel: 'mail',
  account: '',
  code: '',
});
const errors = ref<Record<string, string | undefined>>({});

function clearError(field: string): void {
  errors.value[field] = undefined;
}

function isMobile(v: string): boolean {
  return /^1[3-9]\d{9}$/.test(v);
}

function isMail(v: string): boolean {
  return /^[\w.+-]+@[\w-]+(\.[\w-]+)+$/.test(v);
}

/** 前往登录页 */
function goLogin(): void {
  router.push('/login');
}

/** 邮箱链接直达激活 */
async function activateByLinkToken(token: string, account: string): Promise<void> {
  submitting.value = true;
  try {
    const res = await activateByLink(token, account);
    if (res.code === 0 && res.data?.activated) {
      done.value = true;
      ElMessage.success(t('activate.success'));
    } else {
      linkError.value = res.message || t('activate.linkInvalid');
    }
  } catch (err: unknown) {
    linkError.value = err instanceof Error ? err.message : t('activate.failedRetry');
  } finally {
    submitting.value = false;
  }
}

/** 发送激活验证码 */
async function handleSendCode(): Promise<void> {
  const account = form.account.trim();
  if (!account) {
    errors.value.account = form.channel === 'mail' ? t('activate.accountMailPlaceholder') : t('activate.accountMobilePlaceholder');
    return;
  }
  if (form.channel === 'mail' && !isMail(account)) {
    errors.value.account = t('activate.mailInvalid');
    return;
  }
  if (form.channel === 'sms' && !isMobile(account)) {
    errors.value.account = t('activate.mobileInvalid');
    return;
  }

  sending.value = true;
  try {
    const res = await sendCode(form.channel === 'mail' ? 'Mail' : 'Sms', account, 'activate');
    if (res.code === 0) {
      ElMessage.success(t('activate.codeSent'));
    } else {
      ElMessage.error(res.message || t('activate.codeFailed'));
    }
  } catch (err: unknown) {
    ElMessage.error(err instanceof Error ? err.message : t('activate.codeFailed'));
  } finally {
    sending.value = false;
  }
}

/** 验证码激活提交 */
async function handleSubmit(): Promise<void> {
  errors.value = {};
  let valid = true;

  const account = form.account.trim();
  if (!account) {
    errors.value.account = form.channel === 'mail' ? t('activate.accountMailPlaceholder') : t('activate.accountMobilePlaceholder');
    valid = false;
  } else if (form.channel === 'mail' && !isMail(account)) {
    errors.value.account = t('activate.mailInvalid');
    valid = false;
  } else if (form.channel === 'sms' && !isMobile(account)) {
    errors.value.account = t('activate.mobileInvalid');
    valid = false;
  }

  if (!form.code.trim()) {
    errors.value.code = t('activate.codeRequired');
    valid = false;
  }

  if (!valid) return;

  submitting.value = true;
  try {
    const res = await activateByCode(form.channel, account, form.code.trim());
    if (res.code === 0 && res.data?.activated) {
      done.value = true;
      ElMessage.success(t('activate.success'));
    } else {
      ElMessage.error(res.message || t('activate.failed'));
    }
  } catch (err: unknown) {
    ElMessage.error(err instanceof Error ? err.message : t('activate.failedRetry'));
  } finally {
    submitting.value = false;
  }
}

onMounted(() => {
  const token = route.query.token as string | undefined;
  const account = route.query.account as string | undefined;

  // 邮箱激活链接直达
  if (token && account) {
    activateByLinkToken(token, decodeURIComponent(account));
    return;
  }

  // 无 token：根据 query 预填账号（如从邮件点击但链接已失效）
  if (account) {
    form.account = decodeURIComponent(account);
    form.channel = isMobile(form.account) ? 'sms' : 'mail';
  }
});
</script>

<style scoped>
.activate-page {
  position: relative;
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--el-bg-color);
  padding: 20px;
}

.activate-card {
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
}

.activate-form {
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

.activate-form :deep(.el-input__wrapper) {
  background: var(--el-color-primary-light-9);
  border: 1px solid var(--el-border-color);
  border-radius: 12px;
  height: 48px;
  padding: 0 16px;
  box-shadow: none !important;
  transition: all 0.25s ease;
}

.activate-form :deep(.el-input__wrapper.is-focus) {
  background: var(--el-bg-color-overlay);
  border-color: var(--el-color-primary);
  box-shadow: 0 0 0 3px var(--el-color-primary-light-8) !important;
}

.activate-form :deep(.el-input.is-error .el-input__wrapper) {
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

/* 结果态 */
.result-box {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  padding: 16px 0;
  text-align: center;
}

.result-icon {
  width: 56px;
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 28px;
  border-radius: 50%;
  background: var(--el-color-success-light-9);
  color: var(--el-color-success);
}

.result-title {
  font-size: 18px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.result-desc {
  font-size: 14px;
  line-height: 1.7;
  color: var(--el-text-color-regular);
}

.link-error {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 16px;
}

.fallback-tip {
  margin: 0;
  font-size: 13px;
  color: var(--el-text-color-secondary);
}
</style>
