<template>
  <div class="service-page">
    <div class="service-surface">
      <a-card v-if="guide" class="service-card" :title="title">
        <p class="service-summary">{{ guide.summary }}</p>
        <a-space wrap class="service-links">
          <a-button
            v-for="link in guide.links"
            :key="link.path"
            type="primary"
            @click="go(link.path)"
          >
            {{ link.label }}
          </a-button>
        </a-space>
        <a-descriptions
          v-if="guide.endpoints.length"
          class="service-endpoints"
          title="相关接口"
          :column="1"
          size="small"
          bordered
        >
          <a-descriptions-item
            v-for="ep in guide.endpoints"
            :key="ep"
            label="Endpoint"
          >
            <code>{{ ep }}</code>
          </a-descriptions-item>
        </a-descriptions>
      </a-card>
      <a-empty v-else description="无法识别该服务接口" />
    </div>
  </div>
</template>

<script setup lang="ts">
/**
 * 顶层服务控制器页（Auth/Sso/Mfa/AI/Automation/CubeController）。
 * 无 GetPage，不走 DefaultList；对照 Cube.Vue cube-v1 的专用页，ArcoVue 用指南卡 + 跳转账号中心。
 */
import { useServiceApiPage } from './useServiceApiPage';

const props = defineProps<{
  type?: string;
}>();

const { guide, title, go } = useServiceApiPage(props);
</script>

<style scoped>
.service-page {
  display: flex;
  flex-direction: column;
  min-width: 0;
}
.service-surface {
  padding: 16px;
  min-width: 0;
}
.service-card {
  max-width: 880px;
}
.service-summary {
  margin: 0 0 16px;
  color: var(--color-text-2);
  line-height: 1.6;
}
.service-links {
  margin-bottom: 20px;
}
.service-endpoints {
  margin-top: 8px;
}
.service-endpoints code {
  font-size: 13px;
  color: var(--color-text-1);
}
</style>
