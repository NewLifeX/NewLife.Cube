import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';

type ActivateChannel = 'mail' | 'sms';

/** 账号激活页：邮件链接直达 + 邮箱/短信验证码 */
export function useActivatePage() {
  const route = useRoute();
  const router = useRouter();

  const linkMode = ref(false);
  const linkLoading = ref(false);
  const linkError = ref('');
  const done = ref(false);

  const sending = ref(false);
  const submitting = ref(false);
  const formError = ref('');

  const form = reactive({
    channel: 'mail' as ActivateChannel,
    account: '',
    code: '',
  });

  const canSubmit = computed(
    () => !!form.account.trim() && !!form.code.trim() && !submitting.value,
  );

  function goLogin() {
    void router.push('/login');
  }

  async function activateByLink(token: string, account: string) {
    linkMode.value = true;
    linkLoading.value = true;
    linkError.value = '';
    try {
      await cubeApi.user.activateByLink(token, account);
      done.value = true;
      Message.success('激活成功');
      window.setTimeout(() => {
        void router.push('/login');
      }, 1000);
    } catch (e: unknown) {
      linkError.value = formatApiError(e, '激活失败');
      Message.error(linkError.value);
    } finally {
      linkLoading.value = false;
    }
  }

  async function onSendCode() {
    if (!form.account.trim()) {
      Message.warning('请输入账号');
      return;
    }
    sending.value = true;
    formError.value = '';
    try {
      await cubeApi.user.sendActivateCode(form.channel, form.account.trim());
      Message.success('验证码已发送');
    } catch (e: unknown) {
      const msg = formatApiError(e, '发送失败');
      formError.value = msg;
      Message.error(msg);
    } finally {
      sending.value = false;
    }
  }

  async function onSubmit() {
    if (!canSubmit.value) return;
    submitting.value = true;
    formError.value = '';
    try {
      await cubeApi.user.activateByCode({
        channel: form.channel,
        account: form.account.trim(),
        code: form.code.trim(),
      });
      done.value = true;
      Message.success('激活成功');
      window.setTimeout(() => {
        void router.push('/login');
      }, 1000);
    } catch (e: unknown) {
      const msg = formatApiError(e, '激活失败');
      formError.value = msg;
      Message.error(msg);
    } finally {
      submitting.value = false;
    }
  }

  onMounted(() => {
    const token = String(route.query.token ?? '').trim();
    const account = String(route.query.account ?? '').trim();
    if (token && account) {
      form.account = account;
      void activateByLink(token, account);
    }
  });

  return {
    linkMode,
    linkLoading,
    linkError,
    done,
    sending,
    submitting,
    formError,
    form,
    canSubmit,
    goLogin,
    onSendCode,
    onSubmit,
  };
}
