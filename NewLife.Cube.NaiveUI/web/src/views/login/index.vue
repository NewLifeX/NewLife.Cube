<template>
  <div class="login-container">
    <n-card class="login-card" :bordered="false">
      <div class="login-header">
        <img v-if="logoSrc" :src="logoSrc" class="login-logo" alt="" />
        <h2>{{ loginConfig?.displayName ?? '魔方管理平台' }}</h2>
        <p v-if="loginConfig?.loginTip" class="login-tip">{{ loginConfig.loginTip }}</p>
      </div>

      <n-tabs v-model:value="activeTab">
        <!-- 密码登录 -->
        <n-tab-pane v-if="loginConfig?.allowLogin !== false" name="password" tab="密码登录">
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
        </n-tab-pane>

        <!-- 手机验证码登录 -->
        <n-tab-pane v-if="loginConfig?.enableSms" name="sms" tab="手机验证码">
          <n-form label-placement="left">
            <n-form-item label="手机号">
              <n-input v-model:value="codeForm.username" placeholder="请输入手机号" />
            </n-form-item>
            <n-form-item label="验证码">
              <n-input-group>
                <n-input v-model:value="codeForm.code" placeholder="请输入验证码" style="flex: 1" />
                <n-button :disabled="smsCountdown > 0" @click="sendCode('Sms')">
                  {{ smsCountdown > 0 ? `${smsCountdown}s后重发` : '获取验证码' }}
                </n-button>
              </n-input-group>
            </n-form-item>
            <n-button type="primary" block :loading="codeLoading" @click="handleCodeLogin(1)">登 录</n-button>
          </n-form>
        </n-tab-pane>

        <!-- 邮箱验证码登录 -->
        <n-tab-pane v-if="loginConfig?.enableMail" name="email" tab="邮箱验证码">
          <n-form label-placement="left">
            <n-form-item label="邮箱">
              <n-input v-model:value="emailForm.username" placeholder="请输入邮箱地址" />
            </n-form-item>
            <n-form-item label="验证码">
              <n-input-group>
                <n-input v-model:value="emailForm.code" placeholder="请输入验证码" style="flex: 1" />
                <n-button :disabled="mailCountdown > 0" @click="sendCode('Mail', emailForm.username)">
                  {{ mailCountdown > 0 ? `${mailCountdown}s后重发` : '获取验证码' }}
                </n-button>
              </n-input-group>
            </n-form-item>
            <n-button type="primary" block :loading="mailLoading" @click="handleCodeLogin(2, emailForm)">登 录</n-button>
          </n-form>
        </n-tab-pane>
      </n-tabs>

      <!-- OAuth 登录 -->
      <div v-if="loginConfig?.providers?.length" class="oauth-section">
        <n-divider>其他登录方式</n-divider>
        <n-space justify="center">
          <div
            v-for="p in loginConfig.providers"
            :key="p.name"
            class="oauth-item"
            :title="p.nickName ?? p.name"
            @click="handleOAuth(p.name)"
          >
            <img v-if="p.logo" :src="p.logo" class="oauth-logo" :alt="p.nickName ?? p.name" />
            <span v-else class="oauth-fallback">{{ (p.nickName ?? p.name).charAt(0).toUpperCase() }}</span>
            <span class="oauth-name">{{ p.nickName ?? p.name }}</span>
          </div>
        </n-space>
      </div>

      <!-- 注册入口 -->
      <div v-if="loginConfig?.allowRegister" class="register-link">
        <span>还没有账号？</span>
        <n-button text type="primary" @click="router.push('/register')">立即注册</n-button>
      </div>

      <!-- 版权信息 -->
      <div v-if="siteInfoData?.copyright || siteInfoData?.registration" class="login-footer">
        <div v-if="siteInfoData.copyright" v-html="siteInfoData.copyright"></div>
        <div v-if="siteInfoData.registration">
          <a href="https://www.beianx.cn/" target="_blank" rel="noopener noreferrer">{{ siteInfoData.registration }}</a>
        </div>
      </div>
    </n-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useMessage, type FormInst, type FormRules } from 'naive-ui';
import { useUserStore } from '@/stores/user';
import api from '@/api';
import type { LoginConfig, SiteInfo } from '@cube/api-core';

const router = useRouter();
const route = useRoute();
const message = useMessage();
const userStore = useUserStore();

const formRef = ref<FormInst | null>(null);
const loading = ref(false);
const codeLoading = ref(false);
const mailLoading = ref(false);
const smsCountdown = ref(0);
const mailCountdown = ref(0);
const activeTab = ref('password');
const loginConfig = ref<LoginConfig | null>(null);
const siteInfoData = ref<SiteInfo | null>(null);

const form = ref({ username: '', password: '' });
const codeForm = ref({ username: '', code: '' });
const emailForm = ref({ username: '', code: '' });

const rules: FormRules = {
  username: { required: true, message: '请输入用户名', trigger: 'blur' },
  password: { required: true, message: '请输入密码', trigger: 'blur' },
};

const logoSrc = computed(() => siteInfoData.value?.loginLogo || loginConfig.value?.logo || '');

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

async function sendCode(channel: 'Sms' | 'Mail', username?: string) {
  const name = username ?? codeForm.value.username;
  if (!name) {
    message.warning(channel === 'Sms' ? '请输入手机号' : '请输入邮箱地址');
    return;
  }
  try {
    await api.user.sendCode({ channel, username: name, action: 'login' });
    message.success(channel === 'Sms' ? '验证码已发送' : '验证码已发送至您的邮箱');
    const countRef = channel === 'Sms' ? smsCountdown : mailCountdown;
    countRef.value = 60;
    const timer = setInterval(() => {
      countRef.value--;
      if (countRef.value <= 0) clearInterval(timer);
    }, 1000);
  } catch (err: any) {
    message.error(err?.message ?? '发送失败');
  }
}

async function handleCodeLogin(loginCategory: 1 | 2, formData?: { username: string; code: string }) {
  const data = formData ?? codeForm.value;
  const loadingRef = loginCategory === 1 ? codeLoading : mailLoading;
  if (!data.username) {
    message.warning(loginCategory === 1 ? '请输入手机号' : '请输入邮箱地址');
    return;
  }
  if (!data.code) {
    message.warning('请输入验证码');
    return;
  }
  loadingRef.value = true;
  try {
    const res = await api.user.loginByCode({ username: data.username, password: data.code, loginCategory });
    if (res.data?.accessToken) {
      api.tokenManager.setToken(res.data.accessToken);
    }
    message.success('登录成功');
    const redirect = (route.query.redirect as string) ?? '/';
    router.replace(redirect);
  } catch (err: any) {
    message.error(err?.message ?? '登录失败');
  } finally {
    loadingRef.value = false;
  }
}

function handleOAuth(provider: string) {
  window.location.href = `/Sso/Login/${provider}`;
}

onMounted(async () => {
  try {
    const [siteRes, configRes] = await Promise.all([
      api.user.getSiteInfo(),
      api.user.getLoginConfig(),
    ]);
    siteInfoData.value = siteRes.data;
    loginConfig.value = configRes.data;
    if (configRes.data?.allowLogin === false) {
      if (configRes.data.enableSms) activeTab.value = 'sms';
      else if (configRes.data.enableMail) activeTab.value = 'email';
    }
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
  margin: 4px 0;
  font-size: 20px;
}
.login-tip {
  color: #999;
  font-size: 13px;
}
.oauth-section {
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
  color: inherit;
  text-decoration: underline;
}
</style>

