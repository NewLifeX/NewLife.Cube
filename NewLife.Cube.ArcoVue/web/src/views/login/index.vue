<template>
  <div class="login-shell">
    <aside class="login-brand" :style="bgStyle">
      <div class="brand-inner">
        <img v-if="logoSrc" :src="logoSrc" class="brand-logo" alt="" />
        <h1 class="brand-name">{{ brandName }}</h1>
        <p v-if="loginConfig?.loginTip" class="brand-tip">{{ loginConfig.loginTip }}</p>
      </div>
      <div v-if="loginConfig?.copyright || loginConfig?.registration" class="brand-footer">
        <div v-if="loginConfig?.copyright" class="brand-copyright" v-html="loginConfig.copyright" />
        <div v-if="loginConfig?.registration" class="brand-beian">{{ loginConfig.registration }}</div>
      </div>
    </aside>

    <main class="login-main">
      <div class="login-panel">
        <div class="mobile-brand">
          <img v-if="logoSrc" :src="logoSrc" class="brand-logo sm" alt="" />
          <h2>{{ brandName }}</h2>
        </div>

        <template v-if="screen === 'form'">
          <h2 class="panel-title">登录</h2>

          <a-empty v-if="noLoginChannel" description="未开放登录" />

          <template v-else>
            <a-tabs v-if="tabs.length > 1" v-model:active-key="activeTab">
              <a-tab-pane v-for="t in tabs" :key="t.key" :title="t.title" />
            </a-tabs>

            <a-form
              v-if="activeTab === 'password' && tabs.some((t) => t.key === 'password')"
              :model="form"
              layout="vertical"
              @submit.prevent="handleLogin"
            >
              <a-form-item label="用户名">
                <a-input v-model="form.username" placeholder="请输入用户名" allow-clear @press-enter="handleLogin" />
              </a-form-item>
              <a-form-item label="密码">
                <a-input-password v-model="form.password" placeholder="请输入密码" allow-clear @press-enter="handleLogin" />
              </a-form-item>
              <a-form-item v-if="showCaptcha" label="验证码">
                <a-input-group class="captcha-group">
                  <a-input v-model="captchaCode" placeholder="计算结果" @press-enter="handleLogin" />
                  <span class="captcha-img" title="点击刷新" @click="refreshCaptcha" v-html="captchaImage" />
                </a-input-group>
              </a-form-item>
              <a-button type="primary" long :loading="loading" @click="handleLogin">登录</a-button>
            </a-form>

            <a-form v-else-if="activeTab === 'sms' && tabs.some((t) => t.key === 'sms')" :model="codeForm" layout="vertical">
              <a-form-item label="手机号">
                <a-input v-model="codeForm.username" placeholder="请输入手机号" allow-clear />
              </a-form-item>
              <a-form-item v-if="showSendCodeCaptcha" label="验证码">
                <a-input-group class="captcha-group">
                  <a-input v-model="captchaCode" placeholder="计算结果" />
                  <span class="captcha-img" @click="refreshCaptcha" v-html="captchaImage" />
                </a-input-group>
              </a-form-item>
              <a-form-item label="短信验证码">
                <a-input-group class="captcha-group">
                  <a-input v-model="codeForm.code" placeholder="请输入验证码" />
                  <a-button :disabled="smsCountdown > 0" @click="sendCode('Sms')">
                    {{ smsCountdown > 0 ? `${smsCountdown}s` : '获取验证码' }}
                  </a-button>
                </a-input-group>
              </a-form-item>
              <a-button type="primary" long :loading="codeLoading" @click="handleCodeLogin('mobile')">登录</a-button>
            </a-form>

            <a-form v-else-if="activeTab === 'mail' && tabs.some((t) => t.key === 'mail')" :model="emailForm" layout="vertical">
              <a-form-item label="邮箱">
                <a-input v-model="emailForm.username" placeholder="请输入邮箱" allow-clear />
              </a-form-item>
              <a-form-item v-if="showSendCodeCaptcha" label="验证码">
                <a-input-group class="captcha-group">
                  <a-input v-model="captchaCode" placeholder="计算结果" />
                  <span class="captcha-img" @click="refreshCaptcha" v-html="captchaImage" />
                </a-input-group>
              </a-form-item>
              <a-form-item label="邮箱验证码">
                <a-input-group class="captcha-group">
                  <a-input v-model="emailForm.code" placeholder="请输入验证码" />
                  <a-button :disabled="mailCountdown > 0" @click="sendCode('Mail', emailForm.username)">
                    {{ mailCountdown > 0 ? `${mailCountdown}s` : '获取验证码' }}
                  </a-button>
                </a-input-group>
              </a-form-item>
              <a-button type="primary" long :loading="mailLoading" @click="handleCodeLogin('mail', emailForm)">登录</a-button>
            </a-form>

            <div class="panel-links">
              <a-link @click="router.push('/forgot-password')">忘记密码？</a-link>
              <a-link v-if="showRegister" @click="router.push('/register')">注册账号</a-link>
            </div>

            <template v-if="showOAuth && oauthProviders.length">
              <a-divider orientation="center">第三方登录</a-divider>
              <div class="oauth-list">
                <a-tooltip
                  v-for="p in oauthProviders"
                  :key="p.name"
                  :content="oauthTip(p)"
                >
                  <button
                    type="button"
                    class="oauth-item"
                    @click="oauthLogin(p.name!)"
                  >
                    <img v-if="p.logo" :src="p.logo" class="oauth-logo" alt="" />
                    <span v-else class="oauth-fallback">{{ (p.nickName || p.name || '?').charAt(0) }}</span>
                  </button>
                </a-tooltip>
              </div>
            </template>
          </template>
        </template>

        <template v-else>
          <h2 class="panel-title">两步验证</h2>
          <p class="mfa-hint">请输入 Authenticator 验证码或备用码</p>
          <a-form layout="vertical" @submit.prevent="handleMfaVerify">
            <a-form-item label="验证码">
              <a-input v-model="mfaCode" placeholder="6 位或备用码" allow-clear />
            </a-form-item>
            <a-space direction="vertical" fill>
              <a-button type="primary" long :loading="mfaLoading" html-type="submit">验证并登录</a-button>
              <a-button long @click="backToForm">返回</a-button>
            </a-space>
          </a-form>
        </template>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import type { OAuthProvider } from '@cube/api-core';
