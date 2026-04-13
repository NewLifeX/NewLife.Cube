import { defineStore } from 'pinia';
import { useUserApi } from '../api/user';
import type { SiteInfo, LoginConfig } from '../model/api/login';

const userApi = useUserApi();

/**
 * 站点信息与登录配置
 * @methods loadSiteInfo 加载站点信息
 * @methods loadLoginConfig 加载登录配置
 */
export const useSiteInfo = defineStore('siteInfo', {
	state: () => ({
		/** 站点信息 */
		siteInfo: {
			displayName: '',
			copyright: '',
			registration: '',
			loginTip: '',
			logo: '',
		} as SiteInfo,
		/** 登录配置 */
		loginConfig: {
			displayName: '',
			logo: '',
			allowLogin: true,
			allowRegister: true,
			enableSms: false,
			enableMail: false,
			loginTip: '',
			providers: [],
		} as LoginConfig,
		/** 是否已加载 */
		siteInfoLoaded: false,
		loginConfigLoaded: false,
	}),
	actions: {
		/** 加载站点信息（全局初始化时调用） */
		async loadSiteInfo() {
			if (this.siteInfoLoaded) return;
			try {
				const res = await userApi.getSiteInfo();
				this.siteInfo = res.data;
				this.siteInfoLoaded = true;
				// 更新浏览器标题
				if (res.data.displayName) {
					document.title = res.data.displayName;
				}
			} catch {
				// 加载失败不阻塞，使用默认值
			}
		},
		/** 加载登录配置（登录页加载时调用） */
		async loadLoginConfig() {
			if (this.loginConfigLoaded) return;
			try {
				const res = await userApi.getLoginConfig();
				this.loginConfig = res.data;
				this.loginConfigLoaded = true;
			} catch {
				// 加载失败不阻塞，使用默认值
			}
		},
	},
});
