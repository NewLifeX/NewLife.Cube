<template>
  <div class="register-wrap">
    <a-card class="register-card" :bordered="false">
      <div class="register-header">
        <h2>注册账号</h2>
      </div>

      <a-tabs v-if="!oauthMode" v-model:active-key="activeTab">
        <a-tab-pane key="password" title="账号注册" />
        <a-tab-pane v-if="enableSmsRegister" key="mobile" title="手机注册" />
        <a-tab-pane v-if="enableMailRegister" key="mail" title="邮笱注册" />
      </a-tabs>

      <a-alert v-if="oauthMode" type="info" style="margin-bottom: 12px">第三方账号首次登录，请补全密码完成本地账号创建</a-alert>

      <a-form :model="form" layout="vertical" @submit="onSubmit">
        <a-form-item v-if="activeTab==='password' || oauthMode" field="username" label="用户名">
          <a-input v-model="form.username" :readonly="oauthMode" allow-clear />
        </a-form-item>

        <a-form-item v-if="activeTab==='password' || activeTab==='mail' || oauthMode" field="email" label="邮笱">
          <a-input v-model="form.email" allow-clear />
        </a-form-item>

        <a-form-item v-if="activeTab==='mobile'" field="mobile" label="手机号">
          <a-input-group>
            <a-input v-model="form.mobile" allow-clear style="flex:1" />
            <a-button :disabled="countdown>0" :loading="sending" @click="sendCode('Sms')">{{ countdown>0 ? `${countdown}s` : '发送验证码' }}</a-button>
          </a-input-group>
        </a-form-item>

        <a-form-item v-if="activeTab==='mail'" field="emailCodeTarget" label="邮笱地址">
          <a-input-group>
            <a-input v-model="form.emailCodeTarget" allow-clear style="flex:1" />
            <a-button :disabled="countdown>0" :loading="sending" @click="sendCode('Mail')">{{ countdown>0 ? `${countdown}s` : '发送验证码' }}</a-button>
          </a-input-group>
        </a-form-item>

        <a-form-item v-if="activeTab==='mobile' || activeTab==='mail'" field="code" label="验证码">
          <a-input v-model="form.code" allow-clear />
        </a-form-item>

        <a-form-item field="password" label="密码">
          <a-input-password v-model="form.password" allow-clear />
        </a-form-item>

        <a-form-item field="confirmPassword" label="确认密码">
          <a-input-password v-model="form.confirmPassword" allow-clear />
        </a-form-item>

        <a-form-item>
          <a-button type="primary" html-type="submit" long :loading="loading">{{ oauthMode ? '完成绑定并登录' : '立即注册' }}</a-button>
        </a-form-item>
      </a-form>

      <div class="register-footer-link">
        <span>已有账号？</span>
        <a-link @click="router.push('/login')">去登录</a-link>
      </div>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { useRegisterPage } from './useRegisterPage';

const {
  oauthMode,
  activeTab,
  enableSmsRegister,
  enableMailRegister,
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
.register-wrap { display:flex; justify-content:center; align-items:center; min-height:100vh; background:linear-gradient(135deg, var(--cube-primary) 0%, color-mix(in srgb, var(--cube-primary) 55%, #1d2129) 100%); }
.register-card { width: 460px; border-radius: 8px; }
.register-header { text-align:center; margin-bottom: 12px; }
.register-footer-link { text-align:center; margin-top: 8px; color: var(--color-text-3); }
</style>
