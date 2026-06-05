<script setup lang="ts">
import { onBeforeMount, onMounted, watch, computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
// import NotFound from '../pages/404.vue'
// import Loading from '../pages/loading.vue'
import { useUserStore } from '../stores/user';
import { useMenuStore } from '../stores/menu';
import { getUrlHashToken } from '../utils/token';
import { useLayout } from '../composables/useLayout';
import TopMenuLayout from './TopMenu/index.vue'; // 兜底默认布局

const router = useRouter();
const route = useRoute();
const userStore = useUserStore();
const menuStore = useMenuStore();
console.log('routes', router.getRoutes());

// 通过 useLayout 获取当前注册的布局组件，无注册时回退到 TopMenuLayout
const { currentComponent } = useLayout();
const MainLayout = computed(() => currentComponent.value ?? TopMenuLayout);

// 使用计算属性获取响应式的 meta 对象
const meta = computed(() => route.meta);

function checkLogin() {
  const token = getUrlHashToken();

  // 如果token存在，说明UrlHashToken存在，重新路由，确保UrlHashToken消失，否则导航完之后UrlHashToken会存在
  // 同时路由必须是匹配上了才跳转，否则初始路由默认“/”，就会跳转到“/”而丢失原本路由
  if (token && route.matched.length > 0) {
    router.push({ path: route.path, query: route.query });
  }
}

// 监听整个路由对象变化
watch(
  route,
  (newRoute, oldRoute) => {
    console.log('路由变化', {
      newPath: newRoute.path,
      oldPath: oldRoute?.path,
      newMeta: newRoute.meta,
      oldMeta: oldRoute?.meta,
    });
    // 在这里可以添加路由变化时需要执行的逻辑
  },
  { deep: true },
);

onBeforeMount(() => {
  // 当刷新页面时，导航守卫before还没执行，这里先执行了，因此这里先检查登录情况
  checkLogin();
});

onMounted(async () => {
  try {
    await userStore.fetchUserInfoAsync();
    await menuStore.fetchMenuAsync();
  } catch (e) {
    console.error(e);
  }
});
</script>

<template>
  <!-- 如果是登录页面，直接返回 -->
  <template v-if="route.path === '/login'">
    <slot />
  </template>

  <!-- 无布局 -->
  <template v-else-if="meta.layout === false">
    <slot />
  </template>

  <!-- 布局 -->
  <template v-else>
    <component :is="MainLayout">
      <!-- 组件缓存 -->
      <KeepAlive v-if="meta.keepAlive">
        <Transition
          v-if="meta.transition"
          v-bind="typeof meta.transition === 'object' ? meta.transition : {}"
        >
          <slot />
        </Transition>
        <slot v-else />
      </KeepAlive>

      <!-- 无缓存但有过渡 -->
      <Transition
        v-else-if="meta.transition"
        v-bind="typeof meta.transition === 'object' ? meta.transition : {}"
      >
        <slot />
      </Transition>

      <!-- 无缓存无过渡 -->
      <slot v-else />
    </component>
  </template>
</template>
