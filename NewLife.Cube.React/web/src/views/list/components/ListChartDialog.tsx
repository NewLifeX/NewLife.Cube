/**
 * 图表弹窗（ECharts 懒加载）
 *
 * GetChartData 返回 ECharts 配置数组（option 对象），逐个渲染。
 */
import { useEffect, useRef } from 'react';
import { Modal, Spin } from 'antd';
import * as echarts from 'echarts';

export interface ListChartDialogProps {
  open: boolean;
  charts: unknown[];
  onClose: () => void;
}

export default function ListChartDialog({ open, charts, onClose }: ListChartDialogProps) {
  return (
    <Modal open={open} title="图表" onCancel={onClose} footer={null} width={800}>
      {charts.map((option, i) => (
        <ChartPanel key={i} option={option} />
      ))}
    </Modal>
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
        <div ref={ref} style={{ width: 720, height: 360 }} />
      </Spin>
    </div>
  );
}
