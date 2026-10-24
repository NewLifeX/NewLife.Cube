<!--
  安全中心 — 邮箱/手机验证状态与验证/更换入口

  展示当前用户邮箱/手机验证状态（/Auth/Info 的 mailVerified/mobileVerified），
  支持已验证用户验证/更换联系方式（发码走 /Auth/SendCode action=bind，提交走 /Auth/VerifyContact）。
-->
<template>
  <div class="security-page">
    <h2 class="page-title">{{ t('security.title') }}</h2>

    <!-- 邮箱 -->
    <el-card class="contact-card" shadow="never">
      <div class="contact-row">
        <div class="contact-info">
          <div class="contact-label">{{ t('security.mail') }}</div>
          <div class="contact-value">
            <span class="contact-text">{{ info?.mail || t('security.unbound') }}</span>
            <el-tag :type="info?.mailVerified ? 'success' : 'warning'" size="small">
              {{ info?.mailVerified ? t('security.verified') : t('security.unverified') }}
            </el-tag>
          </div>
        </div>
        <el-button
          v-if="info?.mail"
          type="primary"
          plain
          size="small"
          @click="openPanel('mail')"
        >
          {{ info?.mailVerified ? t('security.change') : t('security.verify') }}
        </el-button>
      </div>
    </el-card>

    <!-- 手机 -->
    <el-card class="contact-card" shadow="never">
      <div class="contact-row">
        <div class="contact-info">
          <div class="contact-label">{{ t('security.mobile') }}</div>
          <div class="contact-value">
            <span class="contact-text">{{ info?.mobile || t('security.unbound') }}</span>
            <el-tag :type="info?.mobileVerified ? 'success' : 'warning'" size="small">
              {{ info?.mobileVerified ? t('security.verified') : t('security.unverified') }}
            </el-tag>
          </div>
        </div>
        <el-button
          v-if="info?.mobile"
          type="primary"
          plain
          size="small"
          @click="openPanel('sms')"
        >
          {{ info?.mobileVerified ? t('security.change') : t('security.verify') }}
        </el-button>
      </div>
    </el-card>

    <!-- 验证/更换面板 -->
    <el-card v-if="panel" class="verify-panel" shadow="never">
      <template #header>
        <span>
          {{ panel === 'mail' ? t('security.verifyMailPanel') : t('security.verifyMobilePanel') }}
          <el-button text type="primary" size="small" @click="panel = null">{{ t('security.collapse') }}</el-button>
        </span>
      </template>
      <el-form label-width="90px" @submit.prevent>
        <el-form-item :label="t('security.newAccount')" required>
          <el-input
            v-model="verifyForm.account"
            :placeholder="panel === 'mail' ? t('security.accountPlaceholderMail') : t('security.accountPlaceholderMobile')"
            data-cy="verify-account"
          />
        </el-form-item>
        <el-form-item :label="t('security.code')" required>
          <div class="code-row">
            <el-input
              v-model="verifyForm.code"
              :placeholder="t('security.codePlaceholder')"
              maxlength="6"
              data-cy="verify-code"
              @keyup.enter="handleVerify"
            />
            <el-button :disabled="sending" @click="handleSendVerifyCode">
              {{ sending ? t('security.sending') : t('security.sendCode') }}
            </el-button>
          </div>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="submitting" @click="handleVerify" data-cy="verify-submit">
            {{ t('security.submit') }}
          </el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <p class="security-tip">
      {{ t('security.tip') }}
    </p>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import request from '../utils/request';
import { sendCode, verifyContact } from '../utils/loginApi';

const { t } = useI18n();

interface SecurityInfo {
  mail?: string;
  mobile?: string;
  mailVerified?: boolean;
  mobileVerified?: boolean;
}

/** 当前用户信息 */
const info = ref<SecurityInfo | null>(null);
/** 展开的验证面板：mail / sms / null */
const panel = ref<'mail' | 'sms' | null>(null);
/** 提交中 */
const submitting = ref(false);
/** 发码中 */
const sending = ref(false);

const verifyForm = reactive<{ account: string; code: string }>({ account: '', code: '' });

function isMobile(v: string): boolean {
  return /^1[3-9]\d{9}$/.test(v);
}

function isMail(v: string): boolean {
  return /^[\w.+-]+@[\w-]+(\.[\w-]+)+$/.test(v);
}

/** 拉取当前用户信息（含验证状态） */
async function fetchInfo(): Promise<void> {
  try {
    const res = (await request.get('/Auth/Info')) as unknown as {
      code?: number;
      data?: SecurityInfo;
    };
    if (res?.code === 0 && res.data) {
      info.value = res.data;
    }
  } catch (err: unknown) {
    console.error('Failed to load user info:', err);
  }
}

/** 展开验证面板 */
function openPanel(channel: 'mail' | 'sms'): void {
  panel.value = channel;
  verifyForm.account = '';
  verifyForm.code = '';
}

/** 发送验证码（action=bind） */
async function handleSendVerifyCode(): Promise<void> {
  const account = verifyForm.account.trim();
  if (!account) {
    ElMessage.warning(panel.value === 'mail' ? t('security.enterMail') : t('security.enterMobile'));
    return;
  }
  if (panel.value === 'mail' && !isMail(account)) {
    ElMessage.warning(t('security.invalidMail'));
    return;
  }
  if (panel.value === 'sms' && !isMobile(account)) {
    ElMessage.warning(t('security.invalidMobile'));
    return;
  }

  sending.value = true;
  try {
    const res = await sendCode(panel.value === 'mail' ? 'Mail' : 'Sms', account, 'bind');
    if (res.code === 0) {
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

/** 提交验证/更换 */
async function handleVerify(): Promise<void> {
  if (!panel.value) return;
  const account = verifyForm.account.trim();
  if (!account) {
    ElMessage.warning(panel.value === 'mail' ? t('security.enterMail') : t('security.enterMobile'));
    return;
  }
  if (!verifyForm.code.trim()) {
    ElMessage.warning(t('security.enterCode'));
    return;
  }

  submitting.value = true;
  try {
    const res = await verifyContact(panel.value, account, verifyForm.code.trim());
    if (res.code === 0 && res.data) {
      ElMessage.success(t('security.verifySuccess'));
      panel.value = null;
      // 刷新验证状态
      await fetchInfo();
    } else {
      ElMessage.error(res.message || t('security.verifyFailed'));
    }
  } catch (err: unknown) {
    ElMessage.error(err instanceof Error ? err.message : t('security.verifyFailedRetry'));
  } finally {
    submitting.value = false;
  }
}

onMounted(() => {
  fetchInfo();
});
</script>

<style scoped>
.security-page {
  max-width: 720px;
  margin: 0 auto;
  padding: 24px 16px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.page-title {
  margin: 0 0 8px;
  font-size: 22px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.contact-card {
  border-radius: 12px;
}

.contact-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.contact-info {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.contact-label {
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.contact-value {
  display: flex;
  align-items: center;
  gap: 10px;
}

.contact-text {
  font-size: 15px;
  color: var(--el-text-color-primary);
  word-break: break-all;
}

.verify-panel {
  border-radius: 12px;
}

.code-row {
  display: flex;
  gap: 8px;
  width: 100%;
}

.code-row .el-input {
  flex: 1;
}

.security-tip {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  line-height: 1.7;
  margin: 0;
}
</style>
