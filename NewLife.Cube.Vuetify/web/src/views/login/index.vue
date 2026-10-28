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
              {{ p.nickName || p.name }}
            </v-btn>
          </div>
        </v-card-text>
      </template>

      <v-card-text v-if="forgotEnabled" class="text-center pt-0 pb-0">
        <v-btn variant="text" color="primary" @click="router.push('/forgot-password')">忘记密码？</v-btn>
      </v-card-text>

      <v-card-text v-if="registerEnabled" class="text-center pt-0">
        <span class="text-body-2 text-medium-emphasis">还没有账号？</span>
        <v-btn variant="text" color="primary" @click="goRegister">立即注册</v-btn>
      </v-card-text>
    </v-card>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import type { OAuthProvider } from '@newlifex/cube';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';

const route = useRoute();
const router = useRouter();
const userStore = useUserStore();

const form = reactive({ username: '', password: '' });
const loading = ref(false);
const oauthProviders = ref<OAuthProvider[]>([]);
const registerEnabled = ref(false);
/** 是否展示「忘记密码」入口：重置密码依赖短信/邮件通道，两个通道都关时无意义 */
const forgotEnabled = ref(false);

onMounted(async () => {
  try {
    const res = await cubeApi.user.getLoginConfig();
    oauthProviders.value = res.data?.oauth ?? [];
    registerEnabled.value = res.data?.register?.enabled === true;
    forgotEnabled.value = res.data?.login?.sms === true || res.data?.login?.mail === true;
  } catch {
    // 配置加载失败不影响密码登录，注册入口保持隐藏
  }
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
  // 非 source=front-end 流程：后端写登录 Cookie 后回跳，保留原 redirect 参数
  const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/#/home';
  window.location.href = `/Sso/Login/${encodeURIComponent(name)}?r=${encodeURIComponent(redirect)}`;
}

/** 进入注册页时保留租户等查询参数 */
function goRegister() {
  router.push({ path: '/register', query: route.query });
}
</script>
