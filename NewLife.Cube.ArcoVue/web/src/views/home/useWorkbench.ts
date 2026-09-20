import { computed, onActivated, onBeforeUnmount, onDeactivated, onMounted, provide, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { Message } from '@arco-design/web-vue';
import { resetMenuRoutesFlag } from '@/router';
import {
  emptyDashboard,
  parseWorkbenchConfig,
  serializeDashboardJson,
  type DashboardConfig,
  type NamedWorkbenchItem,
} from '@newlifex/api-core';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';
import { WIDGET_SURFACE_KEY, type WidgetSurfaceContext } from '@/features/widget/context';
import { isEmbedMode } from '@/core/utils/embedMode';
import { alignWorkbenchSeedLayout, greetingText } from '@/core/utils/workbench';
import {
  defaultPublishSlug,
  isValidNamedSlug,
  parseNamedMenuValue,
  workbenchRoutePath,
} from '@/core/utils/workbenchNamed';

/**
 * 工作台组合逻辑（OSC-260902ef43）。
 * slug 空 = 默认工作台（/home 个人墙，user>role>system）；有 slug = 命名工作台（/Workbench/{slug}，只读命名槽）。
 */
export function useWorkbench(slugArg = '') {
  const router = useRouter();
  const userStore = useUserStore();
  const loading = ref(false);
  const source = ref('system');
  const roleId = ref(0);
  const dashboard = ref<DashboardConfig>(emptyDashboard());
  const editing = ref(false);
  const loadError = ref('');
  /** 铺满视口并覆盖顶栏/侧栏（与列表页 DefaultList 同策略，非浏览器原生全屏） */
  const fullscreen = ref(false);

  /** 命名工作台唯一标识；空 = 默认工作台 */
  const slug = (slugArg ?? '').trim();
  const isNamed = computed(() => !!slug);
  const currentSlug = computed(() => slug);
  const isSystem = computed(() => userStore.userInfo?.isSystem === true);
  const namedList = ref<NamedWorkbenchItem[]>([]);
  const namedTitle = ref('');
  const embed = isEmbedMode();
  const showShare = computed(() => !embed);
  const sharePopoverVisible = ref(false);
  const shareTypePath = computed(() => workbenchRoutePath(slug).replace(/^\//, ''));

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
  const canRestore = computed(() => !isNamed.value && source.value === 'user');
  /** 编辑按钮可用性：默认工作台人人可；命名工作台仅系统角色；分享 embed 只读 */
  const canEditToggle = computed(() => !embed && (isNamed.value ? isSystem.value : true));
  /** ▾ 菜单仅系统角色（命名工作台的发布者/管理者）渲染 */
  const showNamedMenu = computed(() => !embed && isSystem.value);
  const canPublish = computed(() => isSystem.value);
  const canRename = computed(() => isSystem.value && isNamed.value);
  const canDelete = computed(() => isSystem.value && isNamed.value);
  /** 当前工作台名称：默认工作台 / 命名标题 */
  const currentLabel = computed(() => (isNamed.value ? namedTitle.value || currentSlug.value : '默认工作台'));

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
      dashboard.value = next;
      surface.dashboard = next;
      try {
        if (isNamed.value) {
          await cubeApi.workbench.namedPut(slug, { title: namedTitle.value || '命名工作台', homeJson: json });
        } else {
          source.value = 'user';
          await cubeApi.workbench.put(json);
        }
      } catch (e) {
        source.value = prevSource;
        dashboard.value = prevDash;
        surface.dashboard = prevDash;
        throw e;
      }
    },
  });

  provide(WIDGET_SURFACE_KEY, surface);

  async function refreshNamedList() {
    if (!isSystem.value) {
      namedList.value = [];
      return;
    }
    try {
      const res = await cubeApi.workbench.namedList();
      namedList.value = (res.data ?? []) as NamedWorkbenchItem[];
    } catch {
      // 列表失败不阻断工作台本身
    }
  }

  /** 菜单变更后刷新：重置动态路由注册标志并拉最新菜单（租户切换同款），供发布/删除后新路由可命中 */
  async function refreshMenus() {
    resetMenuRoutesFlag();
    try {
      await userStore.fetchMenus();
    } catch {
      // 菜单刷新失败不阻断主流程
    }
  }

  async function load() {
    loading.value = true;
    loadError.value = '';
    try {
      if (isNamed.value) {
        // 命名工作台：只读命名槽（普通用户）；系统角色可进入编辑态保存
        const res = await cubeApi.workbench.namedGet(slug);
        const data = res.data as { title?: string; config?: unknown } | undefined;
        namedTitle.value = data?.title ?? '';
        source.value = 'named';
        dashboard.value = parseWorkbenchConfig(data?.config) ?? emptyDashboard();
        surface.dashboard = dashboard.value;
      } else {
        const res = await cubeApi.workbench.get();
        const data = res.data;
        source.value = data?.source || 'system';
        roleId.value = data?.roleId ?? 0;
        const cfg = alignWorkbenchSeedLayout(parseWorkbenchConfig(data?.config) ?? emptyDashboard());
        dashboard.value = cfg;
        surface.dashboard = cfg;
      }
      if (!embed) await refreshNamedList();
    } catch (e) {
      loadError.value = e instanceof Error ? e.message : isNamed.value ? '加载命名工作台失败' : '加载工作台失败';
      dashboard.value = emptyDashboard();
      surface.dashboard = dashboard.value;
    } finally {
      loading.value = false;
    }
  }

  function toggleEdit() {
    if (!canEditToggle.value) return;
    editing.value = !editing.value;
    surface.canEdit = editing.value;
  }

  function toggleFullscreen() {
    fullscreen.value = !fullscreen.value;
  }

  function onFullscreenKeydown(e: KeyboardEvent) {
    if (e.key === 'Escape' && fullscreen.value) fullscreen.value = false;
  }

  function bindFullscreenEsc() {
    window.addEventListener('keydown', onFullscreenKeydown);
  }

  function unbindFullscreenEsc() {
    window.removeEventListener('keydown', onFullscreenKeydown);
  }

  async function restoreDefault() {
    if (isNamed.value) return;
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

  // ---- 命名工作台操作（发布/重命名/删除/切换） ----
  const publishVisible = ref(false);
  const publishTitle = ref('');
  const publishSlug = ref('');
  const publishError = ref('');
  const renameVisible = ref(false);
  const renameTitle = ref('');
  const deleteVisible = ref(false);

  function openPublish() {
    if (!isSystem.value) return;
    if (isNamed.value) {
      // 有 slug：把当前命名工作台的编辑结果直接更新发布
      void publishUpdate();
      return;
    }
    publishTitle.value = '';
    publishSlug.value = defaultPublishSlug();
    publishError.value = '';
    publishVisible.value = true;
  }

  async function publishUpdate() {
    if (!isSystem.value || !isNamed.value) return;
    try {
      await cubeApi.workbench.namedPut(slug, {
        title: namedTitle.value || '命名工作台',
        homeJson: serializeDashboardJson(dashboard.value, 'workbench'),
      });
      await refreshNamedList();
      Message.success('命名工作台已更新发布');
    } catch (e) {
      Message.error(e instanceof Error ? e.message : '发布失败');
    }
  }

  /** 另存为：确认后创建命名槽 + 挂菜单，再跳转到新页 */
  async function confirmPublish(): Promise<boolean> {
    const title = publishTitle.value.trim();
    const slugV = publishSlug.value.trim().toLowerCase();
    publishError.value = '';
    if (!title || title.length > 40) {
      publishError.value = '标题不能为空且不超过 40 字';
      return false;
    }
    if (!isValidNamedSlug(slugV)) {
      publishError.value = 'slug 须以小写字母开头，仅含小写字母/数字/中划线，长度 ≤32';
      return false;
    }
    // 另存为预检：slug 已被占用时阻止（后端 create 语义 409 兑底）
    const hit = namedList.value.find((n) => n.slug === slugV);
    if (hit) {
      publishError.value = `slug「${slugV}」已被命名工作台「${hit.title || hit.slug}」占用，另存为不允许覆盖`;
      return false;
    }
    try {
      await cubeApi.workbench.namedPut(slugV, {
        title,
        homeJson: serializeDashboardJson(dashboard.value, 'workbench'),
        create: true,
      });
      // 新菜单行需重新注册路由并刷新菜单树，否则 /Workbench/{slug} 无法命中
      await refreshMenus();
      Message.success('已发布，正在打开命名工作台');
      await router.push('/Workbench/' + slugV);
      return true;
    } catch (e) {
      publishError.value = e instanceof Error ? e.message : '发布失败';
      return false;
    }
  }

  function openRename() {
    if (!canRename.value) return;
    renameTitle.value = namedTitle.value || '';
    renameVisible.value = true;
  }

  async function confirmRename(): Promise<boolean> {
    const title = renameTitle.value.trim();
    if (!title || title.length > 40) {
      Message.warning('标题不能为空且不超过 40 字');
      return false;
    }
    try {
      // 仅改标题：先取服务端最新配置再保存，避免用 keep-alive 陈旧墙整份覆盖
      const latest = await cubeApi.workbench.namedGet(slug);
      const cfg = parseWorkbenchConfig(latest.data?.config);
      const payload = cfg ?? dashboard.value;
      await cubeApi.workbench.namedPut(slug, {
        title,
        homeJson: serializeDashboardJson(payload, 'workbench'),
      });
      namedTitle.value = title;
      dashboard.value = payload;
      surface.dashboard = payload;
      try {
        await userStore.fetchMenus();
      } catch {
        // 菜单标题刷新失败不阻断
      }
      Message.success('已重命名命名工作台');
      return true;
    } catch (e) {
      Message.error(e instanceof Error ? e.message : '重命名失败');
      return false;
    }
  }

  function openDelete() {
    if (!canDelete.value) return;
    deleteVisible.value = true;
  }

  async function confirmDelete(): Promise<boolean> {
    try {
      await cubeApi.workbench.namedDelete(slug);
      // 菜单行已删：重置路由标志并刷新菜单树，侧栏不再出现该项
      await refreshMenus();
      Message.success('已删除命名工作台');
      await router.push('/home');
      return true;
    } catch (e) {
      Message.error(e instanceof Error ? e.message : '删除失败');
      return false;
    }
  }

  async function onNamedSelect(value: string) {
    const parsed = parseNamedMenuValue(value);
    if (!parsed) return;
    switch (parsed.action) {
      case 'publish':
        openPublish();
        break;
      case 'rename':
        openRename();
        break;
      case 'delete':
        openDelete();
        break;
      case 'default':
        await router.push('/home');
        break;
      case 'switch':
        await router.push(workbenchRoutePath(parsed.slug));
        break;
    }
  }

  onMounted(() => {
    void load();
  });

  // keep-alive 下离开 /home 仍会保留实例；仅在激活时监听 Esc，避免其它页误退全屏
  onActivated(bindFullscreenEsc);
  onDeactivated(unbindFullscreenEsc);
  onBeforeUnmount(unbindFullscreenEsc);

  return {
    loading,
    source,
    dashboard,
    editing,
    loadError,
    hello,
    todayLabel,
    isNamed,
    currentSlug,
    currentLabel,
    canRestore,
    canEditToggle,
    canPublish,
    canRename,
    canDelete,
    showNamedMenu,
    namedList,
    fullscreen,
    showShare,
    sharePopoverVisible,
    shareTypePath,
    publishVisible,
    publishTitle,
    publishSlug,
    publishError,
    renameVisible,
    renameTitle,
    deleteVisible,
    toggleEdit,
    toggleFullscreen,
    restoreDefault,
    onNamedSelect,
    confirmPublish,
    confirmRename,
    confirmDelete,
    load,
  };
}
