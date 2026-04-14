<template>
  <div class="login-wrap">
    <t-card class="login-card" :bordered="false">
      <template #header>
        <div class="login-header">
          <img v-if="logoSrc" :src="logoSrc" class="login-logo" alt="" />
          <h2>{{ appStore.siteTitle }}</h2>
          <p v-if="loginConfig?.loginTip" class="login-tip">{{ loginConfig.loginTip }}</p>
        </div>
      </template>

      <t-alert v-if="error" theme="error" :message="error" style="margin-bottom: 16px" close />

      <t-tabs v-model="activeTab">
        <!-- 密码登录 -->
        <t-tab-panel v-if="loginConfig?.allowLogin !== false" value="password" label="密码登录">
          <t-form @submit="handleLogin" :label-width="0" style="margin-top: 16px">
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
        </t-tab-panel>

        <!-- 手机验证码登录 -->
        <t-tab-panel v-if="loginConfig?.enableSms" value="sms" label="手机验证码">
          <t-form :label-width="0" style="margin-top: 16px">
            <t-form-item>
              <t-input v-model="codeUsername" placeholder="请输入手机号" size="large" clearable />
            </t-form-item>
            <t-form-item>
              <t-input v-model="codeVal" placeholder="请输入验证码" size="large" clearable>
                <template #suffix>
                  <t-button variant="text" :disabled="smsCountdown > 0" @click="sendCode('Sms')">
                    {{ smsCountdown > 0 ? `${smsCountdown}s后重发` : '获取验证码' }}
                  </t-button>
                </template>
              </t-input>
            </t-form-item>
            <t-form-item>
              <t-button theme="primary" block size="large" :loading="codeLoading" @click="handleCodeLogin(1)">登录</t-button>
            </t-form-item>
          </t-form>
        </t-tab-panel>

        <!-- 邮箱验证码登录 -->
        <t-tab-panel v-if="loginConfig?.enableMail" value="email" label="邮箱验证码">
          <t-form :label-width="0" style="margin-top: 16px">
            <t-form-item>
              <t-input v-model="mailUsername" placeholder="请输入邮箱地址" size="large" clearable />
            </t-form-item>
            <t-form-item>
              <t-input v-model="mailCodeVal" placeholder="请输入验证码" size="large" clearable>
                <template #suffix>
                  <t-button variant="text" :disabled="mailCountdown > 0" @click="sendCode('Mail', mailUsername)">
                    {{ mailCountdown > 0 ? `${mailCountdown}s后重发` : '获取验证码' }}
                  </t-button>
                </template>
              </t-input>
            </t-form-item>
            <t-form-item>
              <t-button theme="primary" block size="large" :loading="mailLoading" @click="handleCodeLogin(2, mailUsername, mailCodeVal)">登录</t-button>
            </t-form-item>
          </t-form>
        </t-tab-panel>
      </t-tabs>

      <!-- OAuth 第三方登录 -->
      <template v-if="loginConfig?.providers?.length">
        <t-divider>第三方登录</t-divider>
        <div class="oauth-list">
          <div
            v-for="item in loginConfig.providers"
            :key="item.name"
            class="oauth-item"
            :title="item.nickName || item.name"
            @click="goOAuth(item.name)"
          >
            <img v-if="item.logo" :src="item.logo" class="oauth-logo" :alt="item.nickName || item.name" />
            <span v-else class="oauth-fallback">{{ (item.nickName || item.name).charAt(0).toUpperCase() }}</span>
            <span class="oauth-name">{{ item.nickName || item.name }}</span>
          </div>
        </div>
      </template>

      <!-- 注册入口 -->
      <div v-if="loginConfig?.allowRegister" class="register-link">
        <span>还没有账号？</span>
        <t-link theme="primary" @click="router.push('/register')">立即注册</t-link>
      </div>

      <!-- 版权信息 -->
      <div v-if="appStore.siteInfo?.copyright || appStore.siteInfo?.registration" class="login-footer">
        <div v-if="appStore.siteInfo.copyright" v-html="appStore.siteInfo.copyright"></div>
        <div v-if="appStore.siteInfo.registration">
          <a href="https://www.beianx.cn/" target="_blank" rel="noopener noreferrer">{{ appStore.siteInfo.registration }}</a>
        </div>
      </div>
    </t-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
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
const codeUsername = ref('');
const codeVal = ref('');
const mailUsername = ref('');
const mailCodeVal = ref('');
const loading = ref(false);
const codeLoading = ref(false);
const mailLoading = ref(false);
const smsCountdown = ref(0);
const mailCountdown = ref(0);
const error = ref('');
const activeTab = ref('password');
const loginConfig = ref<LoginConfig | null>(null);

const logoSrc = computed(() => appStore.siteInfo?.loginLogo || loginConfig.value?.logo || '');

onMounted(async () => {
  try {
    const [siteRes, configRes] = await Promise.all([
      api.user.getSiteInfo(),
      api.user.getLoginConfig(),
    ]);
    if (siteRes?.data) appStore.siteInfo = siteRes.data;
    loginConfig.value = configRes?.data ?? null;
    if (configRes?.data?.allowLogin === false) {
      if (configRes.data.enableSms) activeTab.value = 'sms';
      else if (configRes.data.enableMail) activeTab.value = 'email';
    }
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

async function sendCode(channel: 'Sms' | 'Mail', uname?: string) {
  const name = uname ?? codeUsername.value;
  if (!name) {
    error.value = channel === 'Sms' ? '请输入手机号' : '请输入邮箱地址';
    return;
  }
  error.value = '';
  try {
    await api.user.sendCode({ channel, username: name, action: 'login' });
    const countRef = channel === 'Sms' ? smsCountdown : mailCountdown;
    countRef.value = 60;
    const timer = setInterval(() => {
      countRef.value--;
      if (countRef.value <= 0) clearInterval(timer);
    }, 1000);
  } catch (err: any) {
    error.value = err?.message ?? '发送失败';
  }
}

async function handleCodeLogin(loginCategory: 1 | 2, uname?: string, code?: string) {
  const name = uname ?? codeUsername.value;
  const codeStr = code ?? codeVal.value;
  const loadingRef = loginCategory === 1 ? codeLoading : mailLoading;
  if (!name) { error.value = loginCategory === 1 ? '请输入手机号' : '请输入邮箱地址'; return; }
  if (!codeStr) { error.value = '请输入验证码'; return; }
  loadingRef.value = true;
  error.value = '';
  try {
    const res = await api.user.loginByCode({ username: name, password: codeStr, loginCategory });
    if (res.data?.accessToken) {
      api.tokenManager.setToken(res.data.accessToken);
    }
    await userStore.fetchMenus();
    router.push('/');
  } catch (err: any) {
    error.value = err?.message ?? '登录失败';
  } finally {
    loadingRef.value = false;
  }
}

function goOAuth(name: string) {
  window.location.href = `/Sso/Login/${name}`;
}
</script>

<style scoped>
.login-wrap {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #0052d9 0%, #0077ff 100%);
}
.login-card {
  width: 440px;
  border-radius: 8px;
}
.login-header {
  text-align: center;
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
  gap: 16px;
  justify-content: center;
  margin-top: 8px;
}
.oauth-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  cursor: pointer;
  gap: 4px;
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
  background: #e8e8e8;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  font-weight: 600;
}
.oauth-name {
  font-size: 12px;
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
  color: #999;
}
.login-footer {
  margin-top: 16px;
  text-align: center;
  font-size: 12px;
  color: #999;
}
.login-footer a {
  color: #999;
  text-decoration: none;
}
.login-footer a:hover {
  text-decoration: underline;
}
</style>

