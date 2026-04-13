<template>
  <div class="login-container">
    <n-card class="login-card" :bordered="false">
      <div class="login-header">
        <img v-if="loginConfig?.logo" :src="loginConfig.logo" class="login-logo" alt="" />
        <h2>{{ loginConfig?.displayName ?? '魔方管理平台' }}</h2>
        <p v-if="loginConfig?.loginTip" class="login-tip">{{ loginConfig.loginTip }}</p>
      </div>

      <n-form ref="formRef" :model="form" :rules="rules" label-placement="left">
        <n-form-item path="username" label="用户名">
          <n-input v-model:value="form.username" placeholder="请输入用户名" @keydown.enter="handleLogin" />
        </n-form-item>
        <n-form-item path="password" label="密码">
          <n-input
            v-model:value="form.password"
            type="password"
            show-password-on="click"
            placeholder="请输入密码"
            @keydown.enter="handleLogin"
          />
        </n-form-item>
        <n-button type="primary" block :loading="loading" @click="handleLogin">登 录</n-button>
      </n-form>

      <!-- OAuth 登录 -->
      <div v-if="loginConfig?.providers?.length" class="oauth-section">
        <n-divider>其他登录方式</n-divider>
        <n-space justify="center">
          <n-button
            v-for="p in loginConfig.providers"
            :key="p.name"
            quaternary
            @click="handleOAuth(p.name)"
          >
            <template #icon>
              <img v-if="p.logo" :src="p.logo" style="width: 20px; height: 20px" alt="" />
            </template>
            {{ p.nickName ?? p.name }}
          </n-button>
        </n-space>
      </div>
    </n-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useMessage, type FormInst, type FormRules } from 'naive-ui';
import { useUserStore } from '@/stores/user';
import api from '@/api';
import type { LoginConfig } from '@cube/api-core';

const router = useRouter();
const route = useRoute();
const message = useMessage();
const userStore = useUserStore();

const formRef = ref<FormInst | null>(null);
const loading = ref(false);
const loginConfig = ref<LoginConfig | null>(null);

const form = ref({ username: '', password: '' });

const rules: FormRules = {
  username: { required: true, message: '请输入用户名', trigger: 'blur' },
  password: { required: true, message: '请输入密码', trigger: 'blur' },
};

async function handleLogin() {
  try {
    await formRef.value?.validate();
  } catch {
    return;
  }
  loading.value = true;
  try {
    await userStore.login(form.value.username, form.value.password);
    message.success('登录成功');
    const redirect = (route.query.redirect as string) ?? '/';
    router.replace(redirect);
  } catch (err: any) {
    message.error(err?.message ?? '登录失败');
  } finally {
    loading.value = false;
  }
}

function handleOAuth(provider: string) {
  window.location.href = `/Sso/Login?provider=${encodeURIComponent(provider)}`;
}

onMounted(async () => {
  try {
    const res = await api.user.getLoginConfig();
    loginConfig.value = res.data;
  } catch { /* ignore */ }
});
</script>

<style scoped>
.login-container {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.login-card {
  width: 400px;
  border-radius: 8px;
}

.login-header {
  text-align: center;
  margin-bottom: 24px;
}

.login-logo {
  width: 64px;
  height: 64px;
  margin-bottom: 12px;
}

.login-tip {
  color: #999;
  font-size: 13px;
}

.oauth-section {
  margin-top: 16px;
}
</style>
