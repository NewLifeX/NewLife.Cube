/**
 * 登录页业务：嵌套 LoginConfig、验证码、Challenge、MFA 二步、OAuth source=front-end。
 */
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { Message } from '@arco-design/web-vue';
import { encryptPassword, type LoginConfig, type OAuthProvider } from '@cube/api-core';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';
import { useUserProfileStore } from '@/stores/userProfile';
import {
  buildSsoLoginUrl,
  extractMfaToken,
  isRegisterEnabled,
  needChallenge,
  needLoginCaptcha,
  resolveLoginTabs,
  resolveOAuthProviders,
  type LoginTabKey,
} from './loginConfig';
import { persistSession } from './sessionTokens';

export function useLoginPage() {
  const router = useRouter();
  const route = useRoute();
  const userStore = useUserStore();
  const profileStore = useUserProfileStore();

  const screen = ref<'form' | 'mfa'>('form');
  const mfaToken = ref('');
  const mfaCode = ref('');
  const mfaLoading = ref(false);

  const tenantCode = ref('');
  const loginConfig = ref<LoginConfig | null>(null);
  const oauthProviders = ref<OAuthProvider[]>([]);
  const tabs = computed(() => resolveLoginTabs(loginConfig.value));
  const activeTab = ref<LoginTabKey>('password');
  const showRegister = computed(() => isRegisterEnabled(loginConfig.value));
  const showCaptcha = computed(() => needLoginCaptcha(loginConfig.value));

  const form = reactive({ username: '', password: '' });
  const codeForm = reactive({ username: '', code: '' });
  const emailForm = reactive({ username: '', code: '' });
  const captchaId = ref('');
  const captchaCode = ref('');
  const captchaImage = ref('');

  const loading = ref(false);
  const codeLoading = ref(false);
  const mailLoading = ref(false);
  const smsCountdown = ref(0);
  const mailCountdown = ref(0);

  const brandName = computed(() => loginConfig.value?.name || '魔方管理平台');
  const logoSrc = computed(
    () => loginConfig.value?.loginLogo || loginConfig.value?.logo || '',
  );
  const bgStyle = computed(() => {
    const bg = loginConfig.value?.loginBackground;
    if (!bg) return {};
    return {
      backgroundImage: `url(${bg})`,
      backgroundSize: 'cover',
      backgroundPosition: 'center',
    };
  });
  const noLoginChannel = computed(
    () => tabs.value.length === 0 && oauthProviders.value.length === 0,
  );

  async function afterLoginSuccess() {
    await userStore.fetchUserInfo();
    await userStore.fetchMenus();
    try {
      await profileStore.loadFromServer();
    } catch {
      /* 偏好加载失败不阻断登录 */
    }
  }

  function redirectAfterLogin() {
    const r = (route.query.redirect as string) || '/home';
    router.replace(r.startsWith('/') ? r : '/home');
  }

  function pickAccessToken(data?: Record<string, unknown> | null): string {
    if (!data) return '';
    return String(
      data.accessToken || data.access_token || data.Token || data.token || '',
    );
  }

  function pickRefreshToken(data?: Record<string, unknown> | null): string | undefined {
    if (!data) return undefined;
    const v = data.refreshToken || data.refresh_token || data.RefreshToken;
    return v ? String(v) : undefined;
  }

  async function applyTokens(accessToken?: string, refreshToken?: string, userName?: string) {
    if (!accessToken) return false;
    persistSession(accessToken, refreshToken, userName);
    Message.success('登录成功');
    // 先跳转，再拉用户信息，避免 Info/Profile 挂起时按钮一直转圈像“没反应”
    redirectAfterLogin();
    try {
      await afterLoginSuccess();
    } catch (err: unknown) {
      Message.warning((err as { message?: string })?.message || '已登录，但加载用户信息失败');
    }
    return true;
  }

  async function loadConfig(tenant?: string) {
    try {
      const configRes = await cubeApi.user.getLoginConfig(tenant || undefined);
      loginConfig.value = configRes.data;
      oauthProviders.value = resolveOAuthProviders(configRes.data);
      const t = resolveLoginTabs(configRes.data);
      if (t.length) activeTab.value = t[0].key;
      if (needLoginCaptcha(configRes.data)) await refreshCaptcha();
    } catch {
      /* ignore */
    }
  }

  async function refreshCaptcha() {
    try {
      const res = await cubeApi.user.getCaptcha();
      captchaId.value = res.data?.captchaId || '';
      captchaImage.value = res.data?.image || '';
      captchaCode.value = '';
    } catch {
      captchaId.value = '';
      captchaImage.value = '';
    }
  }

  watch(tenantCode, (v) => {
    const t = v.trim();
    if (t.length >= 2 || t.length === 0) loadConfig(t || undefined);
  });

  onMounted(() => loadConfig());

  async function encryptIfNeeded(password: string): Promise<{ password: string; challengeId?: string }> {
    if (!needChallenge(loginConfig.value)) return { password };
    try {
      const challengeRes = await cubeApi.user.getChallenge();
      const challenge = challengeRes.data;
      if (!challenge?.publicKey || !challenge.challengeId) {
        throw new Error('Challenge 无效');
      }
      const encrypted = await encryptPassword(password, challenge.publicKey);
      return { password: encrypted, challengeId: challenge.challengeId };
    } catch {
      Message.error('加密失败');
      throw new Error('challenge_failed');
    }
  }

  function captchaPayload() {
    if (!showCaptcha.value) return {};
    return { captchaId: captchaId.value, captchaCode: captchaCode.value };
  }

  async function handleLoginResult(
    res: { data?: Record<string, unknown> | null; message?: string | null },
    userName: string,
  ) {
    const access = pickAccessToken(res.data);
    if (access) {
      await applyTokens(access, pickRefreshToken(res.data), userName);
      return;
    }
    const token = extractMfaToken(res.message);
    if (token) {
      mfaToken.value = token;
      mfaCode.value = '';
      screen.value = 'mfa';
      return;
    }
    Message.error(res.message || '登录失败');
    if (showCaptcha.value) await refreshCaptcha();
  }

  async function handleLogin() {
    if (!form.username || !form.password) {
      Message.warning('请输入用户名和密码');
      return;
    }
    if (showCaptcha.value && !captchaCode.value) {
      Message.warning('请输入验证码');
      return;
    }
    if (loading.value) return;
    loading.value = true;
    try {
      const enc = await encryptIfNeeded(form.password);
      const res = await cubeApi.user.login({
        username: form.username,
        password: enc.password,
        ...(enc.challengeId ? { challengeId: enc.challengeId } : {}),
        ...captchaPayload(),
      });
      await handleLoginResult(
        res as unknown as { data?: Record<string, unknown>; message?: string },
        form.username,
      );
    } catch (err: unknown) {
      const e = err as {
        message?: string;
        response?: { data?: { message?: string }; message?: string };
      };
      // ApiError 的完整消息在 err.message；部分路径在 response.message
      const msg =
        e?.response?.data?.message ||
        (e as { response?: { message?: string } })?.response?.message ||
        e?.message ||
        '';
      const token = extractMfaToken(msg);
      if (token) {
        mfaToken.value = token;
        screen.value = 'mfa';
      } else if (msg !== 'challenge_failed') {
        Message.error(msg || '登录失败');
        if (showCaptcha.value) await refreshCaptcha();
      }
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
    if (showCaptcha.value && !captchaCode.value) {
      Message.warning('请输入验证码');
      return;
    }
    try {
      await cubeApi.user.sendCode({
        channel,
        username: name,
        action: 'login',
        ...captchaPayload(),
      });
      Message.success(channel === 'Sms' ? '验证码已发送' : '验证码已发送至您的邮箱');
      const countRef = channel === 'Sms' ? smsCountdown : mailCountdown;
      countRef.value = 60;
      const timer = setInterval(() => {
        countRef.value--;
        if (countRef.value <= 0) clearInterval(timer);
      }, 1000);
    } catch (err: unknown) {
      Message.error((err as { message?: string })?.message || '发送失败');
      if (showCaptcha.value) await refreshCaptcha();
    }
  }

  async function handleCodeLogin(
    loginCategory: 'mobile' | 'mail',
    formData?: { username: string; code: string },
  ) {
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
      const res = await cubeApi.user.login({
        username: data.username,
        password: data.code,
        category: loginCategory,
        ...captchaPayload(),
      });
      await handleLoginResult(
        res as unknown as { data?: Record<string, unknown>; message?: string },
        data.username,
      );
    } catch (err: unknown) {
      const e = err as { message?: string; response?: { data?: { message?: string } } };
      const msg = e?.response?.data?.message || e?.message || '';
      const token = extractMfaToken(msg);
      if (token) {
        mfaToken.value = token;
        screen.value = 'mfa';
      } else {
        Message.error(msg || '登录失败');
      }
    } finally {
      loadingRef.value = false;
    }
  }

  async function handleMfaVerify() {
    if (!mfaCode.value) {
      Message.warning('请输入验证码');
      return;
    }
    mfaLoading.value = true;
    try {
      const res = await cubeApi.user.mfaVerify({ mfaToken: mfaToken.value, code: mfaCode.value });
      const data = (res.data || {}) as unknown as Record<string, unknown>;
      const access = pickAccessToken(data);
      const refresh = pickRefreshToken(data);
      if (access) {
        await applyTokens(access, refresh, form.username || codeForm.username || emailForm.username);
      } else {
        Message.error(res.message || '验证失败');
      }
    } catch (err: unknown) {
      Message.error((err as { message?: string })?.message || '验证码错误');
    } finally {
      mfaLoading.value = false;
    }
  }

  function backToForm() {
    screen.value = 'form';
    mfaToken.value = '';
    mfaCode.value = '';
  }

  function oauthLogin(name: string) {
    const redirect = (route.query.redirect as string) || undefined;
    window.location.href = buildSsoLoginUrl(name, redirect);
  }

  return {
    screen,
    brandName,
    logoSrc,
    bgStyle,
    loginConfig,
    tenantCode,
    tabs,
    activeTab,
    noLoginChannel,
    showRegister,
    showCaptcha,
    captchaImage,
    captchaCode,
    refreshCaptcha,
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
    mfaCode,
    mfaLoading,
    handleMfaVerify,
    backToForm,
  };
}
