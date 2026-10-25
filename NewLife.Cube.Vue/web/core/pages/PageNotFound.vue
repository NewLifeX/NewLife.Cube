<template>
  <div class="page-not-found">
    <div class="status-code">404</div>
    <h1 class="title">{{ t('pageNotFound.title') }}</h1>
    <p class="desc">{{ t('pageNotFound.desc') }}</p>
    <router-link to="/" class="home-link">{{ t('pageNotFound.backHome') }}</router-link>
  </div>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n';

const { t } = useI18n();
</script>

<style scoped>
.page-not-found {
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

.status-code {
  font-size: 5rem;
  line-height: 1;
  font-weight: 700;
  color: var(--el-color-danger);
}

.title {
  margin: 0;
  font-size: 1.75rem;
}

.desc {
  margin: 0 0 8px;
  color: var(--el-text-color-secondary);
}

.home-link {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  height: 40px;
  padding: 0 18px;
  border-radius: var(--el-border-radius-base);
  background: var(--el-color-primary);
  color: var(--el-color-white);
  text-decoration: none;
}

.home-link:hover {
  background: var(--el-color-primary-dark-2);
}
</style>
