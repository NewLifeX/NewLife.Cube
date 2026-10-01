import { computed, watch } from 'vue';
import { useRouter } from 'vue-router';
import { buildAccountLinks, type AccountLink } from '@/core/utils/accountLinks';
import { useUserStore } from '@/stores/user';

function positiveUserId(raw: unknown): number | undefined {
  if (typeof raw !== 'number' || !Number.isInteger(raw) || raw <= 0) return undefined;
  return raw;
}

/** 账号中心「关联」：按当前用户 id 生成入口，禁用项不导航。刷新后 userInfo 为空时补拉一次。 */
export function useAccountLinks() {
  const router = useRouter();
  const userStore = useUserStore();
  let fetching = false;

  watch(
    () => userStore.userInfo?.id,
    () => {
      if (positiveUserId(userStore.userInfo?.id) != null) return;
      if (fetching || !userStore.isLoggedIn) return;
      fetching = true;
      void userStore.fetchUserInfo().catch(() => undefined);
    },
    { immediate: true },
  );

  const links = computed(() => buildAccountLinks(positiveUserId(userStore.userInfo?.id)));

  function openAccountLink(link: AccountLink) {
    if (link.disabled) return;
    void router.push({ path: link.path, query: link.query });
  }

  return { links, openAccountLink };
}
