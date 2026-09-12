/**
 * 用户认证 Store — 基于 @newlifex/auth-logic/pinia 统一适配器
 *
 * 消除本地 findMenu 重复函数，直接使用共享包提供的 createPiniaAuthStore。
 */
import { createPiniaAuthStore } from '@newlifex/auth-logic/pinia';
import { api } from '@/api';
export const useUserStore = createPiniaAuthStore(api, 'user');
