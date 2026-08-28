/**
 * 通知铃铛按钮（对齐 Vue 皮肤 NotificationBell.vue，增强项 E3）
 *
 * 后端通知中心 API 未暴露时作为顶栏入口按钮；点击事件由父级处理。
 * 与 Vue 皮肤功能对等：纯 UI 入口（铃铛图标 + 可选文字标签 + 未读角标），不内联拉取数据。
 */
import { Badge, Button } from 'antd';
import { BellOutlined } from '@ant-design/icons';

export interface NotificationBellProps {
  /** 是否显示文字标签 */
  showLabel?: boolean;
  /** 按钮文本 */
  label?: string;
  /** 未读数（0 不显示角标） */
  count?: number;
  /** 点击回调 */
  onClick?: () => void;
}

export default function NotificationBell({ showLabel, label = '通知', count = 0, onClick }: NotificationBellProps) {
  return (
    <Badge count={count} size="small">
      {/* 可访问名：showLabel 时取文字标签，否则取默认“通知” */}
      <Button
        type="text"
        icon={<BellOutlined />}
        aria-label={showLabel ? label : '通知'}
        title="通知"
        onClick={onClick}
      >
        {showLabel ? label : null}
      </Button>
    </Badge>
  );
}
