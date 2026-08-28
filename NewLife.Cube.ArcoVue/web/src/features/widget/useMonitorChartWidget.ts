import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import cubeApi from '@/api';
import { ensureEchartsTheme, initEcharts } from '@/core/utils/echartsTheme';
import { themeColor } from '@/core/utils/themeColor';
import type { WidgetCardProps } from './context';

interface Point {
  time: string;
  cpu: number;
  mem: number;
}

function readPoint(raw: unknown): Point | null {
  if (!raw || typeof raw !== 'object') return null;
  const r = raw as Record<string, unknown>;
  const cpu = Number(r.cpu ?? r.Cpu);
  const mem = Number(r.mem ?? r.Mem);
  if (!Number.isFinite(cpu) && !Number.isFinite(mem)) return null;
  return {
    time: String(r.time ?? r.Time ?? ''),
    cpu: Number.isFinite(cpu) ? cpu : 0,
    mem: Number.isFinite(mem) ? mem : 0,
  };
}

export function useMonitorChartWidget(props: WidgetCardProps) {
  const chartEl = ref<HTMLElement | null>(null);
  const points = ref<Point[]>([]);
  let chart: ReturnType<typeof initEcharts> | null = null;
  let timer: ReturnType<typeof setInterval> | null = null;
  let ro: ResizeObserver | null = null;
  let raf = 0;

  function push(p: Point | null) {
    if (!p) return;
    const next = [...points.value, p];
    points.value = next.slice(-12);
  }

  async function tick() {
    try {
      const res = await cubeApi.widget.data('Monitor');
      push(readPoint(res.data));
    } catch {
      /* 保持已有点 */
    }
  }

  function scheduleResize() {
    const el = chartEl.value;
    if (!el || !chart) return;
    const paint = () => {
      if (el.clientWidth > 0 && el.clientHeight > 0) chart?.resize();
    };
    paint();
    if (raf) cancelAnimationFrame(raf);
    raf = requestAnimationFrame(() => {
      paint();
      raf = requestAnimationFrame(paint);
    });
  }

  function bindResize(el: HTMLElement) {
    if (ro || typeof ResizeObserver === 'undefined') return;
    ro = new ResizeObserver(() => scheduleResize());
    ro.observe(el);
  }

  async function render() {
    await nextTick();
    const el = chartEl.value;
    if (!el) return;
    await ensureEchartsTheme(undefined);
    if (!chart || chart.getDom() !== el) {
      chart?.dispose();
      chart = initEcharts(el);
      bindResize(el);
    }
    const list = points.value;
    const xs = list.map((p) => p.time);
    const cpus = list.map((p) => p.cpu);
    const mems = list.map((p) => p.mem);
    const cpuColor = themeColor('--primary-6', 'rgb(22, 93, 255)');
    chart.setOption({
      tooltip: { trigger: 'axis' },
      legend: {
        data: ['CPU', '内存'],
        top: 0,
        itemWidth: 12,
        itemHeight: 8,
        textStyle: { fontSize: 11 },
      },
      // 与迷你柱状图 grid 上下边距对齐
      grid: { left: 4, right: 4, top: 22, bottom: 0, containLabel: true },
      xAxis: {
        type: 'category',
        data: xs,
        boundaryGap: false,
        axisLabel: { fontSize: 11, margin: 4, hideOverlap: true },
      },
      yAxis: {
        type: 'value',
        min: 0,
        max: 100,
        axisLabel: { formatter: '{value}%', fontSize: 10, margin: 2 },
        splitNumber: 4,
      },
      series: [
        {
          name: 'CPU',
          type: 'line',
          data: cpus,
          smooth: true,
          showSymbol: false,
          lineStyle: { width: 2, color: cpuColor },
        },
        {
          name: '内存',
          type: 'line',
          data: mems,
          smooth: true,
          showSymbol: false,
          lineStyle: { width: 2, color: '#00b42a' },
        },
      ],
    });
    scheduleResize();
  }

  watch(
    () => props.result,
    (r) => {
      const p = readPoint(r);
      if (p && points.value.length === 0) push(p);
    },
    { immediate: true },
  );

  // 栅格 w/h 变化时强制按新容器尺寸重绘
  watch(
    () => [props.widget.layout?.w, props.widget.layout?.h] as const,
    () => {
      void nextTick(() => scheduleResize());
    },
  );

  watch(points, () => {
    void render();
  });

  onMounted(() => {
    void render();
    timer = setInterval(() => {
      void tick();
    }, 5000);
  });

  onBeforeUnmount(() => {
    if (timer) clearInterval(timer);
    timer = null;
    if (raf) cancelAnimationFrame(raf);
    raf = 0;
    ro?.disconnect();
    ro = null;
    chart?.dispose();
    chart = null;
  });

  return { chartEl };
}
