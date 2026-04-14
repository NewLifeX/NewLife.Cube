<template>
  <div class="register-wrap">
    <a-card class="register-card" :bordered="false">
      <div class="register-header">
        <h2>注册账号</h2>
      </div>

      <a-tabs v-if="!oauthMode" v-model:active-key="activeTab">
        <a-tab-pane key="password" title="账号注册" />
        <a-tab-pane v-if="enableSmsRegister" key="phone" title="手机注册" />
        <a-tab-pane v-if="enableMailRegister" key="email" title="邮箱注册" />
      </a-tabs>

      <a-alert v-if="oauthMode" type="info" style="margin-bottom: 12px">第三方账号首次登录，请补全密码完成本地账号创建</a-alert>

      <a-form :model="form" layout="vertical" @submit="onSubmit">
        <a-form-item v-if="activeTab==='password' || oauthMode" field="username" label="用户名">
          <a-input v-model="form.username" :readonly="oauthMode" allow-clear />
        </a-form-item>

        <a-form-item v-if="activeTab==='password' || activeTab==='email' || oauthMode" field="email" label="邮箱">
          <a-input v-model="form.email" allow-clear />
        </a-form-item>

        <a-form-item v-if="activeTab==='phone'" field="mobile" label="手机号">
          <a-input-group>
            <a-input v-model="form.mobile" allow-clear style="flex:1" />
            <a-button :disabled="countdown>0" :loading="sending" @click="sendCode('Sms')">{{ countdown>0 ? `${countdown}s` : '发送验证码' }}</a-button>
          </a-input-group>
        </a-form-item>

        <a-form-item v-if="activeTab==='email'" field="emailCodeTarget" label="邮箱地址">
          <a-input-group>
            <a-input v-model="form.emailCodeTarget" allow-clear style="flex:1" />
            <a-button :disabled="countdown>0" :loading="sending" @click="sendCode('Mail')">{{ countdown>0 ? `${countdown}s` : '发送验证码' }}</a-button>
          </a-input-group>
        </a-form-item>

        <a-form-item v-if="activeTab==='phone' || activeTab==='email'" field="code" label="验证码">
          <a-input v-model="form.code" allow-clear />
        </a-form-item>

        <a-form-item field="password" label="密码">
          <a-input-password v-model="form.password" allow-clear />
        </a-form-item>

        <a-form-item field="confirmPassword" label="确认密码">
          <a-input-password v-model="form.confirmPassword" allow-clear />
        </a-form-item>

        <a-form-item>
          <a-button type="primary" html-type="submit" long :loading="loading">{{ oauthMode ? '完成绑定并登录' : '立即注册' }}</a-button>
        </a-form-item>
      </a-form>

      <div class="register-footer-link">
        <span>已有账号？</span>
        <a-link @click="router.push('/login')">去登录</a-link>
      </div>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { Message } from '@arco-design/web-vue';
import { RegisterCategory } from '@cube/api-core';
import cubeApi from '@/api';

const router = useRouter();
const route = useRoute();

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
  if (!username) return Message.warning(channel === 'Sms' ? '请输入手机号' : '请输入邮箱地址');
  sending.value = true;
  try {
    await cubeApi.user.sendCode({ channel, username, action: 'register' });
    Message.success('验证码已发送');
    startCountdown();
  } catch (err: any) {
    Message.error(err?.message || '发送失败');
  } finally {
    sending.value = false;
  }
};

const onSubmit = async () => {
  if (!form.password || !form.confirmPassword) return Message.warning('请输入密码和确认密码');
  if (form.password !== form.confirmPassword) return Message.warning('两次密码不一致');
  if (activeTab.value === 'phone' && (!form.mobile || !form.code)) return Message.warning('请填写手机号和验证码');
  if (activeTab.value === 'email' && (!form.emailCodeTarget || !form.code)) return Message.warning('请填写邮箱和验证码');

  const payload = oauthMode.value
    ? { registerCategory: RegisterCategory.OAuthBind, oauthToken: form.oauthToken, username: form.username, email: form.email, password: form.password, confirmPassword: form.confirmPassword }
    : activeTab.value === 'phone'
      ? { registerCategory: RegisterCategory.Phone, username: form.username || form.mobile, mobile: form.mobile, email: form.email, code: form.code, password: form.password, confirmPassword: form.confirmPassword }
      : activeTab.value === 'email'
        ? { registerCategory: RegisterCategory.Email, username: form.username || form.emailCodeTarget, email: form.emailCodeTarget, code: form.code, password: form.password, confirmPassword: form.confirmPassword }
        : { registerCategory: RegisterCategory.Password, username: form.username, email: form.email, password: form.password, confirmPassword: form.confirmPassword };

  loading.value = true;
  try {
    const res = await cubeApi.user.register(payload as any);
    const token = res.data?.accessToken || (res.data as any)?.token;
    if (token) {
      cubeApi.tokenManager.setToken(token);
      Message.success('注册成功，已自动登录');
      router.push('/home');
      return;
    }
    Message.success('注册成功，请登录');
    router.push('/login');
  } catch (err: any) {
    Message.error(err?.message || '注册失败');
  } finally {
    loading.value = false;
  }
};

onMounted(async () => {
  try {
    const cfg = await cubeApi.user.getLoginConfig();
    config.value = cfg.data;
  } catch { /* ignore */ }

  const oauthToken = (route.query.oauthToken as string) || '';
  if (!oauthToken) return;
  oauthMode.value = true;
  form.oauthToken = oauthToken;
  try {
    const rs = await cubeApi.user.getOAuthPendingInfo(oauthToken);
    form.username = rs.data?.username || '';
    form.email = rs.data?.email || '';
    form.mobile = rs.data?.mobile || '';
  } catch {
    Message.warning('OAuth预填信息已过期，请重新发起登录');
  }
});
</script>

<style scoped>
.register-wrap { display:flex; justify-content:center; align-items:center; min-height:100vh; background:linear-gradient(135deg,#165dff 0%, #722ed1 100%); }
.register-card { width: 460px; border-radius: 8px; }
.register-header { text-align:center; margin-bottom: 12px; }
.register-footer-link { text-align:center; margin-top: 8px; color:#86909c; }
</style>
