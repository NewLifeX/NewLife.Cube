import { computed, onMounted, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { Message } from '@arco-design/web-vue';
import type { LoginConfig } from '@cube/api-core';
import cubeApi from '@/api';
import { useAppStore } from '@/stores/app';
import { needSendCodeCaptcha, validatePasswordStrength } from './loginConfig';
import { normalizeCaptchaImageHtml } from './captchaImage';

/** 忘记密码页：验证码发送与密码重置 */
export function useForgotPassword() {
  const router = useRouter();
  const appStore = useAppStore();

  const step = ref<'input' | 'verify'>('input');
  const sending = ref(false);
  const submitting = ref(false);
  const countdown = ref(0);
  const error = ref('');
  const config = ref<LoginConfig | null>(null);
  const captchaId = ref('');
  const captchaCode = ref('');
  const captchaImage = ref('');

  const form = reactive({
    username: '',
    channel: 'Sms' as 'Sms' | 'Mail',
    code: '',
    newPassword: '',
    confirmPassword: '',
  });

  let _timer: ReturnType<typeof setInterval> | null = null;

  const showSendCodeCaptcha = computed(() => needSendCodeCaptcha(config.value));
  const passwordHint = computed(() => {
    const p = config.value?.security?.passwordStrength;
    if (!p || p === '*') return '';
    return '请按系统密码强度要求设置密码';
  });

  const startCountdown = (seconds = 60) => {
    if (_timer) clearInterval(_timer);
    countdown.value = seconds;
    _timer = setInterval(() => {
      countdown.value -= 1;
      if (countdown.value <= 0) { clearInterval(_timer!); _timer = null; }
    }, 1000);
  };

  async function refreshCaptcha() {
    try {
      const res = await cubeApi.user.getCaptcha();
      captchaId.value = res.data?.captchaId || '';
      captchaImage.value = normalizeCaptchaImageHtml(res.data?.image);
      captchaCode.value = '';
    } catch {
      captchaId.value = '';
      captchaImage.value = '';
    }
  }

  const confirmPwdValidator = (_: unknown, value: string, callback: (msg?: string) => void) => {
    if (value !== form.newPassword) callback('两次密码不一致');
    else callback();
  };

  const sendWithCaptcha = async () => {
    await cubeApi.user.sendCode({
      channel: form.channel,
      username: form.username,
      action: 'reset',
      ...(showSendCodeCaptcha.value
        ? { captchaId: captchaId.value, captchaCode: captchaCode.value }
        : {}),
    });
  };

  const onSendCode = async () => {
    if (!form.username) { Message.warning('请输入手机号或邮箱'); return; }
    if (showSendCodeCaptcha.value && !captchaCode.value) {
      Message.warning('请输入图片验证码');
      return;
    }
    sending.value = true;
    error.value = '';
    try {
      await sendWithCaptcha();
      step.value = 'verify';
      startCountdown();
      Message.success('验证码已发送');
      if (showSendCodeCaptcha.value) await refreshCaptcha();
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : '发送失败，请稍后重试';
      Message.error(msg);
      if (showSendCodeCaptcha.value) await refreshCaptcha();
    } finally {
      sending.value = false;
    }
  };

  const onResend = async () => {
    if (countdown.value > 0) return;
    if (showSendCodeCaptcha.value && !captchaCode.value) {
      Message.warning('请输入图片验证码');
      return;
    }
    sending.value = true;
    error.value = '';
    try {
      await sendWithCaptcha();
      startCountdown();
      Message.success('验证码已重新发送');
      if (showSendCodeCaptcha.value) await refreshCaptcha();
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : '发送失败';
      Message.error(msg);
      if (showSendCodeCaptcha.value) await refreshCaptcha();
    } finally {
      sending.value = false;
    }
  };

  const onConfirmReset = async () => {
    if (!form.code) { error.value = '请输入验证码'; return; }
    if (!form.newPassword) { error.value = '请输入新密码'; return; }
    if (form.newPassword !== form.confirmPassword) { error.value = '两次密码不一致'; return; }
    const strengthErr = validatePasswordStrength(form.newPassword, config.value?.security?.passwordStrength);
    if (strengthErr) { error.value = strengthErr; return; }
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

  onMounted(async () => {
    try {
      const cfg = await cubeApi.user.getLoginConfig();
      config.value = cfg.data;
      if (cfg.data) appStore.loginConfig = cfg.data;
      if (needSendCodeCaptcha(cfg.data)) await refreshCaptcha();
    } catch { /* ignore */ }
  });

  return {
    step,
    form,
    sending,
    submitting,
    countdown,
    error,
    router,
    showSendCodeCaptcha,
    passwordHint,
    captchaImage,
    captchaCode,
    refreshCaptcha,
    onSendCode,
    onResend,
    onConfirmReset,
    confirmPwdValidator,
  };
}
