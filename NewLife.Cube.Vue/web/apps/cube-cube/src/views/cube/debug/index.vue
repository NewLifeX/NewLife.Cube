<template>
  <div class="debug-page">
    <h1>{{ t('microDebug.title') }}</h1>

    <el-card style="margin-bottom: 20px">
      <template #header>
        <h3>{{ t('microDebug.routeStatus') }}</h3>
      </template>
      <p>
        {{ t('microDebug.routeStatus') }}:
        {{ routesInitialized ? t('microDebug.initialized') : t('microDebug.notInitialized') }}
      </p>
      <p>{{ t('microDebug.currentPath') }}: {{ route.path }}</p>
      <p>{{ t('microDebug.currentRouteName') }}: {{ route.name }}</p>
    </el-card>

    <el-card style="margin-bottom: 20px">
      <template #header>
        <h3>{{ t('microDebug.registeredRoutes') }}</h3>
      </template>
      <el-table :data="routesList" border style="width: 100%">
        <el-table-column prop="path" :label="t('microDebug.path')" width="200" />
        <el-table-column prop="name" :label="t('appAdmin.name')" width="180" />
        <el-table-column prop="component" :label="t('microDebug.component')" />
      </el-table>
    </el-card>

    <el-card>
      <template #header>
        <h3>{{ t('microDebug.microAppConfig') }}</h3>
      </template>
      <el-table :data="microAppsList" border style="width: 100%">
        <el-table-column prop="name" :label="t('appAdmin.appName')" width="150" />
        <el-table-column prop="prefix" :label="t('microDebug.prefix')" width="100" />
        <el-table-column prop="status" :label="t('userSearch.status')" width="100" />
        <el-table-column prop="routeCount" :label="t('microDebug.routeCount')" width="100" />
      </el-table>
    </el-card>

    <div style="margin-top: 20px">
      <el-button @click="refreshStatus">{{ t('microDebug.refreshStatus') }}</el-button>
      <el-button @click="testRoute">{{ t('microDebug.testRoute') }}</el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter, useRoute } from 'vue-router';

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const routesInitialized = ref(false);
const routesList = ref<Array<{path: string, name: string | symbol | undefined, component: string}>>([]);
const microAppsList = ref<Array<{name: string, prefix?: string, status: string, routeCount: number}>>([]);

// 获取路由状态
const refreshStatus = () => {
  try {
    // 检查是否有isRoutesInitialized函数
    const microAppRouter = (window as unknown as Record<string, unknown>).microAppRouter;
    if (microAppRouter && typeof microAppRouter === 'object' && 'isRoutesInitialized' in microAppRouter) {
      routesInitialized.value = (microAppRouter.isRoutesInitialized as () => boolean)();
    }

    // 获取所有路由
    const routes = router.getRoutes();
    routesList.value = routes.map(route => ({
      path: route.path,
      name: route.name,
      component: route.components?.default ? (route.components.default as { name?: string }).name || 'Component' : 'No component'
    }));

    // 查找Cube相关路由
    const cubeRoutes = routes.filter(route => route.path.startsWith('/Cube'));
    console.log('Cube routes:', cubeRoutes);

  } catch (error) {
    console.error('Failed to get route status:', error);
  }
};

// 测试路由导航
const testRoute = () => {
  router.push('/Cube/Test').catch(error => {
    console.error('Route navigation failed:', error);
  });
};

onMounted(() => {
  refreshStatus();

  // 监听路由变化
  router.afterEach(() => {
    refreshStatus();
  });
});
</script>

<style scoped>
.debug-page {
  padding: 20px;
}

.debug-page h1 {
  color: #409eff;
  margin-bottom: 20px;
}
</style>
