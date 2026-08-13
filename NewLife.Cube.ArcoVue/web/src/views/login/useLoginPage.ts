import { reactive, ref, onMounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import { Message } from '@arco-design/web-vue';
import type { OAuthProvider, LoginConfig } from '@cube/api-core';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';
import { useUserProfileStore } from '@/stores/userProfile';

/** 登录页全部业务 TS：登录配置加载、密码/验证码登录与 OAuth 跳转（自 index.vue script setup 原样搬移） */
export function useLoginPage() {
  const router = useRouter();
  const userStore = useUserStore();
  const profileStore = useUserProfileStore();

  async function afterLoginSuccess() {
    await userStore.fetchUserInfo();
    await userStore.fetchMenus();
    await profileStore.loadFromServer();
  }

  const form = reactive({ username: '', password: '' });
  const codeForm = reactive({ username: '', code: '' });
  const emailForm = reactive({ username: '', code: '' });
  const loading = ref(false);
  const codeLoading = ref(false);
  const mailLoading = ref(false);
  const smsCountdown = ref(0);
  const mailCountdown = ref(0);
  const activeTab = ref('password');
  const oauthProviders = ref<OAuthProvider[]>([]);
  const loginConfig = ref<LoginConfig | null>(null);

  const logoSrc = computed(() => loginConfig.value?.loginLogo || loginConfig.value?.logo || '');

  onMounted(async () => {
    try {
      const configRes = await cubeApi.user.getLoginConfig();
      loginConfig.value = configRes.data;
      oauthProviders.value = configRes.data?.providers ?? [];
      // 自动切换到第一个可用 tab
      if (configRes.data?.allowLogin === false) {
        if (configRes.data.enableSms) activeTab.value = 'sms';
        else if (configRes.data.enableMail) activeTab.value = 'email';
      }
    } catch { /* ignore */ }
  });

  async function handleLogin() {
    if (!form.username || !form.password) {
      Message.warning('请输入用户名和密码');
      return;
    }
    loading.value = true;
    try {
      // 参考 NewLife.Cube.Vue 登录处理：直接明文密码登录，绕开 RSA Challenge 加密。
      // 后端 AllowPlainPassword 默认开启；RSA 解密在部分后端版本存在 PEM/XML 兼容问题，
      // 使用 Challenge 加密会触发 "No supported key formats were found" 错误。
      const res = await cubeApi.user.login({ username: form.username, password: form.password });
      if (res.data?.accessToken) {
        cubeApi.tokenManager.setToken(res.data.accessToken);
        await afterLoginSuccess();
        Message.success('登录成功');
        router.push('/home');
      } else {
        Message.error(res.message || '登录失败');
      }
    } catch (err: any) {
      Message.error(err?.message || '登录失败');
    } finally {
      loading.value = false;
    }
  }

  async function sendCode(channel: 'Sms' | 'Mail', username?: string) {
    const name = username ?? codeForm.username;
    if (!name) {
      Message.warning(channel === 'Sms' ? '请输入手机号' : '请输入邮箱地址');
      return;
    }
    try {
      await cubeApi.user.sendCode({ channel, username: name, action: 'login' });
      Message.success(channel === 'Sms' ? '验证码已发送' : '验证码已发送至您的邮箱');
      const countRef = channel === 'Sms' ? smsCountdown : mailCountdown;
      countRef.value = 60;
      const timer = setInterval(() => {
        countRef.value--;
        if (countRef.value <= 0) clearInterval(timer);
      }, 1000);
    } catch (err: any) {
      Message.error(err?.message || '发送失败');
    }
  }

  async function handleCodeLogin(loginCategory: 'mobile' | 'mail', formData?: { username: string; code: string }) {
    const data = formData ?? codeForm;
    const loadingRef = loginCategory === 'mobile' ? codeLoading : mailLoading;
    if (!data.username) {
      Message.warning(loginCategory === 'mobile' ? '请输入手机号' : '请输入邮箱地址');
      return;
    }
    if (!data.code) {
      Message.warning('请输入验证码');
      return;
    }
    loadingRef.value = true;
    try {
      const res = await cubeApi.user.login({ username: data.username, password: data.code, category: loginCategory });
      if (res.data?.accessToken) {
        cubeApi.tokenManager.setToken(res.data.accessToken);
        await afterLoginSuccess();
      }
      Message.success('登录成功');
      router.push('/home');
    } catch (err: any) {
      Message.error(err?.message || '登录失败');
    } finally {
      loadingRef.value = false;
    }
  }

  function oauthLogin(name: string) {
    window.location.href = `/Sso/Login/${name}?r=${encodeURIComponent(new URLSearchParams(window.location.search).get('redirect') || '/')}`;
  }

  return {
    logoSrc,
    loginConfig,
    activeTab,
    form,
    handleLogin,
    codeForm,
    smsCountdown,
    sendCode,
    codeLoading,
    handleCodeLogin,
    emailForm,
    mailCountdown,
    mailLoading,
    oauthProviders,
    oauthLogin,
    router,
    loading,
  };
}
