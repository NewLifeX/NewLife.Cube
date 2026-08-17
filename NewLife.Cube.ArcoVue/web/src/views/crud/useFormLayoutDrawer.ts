import { computed, ref, watch } from 'vue';
import { Message } from '@arco-design/web-vue';
import type { FieldMeta } from '@/core/types/field';
import { isAuditField, isTenantField } from '@/core/utils/fieldControl';
import { normalizeFormLayout } from '@/core/utils/fieldGroups';
import {
  buildFormJsonWire,
  emptyFormLayout,
  type FormLayout,
  type FormMode,
} from '@/core/utils/viewProfile';
import { useViewProfileStore } from '@/stores/viewProfile';
import { useTenantStore } from '@/stores/tenant';

/** FormLayoutDrawer 组件 props 类型（与 FormLayoutDrawer.vue defineProps 泛型逐字一致） */
interface FormLayoutDrawerProps {
  visible: boolean;
  typePath: string;
  addFields: FieldMeta[];
  editFields: FieldMeta[];
  detailFields: FieldMeta[];
  /** 无配置权限时不渲染入口（父级已控）；组件内再防御禁用 */
  canConfigure: boolean;
}

/** FormLayoutDrawer 组件 emits 类型（与 FormLayoutDrawer.vue defineEmits 泛型逐字一致） */
interface FormLayoutDrawerEmits {
  'update:visible': [boolean];
}

type FormLayoutDrawerEmit = <K extends keyof FormLayoutDrawerEmits>(event: K, ...args: FormLayoutDrawerEmits[K]) => void;

