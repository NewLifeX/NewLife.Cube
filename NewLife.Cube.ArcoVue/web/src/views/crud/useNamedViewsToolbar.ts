import { computed } from 'vue';
import { DEFAULT_VIEW_NAME, type NamedView } from '@/core/utils/viewProfile';

/** NamedViewsToolbar 组件 props 类型（与 NamedViewsToolbar.vue defineProps 泛型逐字一致） */
interface NamedViewsToolbarProps {
  views: NamedView[];
  activeId: string;
}

/** NamedViewsToolbar 组件 emits 类型（与 NamedViewsToolbar.vue defineEmits 泛型逐字一致） */
interface NamedViewsToolbarEmits {
  switch: [id: string];
  create: [name: string];
  rename: [id: string, name: string];
  remove: [id: string];
  reset: [];
  openConfig: [];
}

type NamedViewsToolbarEmit = <K extends keyof NamedViewsToolbarEmits>(event: K, ...args: NamedViewsToolbarEmits[K]) => void;

/** NamedViewsToolbar 组件全部业务 TS：当前视图名与下拉操作（自 NamedViewsToolbar.vue script setup 原样搬移） */
export function useNamedViewsToolbar(props: NamedViewsToolbarProps, emit: NamedViewsToolbarEmit) {
  const activeName = computed(
    () => props.views.find((v) => v.id === props.activeId)?.name || DEFAULT_VIEW_NAME,
  );

  function onSelect(val: string | number | Record<string, unknown> | undefined) {
    const key = String(val);
    if (key.startsWith('switch:')) {
      emit('switch', key.slice('switch:'.length));
      return;
    }
    if (key === 'new') {
      const name = window.prompt('新视图名称（表格视图）', '未命名');
      if (name) emit('create', name);
      return;
    }
    if (key === 'rename') {
      const cur = props.views.find((v) => v.id === props.activeId);
      const name = window.prompt('重命名视图', cur?.name || '');
      if (name) emit('rename', props.activeId, name);
      return;
    }
    if (key === 'delete') {
      if (window.confirm('删除当前视图？')) emit('remove', props.activeId);
      return;
    }
    if (key === 'reset') {
      if (window.confirm(`恢复为默认「${DEFAULT_VIEW_NAME}」视图并清除已保存配置？`)) emit('reset');
    }
  }

  return {
    activeName,
    onSelect,
  };
}
