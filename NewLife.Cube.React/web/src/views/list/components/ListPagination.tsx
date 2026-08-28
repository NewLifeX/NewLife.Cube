/**
 * 列表分页 + 统计行
 */
import { Pagination, Space } from 'antd';

export interface ListPaginationProps {
  total: number;
  current: number;
  pageSize: number;
  statData?: Record<string, unknown> | null;
  onChange?: (page: number, pageSize: number) => void;
}

export default function ListPagination({ total, current, pageSize, statData, onChange }: ListPaginationProps) {
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 8 }}>
      <Space size={16} wrap>
        {statData &&
          Object.entries(statData).map(([k, v]) => (
            <span key={k} style={{ fontSize: 13, color: 'rgba(0,0,0,0.65)' }}>
              {k}: <strong>{String(v ?? '')}</strong>
            </span>
          ))}
      </Space>
      <Pagination
        showSizeChanger
        showQuickJumper
        showTotal={(t) => `共 ${t} 条`}
        total={total}
        current={current}
        pageSize={pageSize}
        pageSizeOptions={[10, 20, 50, 100]}
        onChange={onChange}
      />
    </div>
  );
}
