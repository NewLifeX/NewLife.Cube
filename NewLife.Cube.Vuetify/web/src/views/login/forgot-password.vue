<template>
  <div class="d-flex justify-center align-center" style="min-height: 100vh">
    <v-card width="440" class="pa-6">
      <v-card-title class="text-center text-h5 font-weight-bold">重置密码</v-card-title>
      <v-card-text>
        <v-form v-if="step === 'input'" @submit.prevent="onSendCode">
          <v-text-field
            v-model="form.username"
            label="手机号或邮箱"
            prepend-inner-icon="mdi-account"
            variant="outlined"
            density="comfortable"
            class="mb-2"
            :error-messages="errors.username"
          />
          <v-radio-group v-model="form.channel" inline class="mb-2">
            <v-radio label="短信" value="Sms" />
            <v-radio label="邮箱" value="Mail" />
          </v-radio-group>
          <v-alert v-if="error" type="error" density="compact" class="mb-3">{{ error }}</v-alert>
          <v-btn type="submit" color="primary" block size="large" :loading="sending">发送验证码</v-btn>
        </v-form>

        <v-form v-else @submit.prevent="onConfirmReset">
          <v-text-field
            v-model="form.code"
            label="验证码"
            prepend-inner-icon="mdi-shield-key"
            variant="outlined"
            density="comfortable"
            class="mb-2"
            :error-messages="errors.code"
          >
            <template #append>
              <v-btn variant="text" size="small" :disabled="countdown > 0" :loading="sending" @click="onResend">
                {{ countdown > 0 ? `${countdown}s` : '重新发送' }}
              </v-btn>
            </template>
          </v-text-field>
          <v-text-field
            v-model="form.newPassword"
            label="新密码"
            type="password"
            prepend-inner-icon="mdi-lock"
            variant="outlined"
            density="comfortable"
            class="mb-2"
            :error-messages="errors.newPassword"
          />
          <v-text-field
            v-model="form.confirmPassword"
            label="确认密码"
            type="password"
            prepend-inner-icon="mdi-lock-check"
            variant="outlined"
            density="comfortable"
            class="mb-3"
            :error-messages="errors.confirmPassword"
          />
          <v-alert v-if="error" type="error" density="compact" class="mb-3">{{ error }}</v-alert>
          <v-btn type="submit" color="primary" block size="large" :loading="submitting">确认重置</v-btn>
        </v-form>
      </v-card-text>

      <v-card-text class="text-center pt-0">
        <v-btn variant="text" @click="step === 'input' ? router.push('/login') : (step = 'input')">
          {{ step === 'input' ? '返回登录' : '上一步' }}
        </v-btn>
      </v-card-text>
    </v-card>
  </div>
</template>

<script setup lang="ts">
import { onBeforeUnmount, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import cubeApi from '@/api';
import { validateResetPassword, type ResetPasswordForm } from '@/utils/forgot-password';

const router = useRouter();

const step = ref<'input' | 'verify'>('input');
const sending = ref(false);
const submitting = ref(false);
const countdown = ref(0);
const error = ref('');
const errors = reactive<Record<keyof ResetPasswordForm, string>>({
  username: '',
  channel: '',
  code: '',
  newPassword: '',
  confirmPassword: '',
});

const form = reactive<ResetPasswordForm>({
  username: '',
  channel: 'Sms',
  code: '',
  newPassword: '',
  confirmPassword: '',
});

let timer: ReturnType<typeof setInterval> | null = null;

const startCountdown = (seconds = 60) => {
  if (timer) clearInterval(timer);
  countdown.value = seconds;
  timer = setInterval(() => {
    countdown.value -= 1;
    if (countdown.value <= 0) {
      clearInterval(timer!);
      timer = null;
    }
  }, 1000);
};

const clearErrors = () => {
  for (const key of Object.keys(errors) as (keyof ResetPasswordForm)[]) errors[key] = '';
};

const sendCode = async () => {
  sending.value = true;
  error.value = '';
  try {
    await cubeApi.user.sendCode({ channel: form.channel, username: form.username, action: 'reset' });
    step.value = 'verify';
    startCountdown();
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : '发送失败，请稍后重试';
  } finally {
    sending.value = false;
  }
};

const onSendCode = async () => {
  clearErrors();
  if (!form.username) {
    errors.username = '请输入手机号或邮箱';
    return;
  }
  await sendCode();
};

const onResend = async () => {
  if (countdown.value > 0) return;
  await sendCode();
};

const onConfirmReset = async () => {
  clearErrors();
  const message = validateResetPassword(form);
  if (message) {
    const [field, text] = message;
    errors[field] = text;
    return;
  }

  submitting.value = true;
  error.value = '';
  try {
    await cubeApi.user.resetPassword({
      username: form.username,
      code: form.code,
      newPassword: form.newPassword,
      confirmPassword: form.confirmPassword,
    });
    router.push('/login');
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : '重置失败，请重试';
  } finally {
    submitting.value = false;
  }
};

onBeforeUnmount(() => {
  if (timer) clearInterval(timer);
});
</script>
