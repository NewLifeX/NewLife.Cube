<template>
  <div style="display: flex; justify-content: center; align-items: center; min-height: 100vh; background: linear-gradient(135deg, #165DFF 0%, #722ED1 100%);">
    <a-card style="width: 400px;" :bordered="false">
      <template #title>
        <div style="text-align: center; font-size: 20px; font-weight: bold;">登录</div>
      </template>
      <a-form :model="form" @submit="handleLogin" layout="vertical">
        <a-form-item field="username" label="用户名">
          <a-input v-model="form.username" placeholder="请输入用户名" allow-clear />
        </a-form-item>
        <a-form-item field="password" label="密码">
          <a-input-password v-model="form.password" placeholder="请输入密码" allow-clear />
        </a-form-item>
        <a-form-item>
          <a-button type="primary" html-type="submit" long :loading="loading">登录</a-button>
        </a-form-item>
      </a-form>
      <a-divider v-if="oauthProviders.length">第三方登录</a-divider>
      <div v-if="oauthProviders.length" style="display: flex; gap: 8px; justify-content: center;">
        <a-button v-for="p in oauthProviders" :key="p.name" @click="oauthLogin(p.name)">
          {{ p.nickName || p.name }}
        </a-button>
      </div>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { Message } from '@arco-design/web-vue';
import type { OAuthProvider } from '@cube/api-core';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';

const router = useRouter();
const userStore = useUserStore();

const form = reactive({ username: '', password: '' });
const loading = ref(false);
const oauthProviders = ref<OAuthProvider[]>([]);

onMounted(async () => {
  const res = await cubeApi.user.getLoginConfig();
  if (res.data?.providers) oauthProviders.value = res.data.providers;
});

async function handleLogin() {
  if (!form.username || !form.password) {
    Message.warning('请输入用户名和密码');
    return;
  }
  loading.value = true;
  try {
    const res = await userStore.login(form.username, form.password);
    if (res.data) {
      Message.success('登录成功');
      router.push('/home');
    } else {
      Message.error(res.message || '登录失败');
    }
  } finally {
    loading.value = false;
  }
}

function oauthLogin(name: string) {
  window.location.href = `/Sso/LoginExternal?provider=${name}`;
}
</script>
