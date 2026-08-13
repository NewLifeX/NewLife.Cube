import { reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';

/** 忘记密码页全部业务 TS：验证码发送与密码重置（自 forgot-password.vue script setup 原样搬移） */
export function useForgotPassword() {
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

  return {
    step,
    form,
    sending,
    submitting,
    countdown,
    error,
    router,
    onSendCode,
    onResend,
    onConfirmReset,
    confirmPwdValidator,
  };
}
