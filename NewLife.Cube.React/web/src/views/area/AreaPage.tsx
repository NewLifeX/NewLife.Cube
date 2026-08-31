/**
 * 地区管理页（/Cube/Area）：列表 | 地图 双模式切换
 *
 * - 列表模式：复用通用实体列表页（EntityListPage）
 * - 地图模式：ECharts 中国地图散点（AreaMap，对齐 MVC Map.cshtml）
 * 由 DefaultListPage 探测到实体页后按 /Cube/Area 分发。
 */
import { useState } from 'react';
import { Segmented } from 'antd';
import EntityListPage from '@/views/list/EntityListPage';
import AreaMap from './AreaMap';

export interface AreaPageProps {
  /** 实体路径前缀，如 '/Cube/Area' */
  type: string;
}

export default function AreaPage({ type }: AreaPageProps) {
  const [mode, setMode] = useState<'list' | 'map'>('list');

  return (
    <div className="cube-area-page">
      <Segmented
        value={mode}
        onChange={(v) => setMode(v as 'list' | 'map')}
        options={[
          { label: '列表', value: 'list' },
          { label: '地图', value: 'map' },
        ]}
        style={{ marginBottom: 8 }}
      />
      {mode === 'list' ? <EntityListPage key={type} type={type} /> : <AreaMap type={type} />}
    </div>
  );
}
