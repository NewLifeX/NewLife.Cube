import { nextTick, ref, watch } from 'vue';
import type { WidgetCardProps } from './context';

/** 卡片标题点击进入行内编辑 */
export function useWidgetTitleEdit(props: WidgetCardProps) {
  const editing = ref(false);
  const draft = ref(props.widget.title || '');
  const inputRef = ref<{ focus?: () => void } | null>(null);

  watch(
    () => props.widget.title,
    (t) => {
      if (!editing.value) draft.value = t || '';
    },
  );

  async function startEdit(ev?: Event) {
    ev?.stopPropagation();
    ev?.preventDefault();
    if (!props.canEdit || !props.onTitleCommit) return;
    editing.value = true;
    draft.value = props.widget.title || '';
    await nextTick();
    inputRef.value?.focus?.();
  }

  function commit() {
    if (!editing.value) return;
    editing.value = false;
    const next = draft.value.trim().slice(0, 40) || props.widget.title || '未命名';
    draft.value = next;
    if (next !== (props.widget.title || '')) props.onTitleCommit?.(next);
  }

  function cancel() {
    editing.value = false;
    draft.value = props.widget.title || '';
  }

  function onKeydown(ev: KeyboardEvent) {
    if (ev.key === 'Enter') {
      ev.preventDefault();
      commit();
    } else if (ev.key === 'Escape') {
      ev.preventDefault();
      cancel();
    }
  }

  return { editing, draft, inputRef, startEdit, commit, cancel, onKeydown };
}
