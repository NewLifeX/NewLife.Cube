/**
 * 加载页
 */
import { Spin } from 'antd';

export default function LoadingPage() {
  return (
    <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
      <Spin size="large" />
    </div>
  );
}
