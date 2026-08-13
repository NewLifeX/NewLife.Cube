import { computed, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useTagsViewStore } from '@/stores/tagsView';
import { useUserProfileStore } from '@/stores/userProfile';

/** TagsView 组件全部业务 TS：页签登记、切换与关闭（自 TagsView.vue script setup 原样搬移） */
export function useTagsView() {
  const route = useRoute();
  const router = useRouter();
  const tagsStore = useTagsViewStore();
  const profileStore = useUserProfileStore();

  const show = computed(() => profileStore.layout.showTabs);

  watch(
    () => route.fullPath,
    () => {
      // 始终登记缓存名，供 keep-alive prune；showTabs 只控制 UI 显隐
      tagsStore.addView(route);
    },
    { immediate: true },
  );

  function go(path: string) {
    if (path !== route.path) router.push(path);
  }

  function close(e: Event, path: string) {
    e.preventDefault?.();
    e.stopPropagation?.();
    const next = tagsStore.removeView(path);
    if (path === route.path) {
      router.push(next?.path || '/home');
    }
  }

  return {
    show,
    tagsStore,
    route,
    go,
    close,
  };
}
