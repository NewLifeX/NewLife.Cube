/**
 * 「添加记录」弹层拖动（OSC-261004d7f4 增强，2026-10-05）。
 *
 * 弹层去标题栏后 Arco 自带 `draggable`（依赖标题栏）失效，改为自实现：
 * 鼠标按住弹层任意位置（输入/按钮等交互元素除外）拖动弹窗本体，
 * 以 CSS 变量 `--map-add-dx/dy` + transform 平移（不破坏 wrapper 居中布局）。
 * 打开时重置位置、关闭时清理监听（弹层 DOM 在 Arco Teleport 容器内，命令式绑定）。
 */
import { ref } from 'vue';

/** 拖动阈值（位移绝对值之和 < 3px 视为点击，不产生位移） */
export const DRAG_THRESHOLD = 3;

/** 交互元素：按住时保留原生行为（输入/选择/点击），不启动拖动 */
export const INTERACTIVE_SELECTOR =
  'input, textarea, button, a, select, [contenteditable="true"], .arco-select-view, .arco-input-wrapper';

/** 弹层本体（modal-class）选择器 */
const DIALOG_SELECTOR = '.map-add-dialog';

export interface DragOrigin {
  /** 按下时指针坐标 */
  x: number;
  y: number;
  /** 按下时已累计的平移量 */
  tx: number;
  ty: number;
}

/** 计算拖动目标偏移；位移小于阈值返回 null（视作点击） */
export function computeDragOffset(
  origin: DragOrigin,
  clientX: number,
  clientY: number,
): { tx: number; ty: number } | null {
  const dx = clientX - origin.x;
  const dy = clientY - origin.y;
  if (Math.abs(dx) + Math.abs(dy) < DRAG_THRESHOLD) return null;
  return { tx: origin.tx + dx, ty: origin.ty + dy };
}

/** 是否为交互元素（命中祖先不启动拖动） */
export function isInteractiveTarget(target: Element | null): boolean {
  if (!target) return false;
  return !!target.closest(INTERACTIVE_SELECTOR);
}

export function useMapAddDrag() {
  const dragging = ref(false);
  let el: HTMLElement | null = null;
  let origin: DragOrigin = { x: 0, y: 0, tx: 0, ty: 0 };
  let tx = 0;
  let ty = 0;
  let bindRetried = false;

  function apply() {
    if (!el) return;
    el.style.setProperty('--map-add-dx', `${tx}px`);
    el.style.setProperty('--map-add-dy', `${ty}px`);
  }

  function onPointerMove(e: PointerEvent) {
    if (!dragging.value) return;
    const next = computeDragOffset(origin, e.clientX, e.clientY);
    if (!next) return;
    tx = next.tx;
    ty = next.ty;
    apply();
  }

  function onPointerUp() {
    if (!dragging.value) return;
    dragging.value = false;
    document.removeEventListener('pointermove', onPointerMove);
    document.removeEventListener('pointerup', onPointerUp);
    document.removeEventListener('pointercancel', onPointerUp);
  }

  function onPointerDown(e: PointerEvent) {
    if (e.button !== 0 || !el) return;
    if (isInteractiveTarget(e.target as Element | null)) return;
    dragging.value = true;
    origin = { x: e.clientX, y: e.clientY, tx, ty };
    // 捕获指针：拖出窗口/在弹层外松开也能收到 move/up
    el.setPointerCapture(e.pointerId);
    e.preventDefault();
    document.addEventListener('pointermove', onPointerMove);
    document.addEventListener('pointerup', onPointerUp);
    document.addEventListener('pointercancel', onPointerUp);
  }

  function bind() {
    el = document.querySelector<HTMLElement>(DIALOG_SELECTOR);
    if (!el) {
      // 首次打开时弹层 DOM 可能尚未挂载：仅重试一次，避免空转
      if (!bindRetried) {
        bindRetried = true;
        window.setTimeout(bind, 0);
      }
      return;
    }
    bindRetried = false;
    tx = 0;
    ty = 0;
    apply();
    el.addEventListener('pointerdown', onPointerDown);
  }

  /** Arco Modal @open：重置位置并绑定拖动 */
  function onOpen() {
    bind();
  }

  /** Arco Modal @close：清理监听与引用 */
  function onClose() {
    onPointerUp();
    el?.removeEventListener('pointerdown', onPointerDown);
    el = null;
  }

  return { dragging, onOpen, onClose };
}
