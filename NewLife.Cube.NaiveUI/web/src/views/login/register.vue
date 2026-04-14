<template>
  <div class="register-container">
    <n-card class="register-card" :bordered="false">
      <div class="register-header"><h2>注册账号</h2></div>

      <n-tabs v-if="!oauthMode" v-model:value="activeTab">
        <n-tab-pane name="password" tab="账号注册" />
        <n-tab-pane v-if="enableSmsRegister" name="phone" tab="手机注册" />
        <n-tab-pane v-if="enableMailRegister" name="email" tab="邮箱注册" />
      </n-tabs>

      <n-alert v-if="oauthMode" type="info" style="margin-bottom:12px">第三方账号首次登录，请补全密码完成本地账号创建</n-alert>

      <n-form :model="form" label-placement="left">
        <n-form-item v-if="activeTab==='password' || oauthMode" label="用户名"><n-input v-model:value="form.username" :readonly="oauthMode" /></n-form-item>
        <n-form-item v-if="activeTab==='password' || activeTab==='email' || oauthMode" label="邮箱"><n-input v-model:value="form.email" /></n-form-item>

        <n-form-item v-if="activeTab==='phone'" label="手机号">
          <n-input-group>
            <n-input v-model:value="form.mobile" style="flex:1" />
            <n-button :disabled="countdown>0" :loading="sending" @click="sendCode('Sms')">{{ countdown>0 ? `${countdown}s` : '发送验证码' }}</n-button>
          </n-input-group>
        </n-form-item>

        <n-form-item v-if="activeTab==='email'" label="邮箱地址">
          <n-input-group>
            <n-input v-model:value="form.emailCodeTarget" style="flex:1" />
            <n-button :disabled="countdown>0" :loading="sending" @click="sendCode('Mail')">{{ countdown>0 ? `${countdown}s` : '发送验证码' }}</n-button>
          </n-input-group>
        </n-form-item>

        <n-form-item v-if="activeTab==='phone' || activeTab==='email'" label="验证码"><n-input v-model:value="form.code" /></n-form-item>
        <n-form-item label="密码"><n-input v-model:value="form.password" type="password" show-password-on="click" /></n-form-item>
        <n-form-item label="确认密码"><n-input v-model:value="form.confirmPassword" type="password" show-password-on="click" /></n-form-item>
        <n-button type="primary" block :loading="loading" @click="onSubmit">{{ oauthMode ? '完成绑定并登录' : '立即注册' }}</n-button>
      </n-form>

      <div class="register-footer-link">
        <span>已有账号？</span>
        <n-button text type="primary" @click="router.push('/login')">去登录</n-button>
      </div>
    </n-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useMessage } from 'naive-ui';
import { RegisterCategory } from '@cube/api-core';
import api from '@/api';

const router = useRouter();
const route = useRoute();
const message = useMessage();

const activeTab = ref<'password' | 'phone' | 'email'>('password');
const oauthMode = ref(false);
const loading = ref(false);
const sending = ref(false);
const countdown = ref(0);
const config = ref<any>(null);

const form = reactive({
  username: '',
  email: '',
  mobile: '',
  emailCodeTarget: '',
  code: '',
  password: '',
  confirmPassword: '',
  oauthToken: '',
});

let timer: ReturnType<typeof setInterval> | null = null;

const enableSmsRegister = computed(() => !!(config.value?.enableSmsRegister ?? config.value?.enableSms));
const enableMailRegister = computed(() => !!(config.value?.enableMailRegister ?? config.value?.enableMail));

const startCountdown = () => {
  if (timer) clearInterval(timer);
  countdown.value = 60;
  timer = setInterval(() => {
    countdown.value -= 1;
    if (countdown.value <= 0 && timer) {
      clearInterval(timer);
      timer = null;
    }
  }, 1000);
};

const sendCode = async (channel: 'Sms' | 'Mail') => {
  const username = channel === 'Sms' ? form.mobile : form.emailCodeTarget;
  if (!username) return message.warning(channel === 'Sms' ? '请输入手机号' : '请输入邮箱地址');
  sending.value = true;
  try {
    await api.user.sendCode({ channel, username, action: 'register' });
    message.success('验证码已发送');
    startCountdown();
  } catch (err: any) {
    message.error(err?.message || '发送失败');
  } finally {
    sending.value = false;
  }
};

const onSubmit = async () => {
  if (!form.password || !form.confirmPassword) return message.warning('请输入密码和确认密码');
  if (form.password !== form.confirmPassword) return message.warning('两次密码不一致');
  if (activeTab.value === 'phone' && (!form.mobile || !form.code)) return message.warning('请填写手机号和验证码');
  if (activeTab.value === 'email' && (!form.emailCodeTarget || !form.code)) return message.warning('请填写邮箱和验证码');

  const payload = oauthMode.value
    ? { registerCategory: RegisterCategory.OAuthBind, oauthToken: form.oauthToken, username: form.username, email: form.email, password: form.password, confirmPassword: form.confirmPassword }
    : activeTab.value === 'phone'
      ? { registerCategory: RegisterCategory.Phone, username: form.username || form.mobile, mobile: form.mobile, email: form.email, code: form.code, password: form.password, confirmPassword: form.confirmPassword }
      : activeTab.value === 'email'
        ? { registerCategory: RegisterCategory.Email, username: form.username || form.emailCodeTarget, email: form.emailCodeTarget, code: form.code, password: form.password, confirmPassword: form.confirmPassword }
        : { registerCategory: RegisterCategory.Password, username: form.username, email: form.email, password: form.password, confirmPassword: form.confirmPassword };

  loading.value = true;
  try {
    const res = await api.user.register(payload as any);
    const token = res.data?.accessToken || (res.data as any)?.token;
    if (token) {
      api.tokenManager.setToken(token);
      message.success('注册成功，已自动登录');
      router.push('/');
      return;
    }
    message.success('注册成功，请登录');
    router.push('/login');
  } catch (err: any) {
    message.error(err?.message || '注册失败');
  } finally {
    loading.value = false;
  }
};

onMounted(async () => {
  try {
    const cfg = await api.user.getLoginConfig();
    config.value = cfg.data;
  } catch { /* ignore */ }

  const oauthToken = (route.query.oauthToken as string) || '';
  if (!oauthToken) return;
  oauthMode.value = true;
  form.oauthToken = oauthToken;
  try {
    const rs = await api.user.getOAuthPendingInfo(oauthToken);
    form.username = rs.data?.username || '';
    form.email = rs.data?.email || '';
    form.mobile = rs.data?.mobile || '';
  } catch {
    message.warning('OAuth预填信息已过期，请重新发起登录');
  }
});
</script>

<style scoped>
.register-container { display:flex; align-items:center; justify-content:center; min-height:100vh; background:linear-gradient(135deg,#667eea 0%, #764ba2 100%); }
.register-card { width: 460px; border-radius: 8px; }
.register-header { text-align:center; margin-bottom: 12px; }
.register-footer-link { text-align:center; margin-top: 8px; color:#666; }
</style>
