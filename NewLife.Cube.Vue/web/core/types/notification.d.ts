/**
 * 通知参数类型定义
 */
export interface NotificationParams {
  /**
   * 通知消息内容
   */
  message: string;

  /**
   * 通知标题（可选）
   */
  title?: string;

  /**
   * 显示时间，单位毫秒（可选）
   */
  duration?: number;
}

/**
 * 通知类型
 */
export type NotificationType = 'success' | 'warning' | 'info' | 'error';
