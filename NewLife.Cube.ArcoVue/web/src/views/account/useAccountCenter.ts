/**
 * 账号中心：资料 / 改密 / 安全 / 绑定 Tab。
 */
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import {
  ACCOUNT_PASSWORD_FIELDS,
  ACCOUNT_PROFILE_FIELDS,
  accountFooterKind,
  accountFooterLabel,
  applyProfileField,
  buildProfilePayload,
  parseAccountTab,
  pickProfileForm,
  resolveSsoAccountUrl,
  type AccountFooterKind,
  type AccountTab,
  type ProfileForm,
} from '@/core/utils/accountCenter';
import { formatApiError } from '@/core/utils/apiError';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';

export function useAccountCenter() {
  const route = useRoute();
  const router = useRouter();
  const appStore = useAppStore();
  const userStore = useUserStore();

  const tab = computed<AccountTab>({
    get: () => parseAccountTab(route.query.tab),
    set: (v) => {
      void router.replace({ path: '/account', query: { tab: v } });
    },
  });

  const ssoProfileUrl = computed(() => resolveSsoAccountUrl(appStore.loginConfig, 'profile'));
  const ssoPasswordUrl = computed(() => resolveSsoAccountUrl(appStore.loginConfig, 'password'));

  const profileLoading = ref(false);
  const profileSaving = ref(false);
  const profile = reactive<ProfileForm>({
    id: 0,
    displayName: '',
    sex: 0,
    mail: '',
    mobile: '',
    name: '',
    roleNames: '',
  });

  const passwordSaving = ref(false);
  const passwordForm = reactive({
    oldPassword: '',
    newPassword: '',
    newPassword2: '',
  });

  async function loadProfile() {
    if (ssoProfileUrl.value) return;
    profileLoading.value = true;
    try {
      const res = await cubeApi.user.profile();
      const next = pickProfileForm(res.data);
      Object.assign(profile, next);
    } catch (e: unknown) {
      Message.error(formatApiError(e, '加载资料失败'));
    } finally {
      profileLoading.value = false;
    }
  }

  async function saveProfile() {
    if (ssoProfileUrl.value) return;
    profileSaving.value = true;
    try {
      await cubeApi.user.updateProfile(buildProfilePayload(profile));
      Message.success('保存成功');
      const info = userStore.userInfo;
      if (info) {
        userStore.userInfo = { ...info, displayName: profile.displayName };
      }
    } catch (e: unknown) {
      Message.error(formatApiError(e, '保存失败'));
    } finally {
      profileSaving.value = false;
    }
  }

  async function savePassword() {
    if (ssoPasswordUrl.value) return;
    const next = passwordForm.newPassword.trim();
    if (!next) {
      Message.warning('请输入新密码');
      return;
    }
    if (next !== passwordForm.newPassword2) {
      Message.warning('两次输入的新密码不一致');
      return;
    }
    passwordSaving.value = true;
    try {
      await cubeApi.user.changePassword({
        oldPassword: passwordForm.oldPassword,
        newPassword: passwordForm.newPassword,
        newPassword2: passwordForm.newPassword2,
      });
      Message.success('密码已修改');
      passwordForm.oldPassword = '';
      passwordForm.newPassword = '';
      passwordForm.newPassword2 = '';
    } catch (e: unknown) {
      Message.error(formatApiError(e, '修改密码失败'));
    } finally {
      passwordSaving.value = false;
    }
  }

  function goSso(kind: 'profile' | 'password') {
    const url = kind === 'password' ? ssoPasswordUrl.value : ssoProfileUrl.value;
    if (url) window.location.assign(url);
  }

  function onProfileField(name: string, value: unknown) {
    applyProfileField(profile, name, value);
  }

  function profileValue(name: string): unknown {
    return (profile as unknown as Record<string, unknown>)[name];
  }

  const footerKind = computed<AccountFooterKind | null>(() =>
    accountFooterKind(tab.value, !!ssoProfileUrl.value, !!ssoPasswordUrl.value),
  );
  const footerLabel = computed(() => accountFooterLabel(footerKind.value));
  const footerLoading = computed(() => {
    const k = footerKind.value;
    if (k === 'save') return profileSaving.value;
    if (k === 'password') return passwordSaving.value;
    return false;
  });

  function onFooterClick() {
    const k = footerKind.value;
    if (k === 'save') void saveProfile();
    else if (k === 'password') void savePassword();
    else if (k === 'ssoProfile') goSso('profile');
    else if (k === 'ssoPassword') goSso('password');
  }

  function onTabChange(key: string | number) {
    tab.value = parseAccountTab(key);
  }

  onMounted(() => {
    void loadProfile();
  });

  watch(ssoProfileUrl, (url) => {
    if (!url) void loadProfile();
  });

  return {
    tab,
    onTabChange,
    ssoProfileUrl,
    ssoPasswordUrl,
    profileLoading,
    profile,
    profileFields: ACCOUNT_PROFILE_FIELDS,
    passwordFields: ACCOUNT_PASSWORD_FIELDS,
    passwordForm,
    onProfileField,
    profileValue,
    footerKind,
    footerLabel,
    footerLoading,
    onFooterClick,
  };
}
