/**
 * 用户认证 Store — 基于 @cube/auth-logic/pinia 统一适配器
 *
 * 消除本地 findMenu 重复函数，直接使用共享包提供的 createPiniaAuthStore。
 */
import { createPiniaAuthStore } from '@cube/auth-logic/pinia';
import cubeApi from '@/api';

export const useUserStore = createPiniaAuthStore(cubeApi, 'user');
