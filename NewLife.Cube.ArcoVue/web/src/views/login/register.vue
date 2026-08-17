<template>
  <div class="auth-shell">
    <aside class="auth-brand">
      <div class="brand-inner">
        <img v-if="logoSrc" :src="logoSrc" class="brand-logo" alt="" />
        <h1 class="brand-name">{{ brandName }}</h1>
      </div>
    </aside>
    <main class="auth-main">
      <div class="auth-panel">
        <h2 class="panel-title">注册账号</h2>

        <a-empty v-if="!registerOpen" description="未开放注册" />

        <template v-else>
          <a-tabs v-model:active-key="activeTab">
            <a-tab-pane v-if="enablePassword" key="password" title="账号注册" />
            <a-tab-pane v-if="enableSmsRegister" key="mobile" title="手机注册" />
            <a-tab-pane v-if="enableMailRegister" key="mail" title="邮箱注册" />
          </a-tabs>

          <a-form :model="form" layout="vertical" @submit.prevent="onSubmit">
            <a-form-item v-if="activeTab === 'password'" field="username" label="用户名">
              <a-input v-model="form.username" allow-clear />
            </a-form-item>

            <a-form-item v-if="activeTab === 'password' || activeTab === 'mail'" field="email" label="邮箱">
              <a-input v-model="form.email" allow-clear />
            </a-form-item>

            <a-form-item v-if="activeTab === 'mobile'" field="mobile" label="手机号">
              <a-input v-model="form.mobile" allow-clear />
            </a-form-item>

            <a-form-item v-if="activeTab === 'mail'" field="emailCodeTarget" label="邮箱地址">
              <a-input v-model="form.emailCodeTarget" allow-clear />
            </a-form-item>

            <a-form-item
              v-if="showSendCodeCaptcha && (activeTab === 'mobile' || activeTab === 'mail')"
              label="图片验证码"
            >
              <a-input-group class="captcha-group">
                <a-input v-model="captchaCode" placeholder="计算结果" />
                <span class="captcha-img" @click="refreshCaptcha" v-html="captchaImage" />
              </a-input-group>
            </a-form-item>

            <a-form-item v-if="activeTab === 'mobile'" label="短信验证码">
              <a-input-group class="captcha-group">
                <a-input v-model="form.code" allow-clear />
                <a-button :disabled="countdown > 0" :loading="sending" @click="sendCode('Sms')">
                  {{ countdown > 0 ? `${countdown}s` : '发送验证码' }}
                </a-button>
              </a-input-group>
            </a-form-item>

            <a-form-item v-if="activeTab === 'mail'" label="邮箱验证码">
              <a-input-group class="captcha-group">
                <a-input v-model="form.code" allow-clear />
                <a-button :disabled="countdown > 0" :loading="sending" @click="sendCode('Mail')">
                  {{ countdown > 0 ? `${countdown}s` : '发送验证码' }}
                </a-button>
              </a-input-group>
            </a-form-item>

            <a-form-item v-if="showCaptcha && activeTab === 'password'" label="图片验证码">
              <a-input-group class="captcha-group">
                <a-input v-model="captchaCode" placeholder="计算结果" />
                <span class="captcha-img" @click="refreshCaptcha" v-html="captchaImage" />
              </a-input-group>
            </a-form-item>

            <a-form-item field="password" label="密码">
              <a-input-password v-model="form.password" allow-clear />
              <div v-if="passwordHint" class="field-hint">{{ passwordHint }}</div>
            </a-form-item>

            <a-form-item field="confirmPassword" label="确认密码">
              <a-input-password v-model="form.confirmPassword" allow-clear />
            </a-form-item>

            <a-form-item>
              <a-button type="primary" html-type="submit" long :loading="loading">立即注册</a-button>
            </a-form-item>
          </a-form>

          <div class="footer-link">
            <span>已有账号？</span>
            <a-link @click="router.push('/login')">去登录</a-link>
          </div>
        </template>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { useRegisterPage } from './useRegisterPage';

const {
  activeTab,
  enablePassword,
  enableSmsRegister,
  enableMailRegister,
  registerOpen,
  showCaptcha,
  showSendCodeCaptcha,
  passwordHint,
  captchaImage,
  captchaCode,
  refreshCaptcha,
  brandName,
  logoSrc,
  form,
  onSubmit,
  countdown,
  sending,
  sendCode,
  loading,
  router,
} = useRegisterPage();
</script>

<style scoped>
.auth-shell {
  display: flex;
  min-height: 100vh;
  background: var(--color-bg-1);
}
.auth-brand {
  display: none;
  flex: 1;
  background: linear-gradient(
    145deg,
    var(--cube-primary) 0%,
    color-mix(in srgb, var(--cube-primary) 50%, #1d2129) 100%
  );
  color: #fff;
  padding: 48px 40px;
  align-items: center;
}
.brand-logo {
  width: 64px;
  height: 64px;
  object-fit: contain;
  margin-bottom: 16px;
}
.brand-name {
  margin: 0;
  font-size: 28px;
  font-weight: 600;
}
.auth-main {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 32px 20px;
}
.auth-panel {
  width: 100%;
  max-width: 420px;
}
.panel-title {
  margin: 0 0 16px;
  font-size: 24px;
  font-weight: 600;
}
.footer-link {
  text-align: center;
  margin-top: 8px;
  color: var(--color-text-3);
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
.field-hint {
  margin-top: 4px;
  font-size: 12px;
  color: var(--color-text-3);
}
@media (min-width: 992px) {
  .auth-brand {
    display: flex;
  }
}
</style>
