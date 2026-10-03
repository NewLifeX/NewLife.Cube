<template>
  <div class="auth-shell">
    <aside class="auth-brand">
      <div class="brand-inner">
        <h1 class="brand-name">账号激活</h1>
        <p class="brand-tip">激活邮箱或手机后即可登录</p>
      </div>
    </aside>
    <main class="auth-main">
      <div class="auth-panel">
        <h2 class="panel-title">账号激活</h2>

        <a-spin :loading="linkLoading" style="width: 100%">
          <template v-if="done">
            <a-result status="success" title="激活成功" subtitle="即将跳转登录页">
              <template #extra>
                <a-button type="primary" @click="goLogin">返回登录</a-button>
              </template>
            </a-result>
          </template>

          <template v-else-if="linkMode && linkError">
            <a-alert type="error" :content="linkError" style="margin-bottom: 16px" />
            <a-button type="primary" long @click="goLogin">返回登录</a-button>
          </template>

          <a-form
            v-else-if="!linkMode"
            :model="form"
            layout="vertical"
            @submit.prevent="onSubmit"
          >
            <a-form-item label="验证渠道">
              <a-radio-group v-model="form.channel" type="button">
                <a-radio value="mail">邮箱</a-radio>
                <a-radio value="sms">短信</a-radio>
              </a-radio-group>
            </a-form-item>
            <a-form-item :label="form.channel === 'mail' ? '邮箱' : '手机号'" field="account">
              <a-input
                v-model="form.account"
                :placeholder="form.channel === 'mail' ? '请输入邮箱' : '请输入手机号'"
                allow-clear
              />
            </a-form-item>
            <a-form-item label="验证码" field="code">
              <a-input-group class="code-group">
                <a-input v-model="form.code" placeholder="请输入验证码" allow-clear />
                <a-button :loading="sending" @click="onSendCode">发送验证码</a-button>
              </a-input-group>
            </a-form-item>
            <a-alert v-if="formError" type="error" :content="formError" style="margin-bottom: 12px" />
            <a-form-item>
              <a-button type="primary" long html-type="submit" :loading="submitting" :disabled="!canSubmit">
                激活
              </a-button>
            </a-form-item>
            <a-form-item>
              <a-link @click="goLogin">返回登录</a-link>
            </a-form-item>
          </a-form>
        </a-spin>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { useActivatePage } from './useActivatePage';

const {
  linkMode,
  linkLoading,
  linkError,
  done,
  sending,
  submitting,
  formError,
  form,
  canSubmit,
  goLogin,
  onSendCode,
  onSubmit,
} = useActivatePage();
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
.code-group {
  width: 100%;
  display: inline-flex;
}
.code-group :deep(.arco-input-wrapper),
.code-group :deep(.arco-input-outer) {
  flex: 1;
  min-width: 0;
}
@media (min-width: 992px) {
  .auth-brand {
    display: flex;
  }
}
</style>