import { useLoginPage } from './useLoginPage';

const {
  screen,
  brandName,
  logoSrc,
  bgStyle,
  loginConfig,
  showOAuth,
  tabs,
  activeTab,
  noLoginChannel,
  showRegister,
  showCaptcha,
  showSendCodeCaptcha,
  captchaImage,
  captchaCode,
  refreshCaptcha,
  form,
  handleLogin,
  codeForm,
  smsCountdown,
  sendCode,
  codeLoading,
  handleCodeLogin,
  emailForm,
  mailCountdown,
  mailLoading,
  oauthProviders,
  oauthLogin,
  router,
  loading,
  mfaCode,
  mfaLoading,
  handleMfaVerify,
  backToForm,
} = useLoginPage();

/** 徽标悬停说明：优先 Remark，回落显示名 */
function oauthTip(p: OAuthProvider) {
  return (p.remark || p.nickName || p.name || '').trim();
}
</script>

<style scoped>
.login-shell {
  display: flex;
  min-height: 100vh;
  background: var(--color-bg-1);
}
.login-brand {
  display: none;
  flex: 1;
  position: relative;
  background: linear-gradient(
    145deg,
    var(--cube-primary) 0%,
    color-mix(in srgb, var(--cube-primary) 50%, #1d2129) 100%
  );
  color: #fff;
  padding: 48px 40px 32px;
  flex-direction: column;
  justify-content: space-between;
}
.brand-inner {
  margin-top: 12vh;
}
.brand-logo {
  width: 64px;
  height: 64px;
  object-fit: contain;
  margin-bottom: 16px;
}
.brand-logo.sm {
  width: 40px;
  height: 40px;
}
.brand-name {
  margin: 0 0 12px;
  font-size: 32px;
  font-weight: 600;
  letter-spacing: 0.02em;
}
.brand-tip {
  margin: 0;
  opacity: 0.85;
  font-size: 15px;
  max-width: 360px;
  line-height: 1.5;
}
.brand-footer {
  font-size: 12px;
  opacity: 0.75;
}
.login-main {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 32px 20px;
}
.login-panel {
  width: 100%;
  max-width: 400px;
}
.mobile-brand {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 20px;
}
.mobile-brand h2 {
  margin: 0;
  font-size: 20px;
}
.panel-title {
  margin: 0 0 16px;
  font-size: 24px;
  font-weight: 600;
}
.panel-links {
  display: flex;
  justify-content: space-between;
  margin-top: 12px;
}
.oauth-list {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  justify-content: center;
}
.oauth-item {
  border: none;
  background: transparent;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: var(--color-text-2);
  padding: 4px;
}
.oauth-logo,
.oauth-fallback {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  object-fit: contain;
}
.oauth-fallback {
  background: var(--color-fill-2);
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
}
.captcha-group {
  width: 100%;
  display: inline-flex;
}
.captcha-group :deep(.arco-input-wrapper),
.captcha-group :deep(.arco-input-outer) {
  flex: 1;
  min-width: 0;
}
.captcha-img {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  min-width: 100px;
  height: 32px;
  cursor: pointer;
  background: var(--color-fill-2);
  padding: 0 4px;
  overflow: hidden;
}
.captcha-img :deep(img),
.captcha-img :deep(svg) {
  display: block;
  height: 28px;
  width: auto;
  max-width: 160px;
}
.mfa-hint {
  color: var(--color-text-3);
  font-size: 13px;
  margin: 0 0 12px;
}
@media (min-width: 992px) {
  .login-brand {
    display: flex;
  }
  .mobile-brand {
    display: none;
  }
}
</style>
