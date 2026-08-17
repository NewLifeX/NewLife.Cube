<template>
  <div class="security-page">
    <a-spin :loading="loading" style="width: 100%">
      <a-typography-title :heading="5">账号安全</a-typography-title>

      <a-card v-if="mfaAvailable" title="两步验证（Authenticator）" class="sec-card">
        <template v-if="!mfaEnabled">
          <template v-if="step === 'idle'">
            <p class="hint">启用后，登录除密码外还需输入 Authenticator 验证码。</p>
            <a-button type="primary" @click="startSetup">开启 MFA</a-button>
          </template>
          <template v-else-if="step === 'setup'">
            <p class="hint">用 Authenticator App 扫描二维码，或手动输入密钥。</p>
            <div v-if="setupQrDataUrl" class="qr-wrap">
              <img :src="setupQrDataUrl" alt="MFA QR" width="180" height="180" />
            </div>
            <a-textarea :model-value="setupUri" :auto-size="{ minRows: 2, maxRows: 4 }" readonly />
            <p v-if="setupSecret" class="secret">
              密钥：{{ setupSecret }}
              <a-button type="text" size="mini" @click="copySecret">复制</a-button>
            </p>
            <a-form-item label="验证码" style="margin-top: 12px">
              <a-input v-model="activateCode" placeholder="6 位验证码" style="max-width: 200px" />
            </a-form-item>
            <a-button type="primary" @click="activate">激活</a-button>
          </template>
          <template v-else>
            <a-alert type="warning" title="请立即保存备用码（仅显示一次）" />
            <ul class="backup-list">
              <li v-for="c in backupCodes" :key="c">{{ c }}</li>
            </ul>
            <a-button @click="copyBackup">复制全部</a-button>
          </template>
        </template>
        <template v-else>
          <p class="hint">MFA 已开启。输入验证码可关闭。</p>
          <a-form-item label="验证码">
            <a-input v-model="disableCode" placeholder="6 位或备用码" style="max-width: 200px" />
          </a-form-item>
          <a-button status="danger" @click="disable">关闭 MFA</a-button>
        </template>
      </a-card>

      <a-card v-if="oauthEnabled" title="第三方账号绑定" class="sec-card">
        <a-empty v-if="!binds.length" description="暂无可用的第三方登录" />
        <a-list v-else :bordered="false">
          <a-list-item v-for="item in binds" :key="item.id">
            <a-list-item-meta :title="item.nickName || item.name">
              <template #avatar>
                <a-avatar v-if="item.logo" :image-url="item.logo" />
                <a-avatar v-else>{{ (item.nickName || item.name || '?').charAt(0) }}</a-avatar>
              </template>
              <template #description>
                <span v-if="item.bound">已绑定{{ item.connectNickName ? ` · ${item.connectNickName}` : '' }}</span>
                <span v-else>未绑定</span>
              </template>
            </a-list-item-meta>
            <template #actions>
              <a-button v-if="!item.bound" type="text" @click="bindProvider(item)">绑定</a-button>
              <a-button v-else type="text" status="danger" @click="unbindProvider(item)">解绑</a-button>
            </template>
          </a-list-item>
        </a-list>
      </a-card>
    </a-spin>
  </div>
</template>

<script setup lang="ts">
import { useSecuritySettings } from './useSecuritySettings';

defineOptions({ name: 'SecuritySettings' });

const {
  loading,
  mfaAvailable,
  mfaEnabled,
  setupUri,
  setupSecret,
  setupQrDataUrl,
  activateCode,
  disableCode,
  backupCodes,
  binds,
  oauthEnabled,
  step,
  startSetup,
  activate,
  disable,
  bindProvider,
  unbindProvider,
  copyBackup,
  copySecret,
} = useSecuritySettings();
</script>

<style scoped>
.security-page {
  max-width: 720px;
  padding: 16px 20px 40px;
}
.sec-card {
  margin-top: 16px;
}
.hint {
  color: var(--color-text-3);
  font-size: 13px;
  margin: 0 0 12px;
}
.secret {
  margin: 8px 0 0;
  font-family: ui-monospace, monospace;
  font-size: 13px;
}
.qr-wrap {
  margin: 8px 0 12px;
  padding: 8px;
  display: inline-block;
  background: #fff;
  border-radius: 4px;
}
.backup-list {
  columns: 2;
  margin: 12px 0;
  font-family: ui-monospace, monospace;
}
</style>
