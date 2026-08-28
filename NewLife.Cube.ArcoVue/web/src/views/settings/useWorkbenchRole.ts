import { computed, onMounted, provide, reactive, ref } from 'vue';
import { Message } from '@arco-design/web-vue';
import {
  emptyDashboard,
  parseWorkbenchConfig,
  serializeDashboardJson,
  type DashboardConfig,
} from '@cube/api-core';
import cubeApi from '@/api';
import { WIDGET_SURFACE_KEY, type WidgetSurfaceContext } from '@/features/widget/context';

interface RoleRow {
  id: number;
  name: string;
  isSystem?: boolean;
}

function pickRole(row: Record<string, unknown>): RoleRow {
  return {
    id: Number(row.id ?? row.Id ?? 0),
    name: String(row.name ?? row.Name ?? row.displayName ?? row.DisplayName ?? ''),
    isSystem: Boolean(row.isSystem ?? row.IsSystem),
  };
}

export function useWorkbenchRole() {
  const roles = ref<RoleRow[]>([]);
  const selectedId = ref(0);
  const loading = ref(false);
  const saving = ref(false);
  const dashboard = ref<DashboardConfig>(emptyDashboard());
  const hasTemplate = ref(false);

  const selected = computed(() => roles.value.find((r) => r.id === selectedId.value) || null);

  const surface = reactive<WidgetSurfaceContext>({
    surface: 'workbench',
    hostFilter: null,
    canEdit: true,
    dashboard: emptyDashboard(),
    saveDashboard: async (next: DashboardConfig) => {
      dashboard.value = next;
      surface.dashboard = next;
    },
  });
  provide(WIDGET_SURFACE_KEY, surface);

  async function loadRoles() {
    try {
      const res = await cubeApi.page.getList<Record<string, unknown>>('/Admin/Role', {
        pageIndex: 1,
        pageSize: 100,
      });
      const rows = (res.data ?? []) as Record<string, unknown>[];
      roles.value = rows.map(pickRole).filter((r) => r.id > 0);
      if (!selectedId.value && roles.value.length) selectedId.value = roles.value[0].id;
    } catch (e) {
      Message.error(e instanceof Error ? e.message : '加载角色失败');
    }
  }

  async function loadRole(id: number) {
    if (!id) return;
    loading.value = true;
    try {
      const res = await cubeApi.workbench.getRole(id);
      const cfg = parseWorkbenchConfig(res.data?.config);
      hasTemplate.value = !!cfg;
      // 尚未配置：预览空壳；保存空墙会改为清除模板，避免阻断系统种子
      dashboard.value = cfg ?? emptyDashboard();
      surface.dashboard = dashboard.value;
    } catch (e) {
      Message.error(e instanceof Error ? e.message : '加载角色工作台失败');
      dashboard.value = emptyDashboard();
      surface.dashboard = dashboard.value;
      hasTemplate.value = false;
    } finally {
      loading.value = false;
    }
  }

  async function save() {
    if (!selectedId.value) return;
    saving.value = true;
    try {
      const widgets = dashboard.value.widgets ?? [];
      // 空 widgets 写入会让未个性化用户 source=role 且墙为空，阻断系统种子 → 改为清除角色域
      if (widgets.length === 0) {
        await cubeApi.workbench.putRole(selectedId.value, '');
        hasTemplate.value = false;
        Message.success('未配置部件：已清除角色模板，用户将继承系统默认工作台。');
        return;
      }
      await cubeApi.workbench.putRole(
        selectedId.value,
        serializeDashboardJson(dashboard.value, 'workbench'),
      );
      hasTemplate.value = true;
      Message.success('角色工作台已保存。已个性化用户不会被覆盖。');
    } catch (e) {
      Message.error(e instanceof Error ? e.message : '保存失败');
    } finally {
      saving.value = false;
    }
  }

  async function clearTemplate() {
    if (!selectedId.value) return;
    saving.value = true;
    try {
      await cubeApi.workbench.putRole(selectedId.value, '');
      hasTemplate.value = false;
      dashboard.value = emptyDashboard();
      surface.dashboard = dashboard.value;
      Message.success('已清除角色模板，用户将继承系统默认工作台。');
    } catch (e) {
      Message.error(e instanceof Error ? e.message : '清除失败');
    } finally {
      saving.value = false;
    }
  }

  onMounted(async () => {
    await loadRoles();
    if (selectedId.value) await loadRole(selectedId.value);
  });

  async function selectRole(id: number) {
    selectedId.value = id;
    await loadRole(id);
  }

  return {
    roles,
    selectedId,
    selected,
    loading,
    saving,
    hasTemplate,
    selectRole,
    save,
    clearTemplate,
  };
}
