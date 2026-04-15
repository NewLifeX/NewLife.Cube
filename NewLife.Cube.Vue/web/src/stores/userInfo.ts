import { defineStore } from 'pinia';
import Cookies from 'js-cookie';
import { Session } from '/@/utils/storage';
import { useUserApi } from '../api/user';
const userApi = useUserApi();

/**
 * 用户信息
 * @methods setUserInfos 设置用户信息
 */
export const useUserInfo = defineStore('userInfo', {
	state: (): UserInfosState => ({
		userInfos: {
			userName: '',
			photo: '',
			time: 0,
			roles: [],
			authBtnList: [],
			displayName: '',
			email: '',
			phone: '',
			RoleName: '',
			lastLoginIP: '',
			lastLoginTime: 0,
},
	}),
	actions: {
		normalizeUserInfos(userInfos: any) {
			const authBtnList = Array.isArray(userInfos?.authBtnList) ? userInfos.authBtnList.filter((item: any) => typeof item === 'string' && !!item) : [];
			return {
				...userInfos,
				authBtnList,
			};
		},
		async setUserInfos() {
			// 存储用户信息到浏览器缓存
			if (Session.get('userInfo')) {
				this.userInfos = this.normalizeUserInfos(Session.get('userInfo'));
			} else {
				const userInfos: any = await this.getApiUserInfo();
				this.userInfos = this.normalizeUserInfos(userInfos);
			}
		},
		// 模拟接口数据
		async getApiUserInfo() {
			return new Promise((resolve) => {
				userApi.info().then(res => {		
					const permissionValue = (res as any)?.data?.permission;
					const permissionText = typeof permissionValue === 'string' ? permissionValue : '';
					const permissionList = permissionText ? permissionText.split(',').filter((item: string) => !!item) : [];
					const defaultBtnRoles: Array<string> = Array.from(new Set(['2#8', '2#255', ...permissionList]));
					// let defaultAuthBtnList: Array<string> = [];
					const userInfos = {
						userName: res.data.name,
						photo: res.data.avatar,
						time: new Date().getTime(),
						// roles: [],
						authBtnList: defaultBtnRoles,
						displayName: res.data.displayName,
						email: res.data.mail,
						phone: res.data.mobile,
						RoleName: res.data.roleName,
						lastLoginIP: res.data.lastLoginIP,
						lastLoginTime: res.data.lastLogin,
					};
					resolve(this.normalizeUserInfos(userInfos));
				})
			});
		},
	},
});
