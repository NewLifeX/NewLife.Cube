/**
 * 服务控制器页业务：按 typePath 解析指南，导航到账号中心等 SPA 入口。
 */
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { resolveServicePageGuide } from '@/core/utils/servicePage';

interface ServiceApiPageProps {
  type?: string;
}

export function useServiceApiPage(props: ServiceApiPageProps) {
  const route = useRoute();
  const router = useRouter();

  const typePath = computed(() => {
    if (props.type) return props.type;
    const metaType = route.meta.typePath as string | undefined;
    if (metaType) return metaType;
    return String(route.path || '');
  });

  const guide = computed(() => resolveServicePageGuide(typePath.value));

  const title = computed(
    () =>
      (route.meta.title as string | undefined) ||
      guide.value?.title ||
      '服务接口',
  );

  function go(path: string) {
    void router.push(path);
  }

  return {
    typePath,
    guide,
    title,
    go,
  };
}
