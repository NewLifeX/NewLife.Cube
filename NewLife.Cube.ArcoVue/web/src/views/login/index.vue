<template>
  <div class="login-wrap">
    <a-card class="login-card" :bordered="false">
      <div class="login-header">
        <img v-if="logoSrc" :src="logoSrc" class="login-logo" alt="" />
        <h2>{{ siteInfo?.displayName || '魔方管理平台' }}</h2>
        <p v-if="loginConfig?.loginTip" class="login-tip">{{ loginConfig.loginTip }}</p>
      </div>

      <a-tabs v-model:active-key="activeTab">
        <!-- 密码登录 -->
        <a-tab-pane v-if="loginConfig?.allowLogin !== false" key="password" title="密码登录">
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
        </a-tab-pane>

        <!-- 手机验证码登录 -->
        <a-tab-pane v-if="loginConfig?.enableSms" key="sms" title="手机验证码">
          <a-form :model="codeForm" layout="vertical">
            <a-form-item label="手机号">
              <a-input v-model="codeForm.username" placeholder="请输入手机号" allow-clear />
            </a-form-item>
            <a-form-item label="验证码">
              <a-input-group>
                <a-input v-model="codeForm.code" placeholder="请输入验证码" style="flex: 1" allow-clear />
                <a-button :disabled="smsCountdown > 0" @click="sendCode('Sms')">
                  {{ smsCountdown > 0 ? `${smsCountdown}s 后重发` : '获取验证码' }}
                </a-button>
              </a-input-group>
            </a-form-item>
            <a-form-item>
              <a-button type="primary" long :loading="codeLoading" @click="handleCodeLogin(1)">登录</a-button>
            </a-form-item>
          </a-form>
        </a-tab-pane>

        <!-- 邮箱验证码登录 -->
        <a-tab-pane v-if="loginConfig?.enableMail" key="email" title="邮箱验证码">
          <a-form :model="emailForm" layout="vertical">
            <a-form-item label="邮箱">
              <a-input v-model="emailForm.username" placeholder="请输入邮箱地址" allow-clear />
            </a-form-item>
            <a-form-item label="验证码">
              <a-input-group>
                <a-input v-model="emailForm.code" placeholder="请输入验证码" style="flex: 1" allow-clear />
                <a-button :disabled="mailCountdown > 0" @click="sendCode('Mail', emailForm.username)">
                  {{ mailCountdown > 0 ? `${mailCountdown}s 后重发` : '获取验证码' }}
                </a-button>
              </a-input-group>
            </a-form-item>
            <a-form-item>
              <a-button type="primary" long :loading="mailLoading" @click="handleCodeLogin(2, emailForm)">登录</a-button>
            </a-form-item>
          </a-form>
        </a-tab-pane>
      </a-tabs>

      <!-- OAuth 第三方登录 -->
      <template v-if="oauthProviders.length">
        <a-divider>第三方登录</a-divider>
        <div class="oauth-list">
          <a
            v-for="p in oauthProviders"
            :key="p.name"
            class="oauth-item"
            :title="p.nickName || p.name"
            @click="oauthLogin(p.name)"
          >
            <img v-if="p.logo" :src="p.logo" class="oauth-logo" :alt="p.nickName || p.name" />
            <span v-else class="oauth-fallback">{{ (p.nickName || p.name).charAt(0).toUpperCase() }}</span>
            <span class="oauth-name">{{ p.nickName || p.name }}</span>
          </a>
        </div>
      </template>

      <!-- 注册入口 -->
      <div v-if="loginConfig?.allowRegister" class="register-link">
        <span>还没有账号？</span>
        <a-link @click="router.push('/register')">立即注册</a-link>
      </div>

      <!-- 版权信息 -->
      <div v-if="siteInfo?.copyright || siteInfo?.registration" class="login-footer">
        <div v-if="siteInfo?.copyright" v-html="siteInfo.copyright" class="login-copyright"></div>
        <div v-if="siteInfo?.registration">
          <a href="https://www.beianx.cn/" target="_blank" rel="noopener noreferrer">{{ siteInfo.registration }}</a>
        </div>
      </div>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, onMounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import { Message } from '@arco-design/web-vue';
import type { OAuthProvider, SiteInfo, LoginConfig } from '@cube/api-core';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';

const router = useRouter();
const userStore = useUserStore();

