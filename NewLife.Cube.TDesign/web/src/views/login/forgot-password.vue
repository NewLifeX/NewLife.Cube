<template>
  <div class="forgot-wrap">
    <t-card class="forgot-card" :bordered="false">
      <template #header>
        <div class="forgot-header">
          <h2>重置密码</h2>
        </div>
      </template>

      <t-alert v-if="error" theme="error" :message="error" style="margin-bottom: 16px" />

      <!-- 步骤一 -->
      <template v-if="step === 'input'">
        <t-form :label-width="0">
          <t-form-item>
            <t-input v-model="form.username" placeholder="请输入手机号或邮箱" size="large" clearable />
          </t-form-item>
          <t-form-item>
            <t-radio-group v-model="form.channel">
              <t-radio value="Sms">短信验证码</t-radio>
              <t-radio value="Mail">邮箱验证码</t-radio>
            </t-radio-group>
          </t-form-item>
          <t-form-item>
            <t-button theme="primary" block size="large" :loading="sending" @click="onSendCode">发送验证码</t-button>
          </t-form-item>
          <t-form-item>
            <t-link theme="primary" @click="router.push('/login')">返回登录</t-link>
          </t-form-item>
        </t-form>
      </template>

      <!-- 步骤二 -->
      <template v-else>
        <t-form :label-width="0">
          <t-form-item>
            <t-input v-model="form.code" placeholder="请输入验证码" size="large" clearable>
              <template #suffix>
                <t-button variant="text" :disabled="countdown > 0" :loading="sending" @click="onResend">
                  {{ countdown > 0 ? `${countdown}s` : '重新发送' }}
                </t-button>
              </template>
            </t-input>
          </t-form-item>
          <t-form-item>
            <t-input v-model="form.newPassword" type="password" placeholder="请输入新密码" size="large" clearable />
          </t-form-item>
          <t-form-item>
            <t-input v-model="form.confirmPassword" type="password" placeholder="请再次输入新密码" size="large" clearable />
          </t-form-item>
          <t-form-item>
            <t-button theme="primary" block size="large" :loading="submitting" @click="onConfirmReset">确认重置</t-button>
          </t-form-item>
          <t-form-item>
            <t-space>
              <t-link @click="step = 'input'">上一步</t-link>
              <t-divider layout="vertical" />
              <t-link theme="primary" @click="router.push('/login')">返回登录</t-link>
            </t-space>
          </t-form-item>
        </t-form>
      </template>
    </t-card>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { MessagePlugin } from 'tdesign-vue-next';
import { api } from '@/api';

const router = useRouter();

const step = ref<'input' | 'verify'>('input');
const sending = ref(false);
const submitting = ref(false);
const countdown = ref(0);
const error = ref('');

const form = reactive({
  username: '',
  channel: 'Sms' as 'Sms' | 'Mail',
  code: '',
  newPassword: '',
  confirmPassword: '',
});

let _timer: ReturnType<typeof setInterval> | null = null;

const startCountdown = (seconds = 60) => {
  if (_timer) clearInterval(_timer);
  countdown.value = seconds;
  _timer = setInterval(() => {
    countdown.value -= 1;
    if (countdown.value <= 0) { clearInterval(_timer!); _timer = null; }
  }, 1000);
};

const onSendCode = async () => {
  if (!form.username) { MessagePlugin.warning('请输入手机号或邮箱'); return; }
  sending.value = true;
  error.value = '';
  try {
    await api.user.sendCode({ channel: form.channel, username: form.username, action: 'reset' });
    step.value = 'verify';
    startCountdown();
    MessagePlugin.success('验证码已发送');
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : '发送失败，请稍后重试';
    MessagePlugin.error(msg);
  } finally {
    sending.value = false;
  }
};

const onResend = async () => {
  if (countdown.value > 0) return;
  sending.value = true;
  error.value = '';
  try {
    await api.user.sendCode({ channel: form.channel, username: form.username, action: 'reset' });
    startCountdown();
    MessagePlugin.success('验证码已重新发送');
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : '发送失败';
    MessagePlugin.error(msg);
  } finally {
    sending.value = false;
  }
};

const onConfirmReset = async () => {
  if (!form.code) { error.value = '请输入验证码'; return; }
  if (!form.newPassword) { error.value = '请输入新密码'; return; }
  if (form.newPassword !== form.confirmPassword) { error.value = '两次密码不一致'; return; }
  submitting.value = true;
  error.value = '';
  try {
    await api.user.resetPassword({
      username: form.username,
      code: form.code,
      newPassword: form.newPassword,
      confirmPassword: form.confirmPassword,
    });
    MessagePlugin.success('密码重置成功，请重新登录');
    router.push('/login');
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : '重置失败，请重试';
    error.value = msg;
  } finally {
    submitting.value = false;
  }
};
</script>

<style scoped>
.forgot-wrap {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background: var(--td-bg-color-page, #f0f2f5);
}
.forgot-card {
  width: 420px;
}
.forgot-header h2 {
  font-size: 22px;
  font-weight: 700;
}
</style>
