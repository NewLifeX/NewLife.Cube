<template>
  <div class="auth-shell">
    <aside class="auth-brand">
      <div class="brand-inner">
        <h1 class="brand-name">重置密码</h1>
        <p class="brand-tip">通过短信或邮箱验证码设置新密码</p>
      </div>
    </aside>
    <main class="auth-main">
      <div class="auth-panel">
        <h2 class="panel-title">重置密码</h2>

        <a-form v-if="step === 'input'" :model="form" layout="vertical" @submit.prevent="onSendCode">
          <a-form-item label="手机号或邮箱" field="username">
            <a-input v-model="form.username" placeholder="请输入手机号或邮箱" allow-clear />
          </a-form-item>
          <a-form-item label="验证渠道">
            <a-radio-group v-model="form.channel" type="button">
              <a-radio value="Sms">短信</a-radio>
              <a-radio value="Mail">邮箱</a-radio>
            </a-radio-group>
          </a-form-item>
          <a-form-item v-if="showSendCodeCaptcha" label="图片验证码">
            <a-input-group class="captcha-group">
              <a-input v-model="captchaCode" placeholder="计算结果" allow-clear />
              <span class="captcha-img" title="点击刷新" @click="refreshCaptcha" v-html="captchaImage" />
            </a-input-group>
          </a-form-item>
          <a-form-item>
            <a-button type="primary" long html-type="submit" :loading="sending">发送验证码</a-button>
          </a-form-item>
          <a-form-item>
            <a-link @click="router.push('/login')">返回登录</a-link>
          </a-form-item>
        </a-form>

        <a-form v-else :model="form" layout="vertical" @submit.prevent="onConfirmReset">
          <a-form-item label="验证码" field="code">
            <a-input-group class="captcha-group">
              <a-input v-model="form.code" placeholder="请输入验证码" allow-clear />
              <a-button :disabled="countdown > 0" :loading="sending" @click="onResend">
                {{ countdown > 0 ? `${countdown}s` : '重新发送' }}
              </a-button>
            </a-input-group>
          </a-form-item>
          <a-form-item v-if="showSendCodeCaptcha" label="图片验证码">
            <a-input-group class="captcha-group">
              <a-input v-model="captchaCode" placeholder="重新发送前需填写" allow-clear />
              <span class="captcha-img" title="点击刷新" @click="refreshCaptcha" v-html="captchaImage" />
            </a-input-group>
          </a-form-item>
          <a-form-item label="新密码" field="newPassword">
            <a-input-password v-model="form.newPassword" placeholder="请输入新密码" allow-clear />
            <div v-if="passwordHint" class="field-hint">{{ passwordHint }}</div>
          </a-form-item>
          <a-form-item label="确认密码" field="confirmPassword">
            <a-input-password v-model="form.confirmPassword" placeholder="请再次输入新密码" allow-clear />
          </a-form-item>
          <a-alert v-if="error" type="error" :content="error" style="margin-bottom: 12px" />
          <a-form-item>
            <a-button type="primary" long html-type="submit" :loading="submitting">确认重置</a-button>
          </a-form-item>
          <a-form-item>
            <a-space>
              <a-link @click="step = 'input'">上一步</a-link>
              <a-divider direction="vertical" />
              <a-link @click="router.push('/login')">返回登录</a-link>
            </a-space>
          </a-form-item>
        </a-form>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { useForgotPassword } from './useForgotPassword';

const {
  step,
  form,
  sending,
  submitting,
  countdown,
  error,
  router,
  showSendCodeCaptcha,
  passwordHint,
  captchaImage,
  captchaCode,
  refreshCaptcha,
  onSendCode,
  onResend,
  onConfirmReset,
} = useForgotPassword();
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
.brand-name {
  margin: 0 0 12px;
  font-size: 28px;
  font-weight: 600;
}
.brand-tip {
  margin: 0;
  opacity: 0.85;
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
  max-width: 400px;
}
.panel-title {
  margin: 0 0 16px;
  font-size: 24px;
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
