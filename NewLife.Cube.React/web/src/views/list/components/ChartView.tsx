/**
 * 图表视图（表格/图表视图切换的图表侧，规范 §7.6）
 *
 * GetChartData 返回 ECharts 配置数组（option 对象），逐个渲染；
 * 独立成视图不占表格空间，自适应容器宽度，窗口缩放时自动重绘。
 */
import { useEffect, useRef } from 'react';
import { Empty, Spin } from 'antd';
import * as echarts from 'echarts';

export interface ChartViewProps {
  /** ECharts 配置数组 */
  charts: unknown[];
}

export default function ChartView({ charts }: ChartViewProps) {
  if (!charts.length) return <Empty description="暂无图表数据" />;

  return (
    <div className="cube-chart-view">
      {charts.map((option, i) => (
        <ChartPanel key={i} option={option} />
      ))}
    </div>
  );
}

function ChartPanel({ option }: { option: unknown }) {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!ref.current || !option || typeof option !== 'object') return;
    const chart = echarts.init(ref.current);
    chart.setOption(option as echarts.EChartsOption);
    const resize = () => chart.resize();
    window.addEventListener('resize', resize);
    return () => {
      window.removeEventListener('resize', resize);
      chart.dispose();
    };
  }, [option]);

  return (
    <div style={{ display: 'flex', justifyContent: 'center', padding: 8 }}>
      <Spin spinning={!option}>
        <div ref={ref} style={{ width: '100%', height: 360 }} />
      </Spin>
    </div>
  );
}
