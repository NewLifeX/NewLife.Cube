/**
 * 注册页：对齐 LoginConfig.register.* 与飞书风视觉
 */
import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { Message } from '@arco-design/web-vue';
import type { AuthCategory, LoginConfig } from '@cube/api-core';
import cubeApi from '@/api';
import { isRegisterEnabled, needLoginCaptcha } from './loginConfig';
import { persistSession } from './sessionTokens';

export function useRegisterPage() {
  const router = useRouter();
  const route = useRoute();

  const activeTab = ref<'password' | 'mobile' | 'mail'>('password');
  const oauthMode = ref(false);
  const loading = ref(false);
  const sending = ref(false);
  const countdown = ref(0);
  const config = ref<LoginConfig | null>(null);
  const captchaId = ref('');
  const captchaCode = ref('');
  const captchaImage = ref('');

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

  const brandName = computed(() => config.value?.name || '魔方管理平台');
  const logoSrc = computed(() => config.value?.loginLogo || config.value?.logo || '');
  const enablePassword = computed(() => config.value?.register?.password !== false);
  const enableSmsRegister = computed(
    () => !!(config.value?.register?.sms ?? config.value?.enableSmsRegister ?? config.value?.enableSms),
  );
  const enableMailRegister = computed(
    () => !!(config.value?.register?.mail ?? config.value?.enableMailRegister ?? config.value?.enableMail),
  );
  const showCaptcha = computed(
    () => !!(config.value?.register?.captcha ?? needLoginCaptcha(config.value)),
  );
  const registerOpen = computed(() => isRegisterEnabled(config.value) || oauthMode.value);

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

  const sendCode = async (channel: 'Sms' | 'Mail') => {
    const username = channel === 'Sms' ? form.mobile : form.emailCodeTarget;
    if (!username) return Message.warning(channel === 'Sms' ? '请输入手机号' : '请输入邮箱地址');
    if (showCaptcha.value && !captchaCode.value) return Message.warning('请输入验证码');
    sending.value = true;
    try {
      await cubeApi.user.sendCode({
        channel,
        username,
        action: 'register',
        ...(showCaptcha.value ? { captchaId: captchaId.value, captchaCode: captchaCode.value } : {}),
      });
      Message.success('验证码已发送');
      startCountdown();
    } catch (err: unknown) {
      Message.error((err as { message?: string })?.message || '发送失败');
      if (showCaptcha.value) await refreshCaptcha();
    } finally {
      sending.value = false;
    }
  };

  const onSubmit = async () => {
    if (!form.password || !form.confirmPassword) return Message.warning('请输入密码和确认密码');
    if (form.password !== form.confirmPassword) return Message.warning('两次密码不一致');
    if (activeTab.value === 'mobile' && (!form.mobile || !form.code)) return Message.warning('请填写手机号和验证码');
    if (activeTab.value === 'mail' && (!form.emailCodeTarget || !form.code)) return Message.warning('请填写邮箱和验证码');
    if (showCaptcha.value && !captchaCode.value && activeTab.value === 'password') {
      return Message.warning('请输入验证码');
    }

    const captcha =
      showCaptcha.value ? { captchaId: captchaId.value, captchaCode: captchaCode.value } : {};

    const payload = oauthMode.value
      ? {
          category: 'oauth' as AuthCategory,
          oauthToken: form.oauthToken,
          username: form.username,
          email: form.email,
          password: form.password,
          confirmPassword: form.confirmPassword,
          ...captcha,
        }
      : activeTab.value === 'mobile'
        ? {
            category: 'mobile' as AuthCategory,
            username: form.username || form.mobile,
            mobile: form.mobile,
            email: form.email,
            code: form.code,
            password: form.password,
            confirmPassword: form.confirmPassword,
            ...captcha,
          }
        : activeTab.value === 'mail'
          ? {
              category: 'mail' as AuthCategory,
              username: form.username || form.emailCodeTarget,
              email: form.emailCodeTarget,
              code: form.code,
              password: form.password,
              confirmPassword: form.confirmPassword,
              ...captcha,
            }
          : {
              category: '' as AuthCategory,
              username: form.username,
              email: form.email,
              password: form.password,
              confirmPassword: form.confirmPassword,
              ...captcha,
            };

    loading.value = true;
    try {
      const res = await cubeApi.user.register(payload);
      const token = res.data?.accessToken;
      if (token) {
        persistSession(token, res.data?.refreshToken, form.username || form.mobile || form.emailCodeTarget);
        Message.success('注册成功，已自动登录');
        router.push('/home');
        return;
      }
      Message.success('注册成功，请登录');
      router.push('/login');
    } catch (err: unknown) {
      Message.error((err as { message?: string })?.message || '注册失败');
      if (showCaptcha.value) await refreshCaptcha();
    } finally {
      loading.value = false;
    }
  };

  onMounted(async () => {
    try {
      const cfg = await cubeApi.user.getLoginConfig();
      config.value = cfg.data;
      if (!isRegisterEnabled(cfg.data) && !(route.query.oauthToken as string)) {
        Message.warning('当前未开放注册');
      }
      if (cfg.data?.register?.captcha) await refreshCaptcha();
      if (cfg.data?.register?.password === false && cfg.data?.register?.sms) activeTab.value = 'mobile';
      else if (cfg.data?.register?.password === false && cfg.data?.register?.mail) activeTab.value = 'mail';
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

  return {
    oauthMode,
    activeTab,
    enablePassword,
    enableSmsRegister,
    enableMailRegister,
    registerOpen,
    showCaptcha,
    captchaImage,
    captchaCode,
    refreshCaptcha,
    brandName,
    logoSrc,
    form,
    onSubmit,
    countdown,
    sending,
    sendCode,
    loading,
    router,
  };
}
