<template>
  <div class="register-wrap">
    <t-card class="register-card" :bordered="false">
      <template #header>
        <div class="register-header"><h2>注册账号</h2></div>
      </template>

      <t-tabs v-if="!oauthMode" v-model="activeTab">
        <t-tab-panel value="password" label="账号注册" />
        <t-tab-panel v-if="enableSmsRegister" value="phone" label="手机注册" />
        <t-tab-panel v-if="enableMailRegister" value="email" label="邮箱注册" />
      </t-tabs>

      <t-alert v-if="oauthMode" theme="info" message="第三方账号首次登录，请补全密码完成本地账号创建" style="margin-bottom:12px" />

      <t-form :label-width="0">
        <t-form-item v-if="activeTab==='password' || oauthMode"><t-input v-model="form.username" placeholder="用户名" :readonly="oauthMode" /></t-form-item>
        <t-form-item v-if="activeTab==='password' || activeTab==='email' || oauthMode"><t-input v-model="form.email" placeholder="邮箱" /></t-form-item>

        <t-form-item v-if="activeTab==='phone'">
          <t-input v-model="form.mobile" placeholder="手机号">
            <template #suffix>
              <t-button variant="text" :disabled="countdown>0" :loading="sending" @click="sendCode('Sms')">{{ countdown>0 ? `${countdown}s` : '发送验证码' }}</t-button>
            </template>
          </t-input>
        </t-form-item>

        <t-form-item v-if="activeTab==='email'">
          <t-input v-model="form.emailCodeTarget" placeholder="邮箱地址">
            <template #suffix>
              <t-button variant="text" :disabled="countdown>0" :loading="sending" @click="sendCode('Mail')">{{ countdown>0 ? `${countdown}s` : '发送验证码' }}</t-button>
            </template>
          </t-input>
        </t-form-item>

        <t-form-item v-if="activeTab==='phone' || activeTab==='email'"><t-input v-model="form.code" placeholder="验证码" /></t-form-item>
        <t-form-item><t-input v-model="form.password" type="password" placeholder="密码" /></t-form-item>
        <t-form-item><t-input v-model="form.confirmPassword" type="password" placeholder="确认密码" /></t-form-item>
        <t-form-item><t-button theme="primary" block :loading="loading" @click="onSubmit">{{ oauthMode ? '完成绑定并登录' : '立即注册' }}</t-button></t-form-item>
      </t-form>

      <div class="register-footer-link">
        <span>已有账号？</span>
        <t-link theme="primary" @click="router.push('/login')">去登录</t-link>
      </div>
    </t-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { RegisterCategory } from '@cube/api-core';
import { api } from '@/api';

const router = useRouter();
const route = useRoute();

const activeTab = ref<'password' | 'phone' | 'email'>('password');
const oauthMode = ref(false);
const loading = ref(false);
const sending = ref(false);
const countdown = ref(0);
const error = ref('');
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
  if (!username) { error.value = channel === 'Sms' ? '请输入手机号' : '请输入邮箱地址'; return; }
  sending.value = true;
  error.value = '';
  try {
    await api.user.sendCode({ channel, username, action: 'register' });
    startCountdown();
  } catch (err: any) {
    error.value = err?.message || '发送失败';
  } finally {
    sending.value = false;
  }
};

const onSubmit = async () => {
  if (!form.password || !form.confirmPassword) { error.value = '请输入密码和确认密码'; return; }
  if (form.password !== form.confirmPassword) { error.value = '两次密码不一致'; return; }
  if (activeTab.value === 'phone' && (!form.mobile || !form.code)) { error.value = '请填写手机号和验证码'; return; }
  if (activeTab.value === 'email' && (!form.emailCodeTarget || !form.code)) { error.value = '请填写邮箱和验证码'; return; }

  const payload = oauthMode.value
    ? { registerCategory: RegisterCategory.OAuthBind, oauthToken: form.oauthToken, username: form.username, email: form.email, password: form.password, confirmPassword: form.confirmPassword }
    : activeTab.value === 'phone'
      ? { registerCategory: RegisterCategory.Phone, username: form.username || form.mobile, mobile: form.mobile, email: form.email, code: form.code, password: form.password, confirmPassword: form.confirmPassword }
      : activeTab.value === 'email'
        ? { registerCategory: RegisterCategory.Email, username: form.username || form.emailCodeTarget, email: form.emailCodeTarget, code: form.code, password: form.password, confirmPassword: form.confirmPassword }
        : { registerCategory: RegisterCategory.Password, username: form.username, email: form.email, password: form.password, confirmPassword: form.confirmPassword };

  loading.value = true;
  error.value = '';
  try {
    const res = await api.user.register(payload as any);
    const token = res.data?.accessToken || (res.data as any)?.token;
    if (token) {
      api.tokenManager.setToken(token);
      router.push('/');
      return;
    }
    router.push('/login');
  } catch (err: any) {
    error.value = err?.message || '注册失败';
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
    error.value = 'OAuth预填信息已过期，请重新发起登录';
  }
});
</script>

<style scoped>
.register-wrap { min-height:100vh; display:flex; align-items:center; justify-content:center; background:linear-gradient(135deg,#0052d9 0%, #0077ff 100%); }
.register-card { width: 460px; border-radius: 8px; }
.register-header { text-align:center; }
.register-footer-link { text-align:center; margin-top: 10px; color:#999; }
</style>
