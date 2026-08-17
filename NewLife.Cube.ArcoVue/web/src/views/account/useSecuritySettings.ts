/**
 * 账号安全：MFA + 第三方绑定
 */
import { computed, onMounted, ref } from 'vue';
import { Message, Modal } from '@arco-design/web-vue';
import type { AuthBindItem } from '@cube/api-core';
import cubeApi from '@/api';
import { useAppStore } from '@/stores/app';
import { isOAuthLoginEnabled } from '@/views/login/loginConfig';
import { buildTotpQrDataUrl, resolveOAuthBindKey } from './mfaQr';

export function useSecuritySettings() {
  const appStore = useAppStore();
  const loading = ref(false);
  const mfaAvailable = ref(false);
  const mfaEnabled = ref(false);
  const setupUri = ref('');
  const setupSecret = ref('');
  const setupQrDataUrl = ref('');
  const activateCode = ref('');
  const disableCode = ref('');
  const backupCodes = ref<string[]>([]);
  const binds = ref<AuthBindItem[]>([]);
  const step = ref<'idle' | 'setup' | 'backup'>('idle');
  /** 有可见第三方提供商时显示绑定区（与登录页同源：oauth 列表，不绑 EnableOAuthServer） */
  const oauthEnabled = computed(() => {
    const cfg = appStore.loginConfig;
    if (!cfg) return true;
    return isOAuthLoginEnabled(cfg);
  });

  async function loadStatus() {
    loading.value = true;
    try {
      const st = await cubeApi.user.mfaStatus();
      mfaAvailable.value = !!st.data?.available;
      mfaEnabled.value = !!st.data?.enabled;
      if (oauthEnabled.value) {
        const bd = await cubeApi.user.listBinds();
        binds.value = bd.data?.providers || [];
      } else {
        binds.value = [];
      }
    } catch (e: unknown) {
      Message.error((e as { message?: string })?.message || '加载失败');
    } finally {
      loading.value = false;
    }
  }

  async function startSetup() {
    try {
      const res = await cubeApi.user.mfaSetup();
      setupUri.value = res.data?.totpUri || res.data?.qrCodeUri || '';
      setupSecret.value = res.data?.secret || '';
      activateCode.value = '';
      setupQrDataUrl.value = setupUri.value ? await buildTotpQrDataUrl(setupUri.value) : '';
      step.value = 'setup';
    } catch (e: unknown) {
      Message.error((e as { message?: string })?.message || '初始化失败');
    }
  }

  async function activate() {
    if (!activateCode.value) {
      Message.warning('请输入验证码');
      return;
    }
    try {
      const res = await cubeApi.user.mfaActivate(activateCode.value);
      backupCodes.value = res.data?.backupCodes || [];
      mfaEnabled.value = true;
      step.value = 'backup';
      Message.success('MFA 已开启');
    } catch (e: unknown) {
      Message.error((e as { message?: string })?.message || '激活失败');
    }
  }

  async function disable() {
    if (!disableCode.value) {
      Message.warning('请输入验证码');
      return;
    }
    try {
      await cubeApi.user.mfaDisable(disableCode.value);
      mfaEnabled.value = false;
      disableCode.value = '';
      step.value = 'idle';
      Message.success('MFA 已关闭');
    } catch (e: unknown) {
      Message.error((e as { message?: string })?.message || '关闭失败');
    }
  }

  function bindProvider(item: AuthBindItem) {
    const key = resolveOAuthBindKey(item);
    window.location.href = `/Sso/Bind/${encodeURIComponent(key)}`;
  }

  function unbindProvider(item: AuthBindItem) {
    const key = resolveOAuthBindKey(item);
    Modal.confirm({
      title: '解除绑定',
      content: `确定解除与「${item.nickName || item.name}」的绑定？`,
      onOk: async () => {
        try {
          await cubeApi.user.unbindOAuth(key);
          Message.success('已解绑');
          await loadStatus();
        } catch (e: unknown) {
          Message.error((e as { message?: string })?.message || '解绑失败');
        }
      },
    });
  }

  function copyBackup() {
    const text = backupCodes.value.join('\n');
    void navigator.clipboard?.writeText(text);
    Message.success('已复制备用码');
  }

  function copySecret() {
    if (!setupSecret.value && !setupUri.value) return;
    void navigator.clipboard?.writeText(setupSecret.value || setupUri.value);
    Message.success('已复制');
  }

  onMounted(loadStatus);

  return {
    loading,
    mfaAvailable,
    mfaEnabled,
    setupUri,
    setupSecret,
    setupQrDataUrl,
    activateCode,
    disableCode,
    backupCodes,
    binds,
    oauthEnabled,
    step,
    startSetup,
    activate,
    disable,
    bindProvider,
    unbindProvider,
    copyBackup,
    copySecret,
  };
}
