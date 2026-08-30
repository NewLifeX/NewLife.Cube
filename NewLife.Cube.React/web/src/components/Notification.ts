/**
 * 通知工具（antd notification 封装，对齐 Vue 皮肤 components/Notification.ts）
 *
 * 通过 App.useApp() 托管实例获取 notification，提示样式跟随明暗主题。
 */
import { getNotification } from '@/utils/antdApp';

export interface NotificationParams {
  message: string;
  title?: string;
  duration?: number;
}

/** 信息通知 */
export function notifyInfo(params: NotificationParams) {
  getNotification().info({
    message: params.title || '信息',
    description: params.message,
    duration: params.duration ?? 3,
  });
}

/** 成功通知 */
export function notifySuccess(params: NotificationParams) {
  getNotification().success({
    message: params.title || '成功',
    description: params.message,
    duration: params.duration ?? 3,
  });
}

/** 错误通知 */
export function notifyError(params: NotificationParams) {
  getNotification().error({
    message: params.title || '错误',
    description: params.message,
    duration: params.duration ?? 5,
  });
}

/** 警告通知 */
export function notifyWarning(params: NotificationParams) {
  getNotification().warning({
    message: params.title || '警告',
    description: params.message,
    duration: params.duration ?? 4,
  });
}

export default { info: notifyInfo, success: notifySuccess, error: notifyError, warning: notifyWarning };