/** FormLayoutDrawer 组件全部业务 TS：三模式表单布局本地编辑与手动保存（自 FormLayoutDrawer.vue script setup 原样搬移） */
export function useFormLayoutDrawer(props: FormLayoutDrawerProps, emit: FormLayoutDrawerEmit) {
  const evpStore = useViewProfileStore();
  const tenantStore = useTenantStore();

  const modeOptions: { value: FormMode; label: string }[] = [
    { value: 'add', label: '新增' },
    { value: 'edit', label: '编辑' },
    { value: 'detail', label: '详情' },
  ];

  const activeMode = ref<FormMode>('edit');
  /** 三个模式的本地编辑态（OSC-0013 手动保存）：勾选/拖动只改本地，Tab 切换保留未保存修改，仅「保存」提交 */
  const localLayouts = ref<Record<FormMode, FormLayout>>({
    add: emptyFormLayout(),
    edit: emptyFormLayout(),
    detail: emptyFormLayout(),
  });

  /** 当前模式的本地布局（编辑态） */
  const currentLayout = computed(() => localLayouts.value[activeMode.value]);

  const drawerWidth = computed(() =>
    typeof window !== 'undefined' && window.innerWidth < 768 ? '100%' : 420,
  );

  /** 当前模式可配置字段：与 FormContent 同源过滤（审计/租户字段；add 另去 PK/readOnly） */
  function modeFields(mode: FormMode): FieldMeta[] {
    const raw =
      mode === 'add'
        ? props.addFields
        : mode === 'edit'
          ? props.editFields
          : props.detailFields;
    let list = raw.filter((f) => !isAuditField(f));
    if (!tenantStore.enableTenant) {
      list = list.filter((f) => !isTenantField(f));
    }
    if (mode === 'add') {
      return list.filter((f) => !f.primaryKey && !f.readOnly);
    }
    if (mode === 'edit') {
      return list.filter((f) => !f.primaryKey);
    }
    return list;
  }

  /** 按本地 order 排序后的展示字段：order 前 + 未列按元数据原序追加 */
  const orderedFields = computed<FieldMeta[]>(() => {
    const fields = modeFields(activeMode.value);
    const orderMap = new Map(currentLayout.value.order.map((n, i) => [n, i]));
    const ordered = fields
      .filter((f) => orderMap.has(f.name))
      .sort((a, b) => orderMap.get(a.name)! - orderMap.get(b.name)!);
    const rest = fields.filter((f) => !orderMap.has(f.name));
    return [...ordered, ...rest];
  });

  /** 当前字段集实际存在的非空 Category */
  const categoryOptions = computed<string[]>(() => {
    const set = new Set<string>();
    for (const f of modeFields(activeMode.value)) {
      const c = (f.category ?? '').trim();
      if (c) set.add(c);
    }
    return [...set];
  });

  /** 打开时从 store 加载三个模式的已保存布局（丢弃上次未保存修改） */
  function loadAll() {
    localLayouts.value = {
      add: normalizeFormLayout(
        evpStore.getFormModeLayout(props.typePath, 'add'),
        modeFields('add'),
      ),
      edit: normalizeFormLayout(
        evpStore.getFormModeLayout(props.typePath, 'edit'),
        modeFields('edit'),
      ),
      detail: normalizeFormLayout(
        evpStore.getFormModeLayout(props.typePath, 'detail'),
        modeFields('detail'),
      ),
    };
  }

  // immediate：组件挂载时 visible 可能已为 true（如页面加载后立即打开），必须加载一次，否则显示默认全部字段
  // Tab 切换不重载：三模式各自保留本地编辑态，未保存修改不因切换丢失
  watch(
    () => props.visible,
    (v) => {
      if (v) loadAll();
    },
    { immediate: true },
  );

  /** 仅更新当前模式的本地布局（不触发保存） */
  function patchCurrentLayout(patch: Partial<FormLayout>) {
    localLayouts.value = {
      ...localLayouts.value,
      [activeMode.value]: { ...currentLayout.value, ...patch },
    };
  }

  function isHidden(name: string): boolean {
    return currentLayout.value.hidden.includes(name);
  }

  function setHidden(name: string, hidden: boolean) {
    const set = new Set(currentLayout.value.hidden);
    if (hidden) set.add(name);
    else set.delete(name);
    patchCurrentLayout({ hidden: [...set] });
  }

  function isCollapsed(category: string): boolean {
    return currentLayout.value.collapsedCategories.includes(category);
  }

  function setCollapsed(category: string, collapsed: boolean) {
    const set = new Set(currentLayout.value.collapsedCategories);
    if (collapsed) set.add(category);
    else set.delete(category);
    patchCurrentLayout({ collapsedCategories: [...set] });
  }

  let dragFrom = -1;

  function onDragStart(idx: number, e: DragEvent) {
    dragFrom = idx;
    e.dataTransfer?.setData('text/plain', String(idx));
  }

  function onDrop(toIdx: number) {
    if (dragFrom < 0 || dragFrom === toIdx) return;
    const names = orderedFields.value.map((f) => f.name);
    const [item] = names.splice(dragFrom, 1);
    names.splice(toIdx, 0, item);
    dragFrom = -1;
    patchCurrentLayout({ order: names });
  }

  /** 手动保存：三个模式一次性提交（未修改模式不写入）；保存后关闭抽屉 */
  function saveLayout() {
    if (!props.canConfigure) return;
    evpStore.setFormJson(props.typePath, buildFormJsonWire(localLayouts.value));
    Message.success('布局已保存');
    emit('update:visible', false);
  }

  /** 手动取消：关闭抽屉并丢弃未保存修改（下次打开重新从 store 加载） */
  function cancel() {
    emit('update:visible', false);
  }

  /** 恢复当前模式默认布局：仅本地清空（保存时才提交删除该模式），不影响视图/筛选/PageSize 域 */
  function resetLayout() {
    if (!props.canConfigure) return;
    patchCurrentLayout(emptyFormLayout());
    Message.success('已恢复默认布局（保存后生效）');
  }

  return {
    modeOptions,
    activeMode,
    drawerWidth,
    orderedFields,
    categoryOptions,
    isHidden,
    setHidden,
    isCollapsed,
    setCollapsed,
    onDragStart,
    onDrop,
    saveLayout,
    cancel,
    resetLayout,
  };
}
