<template>
  <div class="forgot-wrap">
    <a-card class="forgot-card" :bordered="false">
      <div class="forgot-header">
        <h2>重置密码</h2>
      </div>

      <!-- 步骤一：输入账号并获取验证码 -->
      <a-form v-if="step === 'input'" :model="form" layout="vertical" @submit.prevent="onSendCode">
        <a-form-item label="手机号或邮箱" :rules="[{ required: true, message: '请输入手机号或邮箱' }]" field="username">
          <a-input v-model="form.username" placeholder="请输入手机号或邮箱" allow-clear />
        </a-form-item>
        <a-form-item label="验证渠道">
          <a-radio-group v-model="form.channel" type="button">
            <a-radio value="Sms">短信</a-radio>
            <a-radio value="Mail">邮箱</a-radio>
          </a-radio-group>
        </a-form-item>
        <a-form-item>
          <a-button type="primary" long html-type="submit" :loading="sending">发送验证码</a-button>
        </a-form-item>
        <a-form-item>
          <a-link @click="router.push('/login')">返回登录</a-link>
        </a-form-item>
      </a-form>

      <!-- 步骤二：输入验证码和新密码 -->
      <a-form v-else :model="form" layout="vertical" @submit.prevent="onConfirmReset">
        <a-form-item label="验证码" :rules="[{ required: true, message: '请输入验证码' }]" field="code">
          <a-input-group>
            <a-input v-model="form.code" placeholder="请输入验证码" style="flex: 1" allow-clear />
            <a-button :disabled="countdown > 0" :loading="sending" @click="onResend">
              {{ countdown > 0 ? `${countdown}s` : '重新发送' }}
            </a-button>
          </a-input-group>
        </a-form-item>
        <a-form-item label="新密码" :rules="[{ required: true, message: '请输入新密码' }, { minLength: 6, message: '密码不少于6位' }]" field="newPassword">
          <a-input-password v-model="form.newPassword" placeholder="请输入新密码" allow-clear />
        </a-form-item>
        <a-form-item label="确认密码" :rules="[{ required: true, message: '请再次输入密码' }, { validator: confirmPwdValidator }]" field="confirmPassword">
          <a-input-password v-model="form.confirmPassword" placeholder="请再次输入新密码" allow-clear />
        </a-form-item>
        <a-alert v-if="error" type="error" :content="error" class="forgot-error" />
        <a-form-item>
          <a-button type="primary" long html-type="submit" :loading="submitting">确认重置</a-button>
        </a-form-item>
        <a-form-item>
          <a-space>
            <a-link @click="step = 'input'">上一步</a-link>
            <a-divider direction="vertical" />
            <a-link @click="router.push('/login')">返回登录</a-link>
          </a-space>
        </a-form-item>
      </a-form>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';

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

const confirmPwdValidator = (_: unknown, value: string, callback: (msg?: string) => void) => {
  if (value !== form.newPassword) callback('两次密码不一致');
  else callback();
};

const onSendCode = async () => {
  if (!form.username) { Message.warning('请输入手机号或邮箱'); return; }
  sending.value = true;
  error.value = '';
  try {
    await cubeApi.user.sendCode({ channel: form.channel, username: form.username, action: 'reset' });
    step.value = 'verify';
    startCountdown();
    Message.success('验证码已发送');
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : '发送失败，请稍后重试';
    Message.error(msg);
  } finally {
    sending.value = false;
  }
};

const onResend = async () => {
  if (countdown.value > 0) return;
  sending.value = true;
  error.value = '';
  try {
    await cubeApi.user.sendCode({ channel: form.channel, username: form.username, action: 'reset' });
    startCountdown();
    Message.success('验证码已重新发送');
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : '发送失败';
    Message.error(msg);
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
    await cubeApi.user.resetPassword({
      username: form.username,
      code: form.code,
      newPassword: form.newPassword,
      confirmPassword: form.confirmPassword,
    });
    Message.success('密码重置成功，请重新登录');
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
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background: var(--color-bg-2, #f0f2f5);
}
.forgot-card {
  width: 400px;
  padding: 16px;
}
.forgot-header h2 {
  font-size: 22px;
  font-weight: 700;
  margin-bottom: 24px;
}
.forgot-error {
  margin-bottom: 16px;
}
</style>
