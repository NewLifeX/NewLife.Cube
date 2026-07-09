import { defineStore } from 'pinia';
import { getConfig } from '@newlifex/cube-vue/core/configure';
import { type AxiosRequestConfig } from 'axios';
import request from '../utils/request';
import { removeAccessToken } from '../utils/token';
import { useMenuStore } from './menu';
import { gotoPage } from '../utils/router';

const {
  auth: {
    reLoginParams,
  },
} = getConfig();
const loginPageUrl = reLoginParams?.loginPageUrl || '/login';


/** 用户信息 */
export interface UserInfo {
  id: number;
  name: string;
  password: string;
  displayName: string;
  sex: number;
  mail: string;
  mobile: string;
  code: string;
  avatar: string;
  roleID: number;
  roleIds: string;
  roleName: string;
  roleNames: string;
  departmentID: number;
  online: boolean;
  enable: boolean;
  logins: number;
  lastLogin: string;
  lastLoginIP: string;
  registerTime: string;
  registerIP: string;
  ex1: number;
  ex2: number;
  ex3: number;
  ex4: string;
  ex5: string;
  ex6: unknown;
  updateUser: string;
  updateUserID: number;
  updateIP: string;
  updateTime: string;
  remark: string;
  permission: string;
}

const state: {
  userInfo: Partial<UserInfo> | undefined;
  loading: boolean;
} = {
  userInfo: undefined,
  loading: false,
};

export const useUserStore = defineStore('user', {
  state: () => state,
  getters: {
    hasUserInfo: (state) => !!state.userInfo,
  },
  actions: {
    setUserInfo(info: Partial<UserInfo>) {
      this.userInfo = { ...this.userInfo, ...info };
    },
    /**
     * 用户登出
     * 先调用登出接口，再清除用户状态和token，并重定向到登录页
     */
    async logout() {
      try {
        // 获取认证配置
        const config = getConfig();
        const authConfig = config.auth;
        // 默认登出请求配置
        let axiosConfig: AxiosRequestConfig;

        // 如果有专门的登出配置，则使用该配置
        if (authConfig.logoutAxiosConfig) {
          if (typeof authConfig.logoutAxiosConfig === 'function') {
            const configResult = authConfig.logoutAxiosConfig();
            if (configResult instanceof Promise) {
              axiosConfig = await configResult;
            } else {
              axiosConfig = configResult;
            }
          } else {
            axiosConfig = authConfig.logoutAxiosConfig;
          }

          // 调用登出接口
          await request(axiosConfig);
        }
      } catch (error) {
        console.error('登出接口调用失败:', error);
      } finally {
        // 无论接口调用成功与否，都执行本地清理操作

        // 清除用户信息
        this.userInfo = undefined;

        // 清除菜单和路由状态
        const menuStore = useMenuStore();
        menuStore.flatMenus = undefined;
        menuStore.treeMenus = undefined;
        menuStore.activeMenu = undefined;
        menuStore.resetRoutesRegistered();

        // 清除token
        removeAccessToken();

        // 重定向到登录页
        gotoPage(loginPageUrl);
      }
    },
    async fetchUserInfoAsync() {
      if (this.loading) return;
      this.loading = true;
      // 如果没有用户信息，则获取获取用户信息、菜单信息
      if (!this.hasUserInfo) {
        try {
          const config = getConfig();
          const userConfig = config.user;
          const getUserInfoAxiosConfig = userConfig.getUserInfoAxiosConfig;
          let axiosConfig: AxiosRequestConfig;

          if (typeof getUserInfoAxiosConfig === 'function') {
            const configResult = getUserInfoAxiosConfig();
            if (configResult instanceof Promise) {
              axiosConfig = await configResult;
            } else {
              axiosConfig = configResult;
            }
          } else {
            axiosConfig = getUserInfoAxiosConfig;
          }

          const response = await request(axiosConfig) as unknown as Partial<UserInfo> & {
            code?: number;
            data?: unknown;
          };
          // 响应为标准 ApiResponse 结构 { code, data, message }，需取 data 字段作为用户信息；
          // 兼容非标准响应（已解包或无 data 字段）时直接使用 response 本身
          const userInfo =
            response && typeof response === 'object' && 'data' in response && 'code' in response
              ? (response.data as Partial<UserInfo>)
              : response;
          this.setUserInfo(userInfo);
        } catch (error) {
          console.error('Failed to fetch user info:', error);
        } finally {
          this.loading = false;
        }
      }
    },
  },
});
