<template>
  <div class="forgot-container">
    <n-card class="forgot-card" :bordered="false">
      <div class="forgot-header">
        <h2>重置密码</h2>
      </div>

      <!-- 步骤一 -->
      <template v-if="step === 'input'">
        <n-form ref="step1Ref" :model="form" label-placement="left">
          <n-form-item label="账号" path="username" :rule="{ required: true, message: '请输入手机号或邮箱', trigger: 'blur' }">
            <n-input v-model:value="form.username" placeholder="请输入手机号或邮箱" clearable />
          </n-form-item>
          <n-form-item label="验证渠道">
            <n-radio-group v-model:value="form.channel">
              <n-radio value="Sms">短信</n-radio>
              <n-radio value="Mail">邮箱</n-radio>
            </n-radio-group>
          </n-form-item>
          <n-button type="primary" block :loading="sending" @click="onSendCode">发送验证码</n-button>
          <div class="forgot-back mt12">
            <n-button text type="primary" @click="router.push('/login')">返回登录</n-button>
          </div>
        </n-form>
      </template>

      <!-- 步骤二 -->
      <template v-else>
        <n-form ref="step2Ref" :model="form" label-placement="left">
          <n-form-item label="验证码" path="code" :rule="{ required: true, message: '请输入验证码', trigger: 'blur' }">
            <n-input-group>
              <n-input v-model:value="form.code" placeholder="请输入验证码" clearable style="flex: 1" />
              <n-button :disabled="countdown > 0" :loading="sending" @click="onResend">
                {{ countdown > 0 ? `${countdown}s` : '重新发送' }}
              </n-button>
            </n-input-group>
          </n-form-item>
          <n-form-item label="新密码" path="newPassword" :rule="[{ required: true, message: '请输入新密码', trigger: 'blur' }, { min: 6, message: '密码不少于6位', trigger: 'blur' }]">
            <n-input v-model:value="form.newPassword" type="password" show-password-on="click" placeholder="请输入新密码" />
          </n-form-item>
          <n-form-item label="确认密码" path="confirmPassword" :rule="{ required: true, validator: confirmPwdValidator, trigger: 'blur' }">
            <n-input v-model:value="form.confirmPassword" type="password" show-password-on="click" placeholder="请再次输入新密码" />
          </n-form-item>
          <n-alert v-if="error" type="error" :title="error" class="mb12" />
          <n-button type="primary" block :loading="submitting" @click="onConfirmReset">确认重置</n-button>
          <div class="forgot-back mt12">
            <n-button text @click="step = 'input'">上一步</n-button>
            <n-divider vertical />
            <n-button text type="primary" @click="router.push('/login')">返回登录</n-button>
          </div>
        </n-form>
      </template>
    </n-card>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { useMessage, type FormInst } from 'naive-ui';
import api from '@/api';

const router = useRouter();
const message = useMessage();

const step1Ref = ref<FormInst | null>(null);
const step2Ref = ref<FormInst | null>(null);

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

const confirmPwdValidator = (_rule: unknown, value: string): boolean | Error => {
  if (value !== form.newPassword) return new Error('两次密码不一致');
  return true;
};

const onSendCode = async () => {
  try { await step1Ref.value?.validate(); } catch { return; }
  sending.value = true;
  error.value = '';
  try {
    await api.user.sendCode({ channel: form.channel, username: form.username, action: 'reset' });
    step.value = 'verify';
    startCountdown();
    message.success('验证码已发送');
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : '发送失败，请稍后重试';
    message.error(msg);
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
    message.success('验证码已重新发送');
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : '发送失败';
    message.error(msg);
  } finally {
    sending.value = false;
  }
};

const onConfirmReset = async () => {
  try { await step2Ref.value?.validate(); } catch { return; }
  submitting.value = true;
  error.value = '';
  try {
    await api.user.resetPassword({
      username: form.username,
      code: form.code,
      newPassword: form.newPassword,
      confirmPassword: form.confirmPassword,
    });
    message.success('密码重置成功，请重新登录');
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
.forgot-container {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}
.forgot-card {
  width: 440px;
  border-radius: 8px;
}
.forgot-header h2 {
  font-size: 22px;
  font-weight: 700;
  margin-bottom: 16px;
}
.forgot-back {
  display: flex;
  align-items: center;
  gap: 4px;
}
.mt12 { margin-top: 12px; }
.mb12 { margin-bottom: 12px; }
</style>