const form = reactive({ username: '', password: '' });
const codeForm = reactive({ username: '', code: '' });
const emailForm = reactive({ username: '', code: '' });
const loading = ref(false);
const codeLoading = ref(false);
const mailLoading = ref(false);
const smsCountdown = ref(0);
const mailCountdown = ref(0);
const activeTab = ref('password');
const oauthProviders = ref<OAuthProvider[]>([]);
const loginConfig = ref<LoginConfig | null>(null);
const siteInfo = ref<SiteInfo | null>(null);

const logoSrc = computed(() => siteInfo.value?.loginLogo || loginConfig.value?.logo || '');

onMounted(async () => {
  try {
    const [siteRes, configRes] = await Promise.all([
      cubeApi.user.getSiteInfo(),
      cubeApi.user.getLoginConfig(),
    ]);
    siteInfo.value = siteRes.data;
    loginConfig.value = configRes.data;
    oauthProviders.value = configRes.data?.providers ?? [];
    // 自动切换到第一个可用 tab
    if (configRes.data?.allowLogin === false) {
      if (configRes.data.enableSms) activeTab.value = 'sms';
      else if (configRes.data.enableMail) activeTab.value = 'email';
    }
  } catch { /* ignore */ }
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

async function sendCode(channel: 'Sms' | 'Mail', username?: string) {
  const name = username ?? codeForm.username;
  if (!name) {
    Message.warning(channel === 'Sms' ? '请输入手机号' : '请输入邮箱地址');
    return;
  }
  try {
    await cubeApi.user.sendCode({ channel, username: name, action: 'login' });
    Message.success(channel === 'Sms' ? '验证码已发送' : '验证码已发送至您的邮箱');
    const countRef = channel === 'Sms' ? smsCountdown : mailCountdown;
    countRef.value = 60;
    const timer = setInterval(() => {
      countRef.value--;
      if (countRef.value <= 0) clearInterval(timer);
    }, 1000);
  } catch (err: any) {
    Message.error(err?.message || '发送失败');
  }
}

async function handleCodeLogin(loginCategory: 1 | 2, formData?: { username: string; code: string }) {
  const data = formData ?? codeForm;
  const loadingRef = loginCategory === 1 ? codeLoading : mailLoading;
  if (!data.username) {
    Message.warning(loginCategory === 1 ? '请输入手机号' : '请输入邮箱地址');
    return;
  }
  if (!data.code) {
    Message.warning('请输入验证码');
    return;
  }
  loadingRef.value = true;
  try {
    const res = await cubeApi.user.loginByCode({ username: data.username, password: data.code, loginCategory });
    if (res.data?.accessToken) {
      cubeApi.tokenManager.setToken(res.data.accessToken);
      await userStore.fetchUserInfo();
      await userStore.fetchMenus();
    }
    Message.success('登录成功');
    router.push('/home');
  } catch (err: any) {
    Message.error(err?.message || '登录失败');
  } finally {
    loadingRef.value = false;
  }
}

function oauthLogin(name: string) {
  window.location.href = `/Sso/Login/${name}`;
}
</script>

<style scoped>
.login-wrap {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background: linear-gradient(135deg, #165dff 0%, #722ed1 100%);
}
.login-card {
  width: 440px;
  border-radius: 8px;
}
.login-header {
  text-align: center;
  margin-bottom: 16px;
}
.login-logo {
  width: 52px;
  height: 52px;
  margin-bottom: 8px;
}
.login-header h2 {
  margin: 0 0 4px;
  font-size: 20px;
}
.login-tip {
  color: #999;
  font-size: 13px;
  margin: 0;
}
.oauth-list {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  justify-content: center;
  margin-top: 8px;
}
.oauth-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  cursor: pointer;
  color: inherit;
  text-decoration: none;
}
.oauth-item:hover .oauth-name {
  color: var(--primary-6, #165dff);
}
.oauth-logo {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  object-fit: contain;
}
.oauth-fallback {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: #e4e6f0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  font-weight: 600;
}
.oauth-name {
  font-size: 12px;
  margin-top: 4px;
  max-width: 56px;
  text-align: center;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.register-link {
  text-align: center;
  margin-top: 12px;
  font-size: 13px;
  color: #86909c;
}
.login-footer {
  text-align: center;
  margin-top: 16px;
  font-size: 12px;
  color: #86909c;
}
.login-footer a {
  color: #86909c;
  text-decoration: none;
}
.login-footer a:hover {
  color: var(--primary-6, #165dff);
}
</style>

