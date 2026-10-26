<script setup lang="ts">
/**
 * 设置中心导航：把多个配置页（单对象配置 + 列表型配置实体）串成二级 Tab。
 *
 * Tab 标题来自各控制器 `/meta` 的 `entity.displayName`（后端按 Accept-Language 本地化），
 * meta 未就绪时先用菜单标题兜底，避免文案闪烁。
 * 点击 Tab 直接跳转到目标配置页的独立路由，不做页内切换。
 */
import { onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { settingsEntries, settingsRoutePath, useSettingsPage } from './useSettingsPage';

const route = useRoute();
const router = useRouter();
const { tabs, activeController, go, loadAll } = useSettingsPage();

/**
 * 切换 Tab → 跳转到目标配置页路由
 * @param name ElTabs 的 pane name（控制器名）
 */
function handleTabChange(name: string | number) {
  const target = settingsEntries.find((item) => item.controller === name);
  if (target) go(target);
}

onMounted(async () => {
  // 兼容旧设置中心的 ?tab=Xxx 深链：重定向到该配置页的独立路由
  const legacy = String(route.query.tab ?? '');
  const target =
    legacy &&
    settingsEntries.find((item) => item.controller.toLowerCase() === legacy.toLowerCase());
  if (
    target &&
    settingsRoutePath(target).toLowerCase() !== String(route.path ?? '').toLowerCase()
  ) {
    router.replace({ path: settingsRoutePath(target) });
  }

  await loadAll();
});
</script>

<template>
  <div class="settings-tabs">
    <el-tabs :model-value="activeController" @tab-change="handleTabChange">
      <el-tab-pane
        v-for="tab in tabs"
        :key="tab.controller"
        :label="tab.title"
        :name="tab.controller"
      />
    </el-tabs>
  </div>
</template>

<style lang="scss" scoped>
.settings-tabs {
  padding: 0 20px;
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--el-border-radius-base);
}

.settings-tabs :deep(.el-tabs__header) {
  margin-bottom: 0;
}

.settings-tabs :deep(.el-tabs__nav-wrap::after) {
  height: 1px;
  background-color: var(--el-border-color-light);
}
</style>
