import { computed, ref } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { NamedView, ViewKind } from '@/core/utils/viewProfile';
import {
  VIEW_KIND_LABEL,
  canCreateViewKind,
  defaultViewKindName,
  viewKindCreateLabel,
} from '@/core/utils/viewMapping';
import { VIEW_KIND_ICONS } from '@/core/utils/iconRegistry';

/** ViewTabsToolbar 组件 props 类型（与 ViewTabsToolbar.vue defineProps 泛型逐字一致） */
interface ViewTabsToolbarProps {
  views: NamedView[];
  activeId: string;
  fields: FieldMeta[];
  typePath: string;
  /** 系统管理员可把当前视图保存为该实体默认（全局模板）视图 */
  isAdmin?: boolean;
  /** 当前视图是否处于全屏展示状态（切换按钮图标/提示随之变化） */
  fullscreen?: boolean;
}

/** ViewTabsToolbar 组件 emits 类型（与 ViewTabsToolbar.vue defineEmits 泛型逐字一致） */
interface ViewTabsToolbarEmits {
  switch: [id: string];
  create: [kind: ViewKind, name: string];
  rename: [id: string, name: string];
  remove: [id: string];
  duplicate: [id: string];
  reset: [];
  openConfig: [];
  saveAsDefault: [];
  toggleFullscreen: [];
}

type ViewTabsToolbarEmit = <K extends keyof ViewTabsToolbarEmits>(event: K, ...args: ViewTabsToolbarEmits[K]) => void;

/** ViewTabsToolbar 组件全部业务 TS：视图 Tab 切换、新建视图类型下拉与命名弹层（自 ViewTabsToolbar.vue script setup 原样搬移） */
export function useViewTabsToolbar(props: ViewTabsToolbarProps, emit: ViewTabsToolbarEmit) {
  /** 当前激活视图的类型（决定「保存视图为默认XX视图」文案） */
  const activeViewKind = computed<ViewKind>(() => {
    const v = props.views.find((x) => x.id === props.activeId);
    return v?.view ?? 'table';
  });

  const createKinds: ViewKind[] = ['table', 'tree', 'card', 'kanban', 'calendar', 'gantt'];

  const createOptions = computed(() =>
    createKinds.map((kind) => {
      const gate = canCreateViewKind(kind, props.fields, props.typePath);
      return {
        kind,
        label: viewKindCreateLabel(kind),
        ok: gate.ok,
        reason: gate.reason,
      };
    }),
  );

  /** 视图命名弹层（创建/重命名共用）：相对主界面居中、Arco 组件自动跟随主题 */
  const nameModalVisible = ref(false);
  const nameModalTitle = ref('新建视图');
  const nameDraft = ref('');
  /** 'create' 或 'rename' */
  const nameMode = ref<'create' | 'rename'>('create');
  const nameKind = ref<ViewKind>('table');
  const nameTargetId = ref('');

  function openCreateModal(kind: ViewKind) {
    nameMode.value = 'create';
    nameKind.value = kind;
    nameModalTitle.value = `新建${viewKindCreateLabel(kind)}`;
    nameDraft.value = viewKindCreateLabel(kind);
    nameTargetId.value = '';
    nameModalVisible.value = true;
  }

  function openRenameModal(id: string) {
    const cur = props.views.find((v) => v.id === id);
    if (!cur) return;
    nameMode.value = 'rename';
    nameTargetId.value = id;
    nameModalTitle.value = '重命名视图';
    nameDraft.value = cur.name || '';
    nameModalVisible.value = true;
  }

  function closeNameModal() {
    nameModalVisible.value = false;
  }

  function submitName() {
    const name = nameDraft.value.trim();
    if (!name) return;
    if (nameMode.value === 'create') {
      emit('create', nameKind.value, name);
    } else {
      emit('rename', nameTargetId.value, name);
    }
    closeNameModal();
  }

  function onMenuSelect(val: string | number | Record<string, unknown> | undefined) {
    const key = String(val);
    if (key === 'rename') {
      openRenameModal(props.activeId);
      return;
    }
    if (key === 'config') {
      emit('openConfig');
      return;
    }
    if (key === 'duplicate') {
      emit('duplicate', props.activeId);
      return;
    }
    if (key === 'delete') {
      if (window.confirm('删除当前视图？')) emit('remove', props.activeId);
      return;
    }
    if (key === 'saveAsDefault') {
      emit('saveAsDefault');
      return;
    }
    if (key === 'reset') {
      if (window.confirm('将当前视图恢复到创建时的默认配置？')) emit('reset');
    }
  }

  /** 创建视图类型下拉：受控弹出（锚点定位到 add 按钮下方） */
  const createPopupVisible = ref(false);
  const createAnchorRef = ref<HTMLElement | null>(null);

  /** 点击 Tab 条末尾 +（Arco editable add 按钮）：把锚点定位到按钮下方并弹出创建视图类型下拉 */
  function onAddClick(ev: MouseEvent) {
    const btn = ev.currentTarget as HTMLElement | null;
    const anchor = createAnchorRef.value;
    if (!btn || !anchor) return;
    const r = btn.getBoundingClientRect();
    anchor.style.position = 'fixed';
    anchor.style.left = `${r.left}px`;
    anchor.style.top = `${r.bottom + 4}px`;
    createPopupVisible.value = true;
  }

  function onCreatePopupChange(v: boolean) {
    createPopupVisible.value = v;
  }

  function onCreateSelect(val: string | number | Record<string, unknown> | undefined) {
    const kind = String(val) as ViewKind;
    const opt = createOptions.value.find((o) => o.kind === kind);
    if (!opt?.ok) return;
    createPopupVisible.value = false;
    openCreateModal(kind);
  }

  /** Tab 切换：Arco Tabs @change → 切换到指定视图 */
  function onTabChange(key: string | number) {
    emit('switch', String(key));
  }

  return {
    VIEW_KIND_LABEL,
    VIEW_KIND_ICONS,
    defaultViewKindName,
    activeViewKind,
    createOptions,
    nameModalVisible,
    nameModalTitle,
    nameDraft,
    createPopupVisible,
    createAnchorRef,
    closeNameModal,
    submitName,
    onMenuSelect,
    onAddClick,
    onCreatePopupChange,
    onCreateSelect,
    onTabChange,
  };
}
