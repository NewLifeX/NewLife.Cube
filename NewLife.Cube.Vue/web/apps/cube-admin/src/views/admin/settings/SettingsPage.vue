<script setup lang="ts">
/**
 * 设置页外壳：页头 + 设置导航 Tab + 内容。
 *
 * 五个单对象配置页（Core/Sys/Star/XCode/Cube）与四个列表型配置页
 * （SmsConfig/MailConfig/OAuthConfig/AccessRule）共用同一套导航，
 * 每个页面都通过自己的路由地址进入，可直接刷新与收藏。
 * 页面形态由 `settingsEntries` 声明，视图文件只负责占住路由目录。
 */
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import FormPageHeader from '@newlifex/cube-vue/core/views/components/FormPageHeader.vue';
import CubeTable from '@newlifex/cube-vue/core/components/CubeTable/CubeTable.vue';
import SettingsTabs from './SettingsTabs.vue';
import SettingsForm from './SettingsForm.vue';
import { useSettingsPage } from './useSettingsPage';

const route = useRoute();
const { t } = useI18n();
const { entry, pageTitle, status, meta } = useSettingsPage();
</script>

<template>
  <div class="settings-page">
    <FormPageHeader :title="pageTitle" />
    <SettingsTabs />
    <div class="settings-body">
      <div v-if="status === 'error'" class="settings-unavailable">
        {{ t('settingsCenter.unavailable') }}
      </div>
      <div v-else-if="status !== 'ok'" v-loading="true" class="settings-loading" />
      <SettingsForm v-else-if="entry?.kind === 'form'" :meta="meta" />
      <CubeTable v-else :key="route.path" />
    </div>
  </div>
</template>

<style lang="scss" scoped>
.settings-page {
  height: 100%;
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding-bottom: 12px;
  box-sizing: border-box;
}

.settings-body {
  flex: 1;
  min-height: 0;
  overflow: auto;
}

.settings-loading {
  min-height: 240px;
}

.settings-unavailable {
  padding: 48px 0;
  color: var(--el-text-color-secondary);
  text-align: center;
}
</style>
