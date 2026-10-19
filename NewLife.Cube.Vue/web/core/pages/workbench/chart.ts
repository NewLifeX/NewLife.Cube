/**
 * 工作台图表辅助函数
 *
 * 性能监控用轻量 SVG 折线渲染，不引入 ECharts 运行时：
 * 工作台只需要两条 0-100 的百分比曲线，SVG 足够且不增加首屏体积。
 */

/** 把数值限制在 [min, max] 区间 */
export function clamp(value: number, min: number, max: number): number {
  if (!Number.isFinite(value)) return min;
  return Math.min(max, Math.max(min, value));
}

/**
 * 生成 SVG polyline 的 points 属性
 * @param values 百分比序列（0-100）
 * @param width 画布宽度
 * @param height 画布高度
 * @param max 数值上限。默认 100
 * @returns 形如 "0.0,40.0 150.0,20.0" 的点串；无数据返回空串
 */
export function buildSparkPoints(values: number[], width: number, height: number, max = 100): string {
  if (!values.length || width <= 0 || height <= 0) return '';

  const safeMax = max > 0 ? max : 100;
  const pointAt = (value: number, index: number) => {
    const ratio = values.length === 1 ? 0 : index / (values.length - 1);
    const x = ratio * width;
    const y = height - (clamp(value, 0, safeMax) / safeMax) * height;
    return `${x.toFixed(1)},${y.toFixed(1)}`;
  };

  // 单点无法成线，复制为一条水平线，保证首帧可见
  if (values.length === 1) {
    const y = pointAt(values[0], 0).split(',')[1];
    return `0.0,${y} ${width.toFixed(1)},${y}`;
  }

  return values.map(pointAt).join(' ');
}

/**
 * 百分比文本。保留一位小数并去掉无意义的 .0
 * @param value 百分比数值
 */
export function formatPercent(value: number): string {
  if (!Number.isFinite(value)) return '-';
  const fixed = clamp(value, 0, 100).toFixed(1);
  return `${fixed.endsWith('.0') ? fixed.slice(0, -2) : fixed}%`;
}

/**
 * 内存文本。由 MB 换算为 GB，保留一位小数
 * @param used 已用 MB
 * @param total 总 MB
 */
export function formatMemory(used?: number, total?: number): string {
  if (!Number.isFinite(used) || !Number.isFinite(total) || !total) return '-';

  const gb = (value: number) => (value / 1024).toFixed(1);
  return `${gb(used as number)} / ${gb(total as number)} GB`;
}
