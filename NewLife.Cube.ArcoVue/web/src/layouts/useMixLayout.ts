import { computed, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import type { MenuItem } from '@cube/api-core';
import { useUserStore } from '@/stores/user';
import { useUserProfileStore } from '@/stores/userProfile';
import { normalizeMenuUrl } from '@/core/utils/url';

/** 混合布局全部业务 TS：顶/侧菜单联动、激活项同步与折叠开关（自 mix.vue script setup 原样搬移） */
export function useMixLayout() {
  const route = useRoute();
  const router = useRouter();
  const userStore = useUserStore();
  const profileStore = useUserProfileStore();

  const collapsed = computed(() => profileStore.layout.siderCollapsed);
  const siderWidth = computed(() => profileStore.layout.siderWidth || 220);

  const topMenus = computed(() =>
    (userStore.menus || []).filter((m: MenuItem) => m.visible !== false),
  );

  const activeTopId = ref<number>(0);

  function menuContainsPath(item: MenuItem, path: string): boolean {
    if (item.url && normalizeMenuUrl(item.url, 'pascal') === path) return true;
    return (item.children || []).some((c) => menuContainsPath(c, path));
  }

  function syncActiveTop() {
    const path = normalizeMenuUrl(route.path, 'pascal');
    const hit = topMenus.value.find((m) => menuContainsPath(m, path));
    if (hit) {
      activeTopId.value = hit.id;
      return;
    }
    if (!activeTopId.value && topMenus.value.length) {
      activeTopId.value = topMenus.value[0].id;
    }
  }

  watch([() => route.path, topMenus], syncActiveTop, { immediate: true });

  const sideMenus = computed(() => {
    const top = topMenus.value.find((m) => m.id === activeTopId.value);
    if (!top) return [];
    if (top.children?.length) {
      return top.children.filter((m) => m.visible !== false);
    }
    // 一级本身是叶子：侧栏显示自身
    return top.url ? [top] : [];
  });

  const selectedKeys = computed(() => [normalizeMenuUrl(route.path, 'pascal')]);

  function onTopClick(key: string) {
    const id = Number(key);
    activeTopId.value = id;
    const top = topMenus.value.find((m) => m.id === id);
    if (!top) return;
    const firstLeaf = findFirstLeaf(top);
    if (firstLeaf?.url) router.push(normalizeMenuUrl(firstLeaf.url, 'pascal'));
  }

  function findFirstLeaf(item: MenuItem): MenuItem | null {
    if (item.url && (!item.children || !item.children.length)) return item;
    for (const c of item.children || []) {
      if (c.visible === false) continue;
      const leaf = findFirstLeaf(c);
      if (leaf) return leaf;
    }
    return item.url ? item : null;
  }

  function onMenuClick(key: string) {
    if (key && !key.startsWith('sub-')) router.push(key);
  }

  function toggleCollapsed() {
    profileStore.patchLayout({ siderCollapsed: !profileStore.layout.siderCollapsed });
  }

  return {
    collapsed,
    siderWidth,
    topMenus,
    activeTopId,
    sideMenus,
    selectedKeys,
    onTopClick,
    onMenuClick,
    toggleCollapsed,
  };
}
