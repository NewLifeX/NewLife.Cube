
/**
 * 通知工具组件
 * 封装 Element Plus 的 ElNotification 组件
 */
import { ElNotification } from 'element-plus'
import type { NotificationParams } from '../types/notification'

/**
 * 显示信息通知
 * @param {Object} params - 通知参数
 * @param {string} params.message - 通知消息内容
 * @param {string} [params.title] - 通知标题
 * @param {number} [params.duration] - 显示时间，单位毫秒
 */
const info = (params: NotificationParams) => {
  ElNotification({
    type: 'info',
    message: params.message,
    title: params.title || '信息',
    duration: params.duration || 3000,
  })
}

/**
 * 显示成功通知
 * @param {Object} params - 通知参数
 * @param {string} params.message - 通知消息内容
 * @param {string} [params.title] - 通知标题
 * @param {number} [params.duration] - 显示时间，单位毫秒
 */
const success = (params: NotificationParams) => {
  ElNotification({
    type: 'success',
    message: params.message,
    title: params.title || '成功',
    duration: params.duration || 3000,
  })
}

/**
 * 显示错误通知
 * @param {Object} params - 通知参数
 * @param {string} params.message - 通知消息内容
 * @param {string} [params.title] - 通知标题
 * @param {number} [params.duration] - 显示时间，单位毫秒
 */
const error = (params: NotificationParams) => {
  ElNotification({
    type: 'error',
    message: params.message,
    title: params.title || '错误',
    duration: params.duration || 5000,
  })
}

/**
 * 显示警告通知
 * @param {Object} params - 通知参数
 * @param {string} params.message - 通知消息内容
 * @param {string} [params.title] - 通知标题
 * @param {number} [params.duration] - 显示时间，单位毫秒
 */
const warning = (params: NotificationParams) => {
  ElNotification({
    type: 'warning',
    message: params.message,
    title: params.title || '警告',
    duration: params.duration || 4000,
  })
}

/**
 * 根据类型自动显示对应的通知
 * @param {string} type - 通知类型：info, success, error, warning
 * @param {string} message - 通知消息内容
 * @param {string} [title] - 通知标题
 * @param {number} [duration] - 显示时间，单位毫秒
 */
const autoNotification = (type: string, message: string, title?: string, duration?: number) => {
  const params = { message, title, duration }

  switch (type) {
    case 'success':
      success(params)
      break
    case 'error':
      error(params)
      break
    case 'warning':
      warning(params)
      break
    case 'info':
    default:
      info(params)
      break
  }
}

// 导出通知函数
const Notification = {
  info,
  success,
  error,
  warning,
  autoNotification,
}

export default Notification

