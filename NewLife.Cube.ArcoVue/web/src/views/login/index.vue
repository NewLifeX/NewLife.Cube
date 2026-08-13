<template>
  <div class="login-wrap">
    <a-card class="login-card" :bordered="false">
      <div class="login-header">
        <img v-if="logoSrc" :src="logoSrc" class="login-logo" alt="" />
        <h2>{{ loginConfig?.name || '魔方管理平台' }}</h2>
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
              <a-button type="primary" long :loading="codeLoading" @click="handleCodeLogin('mobile')">登录</a-button>
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
              <a-button type="primary" long :loading="mailLoading" @click="handleCodeLogin('mail', emailForm)">登录</a-button>
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

      <!-- 忘记密码 -->
      <div class="forgot-link">
        <a-link @click="router.push('/forgot-password')">忘记密码？</a-link>
      </div>

      <!-- 版权信息 -->
      <div v-if="loginConfig?.copyright || loginConfig?.registration" class="login-footer">
        <div v-if="loginConfig?.copyright" v-html="loginConfig.copyright" class="login-copyright"></div>
        <div v-if="loginConfig?.registration">
          <a href="https://www.beianx.cn/" target="_blank" rel="noopener noreferrer">{{ loginConfig.registration }}</a>
        </div>
      </div>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { useLoginPage } from './useLoginPage';

const {
  logoSrc,
  loginConfig,
  activeTab,
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
} = useLoginPage();
</script>

<style scoped>
.login-wrap {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  /* 登录页渐变跟随主题主色（主色 → 混黑深一阶）；勿硬编码 Arco 默认蓝 */
  background: linear-gradient(
    135deg,
    var(--cube-primary) 0%,
    color-mix(in srgb, var(--cube-primary) 55%, #1d2129) 100%
  );
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
  color: var(--color-text-3);
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
  color: rgb(var(--primary-6, 22, 93, 255));
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
  background: var(--color-fill-2);
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
  color: var(--color-text-3);
}
.login-footer {
  text-align: center;
  margin-top: 16px;
  font-size: 12px;
  color: var(--color-text-3);
}
.login-footer a {
  color: var(--color-text-3);
  text-decoration: none;
}
.login-footer a:hover {
  color: rgb(var(--primary-6, 22, 93, 255));
}
</style>

