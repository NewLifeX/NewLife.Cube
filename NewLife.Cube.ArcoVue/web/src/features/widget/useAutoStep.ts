import { onBeforeUnmount, onMounted, unref, watch, type MaybeRef } from 'vue';

const TICK_MS = 1000;

/**
 * 每秒步进一次；active 为 false 时停止。
 * 支持 Ref / ComputedRef（数据到位后 active 变 true 会自动启动）。
 */
export function useAutoStep(active: MaybeRef<boolean>, step: () => void | boolean) {
  let timer: ReturnType<typeof setInterval> | null = null;

  function stop() {
    if (timer != null) {
      clearInterval(timer);
      timer = null;
    }
  }

  function start() {
    stop();
    if (!unref(active)) return;
    timer = setInterval(() => {
      if (!unref(active)) return;
      step();
    }, TICK_MS);
  }

  onMounted(start);
  onBeforeUnmount(stop);
  watch(
    () => unref(active),
    (on) => {
      if (on) start();
      else stop();
    },
  );

  return { restart: start, stop };
}
