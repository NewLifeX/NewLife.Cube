<template>
  <div style="min-height: 100vh; display: flex; align-items: center; justify-content: center; background: linear-gradient(135deg, #0052d9 0%, #0077ff 100%)">
    <t-card style="width: 400px" :bordered="false">
      <template #header>
        <h2 style="text-align: center; margin: 0">{{ appStore.siteTitle }}</h2>
      </template>

      <t-alert v-if="error" theme="error" :message="error" style="margin-bottom: 16px" close />

      <t-form @submit="handleLogin" :label-width="0">
        <t-form-item>
          <t-input v-model="username" placeholder="请输入用户名" size="large" clearable>
            <template #prefix-icon><user-icon /></template>
          </t-input>
        </t-form-item>
        <t-form-item>
          <t-input v-model="password" type="password" placeholder="请输入密码" size="large" clearable>
            <template #prefix-icon><lock-on-icon /></template>
          </t-input>
        </t-form-item>
        <t-form-item>
          <t-button theme="primary" type="submit" block size="large" :loading="loading">登录</t-button>
        </t-form-item>
      </t-form>

      <template v-if="loginConfig?.oauthItems?.length">
        <t-divider>第三方登录</t-divider>
        <div style="display: flex; justify-content: center; gap: 12px">
          <t-button v-for="item in loginConfig.oauthItems" :key="item.name" variant="outline" @click="window.location.href = item.url">
            {{ item.name }}
          </t-button>
        </div>
      </template>
    </t-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { UserIcon, LockOnIcon } from 'tdesign-icons-vue-next';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';
import { api } from '@/api';
import type { LoginConfig } from '@cube/api-core';

const appStore = useAppStore();
const userStore = useUserStore();
const router = useRouter();

const username = ref('');
const password = ref('');
const loading = ref(false);
const error = ref('');
const loginConfig = ref<LoginConfig | null>(null);

onMounted(async () => {
  try {
    const [siteRes, configRes] = await Promise.all([
      api.config.getSiteInfo(),
      api.config.getLoginConfig(),
    ]);
    if (siteRes?.data) appStore.siteInfo = siteRes.data;
    loginConfig.value = configRes?.data ?? null;
  } catch { /* ignore */ }
});

async function handleLogin() {
  if (!username.value || !password.value) { error.value = '请输入用户名和密码'; return; }
  loading.value = true;
  error.value = '';
  try {
    const ok = await userStore.login(username.value, password.value);
    if (ok) {
      await userStore.fetchMenus();
      router.push('/');
    } else {
      error.value = '用户名或密码错误';
    }
  } catch {
    error.value = '登录失败，请检查网络';
  } finally {
    loading.value = false;
  }
}
</script>
