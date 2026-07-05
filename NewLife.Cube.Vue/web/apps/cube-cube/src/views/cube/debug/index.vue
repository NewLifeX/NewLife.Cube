<template>
  <div class="debug-page">
    <h1>微应用调试页面</h1>

    <el-card style="margin-bottom: 20px">
      <template #header>
        <h3>路由初始化状态</h3>
      </template>
      <p>路由初始化状态: {{ routesInitialized ? '已初始化' : '未初始化' }}</p>
      <p>当前路径: {{ $route.path }}</p>
      <p>当前路由名称: {{ $route.name }}</p>
    </el-card>

    <el-card style="margin-bottom: 20px">
      <template #header>
        <h3>已注册路由</h3>
      </template>
      <el-table :data="routesList" border style="width: 100%">
        <el-table-column prop="path" label="路径" width="200" />
        <el-table-column prop="name" label="名称" width="180" />
        <el-table-column prop="component" label="组件" />
      </el-table>
    </el-card>

    <el-card>
      <template #header>
        <h3>微应用配置</h3>
      </template>
      <el-table :data="microAppsList" border style="width: 100%">
        <el-table-column prop="name" label="应用名称" width="150" />
        <el-table-column prop="prefix" label="前缀" width="100" />
        <el-table-column prop="status" label="状态" width="100" />
        <el-table-column prop="routeCount" label="路由数量" width="100" />
      </el-table>
    </el-card>

    <div style="margin-top: 20px">
      <el-button @click="refreshStatus">刷新状态</el-button>
      <el-button @click="testRoute">测试路由</el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';

const router = useRouter();
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
      component: route.component ? route.component.name || 'Component' : 'No component'
    }));

    // 查找Cube相关路由
    const cubeRoutes = routes.filter(route => route.path.startsWith('/Cube'));
    console.log('Cube相关路由:', cubeRoutes);

  } catch (error) {
    console.error('获取路由状态失败:', error);
  }
};

// 测试路由导航
const testRoute = () => {
  router.push('/Cube/Test').catch(error => {
    console.error('路由导航失败:', error);
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
