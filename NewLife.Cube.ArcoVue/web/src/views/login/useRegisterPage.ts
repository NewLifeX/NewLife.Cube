import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { Message } from '@arco-design/web-vue';
import type { AuthCategory } from '@cube/api-core';
import cubeApi from '@/api';

/** 注册页全部业务 TS：注册配置加载、验证码发送与注册提交（自 register.vue script setup 原样搬移） */
export function useRegisterPage() {
  const router = useRouter();
  const route = useRoute();

  const activeTab = ref<'password' | 'mobile' | 'mail'>('password');
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
    if (activeTab.value === 'mobile' && (!form.mobile || !form.code)) return Message.warning('请填写手机号和验证码');
    if (activeTab.value === 'mail' && (!form.emailCodeTarget || !form.code)) return Message.warning('请填写邮笱和验证码');

    const payload = oauthMode.value
      ? { category: 'oauth' as AuthCategory, oauthToken: form.oauthToken, username: form.username, email: form.email, password: form.password, confirmPassword: form.confirmPassword }
      : activeTab.value === 'mobile'
        ? { category: 'mobile' as AuthCategory, username: form.username || form.mobile, mobile: form.mobile, email: form.email, code: form.code, password: form.password, confirmPassword: form.confirmPassword }
        : activeTab.value === 'mail'
          ? { category: 'mail' as AuthCategory, username: form.username || form.emailCodeTarget, email: form.emailCodeTarget, code: form.code, password: form.password, confirmPassword: form.confirmPassword }
          : { category: '' as AuthCategory, username: form.username, email: form.email, password: form.password, confirmPassword: form.confirmPassword };

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

  return {
    oauthMode,
    activeTab,
    enableSmsRegister,
    enableMailRegister,
    form,
    onSubmit,
    countdown,
    sending,
    sendCode,
    loading,
    router,
  };
}
