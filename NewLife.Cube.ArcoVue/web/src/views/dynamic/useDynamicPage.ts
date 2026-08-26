import { computed, defineAsyncComponent, ref, watch, type Component } from 'vue';
import { useRoute } from 'vue-router';
import { FieldKind } from '@cube/api-core';
import cubeApi from '@/api';
import { getSectionLoader } from '@/core/composables/useSections';
import { routeToApiPrefix } from '@/core/utils/url';
import { detectPageKind, type PageKind } from '@/core/utils/pageKind';
import { mapPageKindToAiPage } from '@/core/utils/aiChatContext';
import { useAppStore } from '@/stores/app';

/** DynamicPage 组件 props 类型（与 DynamicPage.vue defineProps 泛型逐字一致） */
interface DynamicPageProps {
  type?: string;
  authId?: number;
}

/** 页面种类探测结果会话级缓存：同 typePath 不重复探测（OSC-2608139feb） */
const pageKindCache = new Map<string, PageKind>();

/**
 * DynamicPage 页面全部业务 TS：typePath 解析、视图区段覆盖组件加载与页面种类探测。
 * 契约：仅接收 type / authId；不读取布局/主题 store。
 */
export function useDynamicPage(props: DynamicPageProps) {
  const route = useRoute();
  const appStore = useAppStore();

  const typePath = computed(() => {
    if (props.type) return props.type;
    const metaType = route.meta.typePath as string | undefined;
    if (metaType) return metaType;
    return routeToApiPrefix(route.path);
  });

  const authId = computed(() => props.authId ?? (route.meta.menuId as number | undefined));

  const overrideComp = ref<Component | null>(null);

  /** 页面种类（entity/object/home/custom/unknown）；探测完成前为 null */
  const pageKind = ref<PageKind | null>(null);

  /** custom 短路的专用页判定（Admin/Db、Admin/File） */
  const normalizedPath = computed(() => typePath.value.replace(/^\/+/, '').toLowerCase());
  const isDbPage = computed(() => normalizedPath.value === 'admin/db');
  const isFilePage = computed(() => normalizedPath.value === 'admin/file');

  async function resolveOverride() {
    const loader = getSectionLoader(typePath.value, 'DefaultListPage');
    if (!loader) {
      overrideComp.value = null;
      return;
    }
    overrideComp.value = defineAsyncComponent(loader as () => Promise<{ default: Component }>);
  }

  /** 探测页面种类：home/custom 短路 → GetPage → Object 双探（design §2.2 真值表） */
  async function resolveKind() {
    const tp = typePath.value;
    const cached = pageKindCache.get(tp);
    if (cached) {
      pageKind.value = cached;
      return;
    }
    const kind = await detectPageKind(tp, {
      getPage: (t) => cubeApi.page.getPage(t),
      getObjectProbe: async (t) => {
        const fields = await cubeApi.page.getFields(t, FieldKind.List);
        const body = await cubeApi.page.getList(t, { pageIndex: 0, pageSize: 1 });
        return { fields, body };
      },
    });
    pageKindCache.set(tp, kind);
    pageKind.value = kind;
  }

  watch(
    typePath,
    async () => {
      overrideComp.value = null;
      pageKind.value = null;
      await resolveOverride();
      // 有区段覆盖时不再探测；否则按契约探测分发
      if (!overrideComp.value) await resolveKind();
    },
    { immediate: true },
  );

  watch(
    pageKind,
    (k) => {
      if (k === 'home' || k === 'custom' || k === 'unknown') {
        appStore.patchAiContext({
          page: mapPageKindToAiPage(k),
          mode: 'add',
          id: 0,
          typePath: typePath.value,
          queryB64: '',
          applyFill: undefined,
        });
      }
    },
  );

  return {
    typePath,
    authId,
    overrideComp,
    pageKind,
    isDbPage,
    isFilePage,
  };
}
