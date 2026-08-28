import { computed, onMounted, provide, reactive, ref } from 'vue';
import { Message } from '@arco-design/web-vue';
import {
  emptyDashboard,
  parseWorkbenchConfig,
  serializeDashboardJson,
  type DashboardConfig,
} from '@cube/api-core';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';
import { WIDGET_SURFACE_KEY, type WidgetSurfaceContext } from '@/features/widget/context';
import { alignWorkbenchSeedLayout, greetingText } from '@/core/utils/workbench';

export function useWorkbench() {
  const userStore = useUserStore();
  const loading = ref(false);
  const source = ref('system');
  const roleId = ref(0);
  const dashboard = ref<DashboardConfig>(emptyDashboard());
  const editing = ref(false);
  const loadError = ref('');

  // 问候用语用户名（账号），不用昵称/显示名
  const hello = computed(() => greetingText(userStore.userInfo?.name || userStore.displayName || ''));
  const todayLabel = computed(() =>
    new Date().toLocaleDateString('zh-CN', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    }),
  );
  const canRestore = computed(() => source.value === 'user');

  const surface = reactive<WidgetSurfaceContext>({
    surface: 'workbench',
    hostTypePath: undefined,
    hostFilter: null,
    canEdit: false,
    dashboard: emptyDashboard(),
    saveDashboard: async (next: DashboardConfig) => {
      const json = serializeDashboardJson(next, 'workbench');
      const prevDash = dashboard.value;
      const prevSource = source.value;
      // 先更新墙面，失败再回滚并抛错（Host 会 toast）
      source.value = 'user';
      dashboard.value = next;
      surface.dashboard = next;
      try {
        await cubeApi.workbench.put(json);
      } catch (e) {
        source.value = prevSource;
        dashboard.value = prevDash;
        surface.dashboard = prevDash;
        throw e;
      }
    },
  });

  provide(WIDGET_SURFACE_KEY, surface);

  async function load() {
    loading.value = true;
    loadError.value = '';
    try {
      const res = await cubeApi.workbench.get();
      const data = res.data;
      source.value = data?.source || 'system';
      roleId.value = data?.roleId ?? 0;
      const cfg = alignWorkbenchSeedLayout(parseWorkbenchConfig(data?.config) ?? emptyDashboard());
      dashboard.value = cfg;
      surface.dashboard = cfg;
    } catch (e) {
      loadError.value = e instanceof Error ? e.message : '加载工作台失败';
      dashboard.value = emptyDashboard();
      surface.dashboard = dashboard.value;
    } finally {
      loading.value = false;
    }
  }

  function toggleEdit() {
    editing.value = !editing.value;
    surface.canEdit = editing.value;
  }

  async function restoreDefault() {
    try {
      await cubeApi.workbench.put('');
      editing.value = false;
      surface.canEdit = false;
      await load();
      Message.success('已恢复默认工作台');
    } catch (e) {
      Message.error(e instanceof Error ? e.message : '恢复失败');
    }
  }

  onMounted(() => {
    void load();
  });

  return {
    loading,
    source,
    dashboard,
    editing,
    loadError,
    hello,
    todayLabel,
    canRestore,
    toggleEdit,
    restoreDefault,
    load,
  };
}
