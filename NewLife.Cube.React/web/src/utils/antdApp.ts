/**
 * antd App.useApp() 实例托管（供非组件模块复用）
 *
 * antd6 中静态 message / notification 不跟随 ConfigProvider 主题，
 * 通过根组件 <App> 内绑定 useApp() 返回的实例，api 拦截器等非组件场景
 * 即可获得与界面一致的明暗主题提示。
 */
import { App, message, notification } from 'antd';

type AntdAppInstance = ReturnType<typeof App.useApp>;

let antdApp: AntdAppInstance | null = null;

/** 绑定 App.useApp() 实例（由根组件内挂载时调用一次） */
export function bindAntdApp(app: AntdAppInstance) {
  antdApp = app;
}

/** 获取 message 实例（未绑定时用静态 message 兜底） */
export function getMessage(): AntdAppInstance['message'] {
  return antdApp?.message ?? message;
}

/** 获取 notification 实例（未绑定时用静态 notification 兜底） */
export function getNotification(): AntdAppInstance['notification'] {
  return antdApp?.notification ?? notification;
}
