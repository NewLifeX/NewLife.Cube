import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import { canConfirmCloseAccount } from '@/core/utils/closeAccount';
import { clearLocalProfile } from '@/core/utils/userProfile';
import { resetMenuRoutesFlag } from '@/router';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';
import { useTenantStore } from '@/stores/tenant';
import { useTagsViewStore } from '@/stores/tagsView';
import { useUserProfileStore } from '@/stores/userProfile';
import { clearSession } from '@/views/login/sessionTokens';

/** 账号安全：注销确认与 POST CloseAccount */
export function useCloseAccount() {
  const router = useRouter();
  const appStore = useAppStore();
  const userStore = useUserStore();
  const tenantStore = useTenantStore();
  const tagsStore = useTagsViewStore();
  const profileStore = useUserProfileStore();

  const confirmVisible = ref(false);
  const confirmName = ref('');
  const submitting = ref(false);

  const userName = computed(() => userStore.userInfo?.name ?? '');
  const canOpen = computed(() => !!userName.value);
  const canSubmit = computed(() =>
    canConfirmCloseAccount(confirmName.value, userName.value) && !submitting.value,
  );

  function openConfirm() {
    if (!canOpen.value) {
      Message.warning('无法确认当前用户名');
      return;
    }
    confirmName.value = '';
    confirmVisible.value = true;
  }

  function closeConfirm() {
    confirmVisible.value = false;
    confirmName.value = '';
  }

  async function submitClose() {
    if (!canSubmit.value) return;
    submitting.value = true;
    try {
      await cubeApi.user.closeAccount();
      // 账号已注销：Logout 可能失败，AuthLogic 会吞掉并清空 Pinia 用户态
      await userStore.logout();
      clearSession();
      clearLocalProfile();
      appStore.clearSessionMetadata();
      tenantStore.clear();
      profileStore.resetSession();
      tagsStore.clearAll();
      resetMenuRoutesFlag();
      confirmVisible.value = false;
      Message.success('账号已注销');
      await router.push('/login');
    } catch (e: unknown) {
      Message.error(formatApiError(e, '注销失败'));
    } finally {
      submitting.value = false;
    }
  }

  return {
    confirmVisible,
    confirmName,
    submitting,
    userName,
    canOpen,
    canSubmit,
    openConfirm,
    closeConfirm,
    submitClose,
  };
}
