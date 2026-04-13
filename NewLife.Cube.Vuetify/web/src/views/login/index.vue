<template>
  <div class="d-flex justify-center align-center" style="min-height: 100vh; background: linear-gradient(135deg, #1867C0 0%, #5CBBF6 100%);">
    <v-card width="400" class="pa-6">
      <v-card-title class="text-center text-h5 font-weight-bold">登录</v-card-title>
      <v-card-text>
        <v-form @submit.prevent="handleLogin">
          <v-text-field
            v-model="form.username"
            label="用户名"
            prepend-inner-icon="mdi-account"
            variant="outlined"
            density="comfortable"
            class="mb-2"
          />
          <v-text-field
            v-model="form.password"
            label="密码"
            type="password"
            prepend-inner-icon="mdi-lock"
            variant="outlined"
            density="comfortable"
            class="mb-4"
          />
          <v-btn type="submit" color="primary" block size="large" :loading="loading">登录</v-btn>
        </v-form>
      </v-card-text>
      <template v-if="oauthProviders.length">
        <v-divider class="my-4" />
        <v-card-text class="text-center">
          <div class="text-body-2 mb-2">第三方登录</div>
          <div class="d-flex justify-center ga-2">
            <v-btn v-for="p in oauthProviders" :key="p.name" variant="outlined" @click="oauthLogin(p.name)">
              {{ p.displayName || p.name }}
            </v-btn>
          </div>
        </v-card-text>
      </template>
    </v-card>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import type { OAuthProvider } from '@cube/api-core';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';

const router = useRouter();
const userStore = useUserStore();

const form = reactive({ username: '', password: '' });
const loading = ref(false);
const oauthProviders = ref<OAuthProvider[]>([]);

onMounted(async () => {
  const res = await cubeApi.config.getLoginConfig();
  if (res.data?.providers) oauthProviders.value = res.data.providers;
});

async function handleLogin() {
  if (!form.username || !form.password) return;
  loading.value = true;
  try {
    const res = await userStore.login(form.username, form.password);
    if (res.data) {
      router.push('/home');
    }
  } finally {
    loading.value = false;
  }
}

function oauthLogin(name: string) {
  window.location.href = `/Sso/LoginExternal?provider=${name}`;
}
</script>
