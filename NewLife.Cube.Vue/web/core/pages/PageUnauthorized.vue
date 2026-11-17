<template>
  <div class="page-unauthorized" data-cy="page-unauthorized">
    <div class="status-badge" aria-hidden="true">
      <el-icon :size="56" class="badge-icon">
        <Lock />
      </el-icon>
    </div>
    <div class="status-code" aria-hidden="true">401</div>
    <h1 class="title">{{ t('pageUnauthorized.title') }}</h1>
    <p class="desc">{{ t('pageUnauthorized.desc') }}</p>
    <el-button
      type="primary"
      size="large"
      class="login-button"
      data-cy="unauthorized-login"
      :icon="SwitchButton"
      @click="goLogin"
    >
      {{ t('pageUnauthorized.goLogin') }}
    </el-button>
  </div>
</template>

<script setup lang="ts">
import { ElButton, ElIcon } from 'element-plus';
import { Lock, SwitchButton } from '@element-plus/icons-vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';
import { getConfig } from '../configure';
import { normalizeAuthRedirect } from '../utils/authRedirect';

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const {
  auth: { reLoginParams },
} = getConfig();
const loginPageUrl = reLoginParams?.loginPageUrl || '/login';

function goLogin() {
  router.replace({ path: loginPageUrl, query: { redirect: normalizeAuthRedirect(route.query.redirect) } });
}
</script>

<style scoped>
.page-unauthorized {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
  text-align: center;
  padding: 24px;
  background: var(--el-bg-color-page);
  color: var(--el-text-color-primary);
}

.status-badge {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 120px;
  height: 120px;
  margin-bottom: 8px;
  border-radius: 50%;
  background: color-mix(in srgb, var(--el-color-warning) 12%, transparent);
  color: var(--el-color-warning);
}

.status-code {
  font-size: 5rem;
  line-height: 1;
  font-weight: 700;
  color: var(--el-color-warning);
}

.title {
  margin: 0;
  font-size: 1.75rem;
}

.desc {
  margin: 0 0 8px;
  max-width: 480px;
  color: var(--el-text-color-secondary);
}
</style>
